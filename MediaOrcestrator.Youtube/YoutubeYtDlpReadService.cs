using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Youtube;

public sealed class YoutubeYtDlpReadService(ILogger logger)
{
    public async Task<MediaDto?> GetVideoByIdAsync(string videoId, YtDlp ytDlp, CancellationToken cancellationToken)
    {
        var url = string.Format(YoutubeChannel.VideoUrlTemplate, videoId);
        logger.LogDebug("yt-dlp: получение метаданных видео {VideoId}", videoId);

        var info = await ytDlp.GetVideoInfoAsync(url, cancellationToken);

        if (info is null)
        {
            logger.LogWarning("yt-dlp: пустой ответ для видео {VideoId}", videoId);
            return null;
        }

        return BuildMediaDto(videoId, info);
    }

    public MediaDto BuildMediaDto(string fallbackVideoId, YtDlpVideoInfo info)
    {
        var videoId = string.IsNullOrEmpty(info.Id) ? fallbackVideoId : info.Id;
        logger.LogDebug("yt-dlp: метаданные '{Title}' (ID: {Id})", info.Title, videoId);

        return MediaDtoFactory.CreateFull(videoId,
            info.Title,
            string.Format(YoutubeChannel.VideoUrlTemplate, videoId),
            info.Thumbnail ?? "",
            info.Description,
            info.Duration,
            info.Uploader,
            info.UploadDate?.ToString("O"),
            info.ViewCount);
    }
}
