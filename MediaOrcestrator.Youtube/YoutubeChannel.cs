using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeChannel(
    ILogger<YoutubeChannel> logger,
    IYoutubeServiceFactory youtubeServiceFactory,
    IToolPathProvider toolPathProvider,
    YoutubeExplodeReadService explodeService,
    YoutubeYtDlpReadService ytDlpReadService,
    YoutubeApiReadService apiReadService,
    YoutubeCommentsReadService commentsReadService,
    YoutubeUploadService uploadService)
    : ISourceType, IAuthenticatable, IToolConsumer, ISupportsComments
{
    internal const string VideoUrlTemplate = "https://www.youtube.com/watch?v={0}";

    private static readonly string[] OAuthScopes =
    [
        YouTubeService.Scope.YoutubeUpload,
        YouTubeService.Scope.Youtube,
        YouTubeService.Scope.YoutubeForceSsl,
    ];

    private readonly ConcurrentDictionary<string, CachedEntry> _serviceCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

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

    public async Task<List<SettingOption>> GetSettingOptionsAsync(
        string settingKey,
        Dictionary<string, string> currentSettings)
    {
        if (settingKey != "category_id")
        {
            return [];
        }

        if (IsOAuthConfigured(currentSettings))
        {
            try
            {
                using var lease = await AcquireServiceLeaseAsync(currentSettings, CancellationToken.None);

                var request = lease.Service.VideoCategories.List("snippet");
                request.RegionCode = currentSettings.GetValueOrDefault("region_code", "RU");

                var response = await request.ExecuteAsync();

                return response.Items?
                           .Where(c => c.Snippet.Assignable == true)
                           .Select(c => new SettingOption { Value = c.Id, Label = c.Snippet.Title })
                           .ToList()
                       ?? FallbackCategories();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.CategoriesFallback(ex);
            }
        }

        return FallbackCategories();
    }

    public Uri? GetExternalUri(
        string externalId,
        Dictionary<string, string> settings)
    {
        return new(string.Format(VideoUrlTemplate, externalId));
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(
        Dictionary<string, string> settings,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channelUrl = settings["channel_id"];
        logger.StartingChannelFetch(channelUrl);

        string? channelId = null;

        try
        {
            channelId = await explodeService.GetChannelIdAsync(channelUrl, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.ExplodeChannelFallback(ex);
        }

        if (channelId is not null)
        {
            await foreach (var media in explodeService.GetMediaAsync(channelId, isFull, cancellationToken))
            {
                yield return media;
            }

            yield break;
        }

        if (!IsOAuthConfigured(settings))
        {
            logger.OAuthNotConfigured();
            yield break;
        }

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);

        var apiChannel = await apiReadService.GetChannelAsync(lease.Service, channelUrl, cancellationToken);

        if (apiChannel is null)
        {
            logger.ApiChannelNotFound(channelUrl);
            yield break;
        }

        await foreach (var media in apiReadService.GetMediaAsync(lease.Service, apiChannel.Id, isFull, cancellationToken))
        {
            yield return media;
        }

        logger.ChannelFetchCompleted(channelUrl);
    }

    public async IAsyncEnumerable<CommentDto> GetCommentsAsync(
        string externalId,
        Dictionary<string, string> settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsOAuthConfigured(settings))
        {
            throw new InvalidOperationException("Для получения комментариев YouTube необходимо настроить OAuth (client_id и client_secret)");
        }

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);

        await foreach (var comment in commentsReadService.GetCommentsAsync(lease.Service, externalId, cancellationToken))
        {
            yield return comment;
        }
    }

    public async Task<MediaDto?> GetMediaByIdAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await explodeService.GetVideoByIdAsync(externalId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.ExplodeVideoFallback(externalId, ex);
        }

        if (IsOAuthConfigured(settings))
        {
            try
            {
                using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
                var apiResult = await apiReadService.GetVideoByIdAsync(lease.Service, externalId, cancellationToken);

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
                logger.ApiVideoFallback(externalId, ex);
            }
        }
        else
        {
            logger.ApiNotConfiguredFallback(externalId);
        }

        try
        {
            var ytDlp = BuildYtDlp(settings);
            return await ytDlpReadService.GetVideoByIdAsync(externalId, ytDlp, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.YtDlpMetadataFailed(externalId, ex);
        }

        return null;
    }

    public async Task<MediaDto> DownloadAsync(
        string videoId,
        Dictionary<string, string> settings,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.DownloadStart(videoId);

        var tempPath = settings["_system_temp_path"];
        var guid = Guid.NewGuid().ToString();
        var finalPath = Path.Combine(tempPath, guid, "media.mp4");

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        logger.TempDirCreated(Path.GetDirectoryName(finalPath));

        var ytDlp = BuildYtDlp(settings);

        object progressLock = new();
        double oldPercent = -1;
        var currentPart = 0;

        // TODO: Подумать
        Progress<YtDlpProgress> ytDlpProgress = new(p =>
        {
            lock (progressLock)
            {
                if (p.PartNumber != currentPart)
                {
                    currentPart = p.PartNumber;
                    oldPercent = -1;
                    logger.DownloadPartStarted(currentPart);
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

                logger.DownloadProgress(p.PartNumber, p.Progress);
                oldPercent = p.Progress;
                progress?.Report(new(p.Progress * 100));
            }
        });

        logger.StartingYtDlpDownload(videoId);

        YtDlpVideoInfo? info;

        try
        {
            var rateLimitBytes = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);
            info = await ytDlp.DownloadAsync(string.Format(VideoUrlTemplate, videoId), finalPath, ytDlpProgress, rateLimitBytes, cancellationToken);
            logger.DownloadCompleted(videoId, finalPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.DownloadFailed(videoId, ex);
            throw;
        }

        MediaDto media;

        if (info is not null)
        {
            media = ytDlpReadService.BuildMediaDto(videoId, info);
            logger.InfoJsonMetadataParsed(media.Title);
        }
        else
        {
            logger.InfoJsonMissing();

            media = await GetMediaByIdAsync(videoId, settings, cancellationToken)
                    ?? throw new InvalidOperationException($"Видео не найдено: {videoId}");
        }

        media.TempDataPath = finalPath;

        var thumbnailPath = Path.ChangeExtension(finalPath, ".jpg");
        if (File.Exists(thumbnailPath))
        {
            media.TempPreviewPath = thumbnailPath;
            logger.YtDlpThumbnailSaved(thumbnailPath);
        }
        else
        {
            logger.YtDlpThumbnailMissing(thumbnailPath);
        }

        return media;
    }

    public async Task<UploadResult> UploadAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        return await uploadService.UploadVideoAsync(lease.Service, media, settings, progress, cancellationToken);
    }

    public async Task<UploadResult> UpdateAsync(
        string externalId,
        MediaDto tempMedia,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        return await uploadService.UpdateVideoAsync(lease.Service, externalId, tempMedia, settings, cancellationToken);
    }

    public async Task DeleteAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.DeleteStart(externalId);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        await lease.Service.Videos.Delete(externalId).ExecuteAsync(cancellationToken);

        logger.DeleteCompleted(externalId);
    }

    public bool IsAuthenticated(Dictionary<string, string> settings)
    {
        return HasValidCookieFile(settings) || HasCachedOAuthToken(settings);
    }

    public async Task AuthenticateAsync(
        Dictionary<string, string> settings,
        IAuthUI ui,
        CancellationToken ct)
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

            await ConvertPlaywrightToNetscapeAsync(tempJsonPath, authStatePath, ct);
            logger.AuthSaved(authStatePath);
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

    private static bool IsOAuthConfigured(Dictionary<string, string> settings)
    {
        return !string.IsNullOrEmpty(settings.GetValueOrDefault("client_id"))
               && !string.IsNullOrEmpty(settings.GetValueOrDefault("client_secret"))
               && !string.IsNullOrEmpty(settings.GetValueOrDefault("_system_state_path"));
    }

    private static bool HasCachedOAuthToken(Dictionary<string, string> settings)
    {
        if (!IsOAuthConfigured(settings))
        {
            return false;
        }

        var tokenDir = GetTokenDir(settings);

        return Directory.Exists(tokenDir)
               && Directory.EnumerateFiles(tokenDir, "Google.Apis.Auth.OAuth2.Responses.*").Any();
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

    private static string GetTokenDir(Dictionary<string, string> settings)
    {
        return Path.Combine(settings["_system_state_path"], "oauth");
    }

    private static DateTime GetTokenDirMtime(string tokenDir)
    {
        return Directory.Exists(tokenDir)
            ? Directory.GetLastWriteTimeUtc(tokenDir)
            : DateTime.MinValue;
    }

    private static async Task ConvertPlaywrightToNetscapeAsync(
        string playwrightJsonPath,
        string netscapePath,
        CancellationToken cancellationToken)
    {
        PlaywrightAuthState? authState;

        try
        {
            await using var jsonStream = File.OpenRead(playwrightJsonPath);
            authState = await JsonSerializer.DeserializeAsync(jsonStream, YoutubeJsonContext.Default.PlaywrightAuthState, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Файл cookies от браузера повреждён или не дочитан: {playwrightJsonPath}", ex);
        }

        await using var output = File.Create(netscapePath);
        await using var writer = new StreamWriter(output, new UTF8Encoding(false));
        await writer.WriteLineAsync("# Netscape HTTP Cookie File");

        if (authState?.Cookies is null)
        {
            return;
        }

        foreach (var cookie in authState.Cookies)
        {
            var domain = cookie.Domain ?? "";
            if (string.IsNullOrEmpty(domain))
            {
                continue;
            }

            if (!domain.StartsWith('.'))
            {
                domain = "." + domain;
            }

            var flag = "TRUE";
            var path = string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path;
            var secure = cookie.Secure ? "TRUE" : "FALSE";
            var expiry = cookie.Expires > 0 ? (long)cookie.Expires : 0;
            var name = cookie.Name ?? "";
            var value = cookie.Value ?? "";

            await writer.WriteLineAsync($"{domain}\t{flag}\t{path}\t{secure}\t{expiry}\t{name}\t{value}");
        }
    }

    private async Task<ServiceLease> AcquireServiceLeaseAsync(
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var entry = await GetOrCreateCachedAsync(settings, cancellationToken);
            if (entry.TryAcquire())
            {
                return new(entry);
            }

            await Task.Yield();
        }
    }

    private async Task<CachedEntry> GetOrCreateCachedAsync(
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        var clientId = settings.GetValueOrDefault("client_id")
                       ?? throw new InvalidOperationException("Настройка 'client_id' не задана. Укажите OAuth Client ID из Google Cloud Console.");

        var clientSecret = settings.GetValueOrDefault("client_secret")
                           ?? throw new InvalidOperationException("Настройка 'client_secret' не задана. Укажите OAuth Client Secret из Google Cloud Console.");

        var tokenDir = GetTokenDir(settings);
        Directory.CreateDirectory(tokenDir);
        var appName = settings.GetValueOrDefault("app_name", "MediaOrcestrator");

        var lastWriteUtc = GetTokenDirMtime(tokenDir);

        if (_serviceCache.TryGetValue(tokenDir, out var cached) && cached.LastWriteUtc == lastWriteUtc)
        {
            return cached;
        }

        await _cacheLock.WaitAsync(cancellationToken);

        try
        {
            lastWriteUtc = GetTokenDirMtime(tokenDir);

            if (_serviceCache.TryGetValue(tokenDir, out cached) && cached.LastWriteUtc == lastWriteUtc)
            {
                return cached;
            }

            var credential = await BuildCredentialAsync(clientId, clientSecret, tokenDir, cancellationToken);
            var service = youtubeServiceFactory.Create(credential, appName);
            var entry = new CachedEntry(service, lastWriteUtc);
            _serviceCache[tokenDir] = entry;
            cached?.MarkEvicted();
            return entry;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<UserCredential> BuildCredentialAsync(
        string clientId,
        string clientSecret,
        string tokenDir,
        CancellationToken cancellationToken)
    {
        logger.OAuthAuthorizing(tokenDir);

        var clientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecrets,
            OAuthScopes,
            "user",
            cancellationToken,
            new FileDataStore(tokenDir, true));

        if (credential.Token.IsStale)
        {
            logger.OAuthTokenStale();
            var refreshed = await credential.RefreshTokenAsync(cancellationToken);

            if (!refreshed)
            {
                throw new InvalidOperationException("Не удалось обновить OAuth-токен. Удалите файл токена и авторизуйтесь заново.");
            }

            logger.OAuthTokenRefreshed();
        }

        return credential;
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

    private readonly struct ServiceLease(CachedEntry entry) : IDisposable
    {
        public YouTubeService Service => entry.Service;

        public void Dispose()
        {
            entry.Release();
        }
    }

    private sealed class CachedEntry(YouTubeService service, DateTime lastWriteUtc)
    {
        private readonly object _gate = new();
        private int _refCount;
        private bool _evicted;

        public YouTubeService Service { get; } = service;

        public DateTime LastWriteUtc { get; } = lastWriteUtc;

        public bool TryAcquire()
        {
            lock (_gate)
            {
                if (_evicted)
                {
                    return false;
                }

                _refCount++;
                return true;
            }
        }

        public void Release()
        {
            bool dispose;

            lock (_gate)
            {
                _refCount--;
                dispose = _refCount == 0 && _evicted;
            }

            if (dispose)
            {
                Service.Dispose();
            }
        }

        public void MarkEvicted()
        {
            bool dispose;

            lock (_gate)
            {
                if (_evicted)
                {
                    return;
                }

                _evicted = true;
                dispose = _refCount == 0;
            }

            if (dispose)
            {
                Service.Dispose();
            }
        }
    }
}
