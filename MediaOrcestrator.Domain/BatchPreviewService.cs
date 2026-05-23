using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public sealed record BatchPreviewResult(Media Media, Source Target, bool Success, string? ErrorMessage = null);

public sealed record BatchPreviewRequest(Media Media, IReadOnlyList<Source> Targets);

public sealed record BatchPreviewProgress(int Processed, int Total, string? CurrentTitle);

public sealed record BatchPreviewSourceInfo(string SourceId, string Title);

public sealed record BatchPreviewExecutionOptions(bool StopOnError = false, int MaxAttempts = 3);

public sealed class BatchPreviewService(
    Orcestrator orcestrator,
    TempManager tempManager,
    ActionHolder actionHolder,
    ILogger<BatchPreviewService> logger,
    IHttpClientFactory httpClientFactory,
    CoverGenerator coverGenerator)
{
    public IReadOnlyList<BatchPreviewSourceInfo> GetUnauthenticatedSources(IReadOnlyCollection<string> sourceIds)
    {
        var result = new List<BatchPreviewSourceInfo>();

        foreach (var source in orcestrator.GetSources())
        {
            if (!sourceIds.Contains(source.Id)
                || source.Type is not IAuthenticatable auth
                || auth.IsAuthenticated(source.Settings))
            {
                continue;
            }

            result.Add(new(source.Id, source.TitleFull));
        }

        return result;
    }

    public async Task AuthenticateSourcesAsync(
        IReadOnlyCollection<string> sourceIds,
        IAuthUI ui,
        CancellationToken cancellationToken)
    {
        foreach (var source in orcestrator.GetSources())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!sourceIds.Contains(source.Id)
                || source.Type is not IAuthenticatable auth
                || auth.IsAuthenticated(source.Settings))
            {
                continue;
            }

            logger.LogInformation("Источник {Source} не авторизован — открываю вход перед обновлением превью", source.TitleFull);

            try
            {
                await auth.AuthenticateAsync(source.Settings, ui, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Авторизация источника {Source} не удалась", source.TitleFull);
            }
        }
    }

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
        IReadOnlyList<BatchPreviewRequest> requests,
        Source? donor,
        string? localFilePath,
        CoverTemplate? coverTemplate,
        IProgress<BatchPreviewProgress>? progress,
        Action<BatchPreviewResult>? onResult,
        CancellationToken cancellationToken,
        BatchPreviewExecutionOptions? options = null)
    {
        var effective = options ?? new();
        var results = new List<BatchPreviewResult>();
        var tempFiles = new List<string>();

        var context = new BatchContext(donor, localFilePath, coverTemplate, tempFiles, results, onResult, effective);

        progress?.Report(new(0, requests.Count, null));

        try
        {
            for (var i = 0; i < requests.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = requests[i];
                progress?.Report(new(i, requests.Count, request.Media.Title));

                using var perMediaCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var subtask = actionHolder.Register(ShortTitleForAction(request.Media.Title),
                    "В работе",
                    Math.Max(request.Targets.Count, 1),
                    perMediaCts,
                    kind: ActionKind.Metadata);

                try
                {
                    var stats = await ProcessMediaAsync(request.Media, request.Targets, i, context, subtask, perMediaCts.Token);
                    FinishSubtask(subtask, stats);
                }
                catch (OperationCanceledException)
                {
                    subtask.MarkCancelled();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    subtask.Fail(ex.Message, ex);
                    throw;
                }

                if (context.Aborted)
                {
                    logger.LogInformation("Пакетное обновление превью остановлено по флагу StopOnError после '{Title}'", request.Media.Title);
                    break;
                }
            }
        }
        finally
        {
            CleanupTempFiles(tempFiles);
        }

        progress?.Report(new(requests.Count, requests.Count, null));
        return results;
    }

    private static string ShortTitleForAction(string? title)
    {
        const int max = 60;

        if (string.IsNullOrEmpty(title))
        {
            return "«без названия»";
        }

        return title.Length <= max
            ? $"«{title}»"
            : $"«{title.AsSpan(0, max - 1)}…»";
    }

    private static void FinishSubtask(ActionHolder.RunningAction subtask, MediaStats stats)
    {
        if (stats is { Success: 0, Failed: 0 })
        {
            subtask.Finish("Без изменений");
            return;
        }

        if (stats.Failed == 0)
        {
            subtask.Finish($"Готово: {stats.Success}");
            return;
        }

        if (stats.Success == 0)
        {
            subtask.Fail($"Ошибок: {stats.Failed}");
            return;
        }

        subtask.Fail($"Готово {stats.Success}, ошибок {stats.Failed}");
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && uri.Scheme is "http" or "https";
    }

    private async Task<MediaStats> ProcessMediaAsync(
        Media media,
        IReadOnlyList<Source> targets,
        int index,
        BatchContext context,
        ActionHolder.RunningAction subtask,
        CancellationToken cancellationToken)
    {
        var stats = new MediaStats();

        if (targets.Count == 0)
        {
            return stats;
        }

        string? previewPath;

        if (context.CoverTemplate != null)
        {
            subtask.Status = "Генерация обложки";
            previewPath = GenerateCoverPath(context.CoverTemplate, media, index, context.TempFiles);
        }
        else if (context.LocalFilePath != null)
        {
            previewPath = context.LocalFilePath;
        }
        else if (context.Donor != null)
        {
            subtask.Status = $"Скачивание из {context.Donor.TitleFull}";
            previewPath = await DownloadPreviewFromDonorAsync(media, context.Donor, context.TempFiles, context.Options.MaxAttempts, cancellationToken);

            if (previewPath == null)
            {
                foreach (var target in targets)
                {
                    var failure = new BatchPreviewResult(media, target, false, "Превью не найдено в источнике-доноре");
                    context.Results.Add(failure);
                    context.OnResult?.Invoke(failure);
                    subtask.ProgressPlus();
                    stats.Failed++;
                }

                if (context.Options.StopOnError)
                {
                    context.Aborted = true;
                }

                return stats;
            }
        }
        else
        {
            return stats;
        }

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            subtask.Status = target.TitleFull;
            var result = await UploadPreviewToTargetAsync(media, target, previewPath, context.Options.MaxAttempts, cancellationToken);
            context.Results.Add(result);
            context.OnResult?.Invoke(result);
            subtask.ProgressPlus();

            if (result.Success)
            {
                stats.Success++;
            }
            else
            {
                stats.Failed++;

                if (context.Options.StopOnError)
                {
                    context.Aborted = true;
                    break;
                }
            }
        }

        return stats;
    }

    private string GenerateCoverPath(CoverTemplate template, Media media, int index, List<string> tempFiles)
    {
        var number = CoverNumberResolver.Resolve(template, media.Title, index, logger);
        var tempDir = Path.Combine(tempManager.TempPath, Guid.NewGuid().ToString());
        var coverPath = coverGenerator.Generate(template, number, tempDir);
        tempFiles.Add(tempDir);
        tempFiles.Add(coverPath);
        return coverPath;
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
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var link = media.Sources.FirstOrDefault(s => s.SourceId == donor.Id);
        if (link == null)
        {
            return null;
        }

        try
        {
            var dto = await RetryPolicy.ExecuteAsync(ct => donor.Type.GetMediaByIdAsync(link.ExternalId, donor.Settings, ct),
                maxAttempts,
                logger,
                $"Получение метаданных донора '{media.Title}' из {donor.TitleFull}",
                cancellationToken);

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

            await RetryPolicy.ExecuteAsync(async ct =>
                {
                    var httpClient = httpClientFactory.CreateClient("Preview");
                    await using var stream = await httpClient.GetStreamAsync(previewUrl, ct);
                    await using var fileStream = File.Create(tempPath);
                    await stream.CopyToAsync(fileStream, ct);
                }, maxAttempts, logger, $"Скачивание превью для '{media.Title}'", cancellationToken);

            tempFiles.Add(tempPath);
            tempFiles.Add(tempDir);
            return tempPath;
        }
        catch (OperationCanceledException)
        {
            throw;
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
        int maxAttempts,
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

            var uploadResult = await RetryPolicy.ExecuteAsync(ct => target.Type.UpdateAsync(link.ExternalId, tempMedia, target.Settings, ct),
                maxAttempts,
                logger,
                $"Превью '{media.Title}' → {target.TitleFull}",
                cancellationToken);

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

    private sealed class MediaStats
    {
        public int Success;
        public int Failed;
    }

    private sealed class BatchContext(
        Source? donor,
        string? localFilePath,
        CoverTemplate? coverTemplate,
        List<string> tempFiles,
        List<BatchPreviewResult> results,
        Action<BatchPreviewResult>? onResult,
        BatchPreviewExecutionOptions options)
    {
        public Source? Donor { get; } = donor;
        public string? LocalFilePath { get; } = localFilePath;
        public CoverTemplate? CoverTemplate { get; } = coverTemplate;
        public List<string> TempFiles { get; } = tempFiles;
        public List<BatchPreviewResult> Results { get; } = results;
        public Action<BatchPreviewResult>? OnResult { get; } = onResult;
        public BatchPreviewExecutionOptions Options { get; } = options;
        public bool Aborted { get; set; }
    }
}
