using MediaOrcestrator.Core.Configurations;
using MediaOrcestrator.Core.Extensions;
using MediaOrcestrator.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using YoutubeExplode.Channels;
using YoutubeExplode.Videos;

namespace MediaOrcestrator.Core.Services;

public class ChannelService(
    IYoutubeService youtubeService,
    VideoDownloaderService helper,
    DirectoryService directoryService,
    IOptions<DownloadOptions> options,
    ILogger<ChannelService> logger)
{
    private readonly DownloadOptions _options = options.Value;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Основной метод для загрузки видео с канала YouTube по его URL.
    /// </summary>
    /// <param name="channelUrl">URL канала YouTube.</param>
    /// <param name="isDownload">Необходимо ли загружать обнаруженные видео.</param>
    public async Task DownloadVideosAsync(string channelUrl, bool isDownload = true)
    {
        var channel = await youtubeService.GetChannel(channelUrl);
        if (channel == null)
        {
            logger.LogError("Не удалось найти канал по ссылке: {Url}", channelUrl);
            return;
        }

        var channelTitle = channel.Title.GetFileName();
        var channelPath = Path.Combine(_options.VideoFolderPath, channelTitle);
        var dataPath = Path.Combine(channelPath, "data.json");
        var videosPath = Path.Combine(channelPath, "videos");

        EnsureDirectoriesExists(channelPath, videosPath);

        // TODO: Подумать над порядком UpdateVideoTitlesAndRenameFilesAsync и передачей videosPath
        var videos = await LoadVideosDataAsync(channel.Id, channelTitle, dataPath, videosPath);

        if (videos == null)
        {
            return;
        }

        ValidateVideoState(videos, videosPath);
        await SaveVideoData(videos, dataPath);

        if (isDownload)
        {
            await DownloadVideosAsync(videos, videosPath);
        }

        await SaveVideoData(videos, dataPath);
    }

    /// <summary>
    /// Сохраняет данные о видео.
    /// </summary>
    /// <param name="videos">Список видео.</param>
    /// <param name="dataPath">Путь к файлу data.json.</param>
    private async Task SaveVideoData(List<VideoInfo> videos, string dataPath)
    {
        var updatedVideoData = JsonSerializer.Serialize(videos, _serializerOptions);
        await File.WriteAllTextAsync(dataPath, updatedVideoData, Encoding.UTF8);
        logger.LogDebug("Данные видео обновлены и сохранены в файл: {DataPath}", dataPath);
    }

    /// <summary>
    /// Проверяет наличие директории для канала и создает её, если необходимо.
    /// </summary>
    /// <param name="channelPath">Путь к директории канала.</param>
    /// <param name="videosPath">Путь к директории для хранения видео.</param>
    private void EnsureDirectoriesExists(string channelPath, string videosPath)
    {
        logger.LogDebug("Проверка наличия директории для канала");

        if (!Directory.Exists(channelPath))
        {
            logger.LogDebug("Создание директории: {ChannelPath}", channelPath);
            Directory.CreateDirectory(channelPath);
        }

        directoryService.CleanUpDirectories(videosPath);
    }

    /// <summary>
    /// Получает существующие видео из файла данных или загружает новые данные о видео с канала.
    /// </summary>
    /// <param name="channelId">ID канала.</param>
    /// <param name="channelTitle">Название канала.</param>
    /// <param name="dataPath">Путь к файлу data.json.</param>
    /// <param name="videosPath"></param>
    /// <returns>Список видео или null, если данные не найдены.</returns>
    private async Task<List<VideoInfo>?> LoadVideosDataAsync(ChannelId channelId, string channelTitle, string dataPath, string videosPath)
    {
        if (File.Exists(dataPath))
        {
            logger.LogDebug("Чтение данных видео из файла: {DataPath}", dataPath);
            var videoData = await File.ReadAllTextAsync(dataPath);
            var savedVideos = JsonSerializer.Deserialize<List<VideoInfo>>(videoData);

            if (savedVideos is { Count: > 0 })
            {
                FillMissingVideoIds(savedVideos);

                savedVideos = RemoveDuplicates(savedVideos);

                await UpdateVideoTitlesAndRenameFilesAsync(savedVideos, videosPath);

                if (!options.Value.AddOnlyNew)
                {
                    return await FetchNewVideoUploadsAsync(channelId, null);
                }

                return await UpdateVideosAsync(savedVideos, channelId);
            }

            logger.LogWarning("Файл data.json не содержит информации о видео");
        }
        else
        {
            logger.LogDebug("Файл data.json не найден");
        }

        logger.LogDebug("Загрузка информации о загрузках на канале: {Channel}", channelTitle);
        var newVideos = await helper.DownloadVideosFromChannelAsync(channelId);
        newVideos.Reverse();

        return newVideos;
    }

    /// <summary>
    /// Обновляет список видео, добавляя новые загрузки, если они есть.
    /// </summary>
    /// <param name="videos">Список видео для обновления.</param>
    /// <param name="channelId">ID канала.</param>
    private async Task<List<VideoInfo>> UpdateVideosAsync(List<VideoInfo> videos, ChannelId channelId)
    {
        var lastVideo = videos.LastOrDefault();

        if (lastVideo == null)
        {
            return videos;
        }

        var newVideos = await FetchNewVideoUploadsAsync(channelId, lastVideo.Id);

        if (newVideos.Count <= 0)
        {
            return videos;
        }

        var existingIds = videos.Select(x => x.Id).ToHashSet();
        var uniqueNewVideos = newVideos.Where(x => !existingIds.Contains(x.Id)).ToList();

        videos.AddRange(uniqueNewVideos);

        logger.LogInformation("Найдено {Count} новых видео", uniqueNewVideos.Count);

        return videos;
    }

    /// <summary>
    /// Обрабатывает и загружает новые видео.
    /// </summary>
    /// <param name="videos">Список видео.</param>
    /// <param name="videosPath">Путь к директории с видеофайлами.</param>
    private Task DownloadVideosAsync(List<VideoInfo> videos, string videosPath)
    {
        var videosToDownload = videos.Where(x => x.State is VideoState.NotDownloaded or VideoState.Error).ToList();

        if (_options.MaxDownloadsPerRun > 0 && videosToDownload.Count > _options.MaxDownloadsPerRun)
        {
            logger.LogInformation("Применено ограничение на количество загрузок за запуск: {Limit}. Будет загружено {ActualCount} из {RequestedCount} видео.",
                _options.MaxDownloadsPerRun,
                _options.MaxDownloadsPerRun,
                videosToDownload.Count);

            videosToDownload = videosToDownload
                .Take(_options.MaxDownloadsPerRun)
                .ToList();
        }

        return DownloadPendingVideosAsync(videosToDownload, videosPath);
    }

    /// <summary>
    /// Получает новые загруженные видео с канала, которые не были скачаны.
    /// </summary>
    /// <param name="channelId">ID канала.</param>
    /// <param name="lastVideoId">ID последнего загруженного видео. При null будут получены все видео.</param>
    /// <returns>Список новых видео.</returns>
    private async Task<List<VideoInfo>> FetchNewVideoUploadsAsync(ChannelId channelId, string? lastVideoId)
    {
        List<VideoInfo> newVideos = [];
        var uploads = helper.FetchUploadVideosAsync(channelId);

        await foreach (var upload in uploads)
        {
            if (lastVideoId == upload.Id)
            {
                break;
            }

            newVideos.Add(upload);
            logger.LogDebug("Добавлено новое видео: {Title}", upload.Title);
        }

        newVideos.Reverse();
        return newVideos;
    }

    /// <summary>
    /// Извлекает ID видео из YouTube URL.
    /// </summary>
    /// <param name="url">URL видео.</param>
    /// <returns>ID видео или null, если не удалось извлечь.</returns>
    private string? ExtractVideoIdFromUrl(string url)
    {
        try
        {
            if (VideoId.TryParse(url) is { } videoId)
            {
                return videoId.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось извлечь ID из URL: {Url}", url);
        }

        return null;
    }

    /// <summary>
    /// Заполняет отсутствующие ID видео, извлекая их из URL.
    /// </summary>
    /// <param name="videos">Список видео.</param>
    private void FillMissingVideoIds(List<VideoInfo> videos)
    {
        var filledCount = 0;

        for (var i = 0; i < videos.Count; i++)
        {
            var video = videos[i];

            if (!string.IsNullOrWhiteSpace(video.Id))
            {
                continue;
            }

            var extractedId = ExtractVideoIdFromUrl(video.Url);

            if (extractedId != null)
            {
                videos[i] = new()
                {
                    Id = extractedId,
                    Title = video.Title,
                    State = video.State,
                };

                filledCount++;
                logger.LogDebug("Заполнен ID для видео: {Title}", video.Title);
            }
            else
            {
                logger.LogWarning("Не удалось извлечь ID из URL для видео: {Title}", video.Title);
            }
        }

        if (filledCount > 0)
        {
            logger.LogInformation("Заполнено {Count} ID из URL", filledCount);
        }
    }

    /// <summary>
    /// Удаляет дубликаты видео по ID, оставляя первое вхождение.
    /// </summary>
    /// <param name="videos">Список видео.</param>
    /// <returns>Список видео без дубликатов.</returns>
    private List<VideoInfo> RemoveDuplicates(List<VideoInfo> videos)
    {
        HashSet<string> seenIds = [];
        List<VideoInfo> uniqueVideos = [];
        var duplicateCount = 0;

        foreach (var video in videos)
        {
            if (string.IsNullOrWhiteSpace(video.Id) || seenIds.Add(video.Id))
            {
                uniqueVideos.Add(video);
            }
            else
            {
                duplicateCount++;
                logger.LogDebug("Удален дубликат видео: {Title} (ID: {Id})", video.Title, video.Id);
            }
        }

        if (duplicateCount > 0)
        {
            logger.LogInformation("Удалено {Count} дубликатов", duplicateCount);
        }

        return uniqueVideos;
    }

    // TODO: Файлы переимновываются, а data.json обновится позже
    // TODO: Не работает, если файлы не загружены, а статус в data.json не изменился
    /// <summary>
    /// Обновляет названия видео и переименовывает файлы, если название изменилось.
    /// </summary>
    /// <param name="videos">Список видео.</param>
    /// <param name="videosPath">Путь к директории с видеофайлами.</param>
    private async Task UpdateVideoTitlesAndRenameFilesAsync(List<VideoInfo> videos, string videosPath)
    {
        if (!Directory.Exists(videosPath))
        {
            return;
        }

        logger.LogInformation("Обнаружение изменений названий видео");

        var renamedCount = 0;

        for (var i = 0; i < videos.Count; i++)
        {
            var video = videos[i];

            if (video.State != VideoState.Downloaded)
            {
                continue;
            }

            try
            {
                var actualVideo = await youtubeService.GetVideoAsync(video.Url);
                var actualTitle = actualVideo.Title;

                if (!string.IsNullOrWhiteSpace(actualTitle) && !string.Equals(actualTitle, video.Title, StringComparison.Ordinal))
                {
                    logger.LogInformation("Обнаружено изменение названия видео: '{OldTitle}' -> '{NewTitle}'", video.FileName, actualTitle);

                    var renamed = RenameVideoFiles(videosPath, video.FileName, actualTitle.GetFileName());

                    if (renamed)
                    {
                        videos[i] = new()
                        {
                            Id = video.Id,
                            Title = actualVideo.Title,
                            State = video.State,
                        };

                        renamedCount++;
                        logger.LogInformation("Файлы успешно переименованы для видео: {Title}", actualVideo.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось обновить информацию о видео: {Title}", video.Title);
            }
        }

        logger.LogInformation("Переименовано файлов для {Count} видео", renamedCount);
    }

    /// <summary>
    /// Переименовывает все файлы, связанные с видео.
    /// </summary>
    /// <param name="videosPath">Путь к директории с видеофайлами.</param>
    /// <param name="oldFileName">Старое имя файла.</param>
    /// <param name="newFileName">Новое имя файла.</param>
    /// <returns>true, если переименование прошло успешно.</returns>
    private bool RenameVideoFiles(string videosPath, string oldFileName, string newFileName)
    {
        try
        {
            var allFiles = Directory.GetFiles(videosPath);
            var filesToRename = allFiles
                .Where(f => Path.GetFileName(f).StartsWith(oldFileName, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            foreach (var oldFilePath in filesToRename)
            {
                var fileName = Path.GetFileName(oldFilePath);
                var newFileNameFull = fileName.Replace(oldFileName, newFileName);
                var newFilePath = Path.Combine(videosPath, newFileNameFull);

                if (File.Exists(newFilePath))
                {
                    logger.LogWarning("Файл с именем {NewFile} уже существует, пропуск переименования", newFileNameFull);
                    continue;
                }

                File.Move(oldFilePath, newFilePath);
                logger.LogDebug("Переименован файл: {OldFile} -> {NewFile}", fileName, newFileNameFull);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при переименовании файлов: {OldFileName} -> {NewFileName}", oldFileName, newFileName);
            return false;
        }
    }

    /// <summary>
    /// Загружает видео, которые ещё не были скачаны.
    /// </summary>
    /// <param name="videos">Список видео для загрузки.</param>
    /// <param name="videosPath">Путь к директории для хранения видео.</param>
    private async Task DownloadPendingVideosAsync(List<VideoInfo> videos, string videosPath)
    {
        logger.LogInformation("Найдено {DownloadableVideoCount} видео для загрузки", videos.Count);

        var errorCount = 0;

        foreach (var video in videos)
        {
            logger.LogDebug("Загрузка видео: {VideoTitle}", video.Title);

            try
            {
                video.State = await helper.DownloadVideoAsync(video, videosPath);
                errorCount = 0;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка: {Message}", exception.Message);
                Thread.Sleep(5000 * errorCount);
                errorCount++;
            }
        }
    }

    /// <summary>
    /// Проверяет, все ли файлы загруженных видео присутствуют в директории, и обновляет статус видео.
    /// </summary>
    /// <param name="videos">Список видео.</param>
    /// <param name="videosPath">Путь к директории с видео.</param>
    private void ValidateVideoState(List<VideoInfo> videos, string videosPath)
    {
        foreach (var video in videos)
        {
            var videoFilePath = Path.Combine(videosPath, video.FileName);
            var allFiles = Directory.GetFiles(videosPath);

            var mainFileCount = allFiles.Count(x => x.Contains($"{video.FileName}.", StringComparison.InvariantCultureIgnoreCase));
            var infoFileCount = allFiles.Count(x => x.Contains($"{video.FileName}_", StringComparison.InvariantCultureIgnoreCase));

            var neededMainCount = 1;
            var neededInfoCount = 4;

            if (mainFileCount == neededMainCount && infoFileCount == neededInfoCount)
            {
                if (video.State != VideoState.Downloaded)
                {
                    logger.LogDebug("Видео '{Title}' имеет статус 'Не загружено', но все файлы найдены: {FilePath}", video.Title, videoFilePath);
                    video.State = VideoState.Downloaded;
                }
                else
                {
                    logger.LogTrace("Видео '{Title}' имеет валидный статус", video.Title);
                }
            }
            else
            {
                if (video.State == VideoState.Downloaded)
                {
                    logger.LogError("Видео '{Title}' имеет статус 'Загружено', но не найдены необходимые файлы: {FilePath}", video.Title, videoFilePath);
                    logger.LogDebug("Ожидаемое количество основных файлов: {NeededMainCount}, Найдено: {FoundMainCount}", neededMainCount, mainFileCount);
                    logger.LogDebug("Ожидаемое количество информационных файлов: {NeededInfoCount}, Найдено: {FoundInfoCount}", neededInfoCount, infoFileCount);
                    video.State = VideoState.Error;
                }
                else
                {
                    logger.LogTrace("Видео '{Title}' имеет валидный статус", video.Title);
                }
            }
        }
    }
}
