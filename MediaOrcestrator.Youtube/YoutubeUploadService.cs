using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using UploadProgress = MediaOrcestrator.Modules.UploadProgress;

namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeUploadService(ILogger<YoutubeUploadService> logger)
{
    public async Task<UploadResult> UploadVideoAsync(
        YouTubeService service,
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var filePath = media.TempDataPath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = $"Файл видео не найден: {filePath}",
            };
        }

        var video = CreateVideoResource(media, settings);
        var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);

        logger.UploadStart(media.Title, video.Status?.PrivacyStatus);

        await using var rawFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        await using var throttledStream = new ThrottledStream(rawFileStream, uploadBytesPerSecond);
        var request = service.Videos.Insert(video, "snippet,status", throttledStream, "video/*");

        request.ChunkSize = rawFileStream.Length switch
        {
            < 10 * 1024 * 1024 => ResumableUpload.MinimumChunkSize * 4,
            < 100 * 1024 * 1024 => ResumableUpload.MinimumChunkSize * 16,
            _ => ResumableUpload.MinimumChunkSize * 40,
        };

        var bucketedProgress = UploadProgressLogger.CreateBucketed(logger, media.Id, external: progress);

        request.ProgressChanged += progress =>
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    var percent = rawFileStream.Length > 0
                        ? (double)progress.BytesSent / rawFileStream.Length
                        : 0;

                    bucketedProgress.Report(percent);
                    break;

                case UploadStatus.Failed:
                    logger.UploadFailed(progress.Exception?.Message ?? "Неизвестная ошибка");
                    break;
            }
        };

        var response = await request.UploadAsync(cancellationToken);

        if (response.Status == UploadStatus.Failed)
        {
            var errorMessage = response.Exception?.Message ?? "Неизвестная ошибка";

            if (errorMessage.Contains("quotaExceeded", StringComparison.OrdinalIgnoreCase))
            {
                logger.QuotaExceeded();
                errorMessage = "Превышена квота YouTube API (лимит ~6 видео/день)";
            }
            else
            {
                logger.UploadFailed(errorMessage);
            }

            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = errorMessage,
            };
        }

        var uploadedVideo = request.ResponseBody;
        logger.UploadCompleted(uploadedVideo.Id);

        if (!string.IsNullOrEmpty(media.TempPreviewPath) && File.Exists(media.TempPreviewPath))
        {
            await UploadThumbnailAsync(service, uploadedVideo.Id, media.TempPreviewPath, cancellationToken);
        }

        return new()
        {
            Status = MediaStatusHelper.Ok(),
            Id = uploadedVideo.Id,
        };
    }

    public async Task<UploadResult> UpdateVideoAsync(
        YouTubeService service,
        string externalId,
        MediaDto tempMedia,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        logger.UpdateStart(externalId);

        var listRequest = service.Videos.List("snippet,status");
        listRequest.Id = externalId;
        var listResponse = await listRequest.ExecuteAsync(cancellationToken);

        var video = listResponse.Items?.FirstOrDefault();

        if (video is null)
        {
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = $"Видео не найдено на YouTube: {externalId}",
            };
        }

        if (!string.IsNullOrEmpty(tempMedia.Title))
        {
            video.Snippet.Title = tempMedia.Title;
        }

        if (!string.IsNullOrEmpty(tempMedia.Description))
        {
            video.Snippet.Description = tempMedia.Description;
        }

        var updateRequest = service.Videos.Update(video, "snippet,status");
        await updateRequest.ExecuteAsync(cancellationToken);

        logger.UpdateCompleted(externalId);

        if (!string.IsNullOrEmpty(tempMedia.TempPreviewPath) && File.Exists(tempMedia.TempPreviewPath))
        {
            await UploadThumbnailAsync(service, externalId, tempMedia.TempPreviewPath, cancellationToken);
        }

        return new()
        {
            Status = MediaStatusHelper.Ok(),
            Id = externalId,
        };
    }

    private static Video CreateVideoResource(
        MediaDto media,
        Dictionary<string, string> settings)
    {
        var privacyStatus = settings.GetValueOrDefault("privacy_status", "private");
        var publishAt = ParsePublishAt(settings.GetValueOrDefault("publish_at"));

        if (publishAt.HasValue)
        {
            privacyStatus = "private";
        }

        var tags = settings.GetValueOrDefault("tags");
        var categoryId = settings.GetValueOrDefault("category_id");

        return new()
        {
            Snippet = new()
            {
                Title = media.Title,
                Description = media.Description ?? "",
                Tags = ParseTags(tags),
                CategoryId = string.IsNullOrEmpty(categoryId) ? null : categoryId,
            },
            Status = new()
            {
                PrivacyStatus = privacyStatus,
                PublishAtRaw = publishAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            },
        };
    }

    private static DateTime? ParsePublishAt(string? publishAt)
    {
        if (string.IsNullOrWhiteSpace(publishAt))
        {
            return null;
        }

        if (publishAt.StartsWith('+') && int.TryParse(publishAt.AsSpan(1), out var relativeHours))
        {
            return DateTime.Now.AddHours(relativeHours);
        }

        if (!TimeOnly.TryParseExact(publishAt, "H:mm", out var time))
        {
            return null;
        }

        var today = DateTime.Today.Add(time.ToTimeSpan());
        return today > DateTime.Now ? today : today.AddDays(1);
    }

    private static List<string>? ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        var parsed = tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .ToList();

        return parsed.Count > 0 ? parsed : null;
    }

    private async Task UploadThumbnailAsync(
        YouTubeService service,
        string videoId,
        string thumbnailPath,
        CancellationToken cancellationToken)
    {
        logger.ThumbnailUploadStart(videoId);

        var contentType = Path.GetExtension(thumbnailPath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "image/jpeg",
        };

        await using var thumbStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read);
        var thumbRequest = service.Thumbnails.Set(videoId, thumbStream, contentType);
        var thumbResponse = await thumbRequest.UploadAsync(cancellationToken);

        if (thumbResponse.Status == UploadStatus.Failed)
        {
            logger.ThumbnailUploadFailed(videoId, thumbResponse.Exception?.Message);
        }
        else
        {
            logger.ThumbnailUploaded(videoId);
        }
    }
}
