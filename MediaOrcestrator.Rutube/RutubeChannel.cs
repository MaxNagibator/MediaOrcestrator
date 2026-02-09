using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MediaOrcestrator.Rutube;

public class RutubeChannel(ILogger<RutubeChannel> logger) : ISourceType
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
        },
        new()
        {
            Key = "category_id",
            IsRequired = true,
            Title = "идентификатор категории",
        },
    ];

    public MediaDto[] GetMedia()
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings)
    {
        yield return new MediaDto()
        {
            Id = "1",
        };
    }

    public MediaDto GetMediaById()
    {
        throw new NotImplementedException();
    }

    public Task<MediaDto> Download(string videoId, Dictionary<string, string> settings)
    {
        logger.LogInformation("я загрузил брат");
        throw new NotImplementedException();
    }

    public async Task<string> Upload(MediaDto media, Dictionary<string, string> settings)
    {
        logger.LogInformation("я загрузил брат " + media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            throw new Exception("!!!");
        }


        logger.LogInformation("\n--- Video Publication ---");

        List<CategoryInfo> rutubeCategories = new();


        var authStatePath = settings["auth_state_path"];
        string? rutubeCategoryId = settings["category_id"];

        if (!File.Exists(authStatePath))
        {
            logger.LogInformation("auth_state.json not found. Please run the app without arguments first to login.");
            throw new Exception("!!!!!!");
        }

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
            logger.LogInformation("CSRF token not found in auth_state.json.");
            throw new Exception("!!!!!!!!!");
        }

        var rutubeService = new RutubeService(cookieStringBuilder.ToString(), csrfToken, false);
        var huy  = await rutubeService.UploadVideoAsync(filePath, media.Title, media.Description, rutubeCategoryId);
        return huy;
    }
}

//public class RutubeMedia : Media
//{
//    public string Title { get; set; }
//    public string Description { get; set; }

//    public string Id => throw new NotImplementedException();
//}
