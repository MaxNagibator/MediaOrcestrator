using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.Youtube;

public class YoutubeChannel(ILogger<YoutubeChannel> logger, IToolPathProvider toolPathProvider)
    : ISourceType, IAuthenticatable, IToolConsumer, ISupportsComments
{
    internal const string VideoUrlTemplate = "https://www.youtube.com/watch?v={0}";

    private readonly YoutubeAuthService _authService = new(logger);
    private readonly YoutubeExplodeReadService _explodeService = new(logger);
    private readonly YoutubeYtDlpReadService _ytDlpReadService = new(logger);

    private YoutubeUploadService? _uploadService;
    private YoutubeApiReadService? _apiReadService;
    private YoutubeCommentsReadService? _commentsReadService;

    public IReadOnlyList<ToolDescriptor> RequiredTools { get; } =
    [
        WellKnownTools.YtDlpDescriptor,
        WellKnownTools.FFmpegDescriptor,
        WellKnownTools.DenoDescriptor,
    ];

    public SyncDirection ChannelType => SyncDirection.Full;

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
            Description = "JavaScript runtime для YouTube extraction (требуется для yt-dlp). Deno устанавливается и обновляется автоматически через панель инструментов",
        },
        new()
        {
            Key = "speed_limit",
            IsRequired = false,
            Title = "ограничение скорости скачивания (Мбит/с)",
            Description = "Максимальная скорость скачивания видео. Пустое значение — без ограничений",
        },
        new()
        {
            Key = "upload_speed_limit",
            IsRequired = false,
            Title = "ограничение скорости загрузки (Мбит/с)",
            Description = "Максимальная скорость загрузки видео на YouTube. Пустое значение — без ограничений",
        },
        new()
        {
            Key = "app_name",
            IsRequired = false,
            Title = "название приложения в Google Cloud",
            Description = "Должно совпадать с именем проекта в Google Cloud Console",
        },
        new()
        {
            Key = "client_id",
            IsRequired = false,
            Title = "OAuth Client ID",
            Description = "Client ID из Google Cloud Console (необходим для загрузки видео)",
        },
        new()
        {
            Key = "client_secret",
            IsRequired = false,
            Title = "OAuth Client Secret",
            Description = "Client Secret из Google Cloud Console (необходим для загрузки видео)",
        },
        new()
        {
            Key = "privacy_status",
            IsRequired = false,
            Title = "статус видео при загрузке",
            DefaultValue = "private",
            Type = SettingType.Dropdown,
            Options =
            [
                new() { Value = "private", Label = "Приватное" },
                new() { Value = "unlisted", Label = "По ссылке" },
                new() { Value = "public", Label = "Публичное" },
            ],
            Description = "Статус приватности видео при загрузке на YouTube",
        },
        new()
        {
            Key = "publish_at",
            IsRequired = false,
            Title = "отложенная публикация",
            Description = "Время публикации: +3 (через N часов) или 20:00 (время). Если задано, видео загружается как приватное",
        },
        new()
        {
            Key = "tags",
            IsRequired = false,
            Title = "теги видео",
            Description = "Теги через запятую (например: gaming, стрим, обзор)",
        },
        new()
        {
            Key = "category_id",
            IsRequired = false,
            Title = "категория видео",
            DefaultValue = "22",
            Type = SettingType.Dropdown,
            Description = "Категория видео на YouTube",
        },
        new()
        {
            Key = "region_code",
            IsRequired = false,
            Title = "код региона",
            DefaultValue = "RU",
            Description = "ISO 3166-1 alpha-2 код региона для YouTube API (влияет на доступные категории)",
        },
    ];

    private YoutubeUploadService UploadService => _uploadService ??= new(logger, _authService);
    private YoutubeApiReadService ApiReadService => _apiReadService ??= new(logger, _authService);
    private YoutubeCommentsReadService CommentsReadService => _commentsReadService ??= new(logger, _authService);

    public async Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        if (settingKey != "category_id")
        {
            return [];
        }

        if (YoutubeAuthService.IsConfigured(currentSettings))
        {
            try
            {
                using var service = await _authService.CreateServiceAsync(currentSettings, CancellationToken.None);

                var request = service.VideoCategories.List("snippet");
                request.RegionCode = currentSettings.GetValueOrDefault("region_code", "RU");

                var response = await request.ExecuteAsync();

                return response.Items?
                           .Where(c => c.Snippet.Assignable == true)
                           .Select(c => new SettingOption { Value = c.Id, Label = c.Snippet.Title })
                           .ToList()
                       ?? FallbackCategories();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось получить категории через API, используются встроенные");
            }
        }

        return FallbackCategories();
    }

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
    {
        return new(string.Format(VideoUrlTemplate, externalId));
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channelUrl = settings["channel_id"];
        logger.LogInformation("Получение списка медиа для канала: {ChannelUrl}", channelUrl);

        string? channelId = null;

        try
        {
            channelId = await _explodeService.GetChannelIdAsync(channelUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "YoutubeExplode не смог получить канал, пробую YouTube API fallback");
        }

        if (channelId is not null)
        {
            await foreach (var media in _explodeService.GetMediaAsync(channelId, isFull, cancellationToken))
            {
                yield return media;
            }

            yield break;
        }

        if (!YoutubeAuthService.IsConfigured(settings))
        {
            logger.LogError("YoutubeExplode не работает, а OAuth не настроен. Укажите client_id и client_secret для fallback через YouTube API");
            yield break;
        }

        var apiChannel = await ApiReadService.GetChannelAsync(channelUrl, settings, cancellationToken);

        if (apiChannel is null)
        {
            logger.LogError("YouTube API: канал не найден: {ChannelUrl}", channelUrl);
            yield break;
        }

        await foreach (var media in ApiReadService.GetMediaAsync(apiChannel.Id, isFull, settings, cancellationToken))
        {
            yield return media;
        }

        logger.LogInformation("Завершено получение медиа для канала: {ChannelUrl}", channelUrl);
    }

    public IAsyncEnumerable<CommentDto> GetCommentsAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        return CommentsReadService.GetCommentsAsync(externalId, settings, cancellationToken);
    }

    public async Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _explodeService.GetVideoByIdAsync(externalId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "YoutubeExplode не смог получить видео {VideoId}, пробую YouTube API fallback", externalId);
        }

        if (YoutubeAuthService.IsConfigured(settings))
        {
            try
            {
                var apiResult = await ApiReadService.GetVideoByIdAsync(externalId, settings, cancellationToken);

                if (apiResult is not null)
                {
                    return apiResult;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "YouTube API не смог получить видео {VideoId}, пробую yt-dlp fallback", externalId);
            }
        }
        else
        {
            logger.LogInformation("YouTube API не настроен, пробую yt-dlp fallback для видео {VideoId}", externalId);
        }

        try
        {
            var ytDlp = BuildYtDlp(settings);
            return await _ytDlpReadService.GetVideoByIdAsync(externalId, ytDlp, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "yt-dlp не смог получить метаданные видео {VideoId}", externalId);
        }

        return null;
    }

    public async Task<MediaDto> DownloadAsync(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Начало загрузки видео с YouTube. ID: {VideoId}", videoId);

        var tempPath = settings["_system_temp_path"];
        var guid = Guid.NewGuid().ToString();
        var finalPath = Path.Combine(tempPath, guid, "media.mp4");

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        logger.LogDebug("Создана временная директория: {TempPath}", Path.GetDirectoryName(finalPath));

        var ytDlp = BuildYtDlp(settings);

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

        YtDlpVideoInfo? info;

        try
        {
            var rateLimitBytes = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);
            info = await ytDlp.DownloadAsync(string.Format(VideoUrlTemplate, videoId), finalPath, progress, rateLimitBytes, cancellationToken);
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

        MediaDto media;

        if (info is not null)
        {
            media = _ytDlpReadService.BuildMediaDto(videoId, info);
            logger.LogDebug("Метаданные видео получены из info.json yt-dlp. Название: '{Title}'", media.Title);
        }
        else
        {
            logger.LogWarning("yt-dlp не сохранил info.json, запрашиваю метаданные отдельным вызовом");

            media = await GetMediaByIdAsync(videoId, settings, cancellationToken)
                    ?? throw new InvalidOperationException($"Видео не найдено: {videoId}");
        }

        media.TempDataPath = finalPath;

        var thumbnailPath = Path.ChangeExtension(finalPath, ".jpg");
        if (File.Exists(thumbnailPath))
        {
            media.TempPreviewPath = thumbnailPath;
            logger.LogInformation("Превью сохранено через yt-dlp: {ThumbnailPath}", thumbnailPath);
        }
        else
        {
            logger.LogDebug("yt-dlp не сохранил превью рядом с видео: {ThumbnailPath}", thumbnailPath);
        }

        return media;
    }

    public Task<UploadResult> UploadAsync(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        return UploadService.UploadVideoAsync(media, settings, cancellationToken);
    }

    public Task<UploadResult> UpdateAsync(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        return UploadService.UpdateVideoAsync(externalId, tempMedia, settings, cancellationToken);
    }

    public async Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Удаление видео с YouTube. ID: {ExternalId}", externalId);

        using var service = await _authService.CreateServiceAsync(settings, cancellationToken);
        await service.Videos.Delete(externalId).ExecuteAsync(cancellationToken);

        logger.LogInformation("Видео {ExternalId} удалено с YouTube", externalId);
    }

    public bool IsAuthenticated(Dictionary<string, string> settings)
    {
        var hasCookies = HasValidCookieFile(settings);
        var hasOAuth = YoutubeAuthService.HasCachedToken(settings);

        return hasCookies || hasOAuth;
    }

    public async Task AuthenticateAsync(Dictionary<string, string> settings, IAuthUI ui, CancellationToken ct)
    {
        var authStatePath = GetAuthStatePath(settings);
        var tempJsonPath = authStatePath + ".tmp.json";

        try
        {
            var result = await ui.OpenBrowserAsync("https://studio.youtube.com/", tempJsonPath);
            if (result == null)
            {
                return;
            }

            ConvertPlaywrightToNetscape(tempJsonPath, authStatePath);
            logger.LogInformation("YouTube: авторизация сохранена в {Path}", authStatePath);
            await ui.ShowMessageAsync("Авторизация YouTube сохранена!");
        }
        finally
        {
            if (File.Exists(tempJsonPath))
            {
                File.Delete(tempJsonPath);
            }
        }
    }

    private static List<SettingOption> FallbackCategories()
    {
        return
        [
            new() { Value = "1", Label = "Кино и анимация" },
            new() { Value = "2", Label = "Авто и транспорт" },
            new() { Value = "10", Label = "Музыка" },
            new() { Value = "15", Label = "Животные" },
            new() { Value = "17", Label = "Спорт" },
            new() { Value = "20", Label = "Игры" },
            new() { Value = "22", Label = "Люди и блоги" },
            new() { Value = "23", Label = "Комедия" },
            new() { Value = "24", Label = "Развлечения" },
            new() { Value = "25", Label = "Новости и политика" },
            new() { Value = "26", Label = "Хобби и стиль" },
            new() { Value = "27", Label = "Образование" },
            new() { Value = "28", Label = "Наука и технологии" },
        ];
    }

    private static bool HasValidCookieFile(Dictionary<string, string> settings)
    {
        var authStatePath = GetAuthStatePath(settings);
        if (!File.Exists(authStatePath))
        {
            return false;
        }

        try
        {
            var lines = File.ReadLines(authStatePath);
            return lines.Any(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
        }
        catch
        {
            return false;
        }
    }

    private static string GetAuthStatePath(Dictionary<string, string> settings)
    {
        return Path.Combine(settings["_system_state_path"], "auth_state");
    }

    private static void ConvertPlaywrightToNetscape(string playwrightJsonPath, string netscapePath)
    {
        var json = File.ReadAllText(playwrightJsonPath);
        using var doc = JsonDocument.Parse(json);
        var cookies = doc.RootElement.GetProperty("cookies");

        using var writer = new StreamWriter(netscapePath, false, new UTF8Encoding(false));
        writer.WriteLine("# Netscape HTTP Cookie File");

        foreach (var cookie in cookies.EnumerateArray())
        {
            var domain = cookie.GetProperty("domain").GetString() ?? "";
            if (string.IsNullOrEmpty(domain))
            {
                continue;
            }

            if (!domain.StartsWith('.'))
            {
                domain = "." + domain;
            }

            var flag = "TRUE";

            var path = cookie.GetProperty("path").GetString() ?? "/";
            var secure = cookie.GetProperty("secure").GetBoolean() ? "TRUE" : "FALSE";
            var expires = cookie.GetProperty("expires").GetDouble();
            var expiry = expires > 0 ? (long)expires : 0;
            var name = cookie.GetProperty("name").GetString() ?? "";
            var value = cookie.GetProperty("value").GetString() ?? "";

            writer.WriteLine($"{domain}\t{flag}\t{path}\t{secure}\t{expiry}\t{name}\t{value}");
        }
    }

    private YtDlp BuildYtDlp(Dictionary<string, string> settings)
    {
        var ytDlpPath = toolPathProvider.GetToolPath(WellKnownTools.YtDlp)
                        ?? throw new InvalidOperationException("yt-dlp не установлен. Установите через панель управления инструментами.");

        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg)
                         ?? throw new InvalidOperationException("ffmpeg не установлен. Установите через панель управления инструментами.");

        var jsRuntime = settings.GetValueOrDefault("js_runtime", "none");
        var denoPath = toolPathProvider.GetToolPath(WellKnownTools.Deno);
        var jsRuntimeDir = jsRuntime is "deno" && denoPath is not null
            ? Path.GetDirectoryName(denoPath)
            : null;

        var cookiePath = GetAuthStatePath(settings);
        if (!File.Exists(cookiePath))
        {
            cookiePath = "";
        }

        return new(ytDlpPath, ffmpegPath, jsRuntime, jsRuntimeDir, cookiePath);
    }
}
