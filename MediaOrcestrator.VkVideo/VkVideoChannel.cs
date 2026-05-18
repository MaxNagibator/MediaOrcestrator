using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.VkVideo;

public sealed class VkVideoChannel(
    ILogger<VkVideoChannel> logger,
    IVkVideoServiceFactory vkVideoServiceFactory,
    VideoTranscoder videoTranscoder)
    : ISourceType,
        IAuthenticatable,
        ISupportsComments,
        ISupportsCommentPermalinks,
        ISupportsCommentMutations,
        ISupportsCommentAuthors,
        ISupportsCommentLikes
{
    private const string AuthorIdSelf = "user";
    private const string AuthorIdGroupPrefix = "group:";
    private const string SubtypeMetadataKey = "_system_vk_subtype";
    private const string ClipSubtype = "clip";
    private const string VideoSubtype = "video";
    private static readonly TimeSpan MaxShortsDuration = TimeSpan.FromMinutes(3);

    private readonly ConcurrentDictionary<string, CachedEntry> _serviceCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public string Name => "VkVideo";

    public SyncDirection ChannelType => SyncDirection.Full;

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "group_id",
            IsRequired = true,
            Title = "идентификатор группы",
            Description = "ID группы/канала VK Video (например, 237105491)",
        },
        new()
        {
            Key = "publish_at",
            IsRequired = false,
            Title = "время публикации",
            Description = "Отложенная публикация: +N (часы) или ЧЧ:ММ. Если не указано — немедленно",
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
            Title = "ограничение скорости выгрузки (Мбит/с)",
            Description = "Максимальная скорость выгрузки видео. Пустое значение — без ограничений",
        },
    ];

    public Task<List<SettingOption>> GetSettingOptionsAsync(
        string settingKey,
        Dictionary<string, string> currentSettings)
    {
        return Task.FromResult<List<SettingOption>>([]);
    }

    public Uri? GetExternalUri(
        string externalId,
        Dictionary<string, string> settings)
    {
        return GetExternalUri(externalId, settings, null);
    }

    public Uri? GetExternalUri(
        string externalId,
        Dictionary<string, string> settings,
        IReadOnlyList<MetadataItem>? metadata)
    {
        var path = IsClipFromMetadata(metadata) ? "clip" : "video";
        return new($"https://vkvideo.ru/{path}{externalId}");
    }

    public Uri? GetCommentExternalUri(
        string externalMediaId,
        string externalCommentId,
        string? rootExternalCommentId,
        Dictionary<string, string> settings)
    {
        return GetCommentExternalUri(externalMediaId, externalCommentId, rootExternalCommentId, settings, null);
    }

    public Uri? GetCommentExternalUri(
        string externalMediaId,
        string externalCommentId,
        string? rootExternalCommentId,
        Dictionary<string, string> settings,
        IReadOnlyList<MetadataItem>? metadata)
    {
        if (string.IsNullOrEmpty(externalMediaId) || string.IsNullOrEmpty(externalCommentId))
        {
            return null;
        }

        var query = string.IsNullOrEmpty(rootExternalCommentId)
            ? $"?reply={externalCommentId}"
            : $"?thread={rootExternalCommentId}&reply={externalCommentId}";

        var path = IsClipFromMetadata(metadata) ? "clip" : "video";
        return new($"https://vkvideo.ru/{path}{externalMediaId}{query}");
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(
        Dictionary<string, string> settings,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;
        var groupId = ParseGroupId(settings["group_id"]);
        await foreach (var video in service.GetMediaAsync(groupId, cancellationToken))
        {
            yield return CreateMediaDto(video);
        }
    }

    public async Task<MediaDto?> GetMediaByIdAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.RequestingVideoDetails(externalId);
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var (ownerId, videoId) = ParseExternalId(externalId);
        var service = lease.Service;

        var video = await service.GetVideoByIdAsync(ownerId, videoId, cancellationToken);
        if (video == null)
        {
            return null;
        }

        if (IsClip(video))
        {
            var shortVideo = await service.GetShortVideoByIdAsync(ownerId, videoId, cancellationToken);
            if (shortVideo != null)
            {
                ApplyShortVideoData(video, shortVideo);
            }
        }

        return CreateMediaDto(video);
    }

    public async IAsyncEnumerable<CommentDto> GetCommentsAsync(
        string externalId,
        Dictionary<string, string> settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (ownerId, videoId) = ParseExternalId(externalId);
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;

        var authors = new Dictionary<long, AuthorInfo>();

        await foreach (var comment in StreamCommentsAsync(service, ownerId, videoId, null, authors, cancellationToken))
        {
            yield return comment;

            if (comment.Raw is not { } raw
                || !raw.TryGetValue("thread_count", out var threadCountStr)
                || !int.TryParse(threadCountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threadCount)
                || threadCount <= 0
                || !long.TryParse(comment.ExternalId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var commentIdValue))
            {
                continue;
            }

            await foreach (var reply in StreamCommentsAsync(service, ownerId, videoId, commentIdValue, authors, cancellationToken))
            {
                yield return reply;
            }
        }
    }

    public Task<CommentDto> CreateCommentAsync(
        string externalMediaId,
        string? parentExternalCommentId,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        return CreateCommentAsAsync(externalMediaId, parentExternalCommentId, text, null, settings, cancellationToken);
    }

    public async Task<CommentDto> CreateCommentAsAsync(
        string externalMediaId,
        string? parentExternalCommentId,
        string text,
        string? authorId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, videoId) = ParseExternalId(externalMediaId);
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;

        long? parentId = null;
        if (!string.IsNullOrEmpty(parentExternalCommentId)
            && long.TryParse(parentExternalCommentId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            parentId = parsed;
        }

        var fromOwnerId = ParseAuthorId(authorId);

        var newCommentId = await service.CreateVideoCommentAsync(ownerId,
            videoId,
            parentId,
            text,
            fromOwnerId,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return await FetchSingleCommentAsync(service, ownerId, newCommentId, parentId, cancellationToken);
    }

    public async Task<IReadOnlyList<CommentAuthorOption>> GetAvailableAuthorsAsync(
        string externalMediaId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, videoId) = ParseExternalId(externalMediaId);
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        return await lease.Entry.GetOrFetchAuthorsAsync(ct => FetchAuthorsAsync(lease.Service, ownerId, videoId, ct),
            cancellationToken);
    }

    public async Task<CommentDto?> EditCommentAsync(
        string externalMediaId,
        string externalCommentId,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;
        await service.EditVideoCommentAsync(ownerId, commentId, text, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return await FetchSingleCommentAsync(service, ownerId, commentId, null, cancellationToken);
    }

    public async Task DeleteCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        await lease.Service.DeleteVideoCommentAsync(ownerId, commentId, cancellationToken);
    }

    public async Task RestoreCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        await lease.Service.RestoreVideoCommentAsync(ownerId, commentId, cancellationToken);
    }

    public async Task<int> LikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        return await lease.Service.LikeVideoCommentAsync(ownerId, commentId, cancellationToken);
    }

    public async Task<int> UnlikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        return await lease.Service.UnlikeVideoCommentAsync(ownerId, commentId, cancellationToken);
    }

    // TODO: Переделать нормально
    public async Task<MediaDto> DownloadAsync(
        string videoId,
        Dictionary<string, string> settings,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.DownloadingVideo(videoId);
        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;
        var (ownerId, vid) = ParseExternalId(videoId);

        var video = await service.GetVideoByIdAsync(ownerId, vid, cancellationToken)
                    ?? throw new InvalidOperationException($"Видео {videoId} не найдено");

        var downloadUrl = video.Files?.GetBestQualityUrl()
                          ?? throw new InvalidOperationException($"Нет доступных URL для скачивания видео {videoId}");

        var tempPath = settings["_system_temp_path"];
        var guid = Guid.NewGuid().ToString();
        var tempVideoPath = Path.Combine(tempPath, guid, "media.mp4");
        var tempPreviewPath = Path.Combine(tempPath, guid, "preview.jpg");

        Directory.CreateDirectory(Path.Combine(tempPath, guid));

        logger.DownloadingFromUrl(downloadUrl);
        var downloadBytesPerSecond = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);

        await service.DownloadAsync(downloadUrl, async (stream, ct) =>
        {
            await using var throttled = new ThrottledStream(stream, downloadBytesPerSecond);
            await using var fileStream = File.Create(tempVideoPath);
            await throttled.CopyToAsync(fileStream, ct);
        }, progress, cancellationToken);

        logger.VideoSaved(tempVideoPath, new FileInfo(tempVideoPath).Length);

        var previewUrl = video.Image
            .OrderByDescending(i => i.Width)
            .FirstOrDefault()
            ?.Url;

        if (previewUrl != null)
        {
            await service.DownloadAsync(previewUrl, async (stream, ct) =>
            {
                await using var previewFile = File.Create(tempPreviewPath);
                await stream.CopyToAsync(previewFile, ct);
            }, cancellationToken: cancellationToken);
        }

        var mediaDto = CreateMediaDto(video);
        var metadata = mediaDto.Metadata;

        // TODO: Может не добавлять вообще если null
        return new()
        {
            Id = videoId,
            Title = mediaDto.Title,
            Description = video.Description,
            DataPath = downloadUrl,
            PreviewPath = previewUrl ?? string.Empty,
            TempDataPath = tempVideoPath,
            TempPreviewPath = File.Exists(tempPreviewPath) ? tempPreviewPath : string.Empty,
            Metadata = metadata,
        };
    }

    public async Task<UploadResult> UploadAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.UploadingVideo(media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        var frameSize = await videoTranscoder.GetVideoFrameSizeAsync(filePath, cancellationToken)
                        ?? throw new NonRetriableException("Не удалось определить размер кадра видео — невозможно выбрать режим загрузки (обычное / shorts)");

        var duration = await videoTranscoder.GetVideoDurationAsync(filePath, cancellationToken)
                       ?? throw new NonRetriableException("Не удалось определить длительность видео — невозможно выбрать режим загрузки (обычное / shorts)");

        var isShorts = frameSize.IsSquareOrPortrait && duration <= MaxShortsDuration;

        if (frameSize.IsSquareOrPortrait && !isShorts)
        {
            logger.VideoTooLongForShorts(duration, MaxShortsDuration);
        }

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;
        var groupId = ParseGroupId(settings["group_id"]);
        var fileExt = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        // TODO: Копипаста с RutubeChannel, но плагины жи
        long? publishAtUnix = null;
        if (settings.TryGetValue("publish_at", out var publishAtRaw) && !string.IsNullOrWhiteSpace(publishAtRaw))
        {
            var publishAtTrimmed = publishAtRaw.Trim();
            DateTime? publishAt = null;

            if (publishAtTrimmed.StartsWith('+') && double.TryParse(publishAtTrimmed[1..], NumberStyles.Any, CultureInfo.InvariantCulture, out var relativeHours))
            {
                publishAt = DateTime.Now.AddHours(relativeHours);
            }
            else if (TimeOnly.TryParseExact(publishAtTrimmed, "HH:mm", out var timeOnly))
            {
                var today = DateTime.Today.Add(timeOnly.ToTimeSpan());
                publishAt = today > DateTime.Now ? today : today.AddDays(1);
            }

            if (publishAt.HasValue)
            {
                publishAtUnix = new DateTimeOffset(publishAt.Value).ToUnixTimeSeconds();
                logger.ScheduledPublication(publishAt.Value);
            }
        }

        try
        {
            var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);
            var uploadProgress = UploadProgressLogger.CreateBucketed(logger, media.Id, external: progress);
            var previewPath = PreparePreviewForUpload(media.TempPreviewPath, isShorts);
            var result = await service.UploadVideoAsync(isShorts, groupId, filePath, media.Title, media.Description,
                fileExt, previewPath, publishAtUnix, uploadBytesPerSecond, uploadProgress, cancellationToken);

            var externalId = $"{result.OwnerId}_{result.Id}";
            logger.VideoUploaded(externalId);

            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.UploadFailed(ex);
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = ex.Message,
            };
        }
    }

    public async Task<UploadResult> UpdateAsync(
        string externalId,
        MediaDto tempMedia,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.UpdatingVideo(externalId);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var service = lease.Service;
        var (ownerId, videoId) = ParseExternalId(externalId);

        var errorMessage = "";
        SaveThumbResponse? thumbResult = null;
        var video = await service.GetVideoByIdAsync(ownerId, videoId, cancellationToken)
                    ?? throw new NonRetriableException("Не удалось получить видео из VK для определения типа (обычное / shorts)");

        var isShorts = IsClip(video);

        if (!string.IsNullOrEmpty(tempMedia.TempPreviewPath) && File.Exists(tempMedia.TempPreviewPath))
        {
            try
            {
                var previewPath = PreparePreviewForUpload(tempMedia.TempPreviewPath, isShorts);
                thumbResult = await service.UploadThumbnailAsync(isShorts, ownerId, videoId, previewPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.ThumbnailUpdateFailed(ex);
                errorMessage += "Ошибка загрузки превью";
            }
        }

        try
        {
            if (isShorts)
            {
                await service.EditShortVideoAsync(ownerId, videoId, tempMedia.Title, cancellationToken);
            }
            else
            {
                await service.EditVideoAsync(ownerId, videoId, tempMedia.Title, tempMedia.Description, thumbResult, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.MetadataEditFailed(ex);
            if (errorMessage.Length > 0)
            {
                errorMessage += Environment.NewLine;
            }

            errorMessage += "Ошибка редактирования метаданных";
        }

        if (errorMessage.Length == 0)
        {
            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }

        return new()
        {
            Status = MediaStatusHelper.GetById(MediaStatus.PartialOk),
            Id = externalId,
            Message = errorMessage,
        };
    }

    public async Task DeleteAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.DeletingVideo(externalId);

        using var lease = await AcquireServiceLeaseAsync(settings, cancellationToken);
        var (ownerId, videoId) = ParseExternalId(externalId);

        await lease.Service.DeleteVideoAsync(ownerId, videoId, cancellationToken);

        logger.VideoDeleted(externalId);
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
                {
                    var domain = c.GetProperty("domain").GetString() ?? "";
                    return domain.Contains("vkvideo.ru") || domain.Contains("vk.com");
                });
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
        var result = await ui.OpenBrowserAsync("https://cabinet.vkvideo.ru/", authStatePath);
        if (result != null)
        {
            logger.AuthSaved(result);
            await ui.ShowMessageAsync("Авторизация VK Video сохранена!");
        }
    }

    public ConvertType[] GetAvailableConvertTypes()
    {
        return [];
    }

    public Task ConvertAsync(
        int typeId,
        string externalId,
        Dictionary<string, string> settings,
        IProgress<ConvertProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private static long? ParseAuthorId(string? authorId)
    {
        if (string.IsNullOrEmpty(authorId) || string.Equals(authorId, AuthorIdSelf, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (authorId.StartsWith(AuthorIdGroupPrefix, StringComparison.OrdinalIgnoreCase)
            && long.TryParse(authorId.AsSpan(AuthorIdGroupPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed < 0 ? parsed : -parsed;
        }

        return null;
    }

    private static async Task<CommentDto> FetchSingleCommentAsync(
        VkVideoService service,
        long ownerId,
        long commentId,
        long? parentCommentId,
        CancellationToken cancellationToken)
    {
        var response = await service.GetVideoCommentAsync(ownerId, commentId, cancellationToken);

        var item = response.Items.FirstOrDefault()
                   ?? throw new InvalidOperationException($"VK не вернул комментарий {commentId} после операции");

        var authors = new Dictionary<long, AuthorInfo>();
        AppendAuthors(response, authors);

        return MapComment(item, parentCommentId, ownerId, authors);
    }

    private static void AppendAuthors(
        VkCommentsResponse response,
        Dictionary<long, AuthorInfo> authors)
    {
        foreach (var profile in response.Profiles)
        {
            authors.TryAdd(profile.Id, new($"{profile.FirstName} {profile.LastName}".Trim(), profile.Photo100));
        }

        foreach (var group in response.Groups)
        {
            authors.TryAdd(-group.Id, new(group.Name ?? string.Empty, group.Photo100));
        }
    }

    private static async IAsyncEnumerable<CommentDto> StreamCommentsAsync(
        VkVideoService service,
        long ownerId,
        long videoId,
        long? parentCommentId,
        Dictionary<long, AuthorInfo> authors,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int PageSize = 100;
        var offset = 0;
        var total = -1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await service.GetVideoCommentsAsync(ownerId,
                videoId,
                PageSize,
                offset,
                parentCommentId,
                cancellationToken: cancellationToken);

            AppendAuthors(response, authors);

            if (total < 0)
            {
                total = response.Count;
            }

            if (response.Items.Count == 0)
            {
                yield break;
            }

            foreach (var item in response.Items)
            {
                yield return MapComment(item, parentCommentId, ownerId, authors);
            }

            offset += response.Items.Count;
            if (offset >= total)
            {
                yield break;
            }
        }
    }

    private static CommentDto MapComment(
        VkCommentItem item,
        long? parentCommentId,
        long ownerId,
        Dictionary<long, AuthorInfo> authors)
    {
        authors.TryGetValue(item.FromId, out var author);

        var raw = new Dictionary<string, string>
        {
            ["from_id"] = item.FromId.ToString(CultureInfo.InvariantCulture),
        };

        if (item.Thread is { Count: > 0 } thread)
        {
            raw["thread_count"] = thread.Count.ToString(CultureInfo.InvariantCulture);
        }

        if (item.ReplyToUser is { } replyToUser)
        {
            raw["reply_to_user"] = replyToUser.ToString(CultureInfo.InvariantCulture);
        }

        long? parentId;
        if (item.ReplyToComment is { } replyToComment)
        {
            parentId = replyToComment;
        }
        else if (item.ParentsStack.Count > 0)
        {
            parentId = item.ParentsStack[^1];
        }
        else
        {
            parentId = parentCommentId;
        }

        return new()
        {
            ExternalId = item.Id.ToString(CultureInfo.InvariantCulture),
            ParentExternalId = parentId?.ToString(CultureInfo.InvariantCulture),
            AuthorName = author.Name ?? string.Empty,
            AuthorExternalId = item.FromId.ToString(CultureInfo.InvariantCulture),
            AuthorAvatarUrl = author.AvatarUrl,
            Text = item.Text,
            PublishedAt = DateTimeOffset.FromUnixTimeSeconds(item.Date).UtcDateTime,
            LikeCount = item.Likes?.Count,
            IsDeleted = item.Deleted,
            IsAuthor = item.FromId == ownerId,
            LikedByAuthor = item.Likes?.GroupLiked ?? false,
            LikedByMe = item.Likes?.UserLikes == 1,
            CanEdit = item.CanEdit == 1,
            CanDelete = item.CanDelete == 1,
            Raw = raw,
        };
    }

    private static long ParseGroupId(string raw)
    {
        var trimmed = raw.Trim();

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var direct))
        {
            return direct;
        }

        string[] prefixes = ["club", "public", "event"];
        foreach (var prefix in prefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(trimmed.AsSpan(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var prefixed))
            {
                return prefixed;
            }
        }

        throw new FormatException($"Не удалось распарсить group_id '{raw}'. Ожидается число или формат club<id>/public<id>/event<id>.");
    }

    private static ExternalVideoId ParseExternalId(string externalId)
    {
        var parts = externalId.Split('_', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out var ownerId) || !long.TryParse(parts[1], out var videoId))
        {
            throw new ArgumentException($"Некорректный формат ID видео: {externalId}. Ожидается: {{owner_id}}_{{video_id}}");
        }

        return new(ownerId, videoId);
    }

    private static MediaDto CreateMediaDto(VideoItem video)
    {
        var bestPreview = video.Image
            .OrderByDescending(i => i.Width)
            .FirstOrDefault();

        var previewUrl = bestPreview?.Url ?? string.Empty;
        var metadata = BuildMetadata(video);
        metadata.Add(new()
        {
            Key = "PreviewUrl",
            Value = previewUrl,
        });

        metadata.Add(new()
        {
            Key = SubtypeMetadataKey,
            Value = IsClip(video) ? ClipSubtype : VideoSubtype,
        });

        return new()
        {
            Id = video.GetVideoKey(),
            Title = GetTitle(video),
            Description = video.Description,
            DataPath = video.Files?.GetBestQualityUrl() ?? string.Empty,
            PreviewPath = previewUrl,
            Metadata = metadata,
        };
    }

    private static bool IsClipFromMetadata(IReadOnlyList<MetadataItem>? metadata)
    {
        if (metadata == null)
        {
            return false;
        }

        foreach (var item in metadata)
        {
            if (string.Equals(item.Key, SubtypeMetadataKey, StringComparison.Ordinal))
            {
                return string.Equals(item.Value, ClipSubtype, StringComparison.Ordinal);
            }
        }

        return false;
    }

    private static string GetTitle(VideoItem video)
    {
        if (IsClip(video) && !string.IsNullOrWhiteSpace(video.Description))
        {
            return video.Description;
        }

        return video.Title;
    }

    private static bool IsClip(VideoItem video)
    {
        return string.Equals(video.Type, "short_video", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyShortVideoData(
        VideoItem video,
        ShortVideoFullItem shortVideo)
    {
        if (!string.IsNullOrEmpty(shortVideo.Description))
        {
            video.Description = shortVideo.Description;
        }

        if (shortVideo.DurationSeconds > 0)
        {
            video.Duration = shortVideo.DurationSeconds;
        }

        if (shortVideo.Width > 0)
        {
            video.Width = shortVideo.Width;
        }

        if (shortVideo.Height > 0)
        {
            video.Height = shortVideo.Height;
        }

        if (shortVideo.PublishTimestamp > 0)
        {
            video.Date = shortVideo.PublishTimestamp;
        }

        if (shortVideo.Engagement is { } engagement)
        {
            video.Views = engagement.ViewCount;
            video.Comments = engagement.CommentCount;
            video.Likes = new() { Count = engagement.LikeCount };
            video.Reposts = new() { Count = engagement.RepostCount };
        }

        if (shortVideo.Covers.Count > 0)
        {
            video.Image = shortVideo.Covers
                .Select(c => new VideoImage
                {
                    Url = c.Url,
                    Width = c.Width,
                    Height = c.Height,
                })
                .ToList();
        }

        if (shortVideo.Files != null)
        {
            video.Files = shortVideo.Files;
        }
    }

    private static List<MetadataItem> BuildMetadata(VideoItem video)
    {
        return
        [
            new()
            {
                Key = "Duration",
                DisplayName = "Длительность",
                Value = TimeSpan.FromSeconds(video.Duration).ToString(),
                DisplayType = "System.TimeSpan",
            },
            new()
            {
                Key = "CreationDate",
                DisplayName = "Дата создания",
                Value = DateTimeOffset.FromUnixTimeSeconds(video.Date).DateTime.ToString("O"),
                DisplayType = "System.DateTime",
            },
            new()
            {
                Key = "Views",
                DisplayName = "Просмотры",
                Value = video.Views.ToString(),
                DisplayType = "System.Int64",
            },
            new()
            {
                Key = "Likes",
                DisplayName = "Лайки",
                Value = (video.Likes?.Count ?? 0).ToString(),
                DisplayType = "System.Int64",
            },
            new()
            {
                Key = "Resolution",
                DisplayName = "Разрешение",
                Value = $"{video.Width}x{video.Height}",
                DisplayType = "System.String",
            },
        ];
    }

    private static string GetAuthStatePath(Dictionary<string, string> settings)
    {
        return Path.Combine(settings["_system_state_path"], "auth_state");
    }

    private async Task<IReadOnlyList<CommentAuthorOption>> FetchAuthorsAsync(
        VkVideoService service,
        long ownerId,
        long videoId,
        CancellationToken cancellationToken)
    {
        var selfTask = service.GetCurrentUserAsync(cancellationToken);
        var replyAsTask = service.GetReplyAsListAsync(ownerId, videoId, cancellationToken);

        VkUserItem? self = null;
        try
        {
            self = await selfTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить профиль текущего пользователя VK");
        }

        var selfName = self != null
            ? $"{self.FirstName} {self.LastName}".Trim()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(selfName))
        {
            selfName = "Свой профиль";
        }

        var result = new List<CommentAuthorOption>
        {
            new(AuthorIdSelf, selfName, self?.Photo100 ?? self?.Photo50, true),
        };

        VkReplyAsListResponse? response = null;
        try
        {
            response = await replyAsTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить список доступных авторов комментария");
        }

        if (response?.Items != null)
        {
            foreach (var item in response.Items)
            {
                var negativeId = -Math.Abs(item.Id);
                var name = string.IsNullOrWhiteSpace(item.Name) ? item.ScreenName ?? $"club{item.Id}" : item.Name!;
                var avatar = item.Photo100 ?? item.Photo50;
                result.Add(new(AuthorIdGroupPrefix + negativeId.ToString(CultureInfo.InvariantCulture),
                    name,
                    avatar,
                    false));
            }
        }

        return result;
    }

    private string PreparePreviewForUpload(
        string previewPath,
        bool isShorts)
    {
        if (!isShorts || string.IsNullOrEmpty(previewPath) || !File.Exists(previewPath))
        {
            return previewPath;
        }

        try
        {
            var croppedPath = ThumbnailCropper.CropToShortsIfNeeded(previewPath);
            if (!ReferenceEquals(croppedPath, previewPath) && croppedPath != previewPath)
            {
                logger.ThumbnailCropped(croppedPath);
            }

            return croppedPath;
        }
        catch (Exception ex)
        {
            logger.ThumbnailCropFailed(ex);
            return previewPath;
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
        var authStatePath = GetAuthStatePath(settings);
        if (!File.Exists(authStatePath))
        {
            throw new FileNotFoundException($"Файл аутентификации VK Video не найден: {authStatePath}. Выполните авторизацию.", authStatePath);
        }

        var lastWriteUtc = File.GetLastWriteTimeUtc(authStatePath);

        if (_serviceCache.TryGetValue(authStatePath, out var cached) && cached.LastWriteUtc == lastWriteUtc)
        {
            return cached;
        }

        await _cacheLock.WaitAsync(cancellationToken);

        try
        {
            if (_serviceCache.TryGetValue(authStatePath, out cached) && cached.LastWriteUtc == lastWriteUtc)
            {
                return cached;
            }

            var service = await BuildServiceAsync(authStatePath, cancellationToken);
            var entry = new CachedEntry(service, lastWriteUtc);
            _serviceCache[authStatePath] = entry;
            cached?.MarkEvicted();
            return entry;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<VkVideoService> BuildServiceAsync(
        string authStatePath,
        CancellationToken cancellationToken)
    {
        logger.ReadingAuthState(authStatePath);

        var authStateBody = await File.ReadAllTextAsync(authStatePath, cancellationToken);
        using var authState = JsonDocument.Parse(authStateBody);
        var cookies = authState.RootElement.GetProperty("cookies");

        var cookieBuilder = new StringBuilder();

        foreach (var cookie in cookies.EnumerateArray())
        {
            var name = cookie.GetProperty("name").GetString()!;
            var value = cookie.GetProperty("value").GetString()!;
            var domain = cookie.GetProperty("domain").GetString()!;

            if (domain.Contains("vkvideo.ru") || domain.Contains("vk.com"))
            {
                if (cookieBuilder.Length > 0)
                {
                    cookieBuilder.Append("; ");
                }

                cookieBuilder.Append(name).Append('=').Append(value);
            }
        }

        return vkVideoServiceFactory.Create(cookieBuilder.ToString());
    }

    private readonly struct ServiceLease(CachedEntry entry) : IDisposable
    {
        public VkVideoService Service => entry.Service;

        public CachedEntry Entry => entry;

        public void Dispose()
        {
            entry.Release();
        }
    }

    private readonly record struct AuthorInfo(string? Name, string? AvatarUrl);

    private sealed record ExternalVideoId(long OwnerId, long VideoId);

    private sealed class CachedEntry(VkVideoService service, DateTime lastWriteUtc)
    {
        private static readonly TimeSpan AuthorsTtl = TimeSpan.FromMinutes(10);

        private readonly object _gate = new();
        private readonly SemaphoreSlim _authorsLock = new(1, 1);
        private int _refCount;
        private bool _evicted;
        private (DateTime FetchedAt, IReadOnlyList<CommentAuthorOption> Authors)? _authorsSnapshot;

        public VkVideoService Service { get; } = service;

        public DateTime LastWriteUtc { get; } = lastWriteUtc;

        public async Task<IReadOnlyList<CommentAuthorOption>> GetOrFetchAuthorsAsync(
            Func<CancellationToken, Task<IReadOnlyList<CommentAuthorOption>>> fetcher,
            CancellationToken cancellationToken)
        {
            var snapshot = _authorsSnapshot;
            if (snapshot.HasValue && DateTime.UtcNow - snapshot.Value.FetchedAt < AuthorsTtl)
            {
                return snapshot.Value.Authors;
            }

            await _authorsLock.WaitAsync(cancellationToken);
            try
            {
                snapshot = _authorsSnapshot;
                if (snapshot.HasValue && DateTime.UtcNow - snapshot.Value.FetchedAt < AuthorsTtl)
                {
                    return snapshot.Value.Authors;
                }

                var fresh = await fetcher(cancellationToken);
                _authorsSnapshot = (DateTime.UtcNow, fresh);
                return fresh;
            }
            finally
            {
                _authorsLock.Release();
            }
        }

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
