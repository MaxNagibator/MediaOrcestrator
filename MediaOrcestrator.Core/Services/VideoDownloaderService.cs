using MediaOrcestrator.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MediaOrcestrator.Core.Services;

public class VideoDownloaderService(
    DownloadService downloadService,
    IYoutubeService youtubeService,
    DirectoryService directoryService,
    IPictureDownloader pictureDownloader,
    ILogger<VideoDownloaderService> logger)
{
    /// <summary>
    /// Асинхронно загружает видео с указанного канала.
    /// </summary>
    /// <param name="channelUrl">URL канала YouTube.</param>
    /// <returns>Список информации о загруженных видео.</returns>
    public async Task<List<VideoInfo>> DownloadVideosFromChannelAsync(string channelUrl)
    {
        logger.LogInformation("Начинаем загрузку видео с канала: {ChannelUrl}", channelUrl);

        List<VideoInfo> videoInfoList = [];
        var uploads = FetchUploadVideosAsync(channelUrl);

        await foreach (var video in uploads)
        {
            videoInfoList.Add(video);
            logger.LogDebug("Добавлено видео: {Title}", video.Title);
        }

        logger.LogInformation("Загружено {Count} видео из канала: {ChannelUrl}", videoInfoList.Count, channelUrl);
        return videoInfoList;
    }

    /// <summary>
    /// Асинхронно получает видео загрузок для указанного канала.
    /// </summary>
    /// <param name="channelUrl">URL канала YouTube.</param>
    /// <returns>Асинхронная коллекция информации о видео.</returns>
    public async IAsyncEnumerable<VideoInfo> FetchUploadVideosAsync(string channelUrl)
    {
        logger.LogInformation("Получаем видео загрузок для канала: {ChannelUrl}", channelUrl);

        var playlistVideos = youtubeService.GetUploadsAsync(channelUrl);

        await foreach (var video in playlistVideos)
        {
            yield return new()
            {
                Id = video.Id.Value,
                Title = video.Title,
                State = VideoState.NotDownloaded,
            };

            logger.LogTrace("Найдено видео: {Title}", video.Title);
        }
    }

    /// <summary>
    /// Асинхронно загружает отдельное видео.
    /// </summary>
    /// <param name="videoInfo">Информация о видео.</param>
    /// <param name="path">Путь для сохранения видео.</param>
    /// <returns>Состояние загрузки видео.</returns>
    public async Task<VideoState> DownloadVideoAsync(VideoInfo videoInfo, string path)
    {
        var url = videoInfo.Url;

        logger.LogInformation("Начинаем загрузку видео: {VideoTitle} из {Url}", videoInfo.Title, url);
        var (item, stream) = await downloadService.DownloadVideo(url, path);

        if (item == null)
        {
            logger.LogError("Ошибка при загрузке видео: {VideoTitle}", videoInfo.Title);
            return VideoState.Error;
        }

        await SaveVideoMetadataAsync(videoInfo, item, path);

        logger.LogInformation("Загрузка видео завершена: {VideoTitle}", videoInfo.Title);
        directoryService.CleanUpTempFiles(item, stream);
        return VideoState.Downloaded;
    }

    /// <summary>
    /// Асинхронно сохраняет метаданные видео.
    /// </summary>
    /// <param name="videoInfo">Информация о видео.</param>
    /// <param name="item">Загруженный элемент видео.</param>
    /// <param name="path">Путь для сохранения метаданных.</param>
    private async Task SaveVideoMetadataAsync(VideoInfo videoInfo, DownloadItem item, string path)
    {
        logger.LogInformation("Сохраняем метаданные видео: {VideoTitle}", videoInfo.Title);

        var data = new VideoData();
        data.Title = videoInfo.Title;
        data.Description = item.Video.Description;
        data.UploadDate = item.Video.UploadDate;

        var dataJson = JsonSerializer.Serialize(data);
        File.WriteAllText(Path.Combine(path, $"{videoInfo.Id}.json"), dataJson);
        logger.LogDebug($"Метаданные для {videoInfo.Title} успешно сохранены {videoInfo.Id}.json");

        await DownloadThumbnailAsync(videoInfo.ThumbnailUrl, Path.Combine(path, $"{videoInfo.Id}_thumbnail.jpg"));
    }

    /// <summary>
    /// Асинхронно загружает миниатюру видео.
    /// </summary>
    /// <param name="thumbnailUrl">URL миниатюры.</param>
    /// <param name="savePath">Путь для сохранения миниатюры.</param>
    private async Task DownloadThumbnailAsync(string? thumbnailUrl, string savePath)
    {
        if (string.IsNullOrEmpty(thumbnailUrl))
        {
            logger.LogWarning("URL миниатюры пустой или недоступен. Пропуск загрузки миниатюры");
            return;
        }

        try
        {
            logger.LogDebug("Начинаем загрузку миниатюры: {ThumbnailUrl}", thumbnailUrl);
            await pictureDownloader.Download(thumbnailUrl, savePath);

            logger.LogInformation("Миниатюра успешно сохранена в: {Path}", savePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке миниатюры: {ThumbnailUrl}", thumbnailUrl);
        }
    }
}
