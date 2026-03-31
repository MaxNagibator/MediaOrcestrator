using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MediaOrcestrator.VkVideo;

public sealed class VkVideoChannel(ILogger<VkVideoChannel> logger, ILogger<VkVideoService> serviceLogger) : ISourceType
{
    private readonly SemaphoreSlim _serviceLock = new(1, 1);
    private VkVideoService? _cachedService;
    private string? _cachedAuthStatePath;

    public string Name => "VkVideo";

    public SyncDirection ChannelType => SyncDirection.Full;

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "auth_state_path",
            IsRequired = true,
            Title = "путь до файла куки",
            Description = "JSON файл с cookies для авторизации на VK Video (Playwright StorageState)",
        },
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
    ];

    public Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        return Task.FromResult<List<SettingOption>>([]);
    }

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
    {
        return new($"https://vkvideo.ru/video{externalId}");
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Получение списка медиа для VkVideo");
        var service = await CreateServiceAsync(settings);
        var groupId = long.Parse(settings["group_id"]);

        var (videos, sectionId, nextFrom) = await service.GetCatalogFirstPageAsync(groupId);

        foreach (var video in videos)
        {
            yield return CreateMediaDto(video);
        }

        while (sectionId != null && nextFrom != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (nextVideos, newNextFrom) = await service.GetCatalogNextPageAsync(sectionId, nextFrom);

            foreach (var video in nextVideos)
            {
                yield return CreateMediaDto(video);
            }

            nextFrom = newNextFrom;
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

    // TODO: Переделать нормально
    public async Task<MediaDto> Download(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Скачивание видео {VideoId}", videoId);
        var service = await CreateServiceAsync(settings);
        var (ownerId, vid) = ParseExternalId(videoId);

        var video = await service.GetVideoByIdAsync(ownerId, vid)
                    ?? throw new InvalidOperationException($"Видео {videoId} не найдено");

        var downloadUrl = video.Files?.GetBestQualityUrl()
                          ?? throw new InvalidOperationException($"Нет доступных URL для скачивания видео {videoId}");

        // TODO: Сделать как в ютуб. Ещё также можно через yt-dlp скачивать. Он поддерживает. Нужно подумать как обыграть
        var tempDir = Path.GetTempPath();
        var tempVideoPath = Path.Combine(tempDir, $"vkvideo_{ownerId}_{vid}.mp4");
        var tempPreviewPath = Path.Combine(tempDir, $"vkvideo_{ownerId}_{vid}_preview.jpg");

        //logger.LogInformation("Скачивание видео из {Url}", downloadUrl[..Math.Min(80, downloadUrl.Length)] + "...");
        logger.LogInformation("Скачивание видео из {Url}", downloadUrl);
        await using (var videoStream = await service.HttpClient.GetStreamAsync(downloadUrl, cancellationToken))
        {
            await using (var fileStream = File.Create(tempVideoPath))
            {
                await videoStream.CopyToAsync(fileStream, cancellationToken);
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

        var metadata = BuildMetadata(video);

        // TODO: Может не добавлять вообще если null
        metadata.Add(new()
        {
            Key = "PreviewUrl",
            Value = previewUrl ?? string.Empty
        });

        return new()
        {
            Id = videoId,
            Title = video.Title,
            Description = video.Description,
            DataPath = downloadUrl,
            PreviewPath = previewUrl ?? string.Empty,
            TempDataPath = tempVideoPath,
            TempPreviewPath = File.Exists(tempPreviewPath) ? tempPreviewPath : string.Empty,
            Metadata = metadata,
        };
    }

    public async Task<UploadResult> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Загрузка видео на VK Video. Название: '{Title}'", media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        var service = await CreateServiceAsync(settings);
        var groupId = long.Parse(settings["group_id"]);
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
            var result = await service.UploadVideoAsync(groupId, filePath, media.Title, media.Description,
                fileExt, publishAtUnix, cancellationToken);

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

    public async Task<UploadResult> Update(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        logger.LogInformation("Обновление видео {ExternalId} на VK Video", externalId);

        var service = await CreateServiceAsync(settings);
        var (ownerId, videoId) = ParseExternalId(externalId);

        var errorMessage = "";

        try
        {
            await service.EditVideoAsync(ownerId, videoId, tempMedia.Title, tempMedia.Description);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка редактирования метаданных");
            errorMessage += "Ошибка редактирования метаданных";
        }

        if (!string.IsNullOrEmpty(tempMedia.TempPreviewPath) && File.Exists(tempMedia.TempPreviewPath))
        {
            try
            {
                await service.UploadThumbnailAsync(ownerId, videoId, tempMedia.TempPreviewPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка загрузки превью");
                if (errorMessage.Length > 0)
                {
                    errorMessage += Environment.NewLine;
                }

                errorMessage += "Ошибка загрузки превью";
            }
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

    private static (long OwnerId, long VideoId) ParseExternalId(string externalId)
    {
        var parts = externalId.Split('_', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out var ownerId) || !long.TryParse(parts[1], out var videoId))
        {
            throw new ArgumentException($"Некорректный формат ID видео: {externalId}. Ожидается: {{owner_id}}_{{video_id}}");
        }

        return (ownerId, videoId);
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
            Id = $"{video.OwnerId}_{video.Id}",
            Title = video.Title,
            Description = video.Description,
            DataPath = video.Files?.GetBestQualityUrl() ?? string.Empty,
            PreviewPath = previewUrl,
            Metadata = metadata,
        };
    }

    // TODO: Можно напихать другие. Апя модели вроде правильные
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

    private async Task<VkVideoService> CreateServiceAsync(Dictionary<string, string> settings)
    {
        var authStatePath = settings.GetValueOrDefault("auth_state_path");
        if (string.IsNullOrEmpty(authStatePath))
        {
            throw new InvalidOperationException("Путь к файлу аутентификации VK Video не указан в настройках.");
        }

        await _serviceLock.WaitAsync();
        try
        {
            if (_cachedService != null && _cachedAuthStatePath == authStatePath)
            {
                return _cachedService;
            }

            if (!File.Exists(authStatePath))
            {
                throw new FileNotFoundException($"Файл аутентификации VK Video не найден: {authStatePath}", authStatePath);
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

            var oldService = _cachedService;
            _cachedService = new(string.Join("; ", cookiePairs), serviceLogger);
            _cachedAuthStatePath = authStatePath;
            oldService?.Dispose();
            return _cachedService;
        }
        finally
        {
            _serviceLock.Release();
        }
    }
}
