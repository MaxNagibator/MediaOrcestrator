using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public sealed record BatchPreviewResult(Media Media, Source Target, bool Success, string? ErrorMessage = null);

public sealed class BatchPreviewService(Orcestrator orcestrator, TempManager tempManager, ILogger<BatchPreviewService> logger, IHttpClientFactory httpClientFactory)
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
        IProgress<BatchPreviewResult>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchPreviewResult>();
        var tempFiles = new List<string>();

        try
        {
            foreach (var media in medias)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessMediaAsync(media, donor, targets, localFilePath, tempFiles, results, progress, cancellationToken);
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
        Source? donor,
        List<Source> targets,
        string? localFilePath,
        List<string> tempFiles,
        List<BatchPreviewResult> results,
        IProgress<BatchPreviewResult>? progress,
        CancellationToken cancellationToken)
    {
        string? previewPath;

        if (localFilePath != null)
        {
            previewPath = localFilePath;
        }
        else if (donor != null)
        {
            previewPath = await DownloadPreviewFromDonorAsync(media, donor, tempFiles, cancellationToken);

            if (previewPath == null)
            {
                foreach (var target in targets)
                {
                    var failure = new BatchPreviewResult(media, target, false, "Превью не найдено в источнике-доноре");
                    results.Add(failure);
                    progress?.Report(failure);
                }

                return;
            }
        }
        else
        {
            return;
        }

        foreach (var target in targets)
        {
            var result = await UploadPreviewToTargetAsync(media, target, previewPath, cancellationToken);
            results.Add(result);
            progress?.Report(result);
        }
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
}
