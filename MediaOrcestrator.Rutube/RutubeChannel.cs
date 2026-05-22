using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.Rutube;

public sealed class RutubeChannel(
    ILogger<RutubeChannel> logger,
    IRutubeServiceFactory rutubeServiceFactory,
    IToolPathProvider toolPathProvider,
    VideoTranscoder videoTranscoder)
    : ISourceType,
        IAuthenticatable,
        IToolConsumer,
        ISupportsComments,
        ISupportsCommentMutations,
        ISupportsCommentLikes
{
    private readonly ConcurrentDictionary<string, CachedService> _serviceCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public SyncDirection ChannelType => SyncDirection.Full;

    public IReadOnlyList<ToolDescriptor> RequiredTools { get; } =
    [
        WellKnownTools.YtDlpDescriptor,
        WellKnownTools.FFmpegWithProbeDescriptor,
    ];

    public string Name => "Rutube";

    // TODO: Подумать на сортировкой
    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "user_id",
            IsRequired = false,
            Title = "идентификатор чужого канала",
            Description = "Если задан — источник читает видео указанного канала (URL https://rutube.ru/channel/{user_id}/). Запись (загрузка/удаление) в этом режиме отключена. Пусто — работа с собственным каналом по авторизации.",
        },
        new()
        {
            Key = "category_id",
            IsRequired = true,
            Title = "идентификатор категории",
            Description = "Категория RuTube для загружаемых видео. Нажмите 'Загрузить категории' для получения списка",
            Type = SettingType.Dropdown,
        },
        new()
        {
            Key = "publish_at",
            IsRequired = false,
            Title = "время публикации",
            Description = "Время отложенной публикации. Форматы: ЧЧ:ММ (например, 20:00) - публикация сегодня в это время, если оно уже прошло - завтра; +N (например, +3) - публикация через N часов от момента загрузки. Если не указано - видео публикуется немедленно",
        },
        new()
        {
            Key = "upload_speed_limit",
            IsRequired = false,
            Title = "ограничение скорости выгрузки (Мбит/с)",
            Description = "Максимальная скорость выгрузки видео. Пустое значение — без ограничений",
        },
        new()
        {
            Key = "speed_limit",
            IsRequired = false,
            Title = "ограничение скорости скачивания (Мбит/с)",
            Description = "Максимальная скорость скачивания видео. Пустое значение — без ограничений",
        },
    ];

    public Uri? GetExternalUri(
        string externalId,
        Dictionary<string, string> settings)
    {
        return new($"https://rutube.ru/video/{externalId}/");
    }

    public async Task<List<SettingOption>> GetSettingOptionsAsync(
        string settingKey,
        Dictionary<string, string> currentSettings)
    {
        if (settingKey != "category_id")
        {
            return [];
        }

        try
        {
            var rutubeService = await CreateRutubeServiceAsync(currentSettings, CancellationToken.None);
            var categories = await rutubeService.GetCategoriesAsync();

            return categories.Select(x => new SettingOption
                {
                    Value = x.Id.ToString(),
                    Label = x.Name,
                })
                .OrderBy(x => x.Label)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.GetCategoriesFailed(ex);
            return [];
        }
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(
        Dictionary<string, string> settings,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.ListingMedia(Name);
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);
        var userId = GetForeignUserId(settings);
        var apiVideoItems = rutubeService.GetVideoAsync(userId, cancellationToken);
        await foreach (var video in apiVideoItems)
        {
            logger.ProcessingVideo(video.Title, video.Id);
            yield return CreateMediaDto(video);
        }
    }

    public async Task<MediaDto?> GetMediaByIdAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.RequestingChannelMedia(externalId);
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);
        var ownChannel = GetForeignUserId(settings) == null;
        var video = await rutubeService.GetVideoByIdAsync(externalId, ownChannel, cancellationToken);
        return CreateMediaDto(video);
    }

    // TODO: Дублирование с Youtube
    public async Task<MediaDto> DownloadAsync(
        string videoId,
        Dictionary<string, string> settings,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.DownloadStarting(videoId);

        var media = await GetMediaByIdAsync(videoId, settings, cancellationToken)
                    ?? throw new InvalidOperationException($"Видео не найдено: {videoId}");

        logger.ChannelVideoInfoReceived(media.Title);

        var tempPath = settings["_system_temp_path"];
        var guid = Guid.NewGuid().ToString();
        var finalPath = Path.Combine(tempPath, guid, "media.mp4");

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        logger.TempDirectoryCreated(Path.GetDirectoryName(finalPath));

        var ytDlpPath = toolPathProvider.GetToolPath(WellKnownTools.YtDlp)
                        ?? throw new InvalidOperationException("yt-dlp не установлен. Установите через панель управления инструментами.");

        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg)
                         ?? throw new InvalidOperationException("ffmpeg не установлен. Установите через панель управления инструментами.");

        var authStatePath = GetAuthStatePath(settings);
        var netscapeCookiePath = "";
        if (File.Exists(authStatePath))
        {
            netscapeCookiePath = Path.Combine(Path.GetDirectoryName(finalPath)!, "cookies.txt");
            ConvertPlaywrightToNetscape(authStatePath, netscapeCookiePath);
            logger.CookiesConvertedToNetscape(netscapeCookiePath);
        }

        var ytDlp = new YtDlp(ytDlpPath, ffmpegPath, netscapeCookiePath);

        object progressLock = new();
        double oldPercent = -1;
        var currentPart = 0;

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

        try
        {
            var rateLimitBytes = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);
            await ytDlp.DownloadAsync($"https://rutube.ru/video/{videoId}/", finalPath, ytDlpProgress, rateLimitBytes, cancellationToken);
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

        media.TempDataPath = finalPath;
        return media;
    }

    public async Task<UploadResult> UploadAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWritable(settings);
        logger.UploadStarting(media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            logger.VideoFileNotFound(filePath ?? "");
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        logger.FileFound(new FileInfo(filePath).Length);

        var codec = media.Metadata?.FirstOrDefault(x => x.Key == "VideoCodec")?.Value;

        if (string.IsNullOrEmpty(codec))
        {
            logger.DetectingCodec(filePath);
            codec = await videoTranscoder.GetVideoCodecAsync(filePath, cancellationToken);
        }

        if (string.IsNullOrEmpty(codec))
        {
            throw new NonRetriableException("Не удалось определить кодек видео. Убедитесь, что ffprobe доступен.");
        }

        if (string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase))
        {
            logger.TranscodingVp9(filePath);

            var sourceDir = Path.GetDirectoryName(filePath)
                            ?? throw new InvalidOperationException($"Не удалось получить папку файла: {filePath}");

            var transcodedPath = Path.Combine(sourceDir, "media.transcoded.mp4");

            var durationStr = media.Metadata?.FirstOrDefault(x => x.Key == "Duration")?.Value;
            var totalDuration = TimeSpan.Zero;
            if (durationStr != null && TimeSpan.TryParse(durationStr, out var parsed))
            {
                totalDuration = parsed;
            }

            var transcodeOk = await videoTranscoder.TranscodeVp9ToH264Async(filePath,
                transcodedPath,
                totalDuration,
                null,
                cancellationToken);

            if (!transcodeOk || !File.Exists(transcodedPath) || new FileInfo(transcodedPath).Length == 0)
            {
                throw new NonRetriableException($"Не удалось транскодировать VP9 в H.264 для файла: {filePath}");
            }

            logger.TranscodingCompleted(new FileInfo(transcodedPath).Length, transcodedPath);

            filePath = transcodedPath;
            media.TempDataPath = transcodedPath;
            codec = "h264";
        }

        logger.VideoCodec(codec);

        var rutubeCategoryId = settings["category_id"];
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);

        DateTime? publishAt = null;
        if (settings.TryGetValue("publish_at", out var publishAtRaw) && !string.IsNullOrWhiteSpace(publishAtRaw))
        {
            var publishAtTrimmed = publishAtRaw.Trim();
            if (publishAtTrimmed.StartsWith('+') && double.TryParse(publishAtTrimmed[1..], NumberStyles.Any, CultureInfo.InvariantCulture, out var relativeHours))
            {
                publishAt = DateTime.Now.AddHours(relativeHours);
                logger.RelativePublicationScheduled(relativeHours, publishAt.Value);
            }
            else if (TimeOnly.TryParseExact(publishAtTrimmed, "HH:mm", out var timeOnly))
            {
                var today = DateTime.Today.Add(timeOnly.ToTimeSpan());
                publishAt = today > DateTime.Now ? today : today.AddDays(1);
                logger.PublicationScheduled(publishAt.Value);
            }
            else
            {
                logger.PublishAtParseFailed(publishAtRaw);
            }
        }

        try
        {
            var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);
            var uploadProgress = UploadProgressLogger.CreateBucketed(logger, media.Id, external: progress);
            var result = await rutubeService.UploadVideoAsync(null,
                filePath,
                media.Title,
                media.Description,
                rutubeCategoryId,
                media.TempPreviewPath,
                publishAt,
                uploadBytesPerSecond,
                uploadProgress,
                cancellationToken);

            logger.VideoUploaded(result.Status.Id, result.Id, media.Title);

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.UploadFailed(media.Title, ex);
            throw;
        }
    }

    public async Task<UploadResult> UpdateAsync(
        string externalId,
        MediaDto media,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        EnsureWritable(settings);
        logger.UpdateStarting(media.Title);

        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);

        try
        {
            var updated = await rutubeService.UpdateVideoMetadataAsync(externalId, media.Title, media.Description, cancellationToken);

            if (!string.IsNullOrEmpty(media.TempPreviewPath))
            {
                await rutubeService.UploadVideoAsync(externalId,
                    null,
                    media.Title,
                    media.Description,
                    settings.GetValueOrDefault("category_id", string.Empty),
                    media.TempPreviewPath,
                    cancellationToken: cancellationToken);
            }

            var result = new UploadResult
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
                ConfirmedTitle = updated.Title,
            };

            logger.VideoUploaded(result.Status.Id, result.Id, media.Title);

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.UploadFailed(media.Title, ex);
            throw;
        }
    }

    public async Task DeleteAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        EnsureWritable(settings);
        logger.DeletingMedia(externalId);

        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);

        try
        {
            await rutubeService.DeleteVideoAsync(externalId, cancellationToken);
            logger.MediaDeleted(externalId);
        }
        catch (HttpRequestException ex)
        {
            logger.DeleteHttpError(externalId, ex);
            throw new IOException($"Ошибка сети при удалении из RuTube: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<CommentDto> GetCommentsAsync(
        string externalId,
        Dictionary<string, string> settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);

        await foreach (var root in rutubeService.GetCommentRootsAsync(externalId, cancellationToken))
        {
            yield return MapComment(root);

            if (root is { RepliesNumber: <= 0, IsParent: false })
            {
                continue;
            }

            await foreach (var reply in rutubeService.GetCommentRepliesAsync(externalId, root.Id, cancellationToken))
            {
                yield return MapComment(reply, root.Id);
            }
        }
    }

    public async Task<CommentDto> CreateCommentAsync(
        string externalMediaId,
        string? parentExternalCommentId,
        string? rootExternalCommentId,
        string? replyToAuthorName,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);

        var effectiveParent = rootExternalCommentId ?? parentExternalCommentId;
        var effectiveText = !string.IsNullOrWhiteSpace(replyToAuthorName)
                            && rootExternalCommentId != null
                            && rootExternalCommentId != parentExternalCommentId
            ? $"@{replyToAuthorName} {text}"
            : text;

        var item = await rutubeService.CreateCommentAsync(externalMediaId, effectiveParent, effectiveText, cancellationToken);
        return MapComment(item, effectiveParent);
    }

    public async Task<CommentDto?> EditCommentAsync(
        string externalMediaId,
        string externalCommentId,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);
        var item = await rutubeService.EditCommentAsync(externalMediaId, externalCommentId, text, cancellationToken);
        return MapComment(item);
    }

    public async Task DeleteCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);
        await rutubeService.DeleteCommentAsync(externalMediaId, externalCommentId, cancellationToken);
    }

    public Task RestoreCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        throw new NonRetriableException("RuTube не поддерживает восстановление удалённых комментариев");
    }

    public async Task<int> LikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);
        await rutubeService.LikeCommentAsync(externalMediaId, externalCommentId, cancellationToken);
        return await TryFetchRootLikeCountAsync(rutubeService, externalMediaId, externalCommentId, 1, cancellationToken);
    }

    public async Task<int> UnlikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var rutubeService = await CreateRutubeServiceAsync(settings, cancellationToken);
        await rutubeService.UnlikeCommentAsync(externalMediaId, externalCommentId, cancellationToken);
        return await TryFetchRootLikeCountAsync(rutubeService, externalMediaId, externalCommentId, 0, cancellationToken);
    }

    // TODO: Придумать более умный механизм
    public bool IsAuthenticated(Dictionary<string, string> settings)
    {
        var authStatePath = GetAuthStatePath(settings);
        if (!File.Exists(authStatePath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(authStatePath);
            using var doc = JsonDocument.Parse(json);
            var cookies = doc.RootElement.GetProperty("cookies");

            return cookies.EnumerateArray()
                .Any(c =>
                    c.GetProperty("name").GetString() == "csrftoken"
                    && c.GetProperty("domain").GetString() == "studio.rutube.ru");
        }
        catch
        {
            return false;
        }
    }

    public async Task AuthenticateAsync(
        Dictionary<string, string> settings,
        IAuthUI ui,
        CancellationToken ct)
    {
        var authStatePath = GetAuthStatePath(settings);
        var result = await ui.OpenBrowserAsync("https://studio.rutube.ru/", authStatePath);
        if (result != null)
        {
            logger.AuthSaved(result);
            await ui.ShowMessageAsync("Авторизация RuTube сохранена!");
        }
    }

    private static string? GetForeignUserId(Dictionary<string, string> settings)
    {
        if (settings.TryGetValue("user_id", out var userId) && !string.IsNullOrWhiteSpace(userId))
        {
            return userId.Trim();
        }

        return null;
    }

    private static void EnsureWritable(Dictionary<string, string> settings)
    {
        var userId = GetForeignUserId(settings);
        if (userId != null)
        {
            throw new NonRetriableException($"Источник настроен на чужой канал (user_id={userId}). Запись отключена — очистите user_id, чтобы загружать/удалять видео в своём канале.");
        }
    }

    private static async Task<int> TryFetchRootLikeCountAsync(
        RutubeService rutubeService,
        string videoId,
        string commentId,
        int fallback,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var root in rutubeService.GetCommentRootsAsync(videoId, cancellationToken))
            {
                if (root.Id == commentId)
                {
                    return root.LikesNumber;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // fallback
        }

        return fallback;
    }

    private static CommentDto MapComment(
        RutubeCommentItem item,
        string? fallbackParentId = null)
    {
        var raw = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(item.BroId))
        {
            raw["bro_id"] = item.BroId;
        }

        if (item.BroUser != null)
        {
            raw["bro_user_id"] = item.BroUser.Id.ToString(CultureInfo.InvariantCulture);
            raw["bro_user_name"] = item.BroUser.Name;
        }

        if (item.RepliesNumber > 0)
        {
            raw["replies_number"] = item.RepliesNumber.ToString(CultureInfo.InvariantCulture);
        }

        if (item.AuthorReplied)
        {
            raw["author_replied"] = "1";
        }

        if (item.IsPinned)
        {
            raw["is_pinned"] = "1";
        }

        if (item.State != 1)
        {
            raw["state"] = item.State.ToString(CultureInfo.InvariantCulture);
        }

        if (item.DislikesNumber > 0)
        {
            raw["dislikes_number"] = item.DislikesNumber.ToString(CultureInfo.InvariantCulture);
        }

        if (item.CurrentUserDisliked)
        {
            raw["disliked_by_me"] = "1";
        }

        if (item.User.IsOfficial)
        {
            raw["is_official"] = "1";
        }

        return new()
        {
            ExternalId = item.Id,
            ParentExternalId = item.ParentId ?? fallbackParentId,
            AuthorName = item.User.Name,
            AuthorExternalId = item.User.Id.ToString(CultureInfo.InvariantCulture),
            AuthorAvatarUrl = item.User.AvatarUrl,
            Text = item.Text,
            PublishedAt = DateTimeOffset.FromUnixTimeSeconds(item.CreatedTsReal).UtcDateTime,
            LikeCount = item.LikesNumber,
            IsDeleted = item.IsDeleted,
            IsAuthor = false,
            LikedByAuthor = false,
            LikedByMe = item.CurrentUserLiked,
            CanEdit = !item.IsDeleted,
            CanDelete = !item.IsDeleted,
            Raw = raw,
        };
    }

    private static string GetAuthStatePath(Dictionary<string, string> settings)
    {
        return Path.Combine(settings["_system_state_path"], "auth_state");
    }

    private static MediaDto CreateMediaDto(IRutubeVideoInfo video)
    {
        var metadata = new List<MetadataItem>
        {
            new()
            {
                Key = "Duration",
                DisplayName = "Длительность",
                Value = TimeSpan.FromSeconds(video.Duration).ToString(),
                DisplayType = "System.TimeSpan",
            },
            new()
            {
                Key = "Author",
                DisplayName = "Автор",
                Value = video.Author?.Name ?? "",
                DisplayType = "System.String",
            },
            new()
            {
                Key = "CreationDate",
                DisplayName = "Дата создания",
                Value = video.CreatedTsFormatted,
                DisplayType = "System.DateTime",
            },
            new()
            {
                Key = "Views",
                DisplayName = "Просмотры",
                Value = video.Hits.ToString(),
                DisplayType = "System.Int64",
            },
            new()
            {
                Key = "PreviewUrl",
                Value = video.ThumbnailUrl ?? "",
            },
        };

        return new()
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            DataPath = video.VideoUrl ?? "",
            PreviewPath = video.ThumbnailUrl,
            Metadata = metadata,
        };
    }

    // TODO: Дублирование с Youtube
    private static void ConvertPlaywrightToNetscape(
        string playwrightJsonPath,
        string netscapePath)
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

    private async Task<RutubeService> CreateRutubeServiceAsync(
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        var authStatePath = GetAuthStatePath(settings);
        if (!File.Exists(authStatePath))
        {
            throw new FileNotFoundException($"Файл аутентификации RuTube не найден: {authStatePath}. Выполните авторизацию.", authStatePath);
        }

        var lastWriteUtc = File.GetLastWriteTimeUtc(authStatePath);

        if (_serviceCache.TryGetValue(authStatePath, out var cached) && cached.LastWriteUtc == lastWriteUtc)
        {
            return cached.Service;
        }

        await _cacheLock.WaitAsync(cancellationToken);

        try
        {
            if (_serviceCache.TryGetValue(authStatePath, out cached) && cached.LastWriteUtc == lastWriteUtc)
            {
                return cached.Service;
            }

            var service = await BuildRutubeServiceAsync(authStatePath, cancellationToken);
            _serviceCache[authStatePath] = new(service, lastWriteUtc);
            return service;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<RutubeService> BuildRutubeServiceAsync(
        string authStatePath,
        CancellationToken cancellationToken)
    {
        logger.ReadingAuthState(authStatePath);

        var authStateBody = await File.ReadAllTextAsync(authStatePath, cancellationToken);
        using var authState = JsonDocument.Parse(authStateBody);
        var cookies = authState.RootElement.GetProperty("cookies");

        var cookieStringBuilder = new StringBuilder();
        string? studioCsrfToken = null;
        string? rutubeCsrfToken = null;

        foreach (var cookie in cookies.EnumerateArray())
        {
            var name = cookie.GetProperty("name").GetString()!;
            var value = cookie.GetProperty("value").GetString()!;
            var domain = cookie.GetProperty("domain").GetString()!;

            if (domain.Contains("rutube.ru") || domain.Contains("gid.ru"))
            {
                cookieStringBuilder.Append($"{name}={value}; ");
            }

            if (name == "csrftoken")
            {
                if (domain == "studio.rutube.ru")
                {
                    studioCsrfToken = value;
                }
                else if (domain.EndsWith("rutube.ru", StringComparison.OrdinalIgnoreCase))
                {
                    rutubeCsrfToken = value;
                }
            }
        }

        var studioCsrf = studioCsrfToken ?? rutubeCsrfToken;
        if (string.IsNullOrEmpty(studioCsrf))
        {
            throw new InvalidOperationException("CSRF токен не найден в файле аутентификации RuTube. Убедитесь, что вы авторизованы в RuTube Studio.");
        }

        var playerCsrf = rutubeCsrfToken ?? studioCsrfToken!;

        logger.CsrfTokenReceived();

        return rutubeServiceFactory.Create(cookieStringBuilder.ToString(), studioCsrf, playerCsrf);
    }

    private sealed record CachedService(RutubeService Service, DateTime LastWriteUtc);
}
