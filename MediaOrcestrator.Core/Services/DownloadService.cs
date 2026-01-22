using CSharpFunctionalExtensions;
using MediaOrcestrator.Core.Extensions;
using MediaOrcestrator.Core.Models;
using Microsoft.Extensions.Logging;
using YoutubeExplode.Videos.Streams;

namespace MediaOrcestrator.Core.Services;

public class DownloadService(
    IYoutubeService youtubeService,
    IVideoConverter converter,
    ILogger<DownloadService> logger)
{
    private readonly List<DownloadItem> _items = [];

    public bool IsNeedDownloadAny => _items.Any(item => item.IsNeedDownloadAnyStream);

    public Result<DownloadItem> FindItem(string id)
    {
        var item = _items.FirstOrDefault(downloadItem => downloadItem.Id == id);
        return item ?? Result.Failure<DownloadItem>($"DownloadItem c id {id} не найден");
    }

    public async Task<(DownloadItem item, DownloadItemStream stream)> DownloadVideo(string url, string path)
    {
        var item = await AddToQueueAsync(url, path);
        var stream = SetStreamToDownload(item.Id, item.Streams.First().Id).Value;
        await DownloadFromQueueAsync();
        return (item, stream);
    }

    public async Task<DownloadItem> AddToQueueAsync(string url, string path)
    {
        logger.LogDebug("Попытка добавить в очередь: {Url}", url);

        var downloadItem = _items.FirstOrDefault(downloadItem => downloadItem.Url == url);

        if (downloadItem is not null)
        {
            logger.LogDebug("Уже существует в очереди {Id}: {Url}", downloadItem.Id, url);
            return downloadItem;
        }

        var video = await youtubeService.GetVideoAsync(url);

        var streamManifest = await youtubeService.GetStreamManifestAsync(url);

        var highestAudioStream = (IAudioStreamInfo)streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        var highestVideoStream = (IVideoStreamInfo)streamManifest.GetVideoOnlyStreams().GetWithHighestBitrate();

        var stream = DownloadItemStream.Create(0,
            Path.Combine(path, ".temp"),
            path,
            video,
            highestAudioStream,
            highestVideoStream);

        var item = DownloadItem.Create(url, [stream], video).GetValueOrDefault();

        _items.Add(item);
        logger.LogDebug("Добавлено в очередь {Id}: {Url}", item.Id, url);
        return item;
    }

    public Result<DownloadItemStream> SetStreamToDownload(string downloadId, int streamId)
    {
        logger.LogDebug("Попытка установить поток для скачивания: {Id} {StreamId}", downloadId, streamId);

        return FindItem(downloadId)
            .TapError(s => logger.LogError("Не удалось найти элемент загрузки: {Id}, Ошибка: {Error}", downloadId, s))
            .Bind(item => item.GetStream(streamId))
            .TapError(s => logger.LogError("Не удалось получить поток: {StreamId} для элемента: {Id}, Ошибка: {Error}", streamId, downloadId, s))
            .Tap(stream => stream.State = DownloadItemState.Wait)
            .Tap(() => logger.LogDebug("Успешно установлен поток для скачивания: {Id} {StreamId}", downloadId, streamId));
    }

    public async Task DownloadFromQueueAsync()
    {
        var downloadItem = _items.FirstOrDefault(x => x.Streams.Any(itemSteam => itemSteam.State == DownloadItemState.Wait));
        var downloadStream = downloadItem?.GetWaitStreams().FirstOrDefault();

        if (downloadStream == null || downloadItem == null)
        {
            logger.LogWarning("Очередь скачивания пустая");
            return;
        }

        downloadStream.State = DownloadItemState.InProcess;
        logger.LogDebug("Попытка скачать из очереди: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);

        CancellationTokenSource cancellationTokenSource = new();

        try
        {
            await DownloadCombinedStream(downloadStream, downloadItem, cancellationTokenSource.Token);
            downloadStream.State = DownloadItemState.Ready;
            logger.LogDebug("Успешно скачан из очереди: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);
        }
        catch (Exception exception)
        {
            await cancellationTokenSource.CancelAsync();

            downloadStream.State = DownloadItemState.Error;
            logger.LogError(exception, "Не полупилось скачать из очереди: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);
        }
    }

    private async Task DownloadCombinedStream(DownloadItemStream downloadStream, DownloadItem downloadItem, CancellationToken cancellationToken = default)
    {
        var audioPath = downloadStream.TempPath.AddSuffixToFileName("audio");
        var videoPath = downloadStream.TempPath.AddSuffixToFileName("video");

        if (downloadStream.AudioStreamInfo == null || downloadStream.VideoStreamInfo == null)
        {
            logger.LogError("Не удалось объединить ({MethodName}). Нет видео или аудио", nameof(DownloadCombinedStream));
            return;
        }

        var audioTask = youtubeService.DownloadWithProgressAsync(downloadStream.AudioStreamInfo,
            audioPath,
            downloadStream.Title,
            downloadItem.Video.Title,
            cancellationToken);

        var videoTask = youtubeService.DownloadWithProgressAsync(downloadStream.VideoStreamInfo,
            videoPath,
            downloadStream.Title,
            downloadItem.Video.Title,
            cancellationToken);

        await Task.WhenAll(audioTask.AsTask(), videoTask.AsTask());

        logger.LogDebug("Попытка объединить видео и аудио: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);

        double oldPercent = -1;

        Progress<double> progress = new(percent =>
        {
            if (percent - oldPercent < 0.1)
            {
                return;
            }

            logger.LogDebug("Объединение: {Percent:P2}", percent);
            oldPercent = percent;
        });

        await converter.MergeMediaAsync(downloadStream.FilePath, [audioPath, videoPath], progress, cancellationToken);

        logger.LogDebug("Успешно объединено видео и аудио: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);
    }
}
