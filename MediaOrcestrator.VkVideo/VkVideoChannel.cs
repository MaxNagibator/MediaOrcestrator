using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MediaOrcestrator.VkVideo;

public sealed class VkVideoChannel(
    ILogger<VkVideoChannel> logger,
    ILogger<VkVideoService> serviceLogger,
    VideoTranscoder videoTranscoder)
    : ISourceType,
        IAuthenticatable,
        ISupportsComments,
        ISupportsCommentPermalinks,
        ISupportsCommentMutations,
        ISupportsCommentLikes
{
    private readonly SemaphoreSlim _serviceLock = new(1, 1);
    private readonly Dictionary<string, VkVideoService> _cachedServices = new(StringComparer.OrdinalIgnoreCase);

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

    public Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        return Task.FromResult<List<SettingOption>>([]);
    }

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
    {
        return new($"https://vkvideo.ru/video{externalId}");
    }

    public Uri? GetCommentExternalUri(
        string externalMediaId,
        string externalCommentId,
        string? rootExternalCommentId,
        Dictionary<string, string> settings)
    {
        if (string.IsNullOrEmpty(externalMediaId) || string.IsNullOrEmpty(externalCommentId))
        {
            return null;
        }

        var query = string.IsNullOrEmpty(rootExternalCommentId)
            ? $"?reply={externalCommentId}"
            : $"?thread={rootExternalCommentId}&reply={externalCommentId}";

        return new($"https://vkvideo.ru/video{externalMediaId}{query}");
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var service = await CreateServiceAsync(settings);
        var groupId = ParseGroupId(settings["group_id"]);
        await foreach (var video in service.GetMediaAsync(groupId, cancellationToken))
        {
            yield return CreateMediaDto(video);
        }
    }

    public async Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Получение деталей видео {ExternalId}", externalId);
        var service = await CreateServiceAsync(settings);
        var (ownerId, videoId) = ParseExternalId(externalId);

        var video = await service.GetVideoByIdAsync(ownerId, videoId);
        return video != null ? CreateMediaDto(video) : null;
    }

    public async IAsyncEnumerable<CommentDto> GetCommentsAsync(
        string externalId,
        Dictionary<string, string> settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (ownerId, videoId) = ParseExternalId(externalId);
        var service = await CreateServiceAsync(settings);

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

    public async Task<CommentDto> CreateCommentAsync(
        string externalMediaId,
        string? parentExternalCommentId,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, videoId) = ParseExternalId(externalMediaId);
        var service = await CreateServiceAsync(settings);

        long? parentId = null;
        if (!string.IsNullOrEmpty(parentExternalCommentId)
            && long.TryParse(parentExternalCommentId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            parentId = parsed;
        }

        var newCommentId = await service.CreateVideoCommentAsync(ownerId, videoId, parentId, text);
        cancellationToken.ThrowIfCancellationRequested();

        return await FetchSingleCommentAsync(service, ownerId, newCommentId, parentId);
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

        var service = await CreateServiceAsync(settings);
        await service.EditVideoCommentAsync(ownerId, commentId, text);
        cancellationToken.ThrowIfCancellationRequested();

        return await FetchSingleCommentAsync(service, ownerId, commentId, null);
    }

    public async Task DeleteCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        var service = await CreateServiceAsync(settings);
        await service.DeleteVideoCommentAsync(ownerId, commentId);
    }

    public async Task RestoreCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        var service = await CreateServiceAsync(settings);
        await service.RestoreVideoCommentAsync(ownerId, commentId);
    }

    public async Task<int> LikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        var service = await CreateServiceAsync(settings);
        return await service.LikeVideoCommentAsync(ownerId, commentId);
    }

    public async Task<int> UnlikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        var (ownerId, _) = ParseExternalId(externalMediaId);
        var commentId = long.Parse(externalCommentId, CultureInfo.InvariantCulture);

        var service = await CreateServiceAsync(settings);
        return await service.UnlikeVideoCommentAsync(ownerId, commentId);
    }

    // TODO: Переделать нормально
    public async Task<MediaDto> DownloadAsync(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Скачивание видео {VideoId}", videoId);
        var service = await CreateServiceAsync(settings);
        var (ownerId, vid) = ParseExternalId(videoId);

        var video = await service.GetVideoByIdAsync(ownerId, vid)
                    ?? throw new InvalidOperationException($"Видео {videoId} не найдено");

        var downloadUrl = video.Files?.GetBestQualityUrl()
                          ?? throw new InvalidOperationException($"Нет доступных URL для скачивания видео {videoId}");

        var tempPath = settings["_system_temp_path"];
        var guid = Guid.NewGuid().ToString();
        var tempVideoPath = Path.Combine(tempPath, guid, "media.mp4");
        var tempPreviewPath = Path.Combine(tempPath, guid, "preview.jpg");

        Directory.CreateDirectory(Path.Combine(tempPath, guid));

        //logger.LogInformation("Скачивание видео из {Url}", downloadUrl[..Math.Min(80, downloadUrl.Length)] + "...");
        logger.LogInformation("Скачивание видео из {Url}", downloadUrl);
        var downloadBytesPerSecond = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);

        await using (var videoStream = await service.HttpClient.GetStreamAsync(downloadUrl, cancellationToken))
        {
            await using (var throttled = new ThrottledStream(videoStream, downloadBytesPerSecond))
            {
                await using (var fileStream = File.Create(tempVideoPath))
                {
                    await throttled.CopyToAsync(fileStream, cancellationToken);
                }
            }
        }

        logger.LogInformation("Видео сохранено: {Path} ({Size} байт)", tempVideoPath, new FileInfo(tempVideoPath).Length);

        var previewUrl = video.Image
            .OrderByDescending(i => i.Width)
            .FirstOrDefault()
            ?.Url;

        if (previewUrl != null)
        {
            // TODO: Не комильфо снаружи HttpClient тягать
            await using var previewStream = await service.HttpClient.GetStreamAsync(previewUrl, cancellationToken);
            await using var previewFile = File.Create(tempPreviewPath);
            await previewStream.CopyToAsync(previewFile, cancellationToken);
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

    public async Task<UploadResult> UploadAsync(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Загрузка видео на VK Video. Название: '{Title}'", media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        var frameSize = await videoTranscoder.GetVideoFrameSizeAsync(filePath, cancellationToken)
                        ?? throw new NonRetriableException("Не удалось определить размер кадра видео — невозможно выбрать режим загрузки (обычное / shorts)");

        // todo поидее ещё и размер фаила надо проверять, но после первого инцидента будем
        var isShorts = frameSize.IsPortrait;

        var service = await CreateServiceAsync(settings);
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
                logger.LogInformation("Отложенная публикация на {PublishAt}", publishAt.Value);
            }
        }

        try
        {
            var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);
            var uploadProgress = UploadProgressLogger.CreateBucketed(logger, media.Id);
            var previewPath = PreparePreviewForUpload(media.TempPreviewPath, isShorts);
            var result = await service.UploadVideoAsync(isShorts, groupId, filePath, media.Title, media.Description,
                fileExt, previewPath, publishAtUnix, uploadBytesPerSecond, uploadProgress, cancellationToken);

            var externalId = $"{result.OwnerId}_{result.Id}";
            logger.LogInformation("Видео загружено на VK Video. ID: {ExternalId}", externalId);

            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки видео на VK Video");
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = ex.Message,
            };
        }
    }

    public async Task<UploadResult> UpdateAsync(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        logger.LogInformation("Обновление видео {ExternalId} на VK Video", externalId);

        var service = await CreateServiceAsync(settings);
        var (ownerId, videoId) = ParseExternalId(externalId);

        var errorMessage = "";
        SaveThumbResponse? thumbResult = null;
        var isShorts = false;

        if (!string.IsNullOrEmpty(tempMedia.TempPreviewPath) && File.Exists(tempMedia.TempPreviewPath))
        {
            try
            {
                isShorts = await ResolveIsShortsAsync(tempMedia, service, ownerId, videoId, cancellationToken);
                var previewPath = PreparePreviewForUpload(tempMedia.TempPreviewPath, isShorts);
                thumbResult = await service.UploadThumbnailAsync(isShorts, ownerId, videoId, previewPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка загрузки превью");
                errorMessage += "Ошибка загрузки превью";
            }
        }

        try
        {
            await service.EditVideoAsync(ownerId, videoId, tempMedia.Title, tempMedia.Description, isShorts ? null : thumbResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка редактирования метаданных");
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

    public async Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Удаление видео {ExternalId} из VK Video", externalId);

        var service = await CreateServiceAsync(settings);
        var (ownerId, videoId) = ParseExternalId(externalId);

        await service.DeleteVideoAsync(ownerId, videoId);

        logger.LogInformation("Видео {ExternalId} удалено", externalId);
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

    public async Task AuthenticateAsync(Dictionary<string, string> settings, IAuthUI ui, CancellationToken ct)
    {
        var authStatePath = GetAuthStatePath(settings);
        var result = await ui.OpenBrowserAsync("https://cabinet.vkvideo.ru/", authStatePath);
        if (result != null)
        {
            await InvalidateServiceAsync(authStatePath);
            logger.LogInformation("VK Video: авторизация сохранена в {Path}", result);
            await ui.ShowMessageAsync("Авторизация VK Video сохранена!");
        }
    }

    public ConvertType[] GetAvailableConvertTypes()
    {
        return [];
    }

    public Task ConvertAsync(int typeId, string externalId, Dictionary<string, string> settings, IProgress<ConvertProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private static async Task<CommentDto> FetchSingleCommentAsync(
        VkVideoService service,
        long ownerId,
        long commentId,
        long? parentCommentId)
    {
        var response = await service.GetVideoCommentAsync(ownerId, commentId);

        var item = response.Items.FirstOrDefault()
                   ?? throw new InvalidOperationException($"VK не вернул комментарий {commentId} после операции");

        var authors = new Dictionary<long, AuthorInfo>();
        AppendAuthors(response, authors);

        return MapComment(item, parentCommentId, ownerId, authors);
    }

    private static void AppendAuthors(VkCommentsResponse response, Dictionary<long, AuthorInfo> authors)
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
                parentCommentId);

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

    private static CommentDto MapComment(VkCommentItem item, long? parentCommentId, long ownerId, Dictionary<long, AuthorInfo> authors)
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

    private string PreparePreviewForUpload(string previewPath, bool isShorts)
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
                logger.LogInformation("Превью обрезано под клип VK: {CroppedPath}", croppedPath);
            }

            return croppedPath;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось обрезать превью под вертикальный формат, используется оригинал");
            return previewPath;
        }
    }

    private async Task<bool> ResolveIsShortsAsync(
        MediaDto tempMedia,
        VkVideoService service,
        long ownerId,
        long videoId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tempMedia.TempDataPath) && File.Exists(tempMedia.TempDataPath))
        {
            var frameSize = await videoTranscoder.GetVideoFrameSizeAsync(tempMedia.TempDataPath, cancellationToken);
            if (frameSize is (> 0, > 0))
            {
                return frameSize.Value.IsPortrait;
            }
        }

        var video = await service.GetVideoByIdAsync(ownerId, videoId)
                    ?? throw new NonRetriableException("Не удалось получить видео из VK для определения ориентации (обычное / shorts)");

        if (video.Width <= 0 || video.Height <= 0)
        {
            throw new NonRetriableException("VK не вернул размер кадра видео — невозможно выбрать режим обновления превью (обычное / shorts)");
        }

        return video.Width < video.Height;
    }

    private async Task<VkVideoService> CreateServiceAsync(Dictionary<string, string> settings)
    {
        var authStatePath = GetAuthStatePath(settings);

        await _serviceLock.WaitAsync();
        try
        {
            if (_cachedServices.TryGetValue(authStatePath, out var cached))
            {
                return cached;
            }

            if (!File.Exists(authStatePath))
            {
                throw new FileNotFoundException($"Файл аутентификации VK Video не найден: {authStatePath}. Выполните авторизацию.", authStatePath);
            }

            var authStateBody = await File.ReadAllTextAsync(authStatePath);
            using var authState = JsonDocument.Parse(authStateBody);
            var cookies = authState.RootElement.GetProperty("cookies");

            var cookiePairs = new List<string>();

            foreach (var cookie in cookies.EnumerateArray())
            {
                var name = cookie.GetProperty("name").GetString()!;
                var value = cookie.GetProperty("value").GetString()!;
                var domain = cookie.GetProperty("domain").GetString()!;

                if (domain.Contains("vkvideo.ru") || domain.Contains("vk.com"))
                {
                    cookiePairs.Add($"{name}={value}");
                }
            }

            var service = new VkVideoService(string.Join("; ", cookiePairs), serviceLogger);
            _cachedServices[authStatePath] = service;
            return service;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    private async Task InvalidateServiceAsync(string authStatePath)
    {
        await _serviceLock.WaitAsync();
        try
        {
            if (_cachedServices.Remove(authStatePath, out var existing))
            {
                existing.Dispose();
            }
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    private readonly record struct AuthorInfo(string? Name, string? AvatarUrl);

    private sealed record ExternalVideoId(long OwnerId, long VideoId);
}
