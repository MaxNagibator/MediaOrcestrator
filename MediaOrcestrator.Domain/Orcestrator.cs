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

    public async Task GetStorageFullInfo()
    {
        logger.LogInformation("Запуск процесса синхронизации...");

        var mediaCol = db.GetCollection<Media>("medias");
        //mediaCol.DeleteAll();
        var mediaAll = mediaCol.FindAll().ToList();

        var cache = new MediaSourceCache();

        foreach (var media in mediaAll)
        {
            foreach (var source in media.Sources)
            {
                source.Media = media;
                cache.GetMedia(source.SourceId).Add(source);
            }
        }

        logger.LogInformation("Обнаружено {Count} элементов медиа в локальном кэше.", mediaAll.Count);

        var sourceTypes = GetSourceTypes();

        await Parallel.ForEachAsync(GetSources(), async (mediaSource, cancellationToken) =>
        {
            var plugin = sourceTypes.Values.FirstOrDefault(x => x.Name == mediaSource.TypeId);

            if (plugin == null)
            {
                logger.LogError("Плагин для типа {TypeId} не найден.", mediaSource.TypeId);
                return;
            }

            var syncMedia = plugin.GetMedia(mediaSource.Settings);
            var i = 0;
            await foreach (var s in syncMedia)
            {
                i++;
                if (i > 99999)
                {
                    logger.LogWarning("Достигнут лимит в 10 элементов для источника {SourceId}, прерываем.", mediaSource.Id);
                    break;
                }

                var foundMediaSource = cache.GetMedia(mediaSource.Id).FirstOrDefault(x => x.ExternalId == s.Id);
                if (foundMediaSource != null)
                {
                    if (foundMediaSource.Media.Title != s.Title)
                    {
                        // todo write to audit
                        foundMediaSource.Media.Title = s.Title;
                    }
                }
                else
                {
                    var mediaId = Guid.NewGuid().ToString();
                    var myMedia = new Media
                    {
                        Title = s.Title,
                        Id = mediaId,
                        Description = s.Description,
                        Sources = [],
                    };

                    var newMediaSource = new MediaSourceLink
                    {
                        MediaId = mediaId,
                        Media = myMedia,
                        ExternalId = s.Id,
                        Status = "OK",
                        SourceId = mediaSource.Id,
                    };

                    myMedia.Sources.Add(newMediaSource);
                    // поправить циклический зависимость
                    mediaCol.Insert(myMedia);
                    mediaAll.Add(myMedia);
                    cache.GetMedia(mediaSource.Id).Add(newMediaSource);
                }
            }
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
        //    if(media.Sources.Any(x=>x.ExternalId == null))
        //    {
        //        db.GetCollection<Media>("medias").Delete(media.Id);
        //    }
        //}
        //medias = db.GetCollection<Media>("medias").FindAll().ToList();
        return medias;
    }

    public void UpdateMedia(Media media)
    {
        db.GetCollection<Media>("medias").Update(media);
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
            if (fromSource == null || toSource == null || fromSource.IsDisable || toSource.IsDisable)
            {
                item.IsDisable = true;
                continue;
            }

            item.From = fromSource;
            item.To = toSource;
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

    public async Task TransferByRelation(Media media, SourceSyncRelation rel, string fromExternalId)
    {
        var tempMedia = await rel.From.Type.Download(fromExternalId, rel.From.Settings);
        tempMedia.Id = media.Id;
        var externalId = await rel.To.Type.Upload(tempMedia, rel.To.Settings);

        var toMediaSource = new MediaSourceLink
        {
            MediaId = media.Id,
            Media = media,
            ExternalId = externalId,
            Status = "OK",
            SourceId = rel.To.Id,
        };

        media.Sources.Add(toMediaSource);
        UpdateMedia(media);
        logger.LogInformation("Успешно синхронизировано медиа {Media} в {ToSource}. ExternalId: {ExternalId}", media, rel.To, externalId);
    }

   public async Task DeleteMediaFromSourceAsync(Media media, Source source)
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
            await source.Type.DeleteAsync(sourceLink.ExternalId, source.Settings);
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
            if (!_holder.ContainsKey(sourceId))
            {
                _holder[sourceId] = [];
            }

            return _holder[sourceId];
        }
    }
}
