using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Youtube;

public sealed class YoutubeUploadService(ILogger logger, YoutubeAuthService authService)
{
    public async Task<UploadResult> UploadVideoAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
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

        using var service = await authService.CreateServiceAsync(settings, cancellationToken);

        var video = CreateVideoResource(media, settings);
        var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);

        logger.LogInformation("Загрузка видео на YouTube. Название: '{Title}', Статус: {Privacy}", media.Title, video.Status?.PrivacyStatus);

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var request = service.Videos.Insert(video, "snippet,status", fileStream, "video/*");

        var chunkSize = fileStream.Length switch
        {
            < 10 * 1024 * 1024 => ResumableUpload.MinimumChunkSize * 4,
            < 100 * 1024 * 1024 => ResumableUpload.MinimumChunkSize * 16,
            _ => ResumableUpload.MinimumChunkSize * 40,
        };

        if (uploadBytesPerSecond.HasValue)
        {
            chunkSize = Math.Min(chunkSize, (int)Math.Max(uploadBytesPerSecond.Value, ResumableUpload.MinimumChunkSize));
            chunkSize = chunkSize / ResumableUpload.MinimumChunkSize * ResumableUpload.MinimumChunkSize;
        }

        request.ChunkSize = chunkSize;

        var throttle = new UploadThrottle(uploadBytesPerSecond, chunkSize);
        var bucketedProgress = UploadProgressLogger.CreateBucketed(logger, media.Id);

        request.ProgressChanged += progress =>
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    var percent = fileStream.Length > 0
                        ? (double)progress.BytesSent / fileStream.Length
                        : 0;

                    bucketedProgress.Report(percent);

                    throttle.WaitIfNeeded();
                    break;

                case UploadStatus.Failed:
                    logger.LogError("Ошибка загрузки: {Error}", progress.Exception?.Message);
                    break;
            }
        };

        var response = await request.UploadAsync(cancellationToken);

        if (response.Status == UploadStatus.Failed)
        {
            var errorMessage = response.Exception?.Message ?? "Неизвестная ошибка";

            if (errorMessage.Contains("quotaExceeded", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Превышена квота YouTube API (лимит ~6 видео/день)";
            }

            logger.LogError("Загрузка на YouTube не удалась: {Error}", errorMessage);

            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = errorMessage,
            };
        }

        var uploadedVideo = request.ResponseBody;
        logger.LogInformation("Видео загружено на YouTube. ID: {VideoId}", uploadedVideo.Id);

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
        string externalId,
        MediaDto tempMedia,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        using var service = await authService.CreateServiceAsync(settings, cancellationToken);

        logger.LogInformation("Обновление метаданных YouTube видео. ID: {VideoId}", externalId);

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

        logger.LogInformation("Метаданные обновлены для видео: {VideoId}", externalId);

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

    private static Video CreateVideoResource(MediaDto media, Dictionary<string, string> settings)
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
        logger.LogInformation("Загрузка превью для видео: {VideoId}", videoId);

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
            // TODO: Forbidden (403) означает, что канал не верифицирован - кастомные превью требуют подтверждения номера телефона.
            logger.LogWarning("Не удалось загрузить превью для видео {VideoId}: {Error}", videoId, thumbResponse.Exception?.Message);
        }
        else
        {
            logger.LogInformation("Превью загружено для видео: {VideoId}", videoId);
        }
    }
}

file sealed class UploadThrottle(long? bytesPerSecond, int chunkSize)
{
    private DateTime _lastChunkTime = DateTime.UtcNow;

    public void WaitIfNeeded()
    {
        if (!bytesPerSecond.HasValue)
        {
            return;
        }

        var elapsed = (DateTime.UtcNow - _lastChunkTime).TotalSeconds;
        var expectedSeconds = (double)chunkSize / bytesPerSecond.Value;

        if (elapsed < expectedSeconds)
        {
            var delayMs = (int)((expectedSeconds - elapsed) * 1000);
            Thread.Sleep(delayMs);
        }

        _lastChunkTime = DateTime.UtcNow;
    }
}
