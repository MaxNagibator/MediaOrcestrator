
using MediaOrcestrator.Core.Services;

namespace MediaOrcestrator.Domain
{
    public class Orcestrator(PluginManager pluginManager)
    {
        private List<SourceRelation> _relations;

        public Dictionary<string, IMediaSource> GetSources()
        {
            return pluginManager.MediaSources;
        }

        public void Init()
        {
            pluginManager.Init();
            _relations = new List<SourceRelation>();
            _relations.Add(new SourceRelation
            {
                IdFrom = "MediaOrcestrator.Youtube",
                IdTo = "MediaOrcestrator.HardDiskDrive",
            });

            var sources = GetSources();
            foreach (var relation in _relations)
            {
                if (sources.ContainsKey(relation.IdFrom))
                {
                    relation.From = sources[relation.IdFrom];
                }
                if (sources.ContainsKey(relation.IdTo))
                {
                    relation.To = sources[relation.IdTo];
                }
            }
        }

        public async Task Sync()
        {
            var mediaAll = new List<MyMedia>();

            var mediaBySourceId = new Dictionary<string, List<MyMediaSource>>();
            foreach (var media in mediaAll)
            {
                foreach (var source in media.Sources)
                {
                    if (mediaBySourceId.ContainsKey(source.Id))
                    {
                        mediaBySourceId[source.Id] = new List<MyMediaSource>();
                    }
                    mediaBySourceId[source.Id].Add(source);
                }
            }

            await Parallel.ForEachAsync(GetSources(), async (mediaSource, cancellationToken) =>
             {
                 if (mediaSource.Key != "MediaOrcestrator.Youtube")
                 {
                     return;
                 }
                 var source = mediaSource.Value;
                 var sourceId = mediaSource.Key;
                 var syncMedia = mediaSource.Value.GetMedia();

                 await foreach (var s in syncMedia)
                 {
                     var foundMediaSource = mediaBySourceId[sourceId].FirstOrDefault(x => x.Id == s.Title);
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
                         var myMedia = new MyMedia();
                         myMedia.Title = s.Title;
                         myMedia.Id = s.Id;
                         myMedia.Description = s.Description;
                         myMedia.Sources = new List<MyMediaSource>();
                         var newMediaSource = new MyMediaSource { Id = s.Id, Media = myMedia, Status = "OK", SourceId = sourceId };
                         myMedia.Sources.Add(newMediaSource);
                         mediaAll.Add(myMedia);
                         mediaBySourceId[sourceId].Add(newMediaSource);
                     }
                 }
             });
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
            public MyMedia Media { get; set; }
        }
    }

    public static class OrcestratorBuilder
    {
        public static Orcestrator Construct()
        {
            return new Orcestrator(new PluginManager());
        }
    }
}
