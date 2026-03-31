using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public class Orcestrator(PluginManager pluginManager, LiteDatabase db, ILogger<Orcestrator> logger)
{
    public Dictionary<string, ISourceType> GetSourceTypes()
    {
        return pluginManager.MediaSources;
    }

    public void Init(string pluginPath)
    {
        pluginManager.Init(pluginPath);
        var sources = GetSourceTypes();
    }

    public async Task GetStorageFullInfo(bool isFull, Source? filterSource = null)
    {
        logger.LogInformation("Запуск процесса синхронизации {Source}...", filterSource?.TitleFull);

        var mediaCol = db.GetCollection<Media>("medias");
        //mediaCol.DeleteAll();
        var mediaAll = mediaCol.FindAll().ToList();

        var cache = new MediaSourceCache();

        foreach (var media in mediaAll)
        {
            foreach (var source in media.Sources)
            {
                if (filterSource != null)
                {
                    if (source.SourceId != filterSource.Id)
                    {
                        continue;
                    }
                }

                source.Media = media;
                cache.GetMedia(source.SourceId).Add(source);
            }
        }

        logger.LogInformation("Обнаружено {Count} элементов медиа в локальном кэше.", mediaAll.Count);

        var sourceTypes = GetSourceTypes();
        var sources = GetSources();

        if (filterSource != null)
        {
            sources = sources.Where(x => x.Id == filterSource.Id).ToList();
        }

        await Parallel.ForEachAsync(sources, async (mediaSource, cancellationToken) =>
        {
            var plugin = sourceTypes.Values.FirstOrDefault(x => x.Name == mediaSource.TypeId);

            if (plugin == null)
            {
                logger.LogError("Плагин для типа {TypeId} не найден.", mediaSource.TypeId);
                return;
            }

            var syncMedia = plugin.GetMedia(mediaSource.Settings, isFull, cancellationToken);
            var mediaList = new List<MediaDto>();
            var foundIds = new List<string>();
            await foreach (var s in syncMedia)
            {
                foundIds.Add(s.Id);
                var foundMediaSource = cache.GetMedia(mediaSource.Id).FirstOrDefault(x => x.ExternalId == s.Id);
                if (foundMediaSource == null)
                {
                    mediaList.Insert(0, s);
                    continue;
                }

                var hasChange = false;
                if (s.Metadata is { Count: > 0 } && foundMediaSource.Media != null)
                {
                    var providedKeys = new HashSet<string>();
                    foreach (var item in s.Metadata)
                    {
                        providedKeys.Add(item.Key);
                        var existing = foundMediaSource.Media.Metadata
                            .FirstOrDefault(m => m.Key == item.Key && m.SourceId == mediaSource.Id);
                        if (existing != null)
                        {
                            existing.Value = item.Value;
                            existing.DisplayName = item.DisplayName;
                            existing.DisplayType = item.DisplayType;
                        }
                        else
                        {
                            item.SourceId = mediaSource.Id;
                            foundMediaSource.Media.Metadata.Add(item);
                        }
                    }

                    foundMediaSource.Media.Metadata
                        .RemoveAll(m => m.SourceId == mediaSource.Id && !providedKeys.Contains(m.Key));
                    hasChange = true;
                }
                if (foundMediaSource.Status == MediaStatus.Missing
                    || foundMediaSource.Status == MediaStatus.Error)
                {
                    foundMediaSource.Status = MediaStatus.Ok;
                    hasChange = true;
                }
                if (hasChange)
                {
                    mediaCol.Update(foundMediaSource!.Media);
                }
            }

            var sortNumber = cache.GetMedia(mediaSource.Id).Select(x => x.SortNumber).DefaultIfEmpty(1).Max();
            foreach (var s in mediaList)
            {
                var mediaId = Guid.NewGuid().ToString();
                foreach (var item in s.Metadata ?? [])
                {
                    item.SourceId = mediaSource.Id;
                }

                var myMedia = new Media
                {
                    Title = s.Title,
                    Id = mediaId,
                    Description = s.Description,
                    Metadata = s.Metadata ?? [],
                    Sources = [],
                };

                var newMediaSource = new MediaSourceLink
                {
                    MediaId = mediaId,
                    Media = myMedia,
                    ExternalId = s.Id,
                    Status = MediaStatus.Ok,
                    SourceId = mediaSource.Id,
                    SortNumber = sortNumber,
                };

                sortNumber++;

                myMedia.Sources.Add(newMediaSource);
                // поправить циклический зависимость
                mediaCol.Insert(myMedia);
                mediaAll.Add(myMedia);
                cache.GetMedia(mediaSource.Id).Add(newMediaSource);
            }

            var existsVideos = cache.GetMedia(mediaSource.Id);
            foreach (var existsVideo in existsVideos)
            {
                if (!foundIds.Contains(existsVideo.ExternalId))
                {
                    existsVideo.Status = MediaStatus.Missing;
                    mediaCol.Update(existsVideo.Media);
                }
            }
            // save ?:)
        });

        logger.LogInformation("Синхронизация успешно завершена.");

        var zalupa = 1;
    }

    public List<Media> GetMedias()
    {
        // TODO: System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
        var medias = db.GetCollection<Media>("medias").FindAll().ToList();
        //foreach (var media in medias) // времяночка для очистки
        //{
        //    var gr = media.Sources.GroupBy(x => x.SourceId).Where(x => x.Count() > 1).ToArray();
        //    foreach (var source in gr)
        //    {
        //        for (var i = 1; i < source.Count(); i++)
        //        {
        //            media.Sources.Remove(source.Skip(i).First());
        //            db.GetCollection<Media>("medias").Update(media);
        //        }
        //    }
        //    //if (media.Sources.Any(x => x.ExternalId == null))
        //    //{
        //    //    db.GetCollection<Media>("medias").Delete(media.Id);
        //    //}
        //}
        //medias = db.GetCollection<Media>("medias").FindAll().ToList();
        // foreach (var media in medias) // времяночка для очистки
        // {
        //     foreach (var source in media.Sources)
        //   {
        //     if (source.Status == MediaStatus.Missing)
        //   {
        //     source.Status = MediaStatus.Ok;
        // }
        //
        //              db.GetCollection<Media>("medias").Update(media);
        //        }
        //if (media.Sources.Any(x => x.ExternalId == null))
        //{
        //    db.GetCollection<Media>("medias").Delete(media.Id);
        //}
        // }
        return medias;
    }

    public void UpdateMedia(Media media)
    {
        db.GetCollection<Media>("medias").Update(media);
    }

    public async Task<List<BatchRenameResult>> BatchRenameAsync(
        List<Media> medias,
        string find,
        string replace,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchRenameResult>();
        var sources = GetSources();

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
                    await source.Type.Update(sourceLink.ExternalId, tempMedia, source.Settings, cancellationToken);
                    updatedSources.Add(sourceLink);
                }
                catch (NotImplementedException)
                {
                    errorMessage = $"Источник {source.TitleFull} не поддерживает обновление";
                    break;
                }
                catch (NotSupportedException)
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
                        await source.Type.Update(sourceLink.ExternalId, rollbackMedia, source.Settings, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Не удалось откатить название для {Source} ({ExternalId})",
                            source.TitleFull, sourceLink.ExternalId);
                    }
                }

                results.Add(new()
                {
                    Media = media,
                    OldTitle = oldTitle,
                    NewTitle = newTitle,
                    Success = false,
                    ErrorMessage = errorMessage,
                });
            }
            else
            {
                media.Title = newTitle;
                UpdateMedia(media);

                logger.LogInformation("Переименовано: {OldTitle} → {NewTitle}", oldTitle, newTitle);

                results.Add(new()
                {
                    Media = media,
                    OldTitle = oldTitle,
                    NewTitle = newTitle,
                    Success = true,
                });
            }
        }

        return results;
    }

    public async Task ForceUpdateMetadataAsync(Media media, CancellationToken ct = default)
    {
        logger.LogInformation("Начато принудительное обновление метаданных для {MediaTitle}", media.Title);

        media.Metadata.Clear();

        foreach (var sourceLink in media.Sources)
        {
            await UpdateSourceMetadataAsync(media, sourceLink.SourceId, ct);
        }

        UpdateMedia(media);
        logger.LogInformation("Завершено принудительное обновление метаданных для {MediaTitle}", media.Title);
    }

    public async Task ForceUpdateMetadataAsync(Media media, string sourceId, CancellationToken ct = default)
    {
        media.Metadata.RemoveAll(m => m.SourceId == sourceId);
        await UpdateSourceMetadataAsync(media, sourceId, ct);
        UpdateMedia(media);
        logger.LogInformation("Обновлены метаданные источника {SourceId} для {MediaTitle}", sourceId, media.Title);
    }

    private async Task UpdateSourceMetadataAsync(Media media, string sourceId, CancellationToken ct)
    {
        var source = GetSources().FirstOrDefault(s => s.Id == sourceId);
        if (source == null || source.IsDisable)
        {
            return;
        }

        var plugin = GetSourceTypes().Values.FirstOrDefault(x => x.Name == source.TypeId);
        if (plugin == null)
        {
            return;
        }

        var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == sourceId);
        if (sourceLink == null)
        {
            return;
        }

        try
        {
            var dto = await plugin.GetMediaByIdAsync(sourceLink.ExternalId, source.Settings, ct);
            if (dto is { Metadata.Count: > 0 })
            {
                foreach (var item in dto.Metadata)
                {
                    item.SourceId = source.Id;
                    media.Metadata.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении метаданных из источника {SourceTitle}", source.TitleFull);
        }
    }

    public void ClearSourceMetadata(Media media, string sourceId)
    {
        var removedCount = media.Metadata.RemoveAll(m => m.SourceId == sourceId);
        if (removedCount <= 0)
        {
            return;
        }

        UpdateMedia(media);
        logger.LogInformation("Очищено {Count} метаданных для источника {SourceId} в медиа {MediaTitle}", removedCount, sourceId, media.Title);
    }

    public void ClearAllMetadata(Media media)
    {
        if (media.Metadata.Count <= 0)
        {
            return;
        }

        var count = media.Metadata.Count;
        media.Metadata.Clear();
        UpdateMedia(media);
        logger.LogInformation("Очищено {Count} метаданных для медиа {MediaTitle}", count, media.Title);
    }

    public void RemoveMedia(Media media)
    {
        db.GetCollection<Media>("medias").Delete(media.Id);
    }

    public List<Source> GetSources()
    {
        var sources = db.GetCollection<Source>("sources").FindAll().ToList();
        var sourceTypes = GetSourceTypes();
        foreach (var source in sources)
        {
            var sourceType = sourceTypes.Values.FirstOrDefault(x => x.Name == source.TypeId);
            if (sourceType == null)
            {
                source.IsDisable = true;
                continue;
            }

            source.Type = sourceType;
        }

        return sources;
    }

    public void AddSource(string typeId, Dictionary<string, string> settings)
    {
        db.GetCollection<Source>("sources")
            .Insert(new Source
            {
                Id = Guid.NewGuid().ToString(),
                TypeId = typeId,
                Settings = settings,
            });
    }

    public void RemoveSource(string sourceId)
    {
        db.GetCollection<Source>("sources").Delete(sourceId);
    }

    public void UpdateSource(Source source)
    {
        db.GetCollection<Source>("sources").Update(source);
    }

    public List<SourceSyncRelation> GetRelations()
    {
        //db.GetCollection<SourceSyncRelation>("source_relations")
        //    .DeleteMany(x => true);

        var relations = db.GetCollection<SourceSyncRelation>("source_relations").FindAll().ToList();
        var sourceTypes = GetSourceTypes();
        var sources = GetSources();
        foreach (var item in relations)
        {
            //item.From.Type = sourceTypes.Values.First(x => x.Name == item.From.TypeId);
            var fromSource = sources.FirstOrDefault(x => x.Id == item.FromId);
            var toSource = sources.FirstOrDefault(x => x.Id == item.ToId);
            item.From = fromSource;
            item.To = toSource;
            if (fromSource == null || toSource == null || fromSource.IsDisable || toSource.IsDisable)
            {
                item.IsDisable = true;
            }
        }

        return relations;
    }

    public void AddRelation(Source from, Source to)
    {
        db.GetCollection<SourceSyncRelation>("source_relations")
            .Insert(new SourceSyncRelation
            {
                FromId = from.Id,
                ToId = to.Id,
            });
    }

    public void RemoveRelation(Source from, Source to)
    {
        // TODO: Подумать
        db.GetCollection<SourceSyncRelation>("source_relations")
            .DeleteMany(x => x.FromId == from.Id && x.ToId == to.Id);
    }

    public void ClearCollection(string collectionName)
    {
        logger.LogInformation("Очистка коллекции: {CollectionName}", collectionName);
        var collection = db.GetCollection(collectionName);
        var count = collection.Count();
        collection.DeleteAll();
        logger.LogInformation("[✓] Коллекция {CollectionName} очищена. Удалено записей: {Count}", collectionName, count);
    }

    public void ClearDatabase()
    {
        logger.LogInformation("========== Начало очистки базы данных ==========");

        var mediasCollection = db.GetCollection<Media>("medias");
        var mediaCount = mediasCollection.Count();
        mediasCollection.DeleteAll();
        logger.LogInformation("[✓] Коллекция медиа очищена. Удалено записей: {MediaCount}", mediaCount);

        var relationsCollection = db.GetCollection<SourceSyncRelation>("source_relations");
        var relationCount = relationsCollection.Count();
        relationsCollection.DeleteAll();
        logger.LogInformation("[✓] Коллекция связей очищена. Удалено записей: {RelationCount}", relationCount);

        var sourcesCollection = db.GetCollection<Source>("sources");
        var sourceCount = sourcesCollection.Count();
        sourcesCollection.DeleteAll();
        logger.LogInformation("[✓] Коллекция источников очищена. Удалено записей: {SourceCount}", sourceCount);

        logger.LogInformation("========== Очистка БД завершена успешно ==========");
        logger.LogInformation("Всего удалено записей: {TotalCount} (медиа: {MediaCount}, связи: {RelationCount}, источники: {SourceCount})",
            mediaCount + relationCount + sourceCount, mediaCount, relationCount, sourceCount);
    }

    public async Task TransferByRelation(Media media, SourceSyncRelation rel, CancellationToken cancellationToken = default)
    {
        var fromMediaSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
        if (fromMediaSource == null)
        {
            throw new($"MediaSourceLink не найден для {rel.From.Id}");
        }

        if (fromMediaSource.Status != MediaStatus.Ok)
        {
            throw new($"MediaSourceLink для {rel.From.Id} в плохом статусе " + fromMediaSource.Status);
        }

        var toMediaSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
        // todo частично успешные пока повторно не обрабатываем
        if (toMediaSource != null && toMediaSource.Status == MediaStatus.Ok)
        {
            // фаил уже загружен, значит ничего не делаем
            // в будущем возможно обновлять будем
            logger.LogInformation("Пропущено синхронизирование медиа {Media} в {ToSource} / уже имеется", media, rel.To);
            return;
        }

        UploadResult uploadResult;
        var debug = false;
        if (debug)
        {
            if (DateTime.Now.Second % 10 < 5)
            {
                throw new("ошибка");
            }

            uploadResult = new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = Guid.NewGuid().ToString(),
            };
        }
        else
        {
            var tempMedia = await rel.From.Type.Download(fromMediaSource.ExternalId, rel.From.Settings, cancellationToken);
            tempMedia.Id = media.Id;
            if (toMediaSource?.Status == MediaStatus.PartialOk)
            {
                var externalId = toMediaSource.ExternalId;
                uploadResult = await rel.To.Type.Update(externalId, tempMedia, rel.To.Settings, cancellationToken);
            }
            else
            {
                uploadResult = await rel.To.Type.Upload(tempMedia, rel.To.Settings, cancellationToken);
            }
        }

        // если айди есть, значит частично или полностью успех и связь устанавливаем
        if (uploadResult.Id != null)
        {
            if (toMediaSource == null)
            {
                toMediaSource = new()
                {
                    MediaId = media.Id,
                    Media = media,
                    SourceId = rel.To.Id,
                };

                media.Sources.Add(toMediaSource);
            }

            toMediaSource.Status = uploadResult.Status.Id;
            toMediaSource.ExternalId = uploadResult.Id!;
            UpdateMedia(media);
            logger.LogInformation("Успешно синхронизировано медиа {Media} в {ToSource}. ExternalId: {ExternalId}", media, rel.To, uploadResult.Id);
        }
        else
        {
            throw new("Провал синхронизации: " + uploadResult.Status.Text + " " + uploadResult.Message);
        }
    }

    public async Task DeleteMediaFromSourceAsync(Media media, Source source, CancellationToken cancellationToken = default)
    {
        var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == source.Id);
        if (sourceLink == null)
        {
            logger.LogWarning("Попытка удалить медиа {MediaId} из источника {SourceId}, но связь отсутствует", media.Id, source.Id);
            throw new InvalidOperationException($"Медиа {media.Title} отсутствует в источнике {source.TitleFull}");
        }

        logger.LogInformation("Начало удаления медиа {MediaId} ({MediaTitle}) из источника {SourceId} ({SourceTitle})",
            media.Id, media.Title, source.Id, source.TitleFull);

        try
        {
            await source.Type.DeleteAsync(sourceLink.ExternalId, source.Settings, cancellationToken);
            logger.LogInformation("Успешно удалено медиа {MediaId} из источника {SourceId}", media.Id, source.Id);
        }
        catch (NotSupportedException exception)
        {
            logger.LogError(exception, "Тип источника {SourceType} не поддерживает удаление", source.TypeId);
            throw new InvalidOperationException($"Источник {source.TitleFull} не поддерживает удаление", exception);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при удалении медиа {MediaId} из источника {SourceId}", media.Id, source.Id);
            throw;
        }

        CleanupMediaLinks(media, source.Id);
    }

    private void CleanupMediaLinks(Media media, string sourceId)
    {
        logger.LogInformation("Очистка записей в базе данных для медиа {MediaId}, источник {SourceId}", media.Id, sourceId);

        var linkToRemove = media.Sources.FirstOrDefault(s => s.SourceId == sourceId);
        if (linkToRemove != null)
        {
            media.Sources.Remove(linkToRemove);
            logger.LogDebug("Удалена связь MediaSourceLink для источника {SourceId}", sourceId);
        }

        var removedMetadataCount = media.Metadata.RemoveAll(m => m.SourceId == sourceId);
        if (removedMetadataCount > 0)
        {
            logger.LogDebug("Удалено {RemovedCount} элементов метаданных для источника {SourceId}", removedMetadataCount, sourceId);
        }

        if (media.Sources.Count != 0)
        {
            UpdateMedia(media);
            logger.LogInformation("Медиа {MediaId} обновлено в базе данных (осталось источников: {RemainingCount})", media.Id, media.Sources.Count);
        }
        else
        {
            RemoveMedia(media);
            logger.LogInformation("Медиа {MediaId} удалено из базы данных (не осталось источников)", media.Id);
        }
    }

    public class MediaSourceCache
    {
        private readonly Dictionary<string, List<MediaSourceLink>> _holder = new();

        public List<MediaSourceLink> GetMedia(string sourceId)
        {
            if (_holder.TryGetValue(sourceId, out var value))
            {
                return value;
            }

            value = [];
            _holder[sourceId] = value;

            return value;
        }
    }
}

public class BatchRenameResult
{
    public required Media Media { get; init; }
    public required string OldTitle { get; init; }
    public required string NewTitle { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
