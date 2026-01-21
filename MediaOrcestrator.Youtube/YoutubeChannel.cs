using MediaOrcestrator.Core.Services;
using YoutubeExplode;

namespace MediaOrcestrator.Youtube
{
    public class YoutubeChannel : IMediaSource
    {
        public ChannelType ChannelType => ChannelType.OnlyUpload;

        public string Name => "Youtube";

        public async IAsyncEnumerable<IMedia> GetMedia()
        {
            var youtubeClient = new YoutubeClient();
            var channelUrl = "https://www.youtube.com/@bobito217";
            var uploads = youtubeClient.Channels.GetUploadsAsync(channelUrl);
            await foreach (var video in uploads)
            {
                yield return new YoutubeMedia()
                {
                    Id = video.Id.Value,
                    Title = video.Title,
                };
            }
        }

        public IMedia GetMediaById()
        {
            throw new NotImplementedException();
        }

        public IMedia Download()
        {
            Console.WriteLine("ютубный я загрузил брат ");
            throw new NotImplementedException();
        }

        public void Upload(IMedia media)
        {
            Console.WriteLine("ютубный я загрузил брат " + media.Title);
        }
    }

    public class YoutubeMedia : IMedia
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
