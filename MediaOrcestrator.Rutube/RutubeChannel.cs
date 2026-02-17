using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.Rutube;

// TODO: Костыль с ILogger<RutubeService>. Желательно сделать полноценную регистрацию модулей в DI.
public class RutubeChannel(ILogger<RutubeChannel> logger, ILogger<RutubeService> serviceLogger) : ISourceType
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
    ];

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

    public MediaDto[] GetMedia()
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings)
    {
        logger.LogWarning("Получение списка медиа из RuTube не реализовано");
        await Task.CompletedTask;
        yield break;
    }

    public MediaDto GetMediaById()
    {
        throw new NotImplementedException();
    }

    public Task<MediaDto> Download(string videoId, Dictionary<string, string> settings)
    {
        logger.LogWarning("Загрузка видео с RuTube не реализована. ID: {VideoId}", videoId);
        throw new NotImplementedException("Загрузка с RuTube не поддерживается");
    }

    public async Task<string> Upload(MediaDto media, Dictionary<string, string> settings)
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

        try
        {
            var videoId = await rutubeService.UploadVideoAsync(filePath, media.Title, media.Description, rutubeCategoryId);
            logger.LogInformation("Видео успешно загружено на RuTube. Video ID: {SessionId}, Название: '{Title}'", videoId, media.Title);
            return videoId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке видео на RuTube. Название: '{Title}'", media.Title);
            throw;
        }
    }

    public async Task DeleteAsync(string externalId, Dictionary<string, string> settings)
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
}

//public class RutubeMedia : Media
//{
//    public string Title { get; set; }
//    public string Description { get; set; }

//    public string Id => throw new NotImplementedException();
//}
