using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace MediaOrcestrator.Youtube;

public class YoutubeChannel(ILogger<YoutubeChannel> logger, IToolPathProvider toolPathProvider) : ISourceType, IToolConsumer, ILegacyToolPathProvider
{
    private readonly Func<YoutubeClient, string, Task<Channel?>>[] _parsers =
    [
        async (youtubeClient, url) => ChannelId.TryParse(url) is { } id ? await youtubeClient.Channels.GetAsync(id) : null,
        async (youtubeClient, url) => ChannelSlug.TryParse(url) is { } slug ? await youtubeClient.Channels.GetBySlugAsync(slug) : null,
        async (youtubeClient, url) => ChannelHandle.TryParse(url) is { } handle ? await youtubeClient.Channels.GetByHandleAsync(handle) : null,
        async (youtubeClient, url) => UserName.TryParse(url) is { } userName ? await youtubeClient.Channels.GetByUserAsync(userName) : null,
    ];

    private static readonly Dictionary<string, string> LegacySettingDefaults = new()
    {
        ["yt_dlp_path"] = @"c:\Services\utils\yt-dlp.exe",
        ["ffmpeg_path"] = @"c:\Services\utils\ffmpeg\ffmpeg.exe",
    };

    public IReadOnlyList<ToolDescriptor> RequiredTools =>
    [
        new()
        {
            Name = WellKnownTools.YtDlp,
            GitHubRepo = "yt-dlp/yt-dlp",
            AssetPattern = "yt-dlp.exe",
            VersionCommand = "--version",
            ArchiveType = ArchiveType.None,
        },
        new()
        {
            Name = WellKnownTools.FFmpeg,
            GitHubRepo = "BtbN/FFmpeg-Builds",
            AssetPattern = "ffmpeg-N-*-win64-gpl.zip",
            VersionCommand = "-version",
            VersionPattern = @"ffmpeg version N-\d+-\w+-(\d{8})",
            VersionTagPattern = @"autobuild-(\d{4}-\d{2}-\d{2})",
            ArchiveType = ArchiveType.Zip,
            ArchiveExecutablePath = "ffmpeg-*/bin/ffmpeg.exe",
        },
    ];

    public string? GetLegacyToolPath(string toolName)
    {
        var key = toolName switch
        {
            WellKnownTools.YtDlp => "yt_dlp_path",
            WellKnownTools.FFmpeg => "ffmpeg_path",
            _ => null,
        };

        if (key is null)
        {
            return null;
        }

        var legacyDefault = LegacySettingDefaults.GetValueOrDefault(key);
        return legacyDefault is not null && File.Exists(legacyDefault) ? legacyDefault : null;
    }

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
            Key = "js_runtime",
            IsRequired = true,
            Title = "вариант JS runtime",
            DefaultValue = "none",
            Type = SettingType.Dropdown,
            Options =
            [
                new() { Value = "none", Label = "none" },
                new() { Value = "deno", Label = "deno" },
                new() { Value = "node", Label = "node" },
                new() { Value = "quickjs", Label = "quickjs" },
            ],
            Description = "JavaScript runtime для YouTube extraction (требуется для yt-dlp)",
        },
        new()
        {
            Key = "auth_state_path",
            IsRequired = true,
            Title = "путь до фаила куки",
            Description = "JSON файл с cookies и CSRF токеном для авторизации на Youtube (для 18+ видео)",
        },
    ];

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channelUrl = settings["channel_id"];
        logger.LogInformation("Получение списка медиа для канала: {ChannelUrl}", channelUrl);

        using var youtubeClient = new YoutubeClient();

        // Retry для получения канала
        var channel = await RetryAsync(async () => await GetChannel(youtubeClient, channelUrl), 5, 500, cancellationToken);

        if (channel == null)
        {
            logger.LogWarning("Канал не найден: {ChannelUrl}", channelUrl);
            yield break;
        }

        logger.LogDebug("Канал найден. Название: '{ChannelTitle}', ID: {ChannelId}", channel.Title, channel.Id);

        var uploads = youtubeClient.Channels.GetUploadsAsync(channel.Id, cancellationToken);

        await foreach (var video in uploads)
        {
            logger.LogDebug("Обработка видео: '{VideoTitle}' (ID: {VideoId})", video.Title, video.Id);

            if (isFull)
            {
                // Retry для получения полной информации о видео
                var fullVideo = await RetryAsync(async () => await youtubeClient.Videos.GetAsync(video.Id, cancellationToken), 5, 500, cancellationToken);
                yield return CreateMediaDto(fullVideo);
            }
            else
            {
                // todo есть некоторое дублирование
                var thumbnail = video.Thumbnails.TryGetWithHighestResolution();
                var previewPath = thumbnail?.Url ?? string.Empty;

                var metadata = new List<MetadataItem>
            {
                new()
                {
                    Key = "Duration",
                    DisplayName = "Длительность",
                    Value = video.Duration?.ToString() ?? "",
                    DisplayType = "System.TimeSpan",
                },
                new()
                {
                    Key = "Author",
                    DisplayName = "Автор",
                    Value = video.Author.ChannelTitle,
                    DisplayType = "System.String",
                },
            };

                yield return new MediaDto()
                {
                    Id = video.Id.Value,
                    Title = video.Title,
                    DataPath = video.Url,
                    PreviewPath = previewPath,
                    Metadata = metadata,
                };
            }
        }

        logger.LogInformation("Завершено получение медиа для канала: {ChannelUrl}", channelUrl);
    }

    // Вспомогательный метод для retry логики
    private async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxRetries, int delayMs, CancellationToken cancellationToken)
    {
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                logger.LogWarning(ex, "Попытка {RetryCount}/{MaxRetries} не удалась. Повтор через {DelayMs}мс", retryCount, maxRetries, delayMs);

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        // Последняя попытка (если все предыдущие провалились)
        return await action();
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

    public async Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        try
        {
            using var youtubeClient = new YoutubeClient();
            var video = await youtubeClient.Videos.GetAsync(externalId, cancellationToken);
            return CreateMediaDto(video);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить YouTube по ID: {VideoId}", externalId);
            return null;
        }
    }

    public async Task<MediaDto> Download(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Начало загрузки видео с YouTube. ID: {VideoId}", videoId);

        using var youtubeClient = new YoutubeClient();

        var video = await youtubeClient.Videos.GetAsync(videoId, cancellationToken);
        logger.LogDebug("Получена информация о видео. Название: '{Title}', Длительность: {Duration}",
            video.Title, video.Duration);

        var tempPath = settings["temp_path"];
        var guid = Guid.NewGuid().ToString();
        var finalPath = Path.Combine(tempPath, guid, "media.mp4");

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        logger.LogDebug("Создана временная директория: {TempPath}", Path.GetDirectoryName(finalPath));

        var ytDlpPath = toolPathProvider.GetToolPath(WellKnownTools.YtDlp)
            ?? throw new InvalidOperationException("yt-dlp не установлен. Установите через панель управления инструментами.");

        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg)
            ?? throw new InvalidOperationException("ffmpeg не установлен. Установите через панель управления инструментами.");

        var jsRuntime = settings.GetValueOrDefault("js_runtime", "none");
        var cookiePath = settings.GetValueOrDefault("auth_state_path","");
        var ytDlp = new YtDlp(ytDlpPath, ffmpegPath, jsRuntime, cookiePath);

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
            await ytDlp.DownloadAsync($"https://www.youtube.com/watch?v={videoId}", finalPath, progress, cancellationToken);
            logger.LogInformation("Видео успешно загружено. ID: {VideoId}, Путь: {FilePath}", videoId, finalPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке видео через yt-dlp. ID: {VideoId}", videoId);
            throw;
        }

        return CreateMediaDto(video, finalPath);
    }

    public Task<UploadResult> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Загрузка на YouTube не реализована. Медиа: {Title}", media.Title);

        return Task.FromResult(new UploadResult
        {
            Status = MediaStatusHelper.GetById(MediaStatus.Error),
            Message = media.Title,
        });
    }

    public Task<UploadResult> Update(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Удаление из YouTube не поддерживается. Нужно подключать апю ютуба. Media ID: {ExternalId}", externalId);
        throw new NotSupportedException(@"Удалите видео вручную через веб-интерфейс YouTube Studio. (\/)._.(\/)");
    }

    private static MediaDto CreateMediaDto(Video video, string? tempDataPath = null)
    {
        var thumbnail = video.Thumbnails.TryGetWithHighestResolution();
        var previewPath = thumbnail?.Url ?? string.Empty;

        var metadata = new List<MetadataItem>
        {
            new()
            {
                Key = "Duration",
                DisplayName = "Длительность",
                Value = video.Duration?.ToString() ?? "",
                DisplayType = "System.TimeSpan",
            },
            new()
            {
                Key = "Author",
                DisplayName = "Автор",
                Value = video.Author.ChannelTitle,
                DisplayType = "System.String",
            },
            new()
            {
                Key = "CreationDate",
                DisplayName = "Дата создания",
                Value = video.UploadDate.ToString("O"),
                DisplayType = "System.DateTime",
            },
            new()
            {
                Key = "Views",
                DisplayName = "Просмотры",
                Value = video.Engagement.ViewCount.ToString(),
                DisplayType = "System.Int64",
            },
        };

        return new()
        {
            Id = video.Id.Value,
            Title = video.Title,
            DataPath = video.Url,
            PreviewPath = previewPath,
            Metadata = metadata,
            TempDataPath = tempDataPath,
        };
    }
}
