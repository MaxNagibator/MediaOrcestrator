using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public class Orcestrator(PluginManager pluginManager, ILogger<Orcestrator> logger)
{
    private List<SourceRelation> _relations;

    public Dictionary<string, IMediaSource> GetSources()
    {
        return pluginManager.MediaSources;
    }

    public void Init()
    {
        pluginManager.Init();
        _relations =
        [
            new()
            {
                IdFrom = "MediaOrcestrator.Youtube",
                IdTo = "MediaOrcestrator.HardDiskDrive",
            },
        ];

        var sources = GetSources();
        foreach (var relation in _relations)
        {
            if (sources.TryGetValue(relation.IdFrom, out var from))
            {
                relation.From = from;
            }

            if (sources.TryGetValue(relation.IdTo, out var to))
            {
                relation.To = to;
            }
        }
    }

    public async Task Sync()
    {
        logger.LogInformation("Запуск процесса синхронизации...");
        using var db = new LiteDatabase(@"MyData.db");
        var mediaCol = db.GetCollection<MyMedia>("medias");
        //var myMedia = new MyMedia();
        //myMedia.Id = "test2";
        //col.Insert(myMedia);

        //using (var db = new LiteDatabase(@"MyData.db"))
        //{
        //    var col = db.GetCollection<MyMedia>("companies");
        //    var mediaa = col.FindAll();
        //    var huys = mediaa.ToList();
        //    var a2 = mediaa.FirstOrDefault();
        //    var a = 1;
        //}

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

        await Parallel.ForEachAsync(GetSources(), async (mediaSource, cancellationToken) =>
        {
            if (mediaSource.Key != "MediaOrcestrator.Youtube")
            {
                return;
            }

            var source = mediaSource.Value;
            var sourceId = mediaSource.Key;
            var syncMedia = mediaSource.Value.GetMedia();
            var i = 0;
            await foreach (var s in syncMedia)
            {
                i++;

                if (i > 10)
                {
                    logger.LogWarning("Достигнут лимит в 10 элементов для источника {SourceId}, прерываем.", sourceId);
                    break;
                }

                var foundMediaSource = cache.GetMedia(sourceId).FirstOrDefault(x => x.Id == s.Title);
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

                    var newMediaSource = new MyMediaSource
                    {
                        Id = s.Id,
                        Media = myMedia,
                        Status = "OK",
                        SourceId = sourceId,
                    };

                    myMedia.Sources.Add(newMediaSource);
                    // поправить циклический зависимость
                    mediaCol.Insert(myMedia);
                    mediaAll.Add(myMedia);
                    cache.GetMedia(sourceId).Add(newMediaSource);
                }
            }
        });

        logger.LogInformation("Синхронизация успешно завершена.");

        var zalupa = 1;
    }

    public List<MyMedia> GetMediaData()
    {
        using var db = new LiteDatabase(@"MyData.db");
        return db.GetCollection<MyMedia>("medias").FindAll().ToList();
    }

    public class MediaSourceCache
    {
        private readonly Dictionary<string, List<MyMediaSource>> _holder = new();

        public List<MyMediaSource> GetMedia(string sourceId)
        {
            if (!_holder.ContainsKey(sourceId))
            {
                _holder[sourceId] = [];
            }

            return _holder[sourceId];
        }
    }

    public class MyMedia
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public List<MyMediaSource> Sources { get; set; }
    }

    public class MyMediaSource
    {
        public string SourceId { get; set; }
        public string Status { get; set; }
        public string Id { get; set; }

        [BsonIgnore]
        public MyMedia Media { get; set; }
    }
}
