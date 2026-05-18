using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.RateLimiting;

namespace MediaOrcestrator.VkVideo;

public sealed class VkVideoService : IDisposable
{
    private const string ApiBase = "https://api.vkvideo.ru/method/";
    private const string ApiVersion = "5.276";
    private const string ClientId = "52461373";
    private const string OwnerIdKey = "owner_id";
    private const string VideoIdKey = "video_id";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36";
    private const string Origin = "https://cabinet.vkvideo.ru";
    private const string Referer = "https://cabinet.vkvideo.ru/";

    private readonly HttpClient _apiClient;
    private readonly HttpClient _uploadClient;
    private readonly VkVideoOptions _options;
    private readonly ILogger<VkVideoService> _logger;
    private readonly string _cookieString;
    private readonly TokenBucketRateLimiter _rateLimiter;

    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpires = DateTimeOffset.MinValue;

    public VkVideoService(
        HttpClient apiClient,
        HttpClient uploadClient,
        string cookieString,
        VkVideoOptions options,
        ILogger<VkVideoService> logger)
    {
        _apiClient = apiClient;
        _uploadClient = uploadClient;
        _cookieString = cookieString;
        _options = options;
        _logger = logger;
        _rateLimiter = new(new()
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(options.MinRequestIntervalMs),
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
    }

    public void Dispose()
    {
        _rateLimiter.Dispose();
    }

    public async Task DownloadAsync(
        string url,
        Func<Stream, CancellationToken, Task> processor,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateDownloadRequest(url);
        using var response = await _uploadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var total = response.Content.Headers.ContentLength;
        var byteProgress = progress != null && total is > 0
            ? new Progress<long>(bytes => progress.Report(new(Math.Min(100.0, bytes * 100.0 / total.Value))))
            : null;

        await using var tracked = new ProgressStream(stream, byteProgress);
        await processor(tracked, cancellationToken);
    }

    public async IAsyncEnumerable<VideoItem> GetMediaAsync(
        long groupId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.ListingMedia();

        var firstPage = await GetCatalogFirstPageAsync(groupId, cancellationToken);
        var videos = GetCatalogVideosAsync(firstPage, cancellationToken);
        var clips = GetCatalogClipsAsync(firstPage, cancellationToken);
        var yieldedIds = new HashSet<string>();

        await foreach (var video in MergeByDateDescending(videos, clips, cancellationToken))
        {
            if (yieldedIds.Add(video.GetVideoKey()))
            {
                yield return video;
            }
        }
    }

    public async Task<VideoItem?> GetVideoByIdAsync(
        long ownerId,
        long videoId,
        CancellationToken cancellationToken = default)
    {
        var videoKey = $"{ownerId}_{videoId}";
        _logger.RequestingVideo(videoKey);

        var response = await CallApiAsync("video.getByIds",
            Operations.GetVideoById,
            new()
            {
                ["videos"] = videoKey,
                ["video_fields"] = "files,image,subtitles",
            },
            VkVideoJsonContext.Default.VideoGetByIdsResponse,
            cancellationToken);

        return response.Items.Count > 0 ? response.Items[0] : null;
    }

    public async Task<ShortVideoFullItem?> GetShortVideoByIdAsync(
        long ownerId,
        long videoId,
        CancellationToken cancellationToken = default)
    {
        var rawId = $"{ownerId}_{videoId}";

        var response = await CallApiAsync("shortVideo.get",
            Operations.ShortVideoGet,
            new()
            {
                ["short_video_raw_ids"] = rawId,
                ["fields"] = string.Empty,
            },
            VkVideoJsonContext.Default.ShortVideoGetResponse,
            cancellationToken);

        foreach (var item in response.Feed.Items)
        {
            if (item.Item != null && string.Equals(item.Type, "short_video_full", StringComparison.OrdinalIgnoreCase))
            {
                return item.Item;
            }
        }

        return null;
    }

    public async Task EditVideoAsync(
        long ownerId,
        long videoId,
        string title,
        string description,
        SaveThumbResponse? thumb = null,
        CancellationToken cancellationToken = default)
    {
        _logger.EditingVideo(ownerId, videoId);

        var parameters = new Dictionary<string, string>
        {
            [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
            [VideoIdKey] = videoId.ToString(CultureInfo.InvariantCulture),
            ["name"] = title,
            ["desc"] = description,
        };

        if (thumb != null)
        {
            parameters["thumb_id"] = thumb.PhotoId + "_" + thumb.PhotoOwnerId;
            parameters["thumb_hash"] = thumb.PhotoHash;
        }

        var result = await CallApiAsync("video.edit",
            Operations.EditVideo,
            parameters,
            VkVideoJsonContext.Default.EditResponse,
            cancellationToken);

        if (result.Success != 1)
        {
            throw new InvalidOperationException($"Ошибка редактирования видео {ownerId}_{videoId}");
        }

        _logger.VideoEdited(ownerId, videoId);
    }

    public async Task EditShortVideoAsync(
        long ownerId,
        long videoId,
        string description,
        CancellationToken cancellationToken = default)
    {
        _logger.EditingVideo(ownerId, videoId);

        await CallApiNoResultAsync(ApiBase,
            "shortVideo.edit",
            Operations.ShortVideoEdit,
            new()
            {
                [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = videoId.ToString(CultureInfo.InvariantCulture),
                ["description"] = description,
                ["publish_date"] = "0",
            },
            cancellationToken);

        _logger.VideoEdited(ownerId, videoId);
    }

    public async Task DeleteVideoAsync(
        long ownerId,
        long videoId,
        CancellationToken cancellationToken = default)
    {
        _logger.DeletingVideo(ownerId, videoId);

        // TODO: https://github.com/MaxNagibator/MediaOrcestrator/issues/37#issuecomment-4205897816
        // await CallApiNoResultAsync(ApiBase, "video.delete", Operations.DeleteVideo, ...);

        await CallApiNoResultAsync(ApiBase,
            "video.bulkEdit",
            Operations.DeleteVideo,
            new()
            {
                ["video_ids"] = $"{ownerId}_{videoId}",
                [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["action"] = "delete",
            },
            cancellationToken);

        _logger.VideoDeleted(ownerId, videoId);
    }

    public async Task<string> GetThumbUploadUrlAsync(
        long ownerId,
        long videoId,
        CancellationToken cancellationToken = default)
    {
        var response = await CallApiAsync("video.getVideoForEdit",
            Operations.GetVideoForEdit,
            new()
            {
                [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = videoId.ToString(CultureInfo.InvariantCulture),
            },
            VkVideoJsonContext.Default.VideoForEditResponse,
            cancellationToken);

        return response.Item.ThumbUploadUrl
               ?? throw new InvalidOperationException("thumb_upload_url не получен");
    }

    public Task<SaveThumbResponse> UploadThumbnailAsync(
        bool isShorts,
        long ownerId,
        long videoId,
        string thumbnailPath,
        CancellationToken cancellationToken = default)
    {
        return isShorts
            ? UploadShortVideoThumbnailAsync(ownerId, videoId, thumbnailPath, cancellationToken)
            : UploadVideoThumbnailAsync(ownerId, videoId, thumbnailPath, cancellationToken);
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

        _logger.UploadReservingSlot(title);

        VideoSaveResponse saveResponse;
        if (isShorts)
        {
            saveResponse = await CallApiAsync("shortVideo.create",
                Operations.ShortVideoCreate,
                new()
                {
                    ["group_id"] = groupId.ToString(CultureInfo.InvariantCulture),
                    ["file_size"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture),
                },
                VkVideoJsonContext.Default.VideoSaveResponse,
                cancellationToken);
        }
        else
        {
            saveResponse = await CallApiAsync("video.save",
                Operations.VideoSave,
                new()
                {
                    ["group_id"] = groupId.ToString(CultureInfo.InvariantCulture),
                    ["source_file_name"] = fileInfo.Name,
                    ["file_size"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture),
                    ["batch_id"] = $"-{groupId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    ["preview"] = "1",
                    ["thumb_upload"] = "1",
                },
                VkVideoJsonContext.Default.VideoSaveResponse,
                cancellationToken);
        }

        _logger.UploadSlotReserved(saveResponse.VideoId, saveResponse.OwnerId);
        _logger.UploadStartingFile(fileInfo.Length);

        var uploadResponse = await UploadFileAsync(saveResponse.UploadUrl, filePath, fileInfo, uploadBytesPerSecond, uploadProgress, cancellationToken);

        _logger.UploadFileCompleted(uploadResponse.VideoHash);

        if (isShorts)
        {
            await WaitForShortVideoEncodingAsync(saveResponse.OwnerId, saveResponse.VideoId, uploadResponse.VideoHash, cancellationToken);
        }

        SaveThumbResponse? thumbId = null;
        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
        {
            try
            {
                _logger.UploadingThumbnail();
                thumbId = await UploadThumbnailAsync(isShorts, saveResponse.OwnerId, saveResponse.VideoId, thumbnailPath, cancellationToken);
                _logger.ThumbnailUploaded();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ThumbnailFailedSkipping(ex);
            }
        }

        _logger.UploadPublishing();

        if (isShorts)
        {
            var editParams = new Dictionary<string, string>
            {
                [OwnerIdKey] = saveResponse.OwnerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = saveResponse.VideoId.ToString(CultureInfo.InvariantCulture),
                ["description"] = title,
                ["privacy_view"] = "all",
                ["can_make_duet"] = "1",
                ["privacy_comment"] = "all",
                ["audio_raw_id"] = "",
                ["ord_info"] = "{\"is_ads\":false,\"advertisers\":[]}",
            };

            if (thumbId != null)
            {
                editParams["thumb_id"] = thumbId.PhotoId.ToString(CultureInfo.InvariantCulture);
            }

            await CallApiAsync("shortVideo.edit",
                Operations.ShortVideoEdit,
                editParams,
                VkVideoJsonContext.Default.PublishResponse,
                cancellationToken);

            _logger.ShortVideoEditedBeforePublish();

            if (publishAt.HasValue)
            {
                _logger.ShortVideoNoScheduling();
            }

            var publishParams = new Dictionary<string, string>
            {
                [OwnerIdKey] = saveResponse.OwnerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = saveResponse.VideoId.ToString(CultureInfo.InvariantCulture),
                ["wallpost"] = "0",
                ["publish_date"] = "0",
                ["license_agree"] = "1",
                ["ref"] = "video_as_clip_video_upload",
            };

            var publishResponse = await CallApiAsync("shortVideo.publish",
                Operations.ShortVideoPublish,
                publishParams,
                VkVideoJsonContext.Default.PublishResponse,
                cancellationToken);

            _logger.UploadPublished(publishResponse.Video?.DirectUrl);
            return publishResponse.Video
                   ?? throw new InvalidOperationException("Ответ shortVideo.publish не содержит объект video");
        }
        else
        {
            var publishParams = new Dictionary<string, string>
            {
                [OwnerIdKey] = saveResponse.OwnerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = saveResponse.VideoId.ToString(CultureInfo.InvariantCulture),
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
                publishParams["publish_at"] = publishAt.Value.ToString(CultureInfo.InvariantCulture);
            }

            var publishResponse = await CallApiAsync("video.publish",
                Operations.VideoPublish,
                publishParams,
                VkVideoJsonContext.Default.PublishResponse,
                cancellationToken);

            _logger.UploadPublished(publishResponse.Video?.DirectUrl);

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
        string sort = "asc",
        CancellationToken cancellationToken = default)
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

        return CallApiAsync("video.getComments",
            Operations.GetVideoComments,
            parameters,
            VkVideoJsonContext.Default.VkCommentsResponse,
            cancellationToken);
    }

    public Task<VkCommentsResponse> GetVideoCommentAsync(
        long ownerId,
        long commentId,
        CancellationToken cancellationToken = default)
    {
        return CallApiAsync("video.getComment",
            Operations.GetVideoComment,
            new()
            {
                ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
                ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["extended"] = "1",
                ["fields"] = "photo_100,first_name_dat",
            },
            VkVideoJsonContext.Default.VkCommentsResponse,
            cancellationToken);
    }

    public Task<long> CreateVideoCommentAsync(
        long ownerId,
        long videoId,
        long? replyToComment,
        string message,
        long? fromOwnerId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFromOwnerId = fromOwnerId is { } id && id != 0
            ? -Math.Abs(id)
            : (long?)null;

        var fromGroup = normalizedFromOwnerId.HasValue;

        var parameters = new Dictionary<string, string>
        {
            ["video_id"] = videoId.ToString(CultureInfo.InvariantCulture),
            ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
            ["from_group"] = fromGroup ? "1" : "0",
            ["reply_to_comment"] = replyToComment?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["access_key"] = string.Empty,
            ["from_owner_id"] = fromGroup ? normalizedFromOwnerId!.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
            ["message"] = message,
            ["attachments"] = string.Empty,
        };

        return CallApiAsync("video.createComment",
            Operations.CreateComment,
            parameters,
            VkVideoJsonContext.Default.Int64,
            cancellationToken);
    }

    public Task<VkReplyAsListResponse> GetReplyAsListAsync(
        long ownerId,
        long videoId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>
        {
            ["video"] = string.Create(CultureInfo.InvariantCulture, $"{ownerId}_{videoId}"),
            ["force"] = "0",
        };

        return CallApiAsync("videoChannels.getReplyAsList",
            Operations.GetReplyAsList,
            parameters,
            VkVideoJsonContext.Default.VkReplyAsListResponse,
            cancellationToken);
    }

    public async Task<VkUserItem?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await CallApiAsync("users.get",
            Operations.GetCurrentUser,
            new()
            {
                ["fields"] = "photo_50,photo_100",
            },
            VkVideoJsonContext.Default.ListVkUserItem,
            cancellationToken);

        return response.FirstOrDefault();
    }

    public Task EditVideoCommentAsync(
        long ownerId,
        long commentId,
        string message,
        CancellationToken cancellationToken = default)
    {
        return CallApiNoResultAsync(ApiBase,
            "video.editComment",
            Operations.EditComment,
            new()
            {
                ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
                ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["message"] = message,
                ["attachments"] = string.Empty,
            },
            cancellationToken);
    }

    public Task DeleteVideoCommentAsync(
        long ownerId,
        long commentId,
        CancellationToken cancellationToken = default)
    {
        return CallApiNoResultAsync(ApiBase,
            "video.deleteComment",
            Operations.DeleteComment,
            new()
            {
                ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
            },
            cancellationToken);
    }

    public Task RestoreVideoCommentAsync(
        long ownerId,
        long commentId,
        CancellationToken cancellationToken = default)
    {
        return CallApiNoResultAsync(ApiBase,
            "video.restoreComment",
            Operations.RestoreComment,
            new()
            {
                ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["comment_id"] = commentId.ToString(CultureInfo.InvariantCulture),
            },
            cancellationToken);
    }

    public async Task<int> LikeVideoCommentAsync(
        long ownerId,
        long commentId,
        CancellationToken cancellationToken = default)
    {
        var response = await CallApiAsync("likes.add",
            Operations.LikeComment,
            new()
            {
                ["type"] = "video_comment",
                ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["item_id"] = commentId.ToString(CultureInfo.InvariantCulture),
                ["from_group"] = string.Empty,
                ["track_code"] = string.Empty,
                ["ref"] = string.Empty,
            },
            VkVideoJsonContext.Default.VkLikesAddResponse,
            cancellationToken);

        return response.Likes;
    }

    public async Task<int> UnlikeVideoCommentAsync(
        long ownerId,
        long commentId,
        CancellationToken cancellationToken = default)
    {
        var response = await CallApiAsync("likes.delete",
            Operations.UnlikeComment,
            new()
            {
                ["type"] = "video_comment",
                ["owner_id"] = ownerId.ToString(CultureInfo.InvariantCulture),
                ["item_id"] = commentId.ToString(CultureInfo.InvariantCulture),
                ["from_group"] = string.Empty,
                ["track_code"] = string.Empty,
                ["ref"] = string.Empty,
            },
            VkVideoJsonContext.Default.VkLikesAddResponse,
            cancellationToken);

        return response.Likes;
    }

    private static MultipartFormDataContent BuildThumbnailForm(string thumbnailPath)
    {
        FileStream? fileStream = null;
        StreamContent? fileContent = null;
        MultipartFormDataContent? form = null;
        try
        {
            fileStream = File.OpenRead(thumbnailPath);
            fileContent = new(fileStream);

            var contentType = Path.GetExtension(thumbnailPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "image/jpeg",
            };

            fileContent.Headers.ContentType = new(contentType);
            form = new();
            form.Add(fileContent, "photo", Path.GetFileName(thumbnailPath));
            return form;
        }
        catch
        {
            form?.Dispose();
            fileContent?.Dispose();
            fileStream?.Dispose();
            throw;
        }
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

    private static bool IsSameOrNewer(
        VideoItem left,
        VideoItem right)
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

    private static T DeserializeApiResponse<T>(
        byte[] bodyBytes,
        string method,
        JsonTypeInfo<T> typeInfo)
    {
        using var doc = JsonDocument.Parse(bodyBytes);
        if (!doc.RootElement.TryGetProperty("response", out var responseElement))
        {
            throw new InvalidOperationException($"Ответ API {method} не содержит поле 'response'");
        }

        return responseElement.Deserialize(typeInfo)
               ?? throw new InvalidOperationException($"Не удалось десериализовать ответ API {method}");
    }

    private async Task<string> GetShortVideoThumbUploadUrlAsync(
        long ownerId,
        CancellationToken cancellationToken)
    {
        var response = await CallApiAsync("shortVideo.getThumbUploadUrl",
            Operations.ShortVideoGetThumbUploadUrl,
            new()
            {
                [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
            },
            VkVideoJsonContext.Default.ShortVideoThumbUploadUrlResponse,
            cancellationToken);

        return string.IsNullOrEmpty(response.UploadUrl)
            ? throw new InvalidOperationException("upload_url для shorts превью не получен")
            : response.UploadUrl;
    }

    private async Task<SaveThumbResponse> UploadShortVideoThumbnailAsync(
        long ownerId,
        long videoId,
        string thumbnailPath,
        CancellationToken cancellationToken)
    {
        var thumbUploadUrl = await GetShortVideoThumbUploadUrlAsync(ownerId, cancellationToken);

        _logger.UploadingShortsThumbnail(ownerId, videoId);

        using var form = BuildThumbnailForm(thumbnailPath);
        var uploadBody = await PostThumbnailAsync(thumbUploadUrl, form, cancellationToken);

        var saveResult = await CallApiAsync("shortVideo.saveUploadedThumb",
            Operations.ShortVideoSaveThumb,
            new()
            {
                [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = videoId.ToString(CultureInfo.InvariantCulture),
                ["thumb_json"] = uploadBody,
            },
            VkVideoJsonContext.Default.SaveThumbResponse,
            cancellationToken);

        _logger.ShortsThumbnailUploaded(saveResult.PhotoId);
        return saveResult;
    }

    private async Task<SaveThumbResponse> UploadVideoThumbnailAsync(
        long ownerId,
        long videoId,
        string thumbnailPath,
        CancellationToken cancellationToken)
    {
        var thumbUploadUrl = await GetThumbUploadUrlAsync(ownerId, videoId, cancellationToken);

        _logger.UploadingVideoThumbnail(ownerId, videoId);

        using var form = BuildThumbnailForm(thumbnailPath);
        var uploadUrl = thumbUploadUrl + "&ajx=1";
        var uploadBody = await PostThumbnailAsync(uploadUrl, form, cancellationToken);

        var saveResult = await CallApiAsync("video.saveUploadedThumb",
            Operations.VideoSaveThumb,
            new()
            {
                [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
                [VideoIdKey] = videoId.ToString(CultureInfo.InvariantCulture),
                ["thumb_json"] = uploadBody,
                ["thumb_size"] = "l",
            },
            VkVideoJsonContext.Default.SaveThumbResponse,
            cancellationToken);

        _logger.VideoThumbnailUploaded(saveResult.PhotoId);
        return saveResult;
    }

    private async Task<string> PostThumbnailAsync(
        string uploadUrl,
        MultipartFormDataContent form,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = form,
        };

        using var response = await _uploadClient.SendAsync(request, cancellationToken);
        var uploadBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ошибка загрузки превью: {response.StatusCode}");
        }

        return uploadBody;
    }

    private async Task WaitForShortVideoEncodingAsync(
        long ownerId,
        long videoId,
        string hash,
        CancellationToken cancellationToken)
    {
        _logger.WaitingShortsEncoding(ownerId, videoId);

        var pollInterval = TimeSpan.FromSeconds(2);
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10);
        var lastLoggedBucket = -1;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progress = await CallApiAsync("shortVideo.encodeProgress",
                Operations.ShortVideoEncodeProgress,
                new()
                {
                    [VideoIdKey] = videoId.ToString(CultureInfo.InvariantCulture),
                    [OwnerIdKey] = ownerId.ToString(CultureInfo.InvariantCulture),
                    ["hash"] = hash,
                },
                VkVideoJsonContext.Default.ShortVideoEncodeProgressResponse,
                cancellationToken);

            if (progress.IsReady)
            {
                _logger.ShortsEncodingReady(ownerId, videoId, progress.Percents);
                return;
            }

            var bucket = progress.Percents / 10;
            if (bucket > lastLoggedBucket)
            {
                _logger.ShortsEncodingProgress(ownerId, videoId, progress.Percents);
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
            var firstPage = await GetCatalogSectionFirstPageAsync(sectionId, cancellationToken);

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
            var nextPage = await GetCatalogNextPageAsync(sectionId, nextFrom, cancellationToken);

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

        _logger.ListingClips();
        var yieldedIds = new HashSet<string>();
        var clips = GetCatalogSectionVideosAsync(firstPage.ClipsSectionId, null, true, yieldedIds, cancellationToken);

        await foreach (var clip in clips)
        {
            yield return clip;
        }
    }

    private async Task<CatalogFirstPageResult> GetCatalogFirstPageAsync(
        long groupId,
        CancellationToken cancellationToken)
    {
        var screenName = await GetGroupScreenNameAsync(groupId, cancellationToken);
        var catalogUrl = $"https://vkvideo.ru/@{Uri.EscapeDataString(screenName)}/all";
        _logger.RequestingCatalog(catalogUrl);

        CatalogResponse response;

        try
        {
            response = await CallApiAsync("catalog.getVideo",
                Operations.GetCatalog,
                new()
                {
                    ["need_blocks"] = "1",
                    [OwnerIdKey] = "0",
                    ["url"] = catalogUrl,
                },
                VkVideoJsonContext.Default.CatalogResponse,
                cancellationToken);
        }
        catch (VkApiException ex) when (ex.ErrorCode == 104)
        {
            _logger.CatalogEmpty(ex, groupId);
            return new([], null, null, null);
        }

        var sections = FindMediaSections(response.Catalog.Sections);
        return new(response.Videos, sections.VideoSectionId, sections.VideoNextFrom, sections.ClipsSectionId);
    }

    private async Task<CatalogSectionPageResult> GetCatalogSectionFirstPageAsync(
        string sectionId,
        CancellationToken cancellationToken)
    {
        _logger.RequestingSectionFirstPage(sectionId);

        var response = await CallApiAsync("catalog.getSection",
            Operations.GetCatalogSection,
            new()
            {
                ["section_id"] = sectionId,
            },
            VkVideoJsonContext.Default.CatalogSectionResponse,
            cancellationToken);

        return new(response.Videos, FindSectionNextFrom(response.Section));
    }

    private async Task<CatalogSectionPageResult> GetCatalogNextPageAsync(
        string sectionId,
        string startFrom,
        CancellationToken cancellationToken)
    {
        _logger.RequestingSectionNextPage(sectionId, startFrom);

        var response = await CallApiAsync("catalog.getSection",
            Operations.GetCatalogSection,
            new()
            {
                ["section_id"] = sectionId,
                ["start_from"] = startFrom,
            },
            VkVideoJsonContext.Default.CatalogSectionResponse,
            cancellationToken);

        return new(response.Videos, FindSectionNextFrom(response.Section));
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpires.AddMinutes(-1))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpires.AddMinutes(-1))
            {
                return _accessToken;
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                _logger.RequestingAccessToken();

                using var request = CreateApiRequest(HttpMethod.Post, "https://cabinet.vkvideo.ru/al_video.php?act=web_token");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["version"] = "1",
                    ["app_id"] = ClientId,
                });

                using var response = await SendApiAsync(Operations.GetAccessToken, request, cancellationToken);
                var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var body = Encoding.UTF8.GetString(bodyBytes);

                if (!IsHtmlResponse(body))
                {
                    return ParseWebTokenResponse(bodyBytes, body, response.StatusCode);
                }

                _logger.WebTokenHtmlResponse(attempt + 1, response.StatusCode);

                if (attempt == 0 && await TrySolveChallengeAsync(response, body, cancellationToken))
                {
                    _logger.RetryingWebTokenAfterChallenge();
                    continue;
                }

                ThrowHtmlResponseError(body, "web_token");
            }

            throw new InvalidOperationException("Не удалось получить access_token после решения challenge.");
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private Task<T> CallApiAsync<T>(
        string method,
        string operationKey,
        Dictionary<string, string> parameters,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken)
    {
        return CallApiInternalAsync(ApiBase, method, operationKey, parameters, typeInfo, cancellationToken);
    }

    private Task CallApiNoResultAsync(
        string baseUrl,
        string method,
        string operationKey,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        return CallApiRawAsync(baseUrl, method, operationKey, parameters, cancellationToken);
    }

    private async Task<string> GetGroupScreenNameAsync(
        long groupId,
        CancellationToken cancellationToken)
    {
        var response = await CallApiAsync("groups.getById",
            Operations.GetGroupById,
            new()
            {
                ["fields"] = "counters",
                ["group_ids"] = groupId.ToString(CultureInfo.InvariantCulture),
            },
            VkVideoJsonContext.Default.GroupsGetByIdResponse,
            cancellationToken);

        var group = response.Groups.FirstOrDefault(g => g.Id == groupId) ?? response.Groups.FirstOrDefault();
        var screenName = group?.ScreenName;

        if (string.IsNullOrWhiteSpace(screenName))
        {
            screenName = $"club{groupId}";
        }

        _logger.GroupScreenNameResolved(groupId, screenName);
        return screenName;
    }

    private string ParseWebTokenResponse(
        byte[] bodyBytes,
        string body,
        HttpStatusCode statusCode)
    {
        if ((int)statusCode >= 400)
        {
            _logger.WebTokenFailed(statusCode, body);
            throw new HttpRequestException($"Не удалось получить access_token: {statusCode}");
        }

        var tokens = JsonSerializer.Deserialize(bodyBytes, VkVideoJsonContext.Default.ListWebTokenResponse);
        if (tokens == null || tokens.Count == 0 || string.IsNullOrEmpty(tokens[0].AccessToken))
        {
            throw new InvalidOperationException("Не удалось получить access_token. Возможно, cookies устарели — переавторизуйтесь через Playwright.");
        }

        _accessToken = tokens[0].AccessToken;
        _tokenExpires = DateTimeOffset.FromUnixTimeSeconds(tokens[0].Expires);
        _logger.AccessTokenReceived(_tokenExpires);

        return _accessToken;
    }

    private async Task<T> CallApiInternalAsync<T>(
        string baseUrl,
        string method,
        string operationKey,
        Dictionary<string, string> parameters,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken)
    {
        var bodyBytes = await CallApiRawAsync(baseUrl, method, operationKey, parameters, cancellationToken);
        return DeserializeApiResponse(bodyBytes, method, typeInfo);
    }

    private async Task<byte[]> CallApiRawAsync(
        string baseUrl,
        string method,
        string operationKey,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var challengeSolved = false;
        var rateLimitHits = 0;

        while (true)
        {
            await ThrottleAsync(cancellationToken);
            var token = await GetAccessTokenAsync(cancellationToken);

            var requestParams = new Dictionary<string, string>(parameters)
            {
                ["access_token"] = token,
            };

            var url = $"{baseUrl}{method}?v={ApiVersion}&client_id={ClientId}";

            using var request = CreateApiRequest(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(requestParams);

            using var response = await SendApiAsync(operationKey, request, cancellationToken);
            var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var bodyString = Encoding.UTF8.GetString(bodyBytes);

            if (IsHtmlResponse(bodyString))
            {
                _logger.ApiHtmlResponse(method, response.StatusCode);

                if (!challengeSolved && await TrySolveChallengeAsync(response, bodyString, cancellationToken))
                {
                    challengeSolved = true;
                    _logger.RetryingApiAfterChallenge(method);
                    continue;
                }

                ThrowHtmlResponseError(bodyString, method);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.ApiFailed(method, response.StatusCode, bodyString);
                throw new HttpRequestException($"Ошибка API {method}: {response.StatusCode}");
            }

            try
            {
                EnsureNoVkError(bodyBytes, method);
                return bodyBytes;
            }
            catch (VkApiException ex) when (ex.ErrorCode == 6 && rateLimitHits < _options.RateLimitMaxRetries)
            {
                rateLimitHits++;
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, rateLimitHits - 1));
                _logger.RateLimitWait(method, rateLimitHits, _options.RateLimitMaxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("Не удалось получить разрешение rate limiter VK API");
        }
    }

    private void EnsureNoVkError(
        byte[] bodyBytes,
        string method)
    {
        using var doc = JsonDocument.Parse(bodyBytes);

        if (!doc.RootElement.TryGetProperty("error", out var errorElement))
        {
            return;
        }

        var errorCode = errorElement.TryGetProperty("error_code", out var code) ? code.GetInt32() : 0;
        var errorMsg = errorElement.TryGetProperty("error_msg", out var msg) ? msg.GetString() : null;
        _logger.VkApiError(method, errorCode, errorMsg ?? string.Empty);
        throw new VkApiException(errorCode, errorMsg ?? "(no message)", method);
    }

    /// <summary>
    /// Пытается решить VK challenge (rate limit). Извлекает hash429 из redirect URL,
    /// вычисляет key и отправляет решение. Возвращает true если challenge решён.
    /// </summary>
    private async Task<bool> TrySolveChallengeAsync(
        HttpResponseMessage response,
        string body,
        CancellationToken cancellationToken)
    {
        if (!VkChallengeSolver.IsChallengePage(body))
        {
            _logger.HtmlButNotChallenge();
            return false;
        }

        var challengeUri = response.RequestMessage?.RequestUri;
        _logger.ChallengeDetected(challengeUri);

        var result = VkChallengeSolver.TrySolve(challengeUri, body);
        if (!result.Success)
        {
            _logger.ChallengeUnsolvable(result.Error, challengeUri);
            return false;
        }

        _logger.ChallengeSolved(result.Hash429, result.Salt, result.Key);
        _logger.ChallengeSendingSolution(result.SolveUri);

        using var solveRequest = CreateApiRequest(HttpMethod.Get, result.SolveUri!.ToString());
        using var solveResponse = await _apiClient.SendAsync(solveRequest, cancellationToken);
        var solveBody = await solveResponse.Content.ReadAsStringAsync(cancellationToken);

        _logger.ChallengeSolutionResponse(solveResponse.StatusCode, solveBody.Length);

        if (IsHtmlResponse(solveBody))
        {
            // TODO: Нужно отловить и посмотреть, что там
            _logger.ChallengeStillHtml(solveBody.Length > 200 ? solveBody[..200] : solveBody);
            return false;
        }

        return true;
    }

    private async Task<FileUploadResponse> UploadFileAsync(
        string uploadUrl,
        string filePath,
        FileInfo fileInfo,
        long? uploadBytesPerSecond,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
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

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = content,
        };

        request.Headers.TryAddWithoutValidation("Session-ID", Guid.NewGuid().ToString("N")[..14]);
        request.Headers.TryAddWithoutValidation("X-Uploading-Mode", "parallel");
        request.Headers.TryAddWithoutValidation("Origin", Origin);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        using var response = await _uploadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var bodyString = Encoding.UTF8.GetString(bodyBytes);
            _logger.UploadFileFailed(response.StatusCode, bodyString);
            throw new HttpRequestException($"Ошибка загрузки файла: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize(bodyBytes, VkVideoJsonContext.Default.FileUploadResponse)
               ?? throw new InvalidOperationException("Не удалось десериализовать ответ upload");
    }

    [DoesNotReturn]
    private void ThrowHtmlResponseError(
        string body,
        string context)
    {
        var reason = VkChallengeSolver.IsChallengePage(body)
            ? "сервер требует прохождение капчи (rate limit), автоматическое решение не удалось"
            : "сервер вернул HTML вместо JSON. Cookies устарели — переавторизуйтесь через Playwright";

        _logger.HtmlInsteadOfJson(context, body.Length > 200 ? body[..200] : body);

        throw new InvalidOperationException($"Не удалось выполнить {context}: {reason}.");
    }

    private HttpRequestMessage CreateApiRequest(
        HttpMethod method,
        string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Cookie", _cookieString);
        request.Headers.TryAddWithoutValidation("Origin", Origin);
        request.Headers.TryAddWithoutValidation("Referer", Referer);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        return request;
    }

    private HttpRequestMessage CreateDownloadRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        return request;
    }

    private async Task<HttpResponseMessage> SendApiAsync(
        string operationKey,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = ResilienceContextPool.Shared.Get(operationKey, cancellationToken);
        request.SetResilienceContext(context);

        try
        {
            return await _apiClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private static class Operations
    {
        public const string GetAccessToken = "vkvideo.access-token.get";
        public const string GetVideoById = "vkvideo.video.get";
        public const string EditVideo = "vkvideo.video.edit";
        public const string DeleteVideo = "vkvideo.video.delete";
        public const string GetVideoForEdit = "vkvideo.video.get-for-edit";
        public const string VideoSave = "vkvideo.video.save";
        public const string VideoPublish = "vkvideo.video.publish";
        public const string VideoSaveThumb = "vkvideo.video.save-thumb";
        public const string ShortVideoGet = "vkvideo.short-video.get";
        public const string ShortVideoCreate = "vkvideo.short-video.create";
        public const string ShortVideoEdit = "vkvideo.short-video.edit";
        public const string ShortVideoPublish = "vkvideo.short-video.publish";
        public const string ShortVideoGetThumbUploadUrl = "vkvideo.short-video.get-thumb-upload-url";
        public const string ShortVideoSaveThumb = "vkvideo.short-video.save-thumb";
        public const string ShortVideoEncodeProgress = "vkvideo.short-video.encode-progress";
        public const string GetCatalog = "vkvideo.catalog.get";
        public const string GetCatalogSection = "vkvideo.catalog.get-section";
        public const string GetGroupById = "vkvideo.group.get";
        public const string GetVideoComments = "vkvideo.comment.list";
        public const string GetVideoComment = "vkvideo.comment.get";
        public const string CreateComment = "vkvideo.comment.create";
        public const string EditComment = "vkvideo.comment.edit";
        public const string DeleteComment = "vkvideo.comment.delete";
        public const string RestoreComment = "vkvideo.comment.restore";
        public const string LikeComment = "vkvideo.comment.like";
        public const string UnlikeComment = "vkvideo.comment.unlike";
        public const string GetReplyAsList = "vkvideo.comment.reply-as-list";
        public const string GetCurrentUser = "vkvideo.user.get-current";
    }

    private sealed record CatalogSectionsResult(
        string? VideoSectionId,
        string? VideoNextFrom,
        string? ClipsSectionId);
}
