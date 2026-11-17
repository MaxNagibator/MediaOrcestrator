using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeDownloadPolicy(YoutubeVideoKindStore kindStore)
{
    public async Task EnsureAllowedAsync(
        YoutubeDownloadSettings settings,
        IYoutubeVideoInfoReader infoReader,
        string videoId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (settings.DownloadStreams)
        {
            return;
        }

        var kind = kindStore.Get(settings.StateDirectory, videoId);
        if (kind == YoutubeVideoKind.None)
        {
            var videoUrl = string.Format(YoutubeChannel.VideoUrlTemplate, videoId);
            var info = await infoReader.GetVideoInfoAsync(videoUrl, cancellationToken);
            kind = info?.Kind ?? YoutubeVideoKind.None;
            kindStore.Save(settings.StateDirectory, videoId, kind);
        }

        switch (kind)
        {
            case YoutubeVideoKind.Video:
                return;

            case YoutubeVideoKind.Livestream:
                throw new MediaDownloadSkippedException("Скачивание трансляций отключено в настройках источника YouTube");

            default:
                throw new NonRetriableException($"Не удалось определить, является ли видео трансляцией: {videoId}");
        }
    }
}
