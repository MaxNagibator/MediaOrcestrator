using MediaOrcestrator.Modules;
using YoutubeExplode;
using YoutubeExplode.Channels;

namespace MediaOrcestrator.Youtube;

public class YoutubeChannel : ISourceType
{
    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "Youtube";

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "channel_id",
            IsRequired = true,
            Title = "идентификатор канала",
        },
    ];

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings)
    {
        var channelUrl = settings["channel_id"];
        var youtubeClient = new YoutubeClient();
        //var channelUrl = "https://www.youtube.com/@bobito217";
        var channel = await GetChannel(youtubeClient, channelUrl);
        var uploads = youtubeClient.Channels.GetUploadsAsync(channel.Id);
        await foreach (var video in uploads)
        {
            yield return new MediaDto
            {
                Id = video.Id.Value,
                Title = video.Title,
            };
        }
    }

    private readonly Func<YoutubeClient, string, Task<Channel?>>[] _parsers =
    [
        async (youtubeClient, url ) => ChannelId.TryParse(url) is { } id ? await youtubeClient.Channels.GetAsync(id) : null,
        async (youtubeClient, url ) => ChannelSlug.TryParse(url) is { } slug ? await youtubeClient.Channels.GetBySlugAsync(slug) : null,
        async (youtubeClient, url ) => ChannelHandle.TryParse(url) is { } handle ? await youtubeClient.Channels.GetByHandleAsync(handle) : null,
        async (youtubeClient, url ) => UserName.TryParse(url) is { } userName ? await youtubeClient.Channels.GetByUserAsync(userName) : null,
    ];

    public async Task<Channel?> GetChannel(YoutubeClient client, string channelUrl)
    {
        foreach (var parser in _parsers)
        {
            var channel = await parser(client, channelUrl);

            if (channel != null)
            {
                return channel;
            }
        }

        return null;
    }

    public MediaDto GetMediaById()
    {
        throw new NotImplementedException();
    }

    public MediaDto Download()
    {
        Console.WriteLine("ютубный я загрузил брат ");
        throw new NotImplementedException();
    }

    public void Upload(MediaDto media)
    {
        Console.WriteLine("ютубный я загрузил брат " + media.Title);
    }
}
