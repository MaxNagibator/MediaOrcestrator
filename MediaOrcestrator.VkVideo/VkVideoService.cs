using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MediaOrcestrator.VkVideo;

public sealed class VkVideoService : IDisposable
{
    private const string ApiBase = "https://api.vkvideo.ru/method/";
    private const string ApiVkBase = "https://api.vk.com/method/";
    private const string ApiVersion = "5.275";
    private const string ClientId = "52461373";
    private const string OwnerIdKey = "owner_id";
    private const string VideoIdKey = "video_id";

    private const int MinRequestIntervalMs = 350;
    private const int RateLimitMaxRetries = 4;

    private readonly ILogger _logger;
    private readonly string _cookieString;
    private readonly SemaphoreSlim _throttleLock = new(1, 1);

    private DateTimeOffset _nextAllowedCallAt = DateTimeOffset.MinValue;
    private string? _accessToken;
    private DateTimeOffset _tokenExpires = DateTimeOffset.MinValue;

    public VkVideoService(string cookieString, ILogger logger)
    {
        _logger = logger;
        _cookieString = cookieString;

        var handler = new HttpClientHandler { UseCookies = false };
        HttpClient = new(handler);
        HttpClient.Timeout = TimeSpan.FromHours(8);
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
    }

    public HttpClient HttpClient { get; }

    public void Dispose()
    {
        HttpClient.Dispose();
        _throttleLock.Dispose();
    }

    public async IAsyncEnumerable<VideoItem> GetMediaAsync(
        long groupId,
        [EnumeratorCancellation] CancellationToken cancel = default)
    {
        _logger.LogInformation("Получение списка медиа для VkVideo");

        var firstPage = await GetCatalogFirstPageAsync(groupId);
        var videos = GetCatalogVideosAsync(firstPage, cancel);
        var clips = GetCatalogClipsAsync(firstPage, cancel);
        var yieldedIds = new HashSet<string>();

        await foreach (var video in MergeByDateDescending(videos, clips, cancel))
        {
            if (yieldedIds.Add(video.GetVideoKey()))
            {
                yield return video;
            }
        }
    }

    public async Task<VideoItem?> GetVideoByIdAsync(long ownerId, long videoId)
    {
        var videoKey = $"{ownerId}_{videoId}";
        _logger.LogDebug("Запрос деталей видео: {VideoKey}", videoKey);

        var response = await CallVkApiAsync<VideoGetByIdsResponse>("video.getByIds", new()
        {
            ["videos"] = videoKey,
            ["video_fields"] = "files,image,subtitles",
        });

        return response.Items.Count > 0 ? response.Items[0] : null;
    }

    public async Task EditVideoAsync(long ownerId, long videoId, string title, string description, SaveThumbResponse? thumb = null)
    {
        _logger.LogInformation("Редактирование видео {OwnerId}_{VideoId}", ownerId, videoId);

        var parameters = new Dictionary<string, string>
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
            ["name"] = title,
            ["desc"] = description,
        };

        if (thumb != null)
        {
            parameters["thumb_id"] = thumb.PhotoId + "_" + thumb.PhotoOwnerId;
            parameters["thumb_hash"] = thumb.PhotoHash;
        }

        var result = await CallApiAsync<EditResponse>("video.edit", parameters);

        if (result.Success != 1)
        {
            throw new InvalidOperationException($"Ошибка редактирования видео {ownerId}_{videoId}");
        }

        _logger.LogInformation("Видео {OwnerId}_{VideoId} успешно отредактировано", ownerId, videoId);
    }

    public async Task DeleteVideoAsync(long ownerId, long videoId)
    {
        _logger.LogInformation("Удаление видео {OwnerId}_{VideoId}", ownerId, videoId);

        // TODO: https://github.com/MaxNagibator/MediaOrcestrator/issues/37#issuecomment-4205897816
        // await CallApiAsync<int>("video.delete", new()
        // {
        //     [OwnerIdKey] = ownerId.ToString(),
        //     [VideoIdKey] = videoId.ToString(),
        // });

        await CallApiAsync<JsonElement>("video.bulkEdit", new()
        {
            ["video_ids"] = $"{ownerId}_{videoId}",
            [OwnerIdKey] = ownerId.ToString(),
            ["action"] = "delete",
        });

        _logger.LogInformation("Видео {OwnerId}_{VideoId} удалено", ownerId, videoId);
    }

    public async Task<string> GetThumbUploadUrlAsync(long ownerId, long videoId)
    {
        var response = await CallApiAsync<VideoForEditResponse>("video.getVideoForEdit", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
        });

        return response.Item.ThumbUploadUrl
               ?? throw new InvalidOperationException("thumb_upload_url не получен");
    }

    public Task<SaveThumbResponse> UploadThumbnailAsync(bool isShorts, long ownerId, long videoId, string thumbnailPath)
    {
        return isShorts
            ? UploadShortVideoThumbnailAsync(ownerId, videoId, thumbnailPath)
            : UploadVideoThumbnailAsync(ownerId, videoId, thumbnailPath);
    }

    public async Task<PublishVideoResponse> UploadVideoAsync(
        bool isShorts,
        long groupId,
        string filePath,
        string title,
        string description,
        string fileExtension,
        string? thumbnailPath = null,
        long? publishAt = null,
        long? uploadBytesPerSecond = null,
        IProgress<double>? uploadProgress = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        _logger.LogInformation("Шаг 1/3: Резервирование слота для '{Title}'", title);

        VideoSaveResponse saveResponse;
        if (isShorts)
        {
            saveResponse = await CallApiAsync<VideoSaveResponse>("shortVideo.create", new()
            {
                ["group_id"] = groupId.ToString(),
                ["file_size"] = fileInfo.Length.ToString(),
            });
        }
        else
        {
            saveResponse = await CallApiAsync<VideoSaveResponse>("video.save", new()
            {
                ["group_id"] = groupId.ToString(),
                ["source_file_name"] = fileInfo.Name,
                ["file_size"] = fileInfo.Length.ToString(),
                ["batch_id"] = $"-{groupId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                ["preview"] = "1",
                ["thumb_upload"] = "1",
            });
        }

        _logger.LogInformation("Слот зарезервирован. VideoId: {VideoId}, OwnerId: {OwnerId}", saveResponse.VideoId, saveResponse.OwnerId);

        _logger.LogInformation("Шаг 2/3: Загрузка файла ({Size} байт)", fileInfo.Length);

        var uploadResponse = await UploadFileAsync(saveResponse.UploadUrl, filePath, fileInfo, uploadBytesPerSecond, uploadProgress, cancellationToken);

        _logger.LogInformation("Файл загружен. Hash: {Hash}", uploadResponse.VideoHash);

        if (isShorts)
        {
            await WaitForShortVideoEncodingAsync(saveResponse.OwnerId, saveResponse.VideoId, uploadResponse.VideoHash, cancellationToken);
        }

        SaveThumbResponse? thumbId = null;
        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
        {
            try
            {
                _logger.LogInformation("Загрузка превью");
                thumbId = await UploadThumbnailAsync(isShorts, saveResponse.OwnerId, saveResponse.VideoId, thumbnailPath);
                _logger.LogInformation("Превью загружено");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка загрузки превью, публикация продолжится без него");
            }
        }

        _logger.LogInformation("Шаг 3/3: Публикация");

        if (isShorts)
        {
            var editParams = new Dictionary<string, string>
            {
                [OwnerIdKey] = saveResponse.OwnerId.ToString(),
                [VideoIdKey] = saveResponse.VideoId.ToString(),
                ["description"] = title,
                ["privacy_view"] = "all",
                ["can_make_duet"] = "1",
                ["privacy_comment"] = "all",
                ["audio_raw_id"] = "",
                ["ord_info"] = "{\"is_ads\":false,\"advertisers\":[]}",
            };

            if (thumbId != null)
            {
                editParams["thumb_id"] = thumbId.PhotoId.ToString();
            }

            await CallApiAsync<PublishResponse>("shortVideo.edit", editParams);
            _logger.LogInformation("Видео отредактировано перед публикацией");

            if (publishAt.HasValue)
            {
                _logger.LogWarning("Отложенная публикация не поддерживается для shorts — видео будет опубликовано немедленно");
            }

            var publishParams = new Dictionary<string, string>
            {
                [OwnerIdKey] = saveResponse.OwnerId.ToString(),
                [VideoIdKey] = saveResponse.VideoId.ToString(),
                ["wallpost"] = "0",
                ["publish_date"] = "0",
                ["license_agree"] = "1",
                ["ref"] = "video_as_clip_video_upload",
            };

            var publishResponse = await CallApiAsync<PublishResponse>("shortVideo.publish", publishParams);
            _logger.LogInformation("Видео опубликовано: {Url}", publishResponse.Video?.DirectUrl);
            return publishResponse.Video
                   ?? throw new InvalidOperationException("Ответ shortVideo.publish не содержит объект video");
        }
        else
        {
            var publishParams = new Dictionary<string, string>
            {
                ["owner_id"] = saveResponse.OwnerId.ToString(),
                ["video_id"] = saveResponse.VideoId.ToString(),
                ["title"] = title,
                ["description"] = description,
                ["file_ext"] = fileExtension,
                ["repeat"] = "0",
                ["hide_auto_subs"] = "0",
                ["add_to_wall"] = "0",
                ["check_content_id"] = "0",
            };

            if (thumbId != null)
            {
                publishParams["thumb_id"] = thumbId.PhotoId + "_" + thumbId.PhotoOwnerId;
                publishParams["thumb_hash"] = thumbId.PhotoHash;
            }

            if (publishAt.HasValue)
            {
                publishParams["publish_at"] = publishAt.Value.ToString();
            }

            var publishResponse = await CallApiAsync<PublishResponse>("video.publish", publishParams);
            _logger.LogInformation("Видео опубликовано: {Url}", publishResponse.Video?.DirectUrl);

            return publishResponse.Video
                   ?? throw new InvalidOperationException("Ответ video.publish не содержит объект video");
        }
    }

    public Task<VkCommentsResponse> GetVideoCommentsAsync(
        long ownerId,
        long videoId,
        int count = 100,
        int offset = 0,
        long? commentId = null,
        string sort = "asc")
    {
        var parameters = new Dictionary<string, string>
        {
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["video_id"] = videoId.ToString(CultureInfo.InvariantCulture),
            ["count"] = count.ToString(CultureInfo.InvariantCulture),
            ["offset"] = offset.ToString(CultureInfo.InvariantCulture),
            ["sort"] = sort,
            ["need_likes"] = "1",
            ["extended"] = "1",
            ["fields"] = "photo_100,first_name_dat",
        };

        if (commentId.HasValue)
        {
            parameters["comment_id"] = commentId.Value.ToString(CultureInfo.InvariantCulture);
        }

        return CallApiInternalAsync<VkCommentsResponse>(ApiBase, "video.getComments", parameters);
    }

    public Task<VkCommentsResponse> GetVideoCommentAsync(long ownerId, long commentId)
    {
        return CallApiAsync<VkCommentsResponse>("video.getComment", new()
        {
            ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["extended"] = "1",
            ["fields"] = "photo_100,first_name_dat",
        });
    }

    public Task<long> CreateVideoCommentAsync(
        long ownerId,
        long videoId,
        long? replyToComment,
        string message)
    {
        var parameters = new Dictionary<string, string>
        {
            ["video_id"] = videoId.ToString(CultureInfo.InvariantCulture),
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["from_group"] = "0",
            ["reply_to_comment"] = replyToComment?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["access_key"] = string.Empty,
            ["from_owner_id"] = string.Empty,
            ["message"] = message,
            ["attachments"] = string.Empty,
        };

        return CallApiAsync<long>("video.createComment", parameters);
    }

    public Task EditVideoCommentAsync(long ownerId, long commentId, string message)
    {
        return CallApiAsync<int>("video.editComment", new()
        {
            ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["message"] = message,
            ["attachments"] = string.Empty,
        });
    }

    public Task DeleteVideoCommentAsync(long ownerId, long commentId)
    {
        return CallApiAsync<int>("video.deleteComment", new()
        {
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
        });
    }

    public Task RestoreVideoCommentAsync(long ownerId, long commentId)
    {
        return CallApiAsync<int>("video.restoreComment", new()
        {
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
        });
    }

    public async Task<int> LikeVideoCommentAsync(long ownerId, long commentId)
    {
        var response = await CallApiAsync<VkLikesAddResponse>("likes.add", new()
        {
            ["type"] = "video_comment",
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["item_id"] = commentId.ToString(CultureInfo.InvariantCulture),
            ["from_group"] = string.Empty,
            ["track_code"] = string.Empty,
            ["ref"] = string.Empty,
        });

        return response.Likes;
    }

    public async Task<int> UnlikeVideoCommentAsync(long ownerId, long commentId)
    {
        var response = await CallApiAsync<VkLikesAddResponse>("likes.delete", new()
        {
            ["type"] = "video_comment",
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["item_id"] = commentId.ToString(CultureInfo.InvariantCulture),
            ["from_group"] = string.Empty,
            ["track_code"] = string.Empty,
            ["ref"] = string.Empty,
        });

        return response.Likes;
    }

    private static MultipartFormDataContent BuildThumbnailForm(string thumbnailPath)
    {
        var form = new MultipartFormDataContent();
        var fileStream = File.OpenRead(thumbnailPath);
        var fileContent = new StreamContent(fileStream);

        var contentType = Path.GetExtension(thumbnailPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "image/jpeg",
        };

        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "photo", Path.GetFileName(thumbnailPath));
        return form;
    }

    private static bool IsHtmlResponse(string body)
    {
        return string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith('<');
    }

    private static CatalogSectionsResult FindMediaSections(List<CatalogSection>? sections)
    {
        if (sections == null)
        {
            return new(null, null, null);
        }

        string? videoSectionId = null;
        string? videoNextFrom = null;
        string? clipsSectionId = null;

        foreach (var section in sections)
        {
            if (IsClipsSection(section))
            {
                clipsSectionId ??= section.Id;
            }

            if (videoSectionId == null && IsVideosSection(section))
            {
                videoSectionId = section.Id;
                videoNextFrom = FindSectionNextFrom(section);
            }
        }

        if (videoSectionId == null)
        {
            foreach (var section in sections)
            {
                if (IsClipsSection(section))
                {
                    continue;
                }

                var nextFrom = FindSectionNextFrom(section);
                if (nextFrom == null)
                {
                    continue;
                }

                videoSectionId = section.Id;
                videoNextFrom = nextFrom;
                break;
            }
        }

        return new(videoSectionId, videoNextFrom, clipsSectionId);
    }

    private static bool IsVideosSection(CatalogSection section)
    {
        return string.Equals(section.Title, "Видео", StringComparison.OrdinalIgnoreCase)
               || string.Equals(section.Title, "Videos", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClipsSection(CatalogSection section)
    {
        return string.Equals(section.Title, "Клипы", StringComparison.OrdinalIgnoreCase)
               || string.Equals(section.Title, "Clips", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindSectionNextFrom(CatalogSection? section)
    {
        return section?.NextFrom ?? FindVideosBlockNextFrom(section?.Blocks);
    }

    private static string? FindVideosBlockNextFrom(List<CatalogBlock>? blocks)
    {
        if (blocks == null)
        {
            return null;
        }

        foreach (var block in blocks)
        {
            if (block.DataType == "videos")
            {
                return block.NextFrom;
            }
        }

        return null;
    }

    private static async IAsyncEnumerable<VideoItem> MergeByDateDescending(
        IAsyncEnumerable<VideoItem> videos,
        IAsyncEnumerable<VideoItem> clips,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var videoEnumerator = videos.GetAsyncEnumerator(cancellationToken);
        await using var clipEnumerator = clips.GetAsyncEnumerator(cancellationToken);

        var hasVideo = await videoEnumerator.MoveNextAsync();
        var hasClip = await clipEnumerator.MoveNextAsync();

        while (hasVideo || hasClip)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!hasClip || hasVideo && IsSameOrNewer(videoEnumerator.Current, clipEnumerator.Current))
            {
                yield return videoEnumerator.Current;

                hasVideo = await videoEnumerator.MoveNextAsync();
            }
            else
            {
                yield return clipEnumerator.Current;

                hasClip = await clipEnumerator.MoveNextAsync();
            }
        }
    }

    private static bool IsSameOrNewer(VideoItem left, VideoItem right)
    {
        var dateComparison = left.Date.CompareTo(right.Date);
        if (dateComparison != 0)
        {
            return dateComparison > 0;
        }

        var idComparison = left.Id.CompareTo(right.Id);
        if (idComparison != 0)
        {
            return idComparison >= 0;
        }

        return left.OwnerId.CompareTo(right.OwnerId) >= 0;
    }

    private async Task<string> GetShortVideoThumbUploadUrlAsync(long ownerId)
    {
        var response = await CallApiAsync<ShortVideoThumbUploadUrlResponse>("shortVideo.getThumbUploadUrl", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
        });

        return string.IsNullOrEmpty(response.UploadUrl)
            ? throw new InvalidOperationException("upload_url для shorts превью не получен")
            : response.UploadUrl;
    }

    private async Task<SaveThumbResponse> UploadShortVideoThumbnailAsync(long ownerId, long videoId, string thumbnailPath)
    {
        var thumbUploadUrl = await GetShortVideoThumbUploadUrlAsync(ownerId);

        _logger.LogInformation("Загрузка превью для shorts {OwnerId}_{VideoId}", ownerId, videoId);

        using var form = BuildThumbnailForm(thumbnailPath);
        var uploadBody = await PostThumbnailAsync(thumbUploadUrl, form);

        var saveResult = await CallApiAsync<SaveThumbResponse>("shortVideo.saveUploadedThumb", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
            ["thumb_json"] = uploadBody,
        });

        _logger.LogInformation("Превью shorts загружено. PhotoId: {PhotoId}", saveResult.PhotoId);
        return saveResult;
    }

    private async Task<SaveThumbResponse> UploadVideoThumbnailAsync(long ownerId, long videoId, string thumbnailPath)
    {
        var thumbUploadUrl = await GetThumbUploadUrlAsync(ownerId, videoId);

        _logger.LogInformation("Загрузка превью для видео {OwnerId}_{VideoId}", ownerId, videoId);

        using var form = BuildThumbnailForm(thumbnailPath);
        var uploadUrl = thumbUploadUrl + "&ajx=1";
        var uploadBody = await PostThumbnailAsync(uploadUrl, form);

        var saveResult = await CallApiAsync<SaveThumbResponse>("video.saveUploadedThumb", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
            ["thumb_json"] = uploadBody,
            ["thumb_size"] = "l",
        });

        _logger.LogInformation("Превью загружено. PhotoId: {PhotoId}", saveResult.PhotoId);
        return saveResult;
    }

    private async Task<string> PostThumbnailAsync(string uploadUrl, MultipartFormDataContent form)
    {
        var uploadResponse = await HttpClient.PostAsync(uploadUrl, form);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();

        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ошибка загрузки превью: {uploadResponse.StatusCode}");
        }

        return uploadBody;
    }

    private async Task WaitForShortVideoEncodingAsync(long ownerId, long videoId, string hash, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ожидание обработки shorts {OwnerId}_{VideoId}", ownerId, videoId);

        var pollInterval = TimeSpan.FromSeconds(2);
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10);
        var lastLoggedBucket = -1;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progress = await CallApiAsync<ShortVideoEncodeProgressResponse>("shortVideo.encodeProgress", new()
            {
                [VideoIdKey] = videoId.ToString(),
                [OwnerIdKey] = ownerId.ToString(),
                ["hash"] = hash,
            });

            if (progress.IsReady)
            {
                _logger.LogInformation("Shorts {OwnerId}_{VideoId} готов к публикации ({Percents}%)", ownerId, videoId, progress.Percents);
                return;
            }

            var bucket = progress.Percents / 10;
            if (bucket > lastLoggedBucket)
            {
                _logger.LogInformation("Прогресс кодирования shorts {OwnerId}_{VideoId}: {Percents}%", ownerId, videoId, progress.Percents);
                lastLoggedBucket = bucket;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        throw new TimeoutException($"Shorts {ownerId}_{videoId} не обработался за отведённое время");
    }

    private async IAsyncEnumerable<VideoItem> GetCatalogSectionVideosAsync(
        string sectionId,
        string? nextFrom,
        bool fetchFirstPage,
        HashSet<string> yieldedIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (fetchFirstPage)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var firstPage = await GetCatalogSectionFirstPageAsync(sectionId);

            foreach (var video in firstPage.Videos)
            {
                if (yieldedIds.Add(video.GetVideoKey()))
                {
                    yield return video;
                }
            }

            nextFrom = firstPage.NextFrom;
        }

        while (nextFrom != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previousNextFrom = nextFrom;
            var nextPage = await GetCatalogNextPageAsync(sectionId, nextFrom);

            foreach (var video in nextPage.Videos)
            {
                if (yieldedIds.Add(video.GetVideoKey()))
                {
                    yield return video;
                }
            }

            nextFrom = nextPage.NextFrom != previousNextFrom ? nextPage.NextFrom : null;
        }
    }

    private async IAsyncEnumerable<VideoItem> GetCatalogVideosAsync(
        CatalogFirstPageResult firstPage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var yieldedIds = new HashSet<string>();

        foreach (var video in firstPage.Videos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (yieldedIds.Add(video.GetVideoKey()))
            {
                yield return video;
            }
        }

        if (firstPage is { VideoSectionId: not null, VideoNextFrom: not null })
        {
            var videos = GetCatalogSectionVideosAsync(firstPage.VideoSectionId, firstPage.VideoNextFrom, false, yieldedIds, cancellationToken);
            await foreach (var video in videos)
            {
                yield return video;
            }
        }
    }

    private async IAsyncEnumerable<VideoItem> GetCatalogClipsAsync(
        CatalogFirstPageResult firstPage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (firstPage.ClipsSectionId == null)
        {
            yield break;
        }

        _logger.LogInformation("Получение списка клипов для VkVideo");
        var yieldedIds = new HashSet<string>();
        var clips = GetCatalogSectionVideosAsync(firstPage.ClipsSectionId, null, true, yieldedIds, cancellationToken);

        await foreach (var clip in clips)
        {
            yield return clip;
        }
    }

    private async Task<CatalogFirstPageResult> GetCatalogFirstPageAsync(long groupId)
    {
        var screenName = await GetGroupScreenNameAsync(groupId);
        var catalogUrl = $"https://vkvideo.ru/@{Uri.EscapeDataString(screenName)}/all";
        _logger.LogDebug("Запрос каталога: {Url}", catalogUrl);

        CatalogResponse response;

        try
        {
            response = await CallApiAsync<CatalogResponse>("catalog.getVideo", new()
            {
                ["need_blocks"] = "1",
                [OwnerIdKey] = "0",
                ["url"] = catalogUrl,
            });
        }
        catch (VkApiException ex) when (ex.ErrorCode == 104)
        {
            _logger.LogWarning(ex, "Каталог видео пуст или не найден для группы {GroupId}", groupId);
            return new([], null, null, null);
        }

        var sections = FindMediaSections(response.Catalog.Sections);
        return new(response.Videos, sections.VideoSectionId, sections.VideoNextFrom, sections.ClipsSectionId);
    }

    private async Task<CatalogSectionPageResult> GetCatalogSectionFirstPageAsync(string sectionId)
    {
        _logger.LogDebug("Запрос первой страницы секции каталога: section={SectionId}", sectionId);

        var response = await CallApiAsync<CatalogSectionResponse>("catalog.getSection", new()
        {
            ["section_id"] = sectionId,
        });

        return new(response.Videos, FindSectionNextFrom(response.Section));
    }

    private async Task<CatalogSectionPageResult> GetCatalogNextPageAsync(string sectionId, string startFrom)
    {
        _logger.LogDebug("Запрос следующей страницы каталога: section={SectionId}, from={StartFrom}", sectionId, startFrom);

        var response = await CallApiAsync<CatalogSectionResponse>("catalog.getSection", new()
        {
            ["section_id"] = sectionId,
            ["start_from"] = startFrom,
        });

        return new(response.Videos, FindSectionNextFrom(response.Section));
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpires.AddMinutes(-1))
        {
            return _accessToken;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            _logger.LogInformation("Запрос нового access_token через web_token");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://cabinet.vkvideo.ru/al_video.php?act=web_token");
            request.Headers.Add("Cookie", _cookieString);
            request.Headers.Add("Origin", "https://cabinet.vkvideo.ru");
            request.Headers.Add("Referer", "https://cabinet.vkvideo.ru/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["version"] = "1",
                ["app_id"] = ClientId,
            });

            var response = await HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!IsHtmlResponse(body))
            {
                return ParseWebTokenResponse(body, response.StatusCode);
            }

            _logger.LogWarning("Ответ web_token — HTML (попытка {Attempt}/2). Статус: {StatusCode}", attempt + 1, response.StatusCode);

            if (attempt == 0 && await TrySolveChallengeAsync(response, body))
            {
                _logger.LogInformation("Повторяем запрос web_token после решения challenge...");
                continue;
            }

            ThrowHtmlResponseError(body, "web_token");
        }

        throw new InvalidOperationException("Не удалось получить access_token после решения challenge.");
    }

    private Task<T> CallApiAsync<T>(string method, Dictionary<string, string> parameters)
    {
        return CallApiInternalAsync<T>(ApiBase, method, parameters);
    }

    private Task<T> CallVkApiAsync<T>(string method, Dictionary<string, string> parameters)
    {
        return CallApiInternalAsync<T>(ApiVkBase, method, parameters);
    }

    private async Task<string> GetGroupScreenNameAsync(long groupId)
    {
        var response = await CallApiAsync<GroupsGetByIdResponse>("groups.getById", new()
        {
            ["fields"] = "counters",
            ["group_ids"] = groupId.ToString(),
        });

        var group = response.Groups.FirstOrDefault(g => g.Id == groupId) ?? response.Groups.FirstOrDefault();
        var screenName = group?.ScreenName;

        if (string.IsNullOrWhiteSpace(screenName))
        {
            screenName = $"club{groupId}";
        }

        _logger.LogDebug("VK group {GroupId} screen_name={ScreenName}", groupId, screenName);
        return screenName;
    }

    private string ParseWebTokenResponse(string body, HttpStatusCode statusCode)
    {
        if ((int)statusCode >= 400)
        {
            _logger.LogError("Ошибка получения web_token. Статус: {StatusCode}, Ответ: {Body}", statusCode, body);
            throw new HttpRequestException($"Не удалось получить access_token: {statusCode}");
        }

        var tokens = JsonSerializer.Deserialize<List<WebTokenResponse>>(body);
        if (tokens == null || tokens.Count == 0 || string.IsNullOrEmpty(tokens[0].AccessToken))
        {
            throw new InvalidOperationException("Не удалось получить access_token. Возможно, cookies устарели — переавторизуйтесь через Playwright.");
        }

        _accessToken = tokens[0].AccessToken;
        _tokenExpires = DateTimeOffset.FromUnixTimeSeconds(tokens[0].Expires);
        _logger.LogInformation("access_token получен, истекает в {Expires}", _tokenExpires);

        return _accessToken;
    }

    private async Task<T> CallApiInternalAsync<T>(string baseUrl, string method, Dictionary<string, string> parameters)
    {
        var challengeSolved = false;
        var rateLimitHits = 0;

        while (true)
        {
            await ThrottleAsync();
            var token = await GetAccessTokenAsync();

            var requestParams = new Dictionary<string, string>(parameters)
            {
                ["access_token"] = token,
            };

            var url = $"{baseUrl}{method}?v={ApiVersion}&client_id={ClientId}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Origin", "https://cabinet.vkvideo.ru");
            request.Headers.Add("Referer", "https://cabinet.vkvideo.ru/");
            request.Content = new FormUrlEncodedContent(requestParams);

            var response = await HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (IsHtmlResponse(body))
            {
                _logger.LogWarning("Ответ API {Method} — HTML. Статус: {StatusCode}", method, response.StatusCode);

                if (!challengeSolved && await TrySolveChallengeAsync(response, body))
                {
                    challengeSolved = true;
                    _logger.LogInformation("Повторяем запрос API {Method} после решения challenge...", method);
                    continue;
                }

                ThrowHtmlResponseError(body, method);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ошибка API {Method}. Статус: {StatusCode}, Ответ: {Body}", method, response.StatusCode, body);
                throw new HttpRequestException($"Ошибка API {method}: {response.StatusCode}");
            }

            try
            {
                return ParseApiResponse<T>(body, method);
            }
            catch (VkApiException ex) when (ex.ErrorCode == 6 && rateLimitHits < RateLimitMaxRetries)
            {
                rateLimitHits++;
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, rateLimitHits - 1));
                _logger.LogWarning("VK API {Method}: rate limit, попытка {Attempt}/{Max}. Ждём {Delay} мс",
                    method, rateLimitHits, RateLimitMaxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay);
            }
        }
    }

    private async Task ThrottleAsync()
    {
        await _throttleLock.WaitAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var wait = _nextAllowedCallAt - now;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait);
                now = DateTimeOffset.UtcNow;
            }

            _nextAllowedCallAt = now.AddMilliseconds(MinRequestIntervalMs);
        }
        finally
        {
            _throttleLock.Release();
        }
    }

    private T ParseApiResponse<T>(string body, string method)
    {
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("error", out var errorElement))
        {
            var errorCode = errorElement.TryGetProperty("error_code", out var code) ? code.GetInt32() : 0;
            var errorMsg = errorElement.TryGetProperty("error_msg", out var msg) ? msg.GetString() : body;
            _logger.LogError("Ошибка VK API {Method}. Код: {ErrorCode}, Сообщение: {ErrorMsg}", method, errorCode, errorMsg);
            throw new VkApiException(errorCode, errorMsg ?? body, method);
        }

        if (!doc.RootElement.TryGetProperty("response", out var responseElement))
        {
            throw new InvalidOperationException($"Ответ API {method} не содержит поле 'response'");
        }

        return JsonSerializer.Deserialize<T>(responseElement.GetRawText())
               ?? throw new InvalidOperationException($"Не удалось десериализовать ответ API {method}");
    }

    // TODO: Копипаста + нейросетевая дрисня. Дай бог будет работать.

    /// <summary>
    /// Пытается решить VK challenge (rate limit). Извлекает hash429 из redirect URL,
    /// вычисляет key и отправляет решение. Возвращает true если challenge решён.
    /// </summary>
    private async Task<bool> TrySolveChallengeAsync(HttpResponseMessage response, string body)
    {
        if (!VkChallengeSolver.IsChallengePage(body))
        {
            _logger.LogDebug("Ответ — HTML, но не challenge-страница");
            return false;
        }

        var challengeUri = response.RequestMessage?.RequestUri;
        _logger.LogWarning("VK challenge (rate limit) обнаружен. Redirect URI: {Uri}", challengeUri);

        var result = VkChallengeSolver.TrySolve(challengeUri, body);
        if (!result.Success)
        {
            _logger.LogError("Не удалось решить VK challenge: {Error}. URI: {Uri}", result.Error, challengeUri);
            return false;
        }

        _logger.LogInformation("VK challenge решён: hash429={Hash429}, salt={Salt}, key={Key}",
            result.Hash429, result.Salt, result.Key);

        _logger.LogDebug("Отправка решения на {SolveUri}", result.SolveUri);

        using var solveRequest = new HttpRequestMessage(HttpMethod.Get, result.SolveUri);
        solveRequest.Headers.Add("Cookie", _cookieString);
        var solveResponse = await HttpClient.SendAsync(solveRequest);
        var solveBody = await solveResponse.Content.ReadAsStringAsync();

        _logger.LogInformation("Ответ на решение challenge: статус {StatusCode}, длина тела {Length}",
            solveResponse.StatusCode, solveBody.Length);

        if (IsHtmlResponse(solveBody))
        {
            // TODO: Нужно отловить и посмотреть, что там
            _logger.LogWarning("Ответ на решение challenge снова HTML. Начало: {Body}", solveBody.Length > 200 ? solveBody[..200] : solveBody);
            return false;
        }

        return true;
    }

    private async Task<FileUploadResponse> UploadFileAsync(string uploadUrl, string filePath, FileInfo fileInfo, long? uploadBytesPerSecond, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using var throttled = new ThrottledStream(fileStream, uploadBytesPerSecond);
        var byteProgress = progress != null && fileInfo.Length > 0
            ? new Progress<long>(bytes => progress.Report(Math.Min(1.0, (double)bytes / fileInfo.Length)))
            : null;

        await using var stream = new ProgressStream(throttled, byteProgress);
        var content = new StreamContent(stream);

        var mimeType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream",
        };

        content.Headers.ContentType = new(mimeType);
        content.Headers.ContentLength = fileInfo.Length;
        content.Headers.ContentRange = new(0, fileInfo.Length - 1, fileInfo.Length);
        content.Headers.ContentDisposition = new("attachment")
        {
            FileName = fileInfo.Name,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Content = content;
        request.Headers.Add("Session-ID", Guid.NewGuid().ToString("N")[..14]);
        request.Headers.Add("X-Uploading-Mode", "parallel");
        request.Headers.Add("Origin", "https://cabinet.vkvideo.ru");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ошибка загрузки файла. Статус: {StatusCode}, Ответ: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Ошибка загрузки файла: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<FileUploadResponse>(body)
               ?? throw new InvalidOperationException("Не удалось десериализовать ответ upload");
    }

    [DoesNotReturn]
    private void ThrowHtmlResponseError(string body, string context)
    {
        var reason = VkChallengeSolver.IsChallengePage(body)
            ? "сервер требует прохождение капчи (rate limit), автоматическое решение не удалось"
            : "сервер вернул HTML вместо JSON. Cookies устарели — переавторизуйтесь через Playwright";

        _logger.LogError("Ответ {Context} — HTML вместо JSON. Начало: {Body}",
            context, body.Length > 200 ? body[..200] : body);

        throw new InvalidOperationException($"Не удалось выполнить {context}: {reason}.");
    }

    private sealed record CatalogSectionsResult(
        string? VideoSectionId,
        string? VideoNextFrom,
        string? ClipsSectionId);
}
