using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace MediaOrcestrator.Youtube;

public sealed class YoutubeExplodeReadService(ILogger logger)
{
    private const int RetryCount = 5;
    private const int RetryDelayMs = 500;

    private readonly YoutubeClient _client = new();

    public async Task<string?> GetChannelIdAsync(string channelUrl, CancellationToken cancellationToken)
    {
        logger.LogDebug("YoutubeExplode: попытка определить канал по URL: {ChannelUrl}", channelUrl);

        var channel = await RetryHelper.ExecuteAsync(() => ChannelUrlResolver.ResolveAsync<Channel>(channelUrl,
                async id => await _client.Channels.GetAsync(new(id), cancellationToken),
                async slug => await _client.Channels.GetBySlugAsync(new(slug), cancellationToken),
                async handle => await _client.Channels.GetByHandleAsync(new(handle), cancellationToken),
                async userName => await _client.Channels.GetByUserAsync(new(userName), cancellationToken)),
            RetryCount, RetryDelayMs, logger, cancellationToken);

        if (channel is not null)
        {
            logger.LogDebug("Канал найден: '{Title}' (ID: {Id})", channel.Title, channel.Id);
            return channel.Id.Value;
        }

        logger.LogWarning("Не удалось определить канал по URL: {ChannelUrl}", channelUrl);
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
            logger.LogDebug("Обработка видео: '{Title}' (ID: {Id})", video.Title, video.Id);

            if (isFull)
            {
                var fullVideo = await RetryHelper.ExecuteAsync(async () => await _client.Videos.GetAsync(video.Id, cancellationToken),
                    RetryCount, RetryDelayMs, logger, cancellationToken);

                yield return CreateFullMediaDto(fullVideo);
            }
            else
            {
                yield return CreateBasicMediaDto(video);
            }
        }
    }

    public async Task<MediaDto?> GetVideoByIdAsync(string videoId, CancellationToken cancellationToken)
    {
        var video = await _client.Videos.GetAsync(videoId, cancellationToken);
        return CreateFullMediaDto(video);
    }

    private static MediaDto CreateFullMediaDto(Video video, string? tempDataPath = null)
    {
        var previewUrl = video.Thumbnails.TryGetWithHighestResolution()?.Url ?? "";

        return MediaDtoFactory.CreateFull(video.Id.Value,
            video.Title,
            video.Url,
            previewUrl,
            duration: video.Duration,
            author: video.Author.ChannelTitle,
            creationDate: video.UploadDate.ToString("O"),
            viewCount: video.Engagement.ViewCount,
            tempDataPath: tempDataPath);
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
