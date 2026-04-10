using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Domain;

public sealed record BatchPreviewResult(Media Media, Source Target, bool Success, string? ErrorMessage = null);

public sealed class BatchPreviewService(
    Orcestrator orcestrator,
    TempManager tempManager,
    ILogger<BatchPreviewService> logger,
    IHttpClientFactory httpClientFactory,
    CoverGenerator coverGenerator)
{
    public List<Source> GetAvailableDonors(List<Media> medias)
    {
        var sources = orcestrator.GetSources();
        var donorIds = new HashSet<string>();

        foreach (var media in medias)
        {
            foreach (var link in media.Sources)
            {
                if (link.Status == MediaStatus.Ok)
                {
                    donorIds.Add(link.SourceId);
                }
            }
        }

        return sources
            .Where(s => donorIds.Contains(s.Id) && s is { IsDisable: false, Type: not null })
            .ToList();
    }

    public List<Source> GetAvailableTargets(List<Media> medias, Source? excludeDonor)
    {
        var sources = orcestrator.GetSources();
        var targetIds = new HashSet<string>();

        foreach (var media in medias)
        {
            foreach (var link in media.Sources)
            {
                if (link.Status is MediaStatus.Ok or MediaStatus.PartialOk)
                {
                    targetIds.Add(link.SourceId);
                }
            }
        }

        return sources
            .Where(s => targetIds.Contains(s.Id)
                        && s is { IsDisable: false, Type: not null }
                        && s.Type.ChannelType is SyncDirection.OnlyUpload or SyncDirection.Full
                        && (excludeDonor == null || s.Id != excludeDonor.Id))
            .ToList();
    }

    public async Task<List<BatchPreviewResult>> ApplyAsync(
        List<Media> medias,
        Source? donor,
        List<Source> targets,
        string? localFilePath,
        CoverTemplate? coverTemplate,
        IProgress<BatchPreviewResult>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchPreviewResult>();
        var tempFiles = new List<string>();

        var context = new BatchContext(donor, targets, localFilePath, coverTemplate, tempFiles, results, progress);

        try
        {
            for (var i = 0; i < medias.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessMediaAsync(medias[i], i, context, cancellationToken);
            }
        }
        finally
        {
            CleanupTempFiles(tempFiles);
        }

        return results;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && uri.Scheme is "http" or "https";
    }

    private async Task ProcessMediaAsync(
        Media media,
        int index,
        BatchContext context,
        CancellationToken cancellationToken)
    {
        string? previewPath;

        if (context.CoverTemplate != null)
        {
            previewPath = GenerateCoverPath(context.CoverTemplate, media, index, context.TempFiles);
        }
        else if (context.LocalFilePath != null)
        {
            previewPath = context.LocalFilePath;
        }
        else if (context.Donor != null)
        {
            previewPath = await DownloadPreviewFromDonorAsync(media, context.Donor, context.TempFiles, cancellationToken);

            if (previewPath == null)
            {
                foreach (var target in context.Targets)
                {
                    var failure = new BatchPreviewResult(media, target, false, "Превью не найдено в источнике-доноре");
                    context.Results.Add(failure);
                    context.Progress?.Report(failure);
                }

                return;
            }
        }
        else
        {
            return;
        }

        foreach (var target in context.Targets)
        {
            var result = await UploadPreviewToTargetAsync(media, target, previewPath, cancellationToken);
            context.Results.Add(result);
            context.Progress?.Report(result);
        }
    }

    private string GenerateCoverPath(CoverTemplate template, Media media, int index, List<string> tempFiles)
    {
        var number = ResolveNumber(template, media, index);
        var tempDir = Path.Combine(tempManager.TempPath, Guid.NewGuid().ToString());
        var coverPath = coverGenerator.Generate(template, number, tempDir);
        tempFiles.Add(tempDir);
        tempFiles.Add(coverPath);
        return coverPath;
    }

    private int ResolveNumber(CoverTemplate template, Media media, int index)
    {
        if (template.NumberMode != CoverNumberMode.TitleRegex)
        {
            return template.StartNumber + index;
        }

        var pattern = string.IsNullOrWhiteSpace(template.TitleRegexPattern)
            ? CoverTemplate.DefaultTitleRegex
            : template.TitleRegexPattern;

        if (string.IsNullOrEmpty(media.Title))
        {
            logger.LogWarning("Не удалось извлечь номер из названия (пустой Title) для медиа {Id}, использован {Fallback}", media.Id, template.StartNumber + index);
            return template.StartNumber + index;
        }

        try
        {
            var match = Regex.Match(media.Title, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));

            if (match.Success)
            {
                var captured = match.Groups.Count > 1 && match.Groups[1].Success ? match.Groups[1].Value : match.Value;

                if (int.TryParse(captured, out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            logger.LogWarning(ex, "Регулярка для номера зависла на '{Title}', использован {Fallback}", media.Title, template.StartNumber + index);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Невалидная регулярка номера '{Pattern}', использован {Fallback}", pattern, template.StartNumber + index);
        }

        logger.LogWarning("Не удалось извлечь номер из '{Title}' по '{Pattern}', использован {Fallback}", media.Title, pattern, template.StartNumber + index);
        return template.StartNumber + index;
    }

    private void CleanupTempFiles(List<string> tempFiles)
    {
        foreach (var tempFile in tempFiles.AsEnumerable().Reverse())
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                else if (Directory.Exists(tempFile))
                {
                    Directory.Delete(tempFile, true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось удалить временный файл: {Path}", tempFile);
            }
        }
    }

    private async Task<string?> DownloadPreviewFromDonorAsync(
        Media media,
        Source donor,
        List<string> tempFiles,
        CancellationToken cancellationToken)
    {
        var link = media.Sources.FirstOrDefault(s => s.SourceId == donor.Id);
        if (link == null)
        {
            return null;
        }

        try
        {
            var dto = await donor.Type.GetMediaByIdAsync(link.ExternalId, donor.Settings, cancellationToken);
            if (!string.IsNullOrEmpty(dto?.TempPreviewPath) && File.Exists(dto.TempPreviewPath))
            {
                return dto.TempPreviewPath;
            }

            var previewUrl = dto?.PreviewPath;

            if (string.IsNullOrEmpty(previewUrl))
            {
                previewUrl = dto?.Metadata?.FirstOrDefault(m => m.Key == "PreviewUrl")?.Value;
            }

            if (string.IsNullOrEmpty(previewUrl))
            {
                return null;
            }

            if (!IsHttpUrl(previewUrl))
            {
                return File.Exists(previewUrl) ? previewUrl : null;
            }

            var tempDir = Path.Combine(tempManager.TempPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var extension = Path.GetExtension(new Uri(previewUrl).AbsolutePath);

            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg";
            }

            var tempPath = Path.Combine(tempDir, $"preview{extension}");

            var httpClient = httpClientFactory.CreateClient("Preview");
            await using var stream = await httpClient.GetStreamAsync(previewUrl, cancellationToken);
            await using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream, cancellationToken);

            tempFiles.Add(tempPath);
            tempFiles.Add(tempDir);
            return tempPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка скачивания превью из донора для '{Title}'", media.Title);
            return null;
        }
    }

    private async Task<BatchPreviewResult> UploadPreviewToTargetAsync(
        Media media,
        Source target,
        string previewPath,
        CancellationToken cancellationToken)
    {
        var link = media.Sources.FirstOrDefault(s => s.SourceId == target.Id);
        if (link == null)
        {
            return new(media, target, false, "Медиа не привязано к этому источнику");
        }

        try
        {
            var tempMedia = new MediaDto
            {
                Title = media.Title,
                Description = media.Description,
                TempPreviewPath = previewPath,
            };

            var uploadResult = await target.Type.UpdateAsync(link.ExternalId, tempMedia, target.Settings, cancellationToken);

            if (uploadResult.Status.Id == MediaStatus.Ok)
            {
                logger.LogInformation("Превью обновлено: '{Title}' → {Source}", media.Title, target.TitleFull);
                return new(media, target, true);
            }

            var message = uploadResult.Message ?? uploadResult.Status.Text;
            logger.LogWarning("Превью не обновлено: '{Title}' → {Source}: {Message}", media.Title, target.TitleFull, message);
            return new(media, target, false, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки превью для '{Title}' в {Source}", media.Title, target.TitleFull);
            return new(media, target, false, ex.Message);
        }
    }

    private sealed record BatchContext(
        Source? Donor,
        List<Source> Targets,
        string? LocalFilePath,
        CoverTemplate? CoverTemplate,
        List<string> TempFiles,
        List<BatchPreviewResult> Results,
        IProgress<BatchPreviewResult>? Progress);
}
