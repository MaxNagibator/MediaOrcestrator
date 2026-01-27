using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public class Orcestrator(PluginManager pluginManager, LiteDatabase db, ILogger<Orcestrator> logger)
{
    private List<SourceRelation> _relations;

    public Dictionary<string, IMediaSource> GetSources()
    {
        return pluginManager.MediaSources;
    }

    public void Init()
    {
        pluginManager.Init();
        var sources = GetSources();
    }

    public async Task Sync()
    {
        logger.LogInformation("Запуск процесса синхронизации...");

        var mediaCol = db.GetCollection<MyMedia>("medias");
        var mediaAll = mediaCol.FindAll().ToList();

        var cache = new MediaSourceCache();

        foreach (var media in mediaAll)
        {
            foreach (var source in media.Sources)
            {
                cache.GetMedia(source.Id).Add(source);
            }
        }

        logger.LogInformation("Обнаружено {Count} элементов медиа в локальном кэше.", mediaAll.Count);

        await Parallel.ForEachAsync(GetMediaSourceData(), async (mediaSource, cancellationToken) =>
        {
            var sources = GetSources();
            var plugin = sources.Values.FirstOrDefault(x => x.Name == mediaSource.TypeId);

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

                if (i > 10)
                {
                    logger.LogWarning("Достигнут лимит в 10 элементов для источника {SourceId}, прерываем.", mediaSource.Id);
                    break;
                }

                var foundMediaSource = cache.GetMedia(mediaSource.Id).FirstOrDefault(x => x.Id == s.Title);
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
                    var myMedia = new MyMedia
                    {
                        Title = s.Title,
                        Id = s.Id,
                        Description = s.Description,
                        Sources = [],
                    };

                    var newMediaSource = new MyMediaLinkToSource
                    {
                        Id = s.Id,
                        Media = myMedia,
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

    public List<MyMedia> GetMediaData()
    {
        return db.GetCollection<MyMedia>("medias").FindAll().ToList();
    }

    public List<MySource> GetMediaSourceData()
    {
        return db.GetCollection<MySource>("sources").FindAll().ToList();
    }

    public void AddSource(string typeId, Dictionary<string, string> settings)
    {
        db.GetCollection<MySource>("sources")
            .Insert(new MySource
            {
                Id = Guid.NewGuid().ToString(),
                TypeId = typeId,
                Settings = settings,
            });
    }

    public void RemoveSource(string sourceId)
    {
        db.GetCollection<MySource>("sources").Delete(sourceId);
    }

    public List<SourceRelation> GetRelations()
    {
        return db.GetCollection<SourceRelation>("source_relations").FindAll().ToList();
    }

    public void AddLink(MySource from, MySource to)
    {
        db.GetCollection<SourceRelation>("source_relations").Insert(new SourceRelation { From = from, To = to });
    }

    public void RemoveLink(MySource from, MySource to)
    {
        // TODO: Подумать
        db.GetCollection<SourceRelation>("source_relations")
            .DeleteMany(x => x.From.Id == from.Id && x.To.Id == to.Id);
    }

    public class MediaSourceCache
    {
        private readonly Dictionary<string, List<MyMediaLinkToSource>> _holder = new();

        public List<MyMediaLinkToSource> GetMedia(string sourceId)
        {
            if (!_holder.ContainsKey(sourceId))
            {
                _holder[sourceId] = [];
            }

            return _holder[sourceId];
        }
    }
}
