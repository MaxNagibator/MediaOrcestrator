using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.Rutube;

// TODO: Костыль с ILogger<RutubeService>. Желательно сделать полноценную регистрацию модулей в DI.
public class RutubeChannel(ILogger<RutubeChannel> logger, ILogger<RutubeService> serviceLogger) : ISourceType, IAuthenticatable
{
    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "Rutube";

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "auth_state_path",
            IsRequired = true,
            Title = "путь до фаила куки",
            Description = "JSON файл с cookies и CSRF токеном для авторизации на RuTube",
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
    ];

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
    {
        return new($"https://rutube.ru/video/{externalId}/");
    }

    public async Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        if (settingKey != "category_id")
        {
            return [];
        }

        try
        {
            var rutubeService = await CreateRutubeServiceAsync(currentSettings);

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
            logger.LogError(ex, "Ошибка при получении категорий RuTube");
            return [];
        }
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Получение списка медиа для хранища {Name}", Name);
        var rutubeService = await CreateRutubeServiceAsync(settings);
        var apiVideoItems = rutubeService.GetVideoAsync();
        await foreach (var video in apiVideoItems)
        {
            logger.LogDebug("Обработка видео: '{VideoTitle}' (ID: {VideoId})", video.Title, video.Id);

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
                    Value = video.Author.Name,
                    DisplayType = "System.String",
                },
                new()
                {
                    Key = "CreationDate",
                    DisplayName = "Дата создания",
                    Value = video.CreatedTs.ToString("O"),
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

            yield return new()
            {
                Id = video.Id,
                Title = video.Title,
                DataPath = video.VideoUrl,
                PreviewPath = video.ThumbnailUrl,
                Metadata = metadata,
            };
        }
    }

    public Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<MediaDto> Download(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Загрузка видео с RuTube не реализована. ID: {VideoId}", videoId);
        throw new NotImplementedException("Загрузка с RuTube не поддерживается");
    }

    public async Task<UploadResult> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Начало загрузки видео на RuTube. Название: '{Title}'", media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            logger.LogError("Файл видео не найден: {FilePath}", filePath);
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        logger.LogDebug("Файл найден. Размер: {FileSize} байт", new FileInfo(filePath).Length);

        var rutubeCategoryId = settings["category_id"];
        var rutubeService = await CreateRutubeServiceAsync(settings);

        DateTime? publishAt = null;
        if (settings.TryGetValue("publish_at", out var publishAtRaw) && !string.IsNullOrWhiteSpace(publishAtRaw))
        {
            var publishAtTrimmed = publishAtRaw.Trim();
            if (publishAtTrimmed.StartsWith('+') && double.TryParse(publishAtTrimmed[1..], NumberStyles.Any, CultureInfo.InvariantCulture, out var relativeHours))
            {
                publishAt = DateTime.Now.AddHours(relativeHours);
                logger.LogInformation("Отложенная публикация запланирована через {Hours} ч. - на {PublishAt}", relativeHours, publishAt.Value);
            }
            else if (TimeOnly.TryParseExact(publishAtTrimmed, "HH:mm", out var timeOnly))
            {
                var today = DateTime.Today.Add(timeOnly.ToTimeSpan());
                publishAt = today > DateTime.Now ? today : today.AddDays(1);
                logger.LogInformation("Отложенная публикация запланирована на {PublishAt}", publishAt.Value);
            }
            else
            {
                logger.LogWarning("Не удалось разобрать время публикации '{PublishAtRaw}'. Ожидается формат ЧЧ:ММ или +N (часов). Видео будет опубликовано немедленно", publishAtRaw);
            }
        }

        try
        {
            var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);
            var result = await rutubeService.UploadVideoAsync(null, filePath, media.Title, media.Description, rutubeCategoryId, media.TempPreviewPath, publishAt, uploadBytesPerSecond);
            logger.LogInformation("Видео загружено на RuTube. Status: {Status}. Video ID: {SessionId}, Название: '{Title}'", result.Status.Id, result.Id, media.Title);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке видео на RuTube. Название: '{Title}'", media.Title);
            throw;
        }
    }

    public async Task<UploadResult> Update(string externalId, MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Начало обновление данных видео на RuTube. Название: '{Title}'", media.Title);

        var rutubeCategoryId = settings["category_id"];
        var rutubeService = await CreateRutubeServiceAsync(settings);

        try
        {
            // пока тока превью обновляем
            var result = await rutubeService.UploadVideoAsync(externalId, null, media.Title, media.Description, rutubeCategoryId, media.TempPreviewPath);
            logger.LogInformation("Видео загружено на RuTube. Status: {Status}. Video ID: {SessionId}, Название: '{Title}'", result.Status.Id, result.Id, media.Title);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке видео на RuTube. Название: '{Title}'", media.Title);
            throw;
        }
    }

    public async Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Удаление медиа из RuTube. ID: {ExternalId}", externalId);

        var rutubeService = await CreateRutubeServiceAsync(settings);

        try
        {
            await rutubeService.DeleteVideoAsync(externalId);
            logger.LogInformation("Медиа {ExternalId} успешно удалено из источника RuTube", externalId);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Ошибка HTTP при удалении из RuTube: {ExternalId}", externalId);
            throw new IOException($"Ошибка сети при удалении из RuTube: {ex.Message}", ex);
        }
    }

    // TODO: Придумать более умный механизм
    public bool IsAuthenticated(Dictionary<string, string> settings)
    {
        var authStatePath = settings.GetValueOrDefault("auth_state_path");
        if (string.IsNullOrEmpty(authStatePath) || !File.Exists(authStatePath))
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

    public async Task AuthenticateAsync(Dictionary<string, string> settings, IAuthUI ui, CancellationToken ct)
    {
        var authStatePath = settings.GetValueOrDefault("auth_state_path");
        if (string.IsNullOrEmpty(authStatePath))
        {
            await ui.ShowMessageAsync("Укажите путь к файлу куки в настройках.");
            return;
        }

        var result = await ui.OpenBrowserAsync("https://studio.rutube.ru/", authStatePath);
        if (result != null)
        {
            logger.LogInformation("RuTube: авторизация сохранена в {Path}", result);
            await ui.ShowMessageAsync("Авторизация RuTube сохранена!");
        }
    }

    private async Task<RutubeService> CreateRutubeServiceAsync(Dictionary<string, string> settings)
    {
        var authStatePath = settings.GetValueOrDefault("auth_state_path");
        if (string.IsNullOrEmpty(authStatePath))
        {
            throw new InvalidOperationException("Путь к файлу аутентификации RuTube не указан в настройках.");
        }

        if (!File.Exists(authStatePath))
        {
            throw new FileNotFoundException($"Файл аутентификации RuTube не найден: {authStatePath}", authStatePath);
        }

        logger.LogDebug("Чтение данных аутентификации из: {AuthStatePath}", authStatePath);

        var authStateBody = await File.ReadAllTextAsync(authStatePath);
        using var authState = JsonDocument.Parse(authStateBody);
        var cookies = authState.RootElement.GetProperty("cookies");

        var cookieStringBuilder = new StringBuilder();
        string? csrfToken = null;

        foreach (var cookie in cookies.EnumerateArray())
        {
            var name = cookie.GetProperty("name").GetString()!;
            var value = cookie.GetProperty("value").GetString()!;
            var domain = cookie.GetProperty("domain").GetString()!;

            if (domain.Contains("rutube.ru") || domain.Contains("studio.rutube.ru") || domain.Contains("gid.ru"))
            {
                cookieStringBuilder.Append($"{name}={value}; ");
            }

            if (name == "csrftoken" && domain == "studio.rutube.ru")
            {
                csrfToken = value;
            }
        }

        if (string.IsNullOrEmpty(csrfToken))
        {
            throw new InvalidOperationException("CSRF токен не найден в файле аутентификации RuTube. Убедитесь, что вы авторизованы в RuTube Studio.");
        }

        logger.LogDebug("CSRF токен успешно получен");

        return new(cookieStringBuilder.ToString(), csrfToken, serviceLogger);
    }

    public ConvertType[] GetAvailabelConvertTypes()
    {
        return [];
    }

    public Task ConvertAsync(int typeId, string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
