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

    public void Init()
    {
        pluginManager.Init();
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
        db.GetCollection<SourceSyncRelation>("source_relations").Insert(new SourceSyncRelation {
            FromId = from.Id,
            ToId = to.Id
        });
    }

    public void RemoveRelation(Source from, Source to)
    {
        // TODO: Подумать
        db.GetCollection<SourceSyncRelation>("source_relations")
            .DeleteMany(x => x.FromId == from.Id && x.ToId == to.Id);
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
