using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public sealed record BatchRenamePreview(Media Media, string OldTitle, string NewTitle, bool HasChanges);

public sealed record BatchRenameResult(Media Media, string OldTitle, string NewTitle, bool Success, string? ErrorMessage = null);

public sealed class BatchRenameService(Orcestrator orcestrator, ILogger<BatchRenameService> logger)
{
    public List<BatchRenamePreview> Preview(List<Media> medias, string find, string replace)
    {
        return medias.Select(m =>
            {
                var newTitle = m.Title.Replace(find, replace);
                return new BatchRenamePreview(m, m.Title, newTitle, m.Title != newTitle);
            })
            .ToList();
    }

    public async Task<List<BatchRenameResult>> ApplyAsync(
        List<Media> medias,
        string find,
        string replace,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchRenameResult>();
        var sources = orcestrator.GetSources();

        foreach (var media in medias)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var oldTitle = media.Title;
            var newTitle = oldTitle.Replace(find, replace);

            if (oldTitle == newTitle)
            {
                continue;
            }

            var okSources = media.Sources
                .Where(s => s.Status == MediaStatus.Ok)
                .ToList();

            var updatedSources = new List<MediaSourceLink>();
            string? errorMessage = null;

            foreach (var sourceLink in okSources)
            {
                var source = sources.FirstOrDefault(s => s.Id == sourceLink.SourceId);

                if (source?.Type == null)
                {
                    continue;
                }

                try
                {
                    var tempMedia = new MediaDto { Title = newTitle, Description = media.Description };
                    await source.Type.UpdateAsync(sourceLink.ExternalId, tempMedia, source.Settings, cancellationToken);
                    updatedSources.Add(sourceLink);
                }
                catch (Exception ex) when (ex is NotImplementedException or NotSupportedException)
                {
                    errorMessage = $"Источник {source.TitleFull} не поддерживает обновление";
                    break;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Ошибка в {source.TitleFull}: {ex.Message}";
                    break;
                }
            }

            if (errorMessage != null)
            {
                foreach (var sourceLink in updatedSources)
                {
                    var source = sources.FirstOrDefault(s => s.Id == sourceLink.SourceId);

                    if (source?.Type == null)
                    {
                        continue;
                    }

                    try
                    {
                        var rollbackMedia = new MediaDto { Title = oldTitle, Description = media.Description };
                        await source.Type.UpdateAsync(sourceLink.ExternalId, rollbackMedia, source.Settings, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Не удалось откатить название для {Source} ({ExternalId})",
                            source.TitleFull, sourceLink.ExternalId);
                    }
                }

                results.Add(new(media, oldTitle, newTitle, false, errorMessage));
            }
            else
            {
                media.Title = newTitle;
                orcestrator.UpdateMedia(media);

                logger.LogInformation("Переименовано: {OldTitle} → {NewTitle}", oldTitle, newTitle);

                results.Add(new(media, oldTitle, newTitle, true));
            }
        }

        return results;
    }
}
