using MediaOrcestrator.Core.Models;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MediaOrcestrator.Core.Services;

public interface IYoutubeService
{
    ValueTask DownloadAsync(IStreamInfo stream, string path, IProgress<double>? progress, CancellationToken cancellationToken);
    ValueTask DownloadWithProgressAsync(DownloadItemStream downloadStream, CancellationToken token);
    ValueTask DownloadWithProgressAsync(IStreamInfo streamInfo, string path, string streamTitle, string videoTitle, CancellationToken cancellationToken);
    Task<Channel?> GetChannel(string channelUrl);
    ValueTask<StreamManifest> GetStreamManifestAsync(string url);
    IAsyncEnumerable<PlaylistVideo> GetUploadsAsync(string channelUrl);
    ValueTask<Video> GetVideoAsync(string url);
}

public class YoutubeService(
    YoutubeClient youtubeClient,
    ILogger<YoutubeService> logger) : IYoutubeService
{
    private readonly Func<string, Task<Channel?>>[] _parsers =
    [
        async url => ChannelId.TryParse(url) is { } id ? await youtubeClient.Channels.GetAsync(id) : null,
        async url => ChannelSlug.TryParse(url) is { } slug ? await youtubeClient.Channels.GetBySlugAsync(slug) : null,
        async url => ChannelHandle.TryParse(url) is { } handle ? await youtubeClient.Channels.GetByHandleAsync(handle) : null,
        async url => UserName.TryParse(url) is { } userName ? await youtubeClient.Channels.GetByUserAsync(userName) : null,
    ];

    public async Task<Channel?> GetChannel(string channelUrl)
    {
        foreach (var parser in _parsers)
        {
            var channel = await parser(channelUrl);

            if (channel != null)
            {
                return channel;
            }
        }

        return null;
    }

    public IAsyncEnumerable<PlaylistVideo> GetUploadsAsync(string channelUrl)
    {
        return youtubeClient.Channels.GetUploadsAsync(channelUrl);
    }

    public ValueTask DownloadAsync(IStreamInfo stream, string path, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        return youtubeClient.Videos.Streams.DownloadAsync(stream, path, progress, cancellationToken);
    }

    public ValueTask DownloadWithProgressAsync(IStreamInfo streamInfo, string path, string streamTitle, string videoTitle, CancellationToken cancellationToken)
    {
        double oldPercent = -1;

        var streamType = streamInfo switch
        {
            AudioOnlyStreamInfo => "Audio",
            VideoOnlyStreamInfo => "Video",
            MuxedStreamInfo => "Muxed",
            _ => "Unknown",
        };

        Progress<double> progress = new(percent =>
        {
            if (percent - oldPercent < 0.1)
            {
                return;
            }

            logger.LogDebug("{StreamType}: {Percent:P2} {VideoTitle} {StreamTitle}", streamType, percent, videoTitle, streamTitle);
            oldPercent = percent;
        });

        return DownloadAsync(streamInfo, path, progress, cancellationToken);
    }

    public ValueTask DownloadWithProgressAsync(DownloadItemStream downloadStream, CancellationToken token)
    {
        return downloadStream.Stream != null
            ? DownloadWithProgressAsync(downloadStream.Stream, downloadStream.TempPath, downloadStream.Title, downloadStream.FileName, token)
            : ValueTask.CompletedTask;
    }

    public ValueTask<StreamManifest> GetStreamManifestAsync(string url)
    {
        return youtubeClient.Videos.Streams.GetManifestAsync(url);
    }

    public ValueTask<Video> GetVideoAsync(string url)
    {
        return youtubeClient.Videos.GetAsync(url);
    }
}
