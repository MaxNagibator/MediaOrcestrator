using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Xml;

namespace MediaOrcestrator.Youtube;

public sealed class YoutubeApiReadService(ILogger logger, YoutubeAuthService authService)
{
    private const string ChannelParts = "snippet,contentDetails";
    private const string VideoParts = "snippet,contentDetails,statistics";

    public async Task<ApiChannelInfo?> GetChannelAsync(
        string channelUrl,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        using var service = await authService.CreateServiceAsync(settings, cancellationToken);

        var channel = await ChannelUrlResolver.ResolveAsync(channelUrl,
            id => GetChannelByIdAsync(service, id, cancellationToken),
            slug => GetChannelByHandleAsync(service, slug, cancellationToken),
            handle => GetChannelByHandleAsync(service, handle, cancellationToken),
            userName => GetChannelByUsernameAsync(service, userName, cancellationToken));

        if (channel is null)
        {
            logger.LogWarning("YouTube API: не удалось определить канал по URL: {ChannelUrl}", channelUrl);
        }

        return channel;
    }

    public async IAsyncEnumerable<MediaDto> GetMediaAsync(
        string channelId,
        bool isFull,
        Dictionary<string, string> settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var service = await authService.CreateServiceAsync(settings, cancellationToken);

        // Uploads playlist ID = channel ID с заменой "UC" на "UU"
        var uploadsPlaylistId = "UU" + channelId[2..];

        logger.LogInformation("YouTube API: получение видео из плейлиста загрузок {PlaylistId}", uploadsPlaylistId);

        string? pageToken = null;

        do
        {
            var request = service.PlaylistItems.List(ChannelParts);
            request.PlaylistId = uploadsPlaylistId;
            request.MaxResults = 50;
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(cancellationToken);

            if (response.Items is null || response.Items.Count == 0)
            {
                yield break;
            }

            if (isFull)
            {
                var videoIds = string.Join(",", response.Items.Select(i => i.ContentDetails.VideoId));

                var videosRequest = service.Videos.List(VideoParts);
                videosRequest.Id = videoIds;

                var videosResponse = await videosRequest.ExecuteAsync(cancellationToken);

                foreach (var video in videosResponse.Items ?? [])
                {
                    yield return CreateFullMediaDto(video);
                }
            }
            else
            {
                foreach (var item in response.Items)
                {
                    yield return CreateBasicMediaDto(item);
                }
            }

            pageToken = response.NextPageToken;
        } while (pageToken is not null);

        logger.LogInformation("YouTube API: завершено получение видео для канала {ChannelId}", channelId);
    }

    public async Task<MediaDto?> GetVideoByIdAsync(
        string videoId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        using var service = await authService.CreateServiceAsync(settings, cancellationToken);

        var request = service.Videos.List(VideoParts);
        request.Id = videoId;

        var response = await request.ExecuteAsync(cancellationToken);
        var video = response.Items?.FirstOrDefault();

        if (video is not null)
        {
            return CreateFullMediaDto(video);
        }

        logger.LogWarning("YouTube API: видео не найдено: {VideoId}", videoId);
        return null;
    }

    private static string GetThumbnailUrl(ThumbnailDetails? thumbnails)
    {
        return thumbnails?.High?.Url
               ?? thumbnails?.Medium?.Url
               ?? thumbnails?.Default__?.Url
               ?? "";
    }

    private static MediaDto CreateFullMediaDto(Video video)
    {
        var thumbnail = GetThumbnailUrl(video.Snippet.Thumbnails);

        var duration = video.ContentDetails?.Duration is { } isoDuration
            ? XmlConvert.ToTimeSpan(isoDuration)
            : (TimeSpan?)null;

        return MediaDtoFactory.CreateFull(video.Id,
            video.Snippet.Title,
            string.Format(YoutubeChannel.VideoUrlTemplate, video.Id),
            thumbnail,
            video.Snippet.Description,
            duration,
            video.Snippet.ChannelTitle,
            video.Snippet.PublishedAtDateTimeOffset?.ToString("O"),
            (long?)video.Statistics?.ViewCount);
    }

    private static MediaDto CreateBasicMediaDto(PlaylistItem item)
    {
        var videoId = item.ContentDetails.VideoId;
        var thumbnail = GetThumbnailUrl(item.Snippet.Thumbnails);

        return MediaDtoFactory.CreateBasic(videoId,
            item.Snippet.Title,
            string.Format(YoutubeChannel.VideoUrlTemplate, videoId),
            thumbnail,
            author: item.Snippet.ChannelTitle);
    }

    private async Task<ApiChannelInfo?> GetChannelByIdAsync(YouTubeService service, string channelId, CancellationToken ct)
    {
        var request = service.Channels.List(ChannelParts);
        request.Id = channelId;

        var response = await request.ExecuteAsync(ct);
        var channel = response.Items?.FirstOrDefault();

        if (channel is null)
        {
            return null;
        }

        logger.LogDebug("YouTube API: канал найден по ID. Название: '{Title}', ID: {Id}", channel.Snippet.Title, channel.Id);
        return new(channel.Id, channel.Snippet.Title);
    }

    private async Task<ApiChannelInfo?> GetChannelByHandleAsync(YouTubeService service, string handle, CancellationToken ct)
    {
        var request = service.Channels.List(ChannelParts);
        request.ForHandle = handle;

        var response = await request.ExecuteAsync(ct);
        var channel = response.Items?.FirstOrDefault();

        if (channel is null)
        {
            return null;
        }

        logger.LogDebug("YouTube API: канал найден по handle '@{Handle}'. Название: '{Title}', ID: {Id}", handle, channel.Snippet.Title, channel.Id);
        return new(channel.Id, channel.Snippet.Title);
    }

    private async Task<ApiChannelInfo?> GetChannelByUsernameAsync(YouTubeService service, string username, CancellationToken ct)
    {
        var request = service.Channels.List(ChannelParts);
        request.ForUsername = username;

        var response = await request.ExecuteAsync(ct);
        var channel = response.Items?.FirstOrDefault();

        if (channel is null)
        {
            return null;
        }

        logger.LogDebug("YouTube API: канал найден по username '{Username}'. Название: '{Title}', ID: {Id}", username, channel.Snippet.Title, channel.Id);
        return new(channel.Id, channel.Snippet.Title);
    }
}

public sealed record ApiChannelInfo(string Id, string Title);
