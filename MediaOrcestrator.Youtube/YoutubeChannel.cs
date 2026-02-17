using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Videos.Streams;

namespace MediaOrcestrator.Youtube;

public class YoutubeChannel(ILogger<YoutubeChannel> logger) : ISourceType
{
    private readonly Func<YoutubeClient, string, Task<Channel?>>[] _parsers =
    [
        async (youtubeClient, url) => ChannelId.TryParse(url) is { } id ? await youtubeClient.Channels.GetAsync(id) : null,
        async (youtubeClient, url) => ChannelSlug.TryParse(url) is { } slug ? await youtubeClient.Channels.GetBySlugAsync(slug) : null,
        async (youtubeClient, url) => ChannelHandle.TryParse(url) is { } handle ? await youtubeClient.Channels.GetByHandleAsync(handle) : null,
        async (youtubeClient, url) => UserName.TryParse(url) is { } userName ? await youtubeClient.Channels.GetByUserAsync(userName) : null,
    ];

    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "Youtube";

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "channel_id",
            IsRequired = true,
            Title = "идентификатор канала",
            Description = "URL или ID канала YouTube (например: https://www.youtube.com/@channelname или UCxxxxxxxxx)",
        },
        new()
        {
            Key = "temp_path",
            IsRequired = true,
            Title = "путь к временной папке для загрузки",
            DefaultValue = @"E:\bobgroup\projects\mediaOrcestrator\tempDir",
            Description = "Папка для временного хранения загружаемых видео",
        },
        new()
        {
            Key = "yt_dlp_path",
            IsRequired = true,
            Title = "путь к исполняемому файлу yt-dlp",
            DefaultValue = @"c:\Services\utils\yt-dlp.exe",
            Description = "Скачать можно с https://github.com/yt-dlp/yt-dlp/releases",
        },
        new()
        {
            Key = "ffmpeg_path",
            IsRequired = true,
            Title = "путь к исполняемому файлу ffmpeg",
            DefaultValue = @"c:\Services\utils\ffmpeg\ffmpeg.exe",
            Description = "Скачать можно с https://ffmpeg.org/download.html",
        },
    ];

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings)
    {
        //var channelUrl = "https://www.youtube.com/@bobito217";

        var channelUrl = settings["channel_id"];
        logger.LogInformation("Получение списка медиа для канала: {ChannelUrl}", channelUrl);

        using var youtubeClient = new YoutubeClient();
        var channel = await GetChannel(youtubeClient, channelUrl);

        if (channel == null)
        {
            logger.LogWarning("Канал не найден: {ChannelUrl}", channelUrl);
            yield break;
        }

        logger.LogDebug("Канал найден. Название: '{ChannelTitle}', ID: {ChannelId}", channel.Title, channel.Id);

        var uploads = youtubeClient.Channels.GetUploadsAsync(channel.Id);

        await foreach (var video in uploads)
        {
            logger.LogDebug("Обработка видео: '{VideoTitle}' (ID: {VideoId})", video.Title, video.Id);

            yield return new()
            {
                Id = video.Id.Value,
                Title = video.Title,
                DataPath = video.Url,
                PreviewPath = video.Thumbnails.Count > 0 ? video.Thumbnails[0].Url : string.Empty,
            };
        }

        logger.LogInformation("Завершено получение медиа для канала: {ChannelUrl}", channelUrl);
    }

    public async Task<Channel?> GetChannel(YoutubeClient client, string channelUrl)
    {
        logger.LogDebug("Попытка определить канал по URL: {ChannelUrl}", channelUrl);

        foreach (var parser in _parsers)
        {
            var channel = await parser(client, channelUrl);

            if (channel != null)
            {
                logger.LogDebug("Канал успешно определён: '{ChannelTitle}' (ID: {ChannelId})", channel.Title, channel.Id);
                return channel;
            }
        }

        logger.LogWarning("Не удалось определить канал по URL: {ChannelUrl}", channelUrl);
        return null;
    }

    public MediaDto GetMediaById()
    {
        throw new NotImplementedException();
    }

    public async Task<MediaDto> Download(string videoId, Dictionary<string, string> settings)
    {
        logger.LogInformation("Начало загрузки видео с YouTube. ID: {VideoId}", videoId);

        using var youtubeClient = new YoutubeClient();

        var video = await youtubeClient.Videos.GetAsync(videoId);
        logger.LogDebug("Получена информация о видео. Название: '{Title}', Длительность: {Duration}",
            video.Title, video.Duration);

        var tempPath = settings["temp_path"];
        var guid = Guid.NewGuid().ToString();
        var finalPath = Path.Combine(tempPath, guid, "media.mp4");

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        logger.LogDebug("Создана временная директория: {TempPath}", Path.GetDirectoryName(finalPath));

        var ytDlpPath = settings["yt_dlp_path"];
        var ffmpegPath = settings["ffmpeg_path"];
        var ytDlp = new YtDlp(ytDlpPath, ffmpegPath);

        object progressLock = new();
        double oldPercent = -1;
        var currentPart = 0;

        // TODO: Подумать
        Progress<YtDlpProgress> progress = new(p =>
        {
            lock (progressLock)
            {
                if (p.PartNumber != currentPart)
                {
                    currentPart = p.PartNumber;
                    oldPercent = -1;
                    logger.LogInformation("Загрузка части #{PartNumber} из {TotalParts}", currentPart, p.PartNumber);
                }

                if (Math.Abs(p.Progress - oldPercent) < double.Epsilon)
                {
                    return;
                }

                var isSignificantChange = Math.Abs(p.Progress - oldPercent) >= 0.1;
                var isCompletion = p.Progress >= 1.0;
                var isStart = oldPercent < 0;

                if (!isSignificantChange && !isCompletion && !isStart)
                {
                    return;
                }

                logger.LogInformation("Прогресс загрузки [Часть {PartNumber}]: {Percent:P0}", p.PartNumber, p.Progress);
                oldPercent = p.Progress;
            }
        });

        logger.LogInformation("Запуск загрузки через yt-dlp. URL: https://www.youtube.com/watch?v={VideoId}", videoId);

        try
        {
            await ytDlp.DownloadAsync($"https://www.youtube.com/watch?v={videoId}", finalPath, progress);
            logger.LogInformation("Видео успешно загружено. ID: {VideoId}, Путь: {FilePath}", videoId, finalPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке видео через yt-dlp. ID: {VideoId}", videoId);
            throw;
        }

        return new()
        {
            Id = videoId,
            Title = video.Title,
            Description = video.Description,
            TempDataPath = finalPath,
        };
        // bob217 -> 9I_JIereHga -> bob217
        //  logger.LogDebug("Успешно объединено видео и аудио: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);
    }

    public ValueTask DownloadWithProgressAsync(YoutubeClient youtubeClient, IStreamInfo streamInfo, string path, CancellationToken cancellationToken)
    {
        double oldPercent = -1;

        var streamType = streamInfo switch
        {
            AudioOnlyStreamInfo => "Аудио",
            VideoOnlyStreamInfo => "Видео",
            MuxedStreamInfo => "Объединённый",
            _ => "Неизвестный",
        };

        Progress<double> progress = new(percent =>
        {
            if (percent - oldPercent < 10)
            {
                return;
            }

            logger.LogDebug("Загрузка потока [{StreamType}]: {Percent:P2}", streamType, percent);
            oldPercent = percent;
        });

        return youtubeClient.Videos.Streams.DownloadAsync(streamInfo, path, progress, cancellationToken);
    }

    public Task<string> Upload(MediaDto media, Dictionary<string, string> settings)
    {
        logger.LogWarning("Загрузка на YouTube не реализована. Медиа: {Title}", media.Title);
        return Task.FromResult("not_implemented");
    }

    public Task DeleteAsync(string externalId, Dictionary<string, string> settings)
    {
        logger.LogWarning("Удаление из YouTube не поддерживается. Нужно подключать апю ютуба. Media ID: {ExternalId}", externalId);
        throw new NotSupportedException(@"Удалите видео вручную через веб-интерфейс YouTube Studio. (\/)._.(\/)");
    }
}
