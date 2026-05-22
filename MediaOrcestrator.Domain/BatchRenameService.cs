using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Domain;

public enum BatchRenameMode
{
    Plain = 0,
    Regex = 1,
}

public sealed record BatchRenameOptions(
    string Find,
    string Replace,
    BatchRenameMode Mode = BatchRenameMode.Plain,
    bool IgnoreCase = false,
    IReadOnlyCollection<string>? AllowedSourceIds = null);

public enum BatchRenameSourceOutcome
{
    Updated = 0,
    Skipped = 1,
    NotSupported = 2,
    Failed = 3,
    VerificationFailed = 4,
}

public sealed record BatchRenameSourcePreview(
    string SourceId,
    string SourceTitle,
    bool CanUpdate,
    string? SkipReason);

public sealed record BatchRenamePreview(
    Media Media,
    string OldTitle,
    string NewTitle,
    IReadOnlyList<BatchRenameSourcePreview> Sources,
    string? Error)
{
    public bool TitleChanged => !string.Equals(OldTitle, NewTitle, StringComparison.Ordinal);
    public bool HasChanges => TitleChanged;
}

public sealed record BatchRenameRequest(
    Media Media,
    string NewTitle,
    string? NewDescription,
    IReadOnlyCollection<string>? AllowedSourceIds = null);

public sealed record BatchRenameSourceResult(
    string SourceId,
    string SourceTitle,
    BatchRenameSourceOutcome Outcome,
    string? Message);

public sealed record BatchRenameResult(
    Media Media,
    string OldTitle,
    string NewTitle,
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<BatchRenameSourceResult> Sources);

public sealed record BatchRenameProgress(int Processed, int Total, string? CurrentTitle);

public sealed record BatchRenameSourceInfo(string SourceId, string Title);

public sealed class BatchRenameService(Orcestrator orcestrator, ILogger<BatchRenameService> logger)
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public IReadOnlyList<BatchRenameSourceInfo> GetReferencedSources(IReadOnlyList<Media> medias)
    {
        var sources = orcestrator.GetSources().ToDictionary(s => s.Id);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<BatchRenameSourceInfo>();

        foreach (var media in medias)
        {
            foreach (var link in media.Sources)
            {
                if (!seen.Add(link.SourceId))
                {
                    continue;
                }

                var title = sources.TryGetValue(link.SourceId, out var source)
                    ? source.TitleFull
                    : link.SourceId;

                result.Add(new(link.SourceId, title));
            }
        }

        return result;
    }

    public IReadOnlyList<BatchRenamePreview> Preview(IReadOnlyList<Media> medias, BatchRenameOptions options)
    {
        var sources = orcestrator.GetSources().ToDictionary(s => s.Id);
        var (regex, regexError) = TryCompileRegex(options);

        var previews = new List<BatchRenamePreview>(medias.Count);

        foreach (var media in medias)
        {
            var oldTitle = media.Title ?? string.Empty;
            var newTitle = oldTitle;
            var error = regexError;

            if (error == null && options.Find.Length > 0)
            {
                try
                {
                    newTitle = Apply(oldTitle, options, regex);
                }
                catch (RegexMatchTimeoutException ex)
                {
                    error = "Превышено время выполнения регулярного выражения: " + ex.Message;
                }
            }

            previews.Add(new(media,
                oldTitle,
                newTitle,
                BuildSourcePreviews(media, sources, options.AllowedSourceIds),
                error));
        }

        return previews;
    }

    public async Task<IReadOnlyList<BatchRenameResult>> ApplyAsync(
        IReadOnlyList<BatchRenameRequest> requests,
        IProgress<BatchRenameProgress>? progress,
        Action<BatchRenameResult>? onMediaProcessed,
        CancellationToken cancellationToken)
    {
        var sources = orcestrator.GetSources().ToDictionary(s => s.Id);
        var results = new List<BatchRenameResult>(requests.Count);

        progress?.Report(new(0, requests.Count, null));

        for (var i = 0; i < requests.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = requests[i];
            var media = request.Media;
            var oldTitle = media.Title ?? string.Empty;
            var oldDescription = media.Description ?? string.Empty;

            progress?.Report(new(i, requests.Count, oldTitle));

            var titleChanged = !string.Equals(oldTitle, request.NewTitle, StringComparison.Ordinal);
            var descriptionChanged = request.NewDescription != null
                                     && !string.Equals(oldDescription, request.NewDescription, StringComparison.Ordinal);

            if (!titleChanged && !descriptionChanged)
            {
                var noChange = new BatchRenameResult(media, oldTitle, oldTitle, true, null, []);
                results.Add(noChange);
                onMediaProcessed?.Invoke(noChange);
                continue;
            }

            var result = await ApplySingleAsync(media, request, oldTitle, oldDescription, sources, cancellationToken);
            results.Add(result);
            onMediaProcessed?.Invoke(result);
        }

        progress?.Report(new(requests.Count, requests.Count, null));
        return results;
    }

    private static (Regex? Regex, string? Error) TryCompileRegex(BatchRenameOptions options)
    {
        if (options.Mode != BatchRenameMode.Regex || options.Find.Length == 0)
        {
            return (null, null);
        }

        var opts = RegexOptions.CultureInvariant
                   | (options.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

        try
        {
            return (new(options.Find, opts, RegexTimeout), null);
        }
        catch (ArgumentException ex)
        {
            return (null, "Некорректное регулярное выражение: " + ex.Message);
        }
    }

    private static List<BatchRenameSourcePreview> BuildSourcePreviews(
        Media media,
        IReadOnlyDictionary<string, Source> sources,
        IReadOnlyCollection<string>? allowed)
    {
        var sourcePreviews = new List<BatchRenameSourcePreview>(media.Sources.Count);

        foreach (var link in media.Sources)
        {
            if (!sources.TryGetValue(link.SourceId, out var source))
            {
                sourcePreviews.Add(new(link.SourceId, link.SourceId, false, "Источник не найден"));
                continue;
            }

            if (allowed != null && !allowed.Contains(link.SourceId))
            {
                sourcePreviews.Add(new(link.SourceId, source.TitleFull, false, "Снят пользователем"));
                continue;
            }

            if (source.Type == null)
            {
                sourcePreviews.Add(new(link.SourceId, source.TitleFull, false, "Плагин не загружен"));
                continue;
            }

            if (link.Status != MediaStatus.Ok)
            {
                sourcePreviews.Add(new(link.SourceId, source.TitleFull, false, $"Статус {link.Status}"));
                continue;
            }

            sourcePreviews.Add(new(link.SourceId, source.TitleFull, true, null));
        }

        return sourcePreviews;
    }

    private static string Flatten(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var collapsed = string.Join(" ",
            message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return collapsed.Length > 300 ? collapsed[..299] + "…" : collapsed;
    }

    private static string Apply(string input, BatchRenameOptions options, Regex? regex)
    {
        if (options.Find.Length == 0)
        {
            return input;
        }

        if (options.Mode == BatchRenameMode.Regex)
        {
            return regex == null ? input : regex.Replace(input, options.Replace);
        }

        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return input.Replace(options.Find, options.Replace, comparison);
    }

    private async Task<BatchRenameResult> ApplySingleAsync(
        Media media,
        BatchRenameRequest request,
        string oldTitle,
        string oldDescription,
        IReadOnlyDictionary<string, Source> sources,
        CancellationToken cancellationToken)
    {
        var allowed = request.AllowedSourceIds;
        var perSource = new List<BatchRenameSourceResult>(media.Sources.Count);
        var anyUpdated = false;
        string? mediaError = null;

        foreach (var link in media.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!sources.TryGetValue(link.SourceId, out var source))
            {
                logger.LogWarning("Источник {SourceId} не найден в БД при переименовании медиа {MediaId}",
                    link.SourceId, media.Id);

                perSource.Add(new(link.SourceId, link.SourceId, BatchRenameSourceOutcome.Skipped, "Источник не найден"));
                continue;
            }

            if (allowed != null && !allowed.Contains(link.SourceId))
            {
                perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.Skipped, "Снят пользователем"));
                continue;
            }

            if (source.Type == null)
            {
                logger.LogWarning("Плагин источника {SourceTitle} не загружен — переименование пропущено", source.TitleFull);
                perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.Skipped, "Плагин не загружен"));
                continue;
            }

            if (link.Status != MediaStatus.Ok)
            {
                perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.Skipped, $"Статус {link.Status}"));
                continue;
            }

            var dto = new MediaDto
            {
                Title = request.NewTitle,
                Description = request.NewDescription ?? link.Description ?? string.Empty,
            };

            try
            {
                var uploadResult = await source.Type.UpdateAsync(link.ExternalId, dto, source.Settings, cancellationToken);
                var statusId = uploadResult?.Status?.Id;

                if (statusId == MediaStatus.Ok)
                {
                    var previousTitle = link.Title ?? string.Empty;
                    var titleChanged = await VerifyTitleChangedAsync(link, source, previousTitle, request.NewTitle, uploadResult.ConfirmedTitle, cancellationToken);

                    if (titleChanged == false)
                    {
                        mediaError = $"{source.TitleFull}: площадка приняла запрос, но название осталось прежним";

                        logger.LogWarning("Сверка переименования не прошла: {Source} вернул Ok, но на площадке всё ещё «{Title}»",
                            source.TitleFull, previousTitle);

                        perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.VerificationFailed, mediaError));
                        break;
                    }

                    link.Title = request.NewTitle;

                    if (request.NewDescription != null)
                    {
                        link.Description = request.NewDescription;
                    }

                    anyUpdated = true;
                    perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.Updated, null));
                    continue;
                }

                mediaError = string.IsNullOrEmpty(uploadResult?.Message)
                    ? $"Источник {source.TitleFull} вернул статус {statusId ?? "?"}"
                    : $"{source.TitleFull}: {Flatten(uploadResult.Message)}";

                logger.LogWarning("Источник {Source} вернул не-Ok статус {Status} при переименовании {Title}: {Message}",
                    source.TitleFull, statusId, oldTitle, uploadResult?.Message);

                perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.Failed, mediaError));
                break;
            }
            catch (OperationCanceledException)
            {
                PersistUpdatedLinks();
                throw;
            }
            catch (Exception ex) when (ex is NotImplementedException or NotSupportedException)
            {
                mediaError = $"Источник {source.TitleFull} не поддерживает обновление";
                perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.NotSupported, mediaError));
                break;
            }
            catch (Exception ex)
            {
                mediaError = $"{source.TitleFull}: {Flatten(ex.Message)}";
                logger.LogError(ex, "Ошибка обновления медиа {Title} в {Source}", oldTitle, source.TitleFull);
                perSource.Add(new(link.SourceId, source.TitleFull, BatchRenameSourceOutcome.Failed, mediaError));
                break;
            }
        }

        if (mediaError != null)
        {
            PersistUpdatedLinks();
            return new(media, oldTitle, request.NewTitle, false, mediaError, perSource);
        }

        if (perSource.All(x => x.Outcome == BatchRenameSourceOutcome.Skipped))
        {
            return new(media,
                oldTitle,
                oldTitle,
                false,
                "Ни один источник не поддержал обновление",
                perSource);
        }

        media.Title = request.NewTitle;

        if (request.NewDescription != null)
        {
            media.Description = request.NewDescription;
        }

        orcestrator.UpdateMedia(media);

        logger.LogInformation("Переименовано: {OldTitle} → {NewTitle}", oldTitle, request.NewTitle);

        return new(media, oldTitle, request.NewTitle, true, null, perSource);

        void PersistUpdatedLinks()
        {
            if (anyUpdated)
            {
                orcestrator.UpdateMedia(media);
            }
        }
    }

    private async Task<bool?> VerifyTitleChangedAsync(
        MediaSourceLink link,
        Source source,
        string previousTitle,
        string requestedTitle,
        string? confirmedTitle,
        CancellationToken cancellationToken)
    {
        if (string.Equals(previousTitle, requestedTitle, StringComparison.Ordinal))
        {
            return true;
        }

        if (confirmedTitle != null)
        {
            return !string.Equals(confirmedTitle, previousTitle, StringComparison.Ordinal);
        }

        MediaDto? actual;

        try
        {
            actual = await source.Type!.GetMediaByIdAsync(link.ExternalId, source.Settings, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Не удалось свериться с {Source} после переименования {ExternalId} — апдейт принят на веру",
                source.TitleFull, link.ExternalId);

            return null;
        }

        if (actual == null)
        {
            logger.LogWarning("Площадка {Source} не отдала медиа {ExternalId} для сверки переименования — апдейт принят на веру",
                source.TitleFull, link.ExternalId);

            return null;
        }

        return !string.Equals(actual.Title ?? string.Empty, previousTitle, StringComparison.Ordinal);
    }
}
