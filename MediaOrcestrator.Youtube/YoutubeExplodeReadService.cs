using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeExplodeReadService(
    IHttpClientFactory httpClientFactory,
    ILogger<YoutubeExplodeReadService> logger)
{
    private readonly YoutubeClient _client = new(httpClientFactory.CreateClient(YoutubeServiceFactory.ApiClientName));

    public async Task<string?> GetChannelIdAsync(
        string channelUrl,
        CancellationToken cancellationToken)
    {
        logger.ResolvingChannel(channelUrl);

        var channel = await ChannelUrlResolver.ResolveAsync<Channel>(channelUrl,
            async id => await _client.Channels.GetAsync(new(id), cancellationToken),
            async slug => await _client.Channels.GetBySlugAsync(new(slug), cancellationToken),
            async handle => await _client.Channels.GetByHandleAsync(new(handle), cancellationToken),
            async userName => await _client.Channels.GetByUserAsync(new(userName), cancellationToken));

        if (channel is not null)
        {
            logger.ChannelResolved(channel.Title, channel.Id.Value);
            return channel.Id.Value;
        }

        logger.ChannelResolveFailed(channelUrl);
        return null;
    }

    public async IAsyncEnumerable<MediaDto> GetMediaAsync(
        string channelId,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var uploads = _client.Channels.GetUploadsAsync(new(channelId), cancellationToken);

        await foreach (var video in uploads)
        {
            logger.ProcessingVideo(video.Title, video.Id.Value);

            if (isFull)
            {
                var fullVideo = await _client.Videos.GetAsync(video.Id, cancellationToken);
                yield return CreateFullMediaDto(fullVideo);
            }
            else
            {
                yield return CreateBasicMediaDto(video);
            }
        }
    }

    public async Task<MediaDto?> GetVideoByIdAsync(
        string videoId,
        CancellationToken cancellationToken)
    {
        var video = await _client.Videos.GetAsync(videoId, cancellationToken);
        return video is not null ? CreateFullMediaDto(video) : null;
    }

    private static MediaDto CreateFullMediaDto(
        Video video,
        string? tempDataPath = null)
    {
        var previewUrl = video.Thumbnails.TryGetWithHighestResolution()?.Url ?? "";

        return MediaDtoFactory.CreateFull(video.Id.Value,
            video.Title,
            video.Url,
            previewUrl,
            video.Description,
            video.Duration,
            video.Author.ChannelTitle,
            video.UploadDate.ToString("O"),
            video.Engagement.ViewCount,
            tempDataPath);
    }

    private static MediaDto CreateBasicMediaDto(PlaylistVideo video)
    {
        var previewUrl = video.Thumbnails.TryGetWithHighestResolution()?.Url ?? "";

        return MediaDtoFactory.CreateBasic(video.Id.Value,
            video.Title,
            video.Url,
            previewUrl,
            video.Duration,
            video.Author.ChannelTitle);
    }
}
