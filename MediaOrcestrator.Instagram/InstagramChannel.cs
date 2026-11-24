using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MediaOrcestrator.Instagram;

public sealed class InstagramChannel(InstagramService service, ILogger<InstagramChannel> logger)
    : ISourceType, IAuthenticatable
{
    private const string DefaultApiVersion = "v27.0";

    public string Name => "Instagram";
    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "instagram_user_id",
            Title = "Instagram Professional Account ID",
            Description = "Числовой ID Business/Creator аккаунта Instagram",
            IsRequired = true,
        },
        new()
        {
            Key = "access_token",
            Title = "токен Meta",
            Description = "Page access token с разрешениями instagram_basic и instagram_content_publish",
            IsRequired = true,
            Type = SettingType.Secret,
        },
        new()
        {
            Key = "share_to_feed",
            Title = "показывать Reel в ленте",
            IsRequired = true,
            DefaultValue = "true",
            Type = SettingType.Dropdown,
            Options =
            [
                new() { Value = "true", Label = "Да" },
                new() { Value = "false", Label = "Нет" },
            ],
        },
        new()
        {
            Key = "api_version",
            Title = "версия Meta Graph API",
            Description = $"Оставьте {DefaultApiVersion}, если Meta не требует другую версию",
            IsRequired = true,
            DefaultValue = DefaultApiVersion,
        },
    ];

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
        => null;

    public async Task<UploadResult> UploadAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(media.TempDataPath))
        {
            throw new FileNotFoundException("Файл Reel не найден", media.TempDataPath);
        }

        try
        {
            var caption = BuildCaption(media);
            var uploadProgress = progress is null
                ? null
                : new Progress<double>(value => progress.Report(new UploadProgress(value * 100)));
            var id = await service.PublishReelAsync(
                GetApiVersion(settings),
                Required(settings, "instagram_user_id"),
                Required(settings, "access_token"),
                media.TempDataPath,
                caption,
                !string.Equals(settings.GetValueOrDefault("share_to_feed"), "false", StringComparison.OrdinalIgnoreCase),
                uploadProgress,
                cancellationToken);

            logger.LogInformation("Reel опубликован в Instagram: {MediaId}", id);
            return new() { Id = id, Status = MediaStatusHelper.Ok() };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось опубликовать Reel в Instagram");
            return new() { Status = MediaStatusHelper.GetById(MediaStatus.Error), Message = ex.Message };
        }
    }

    public bool IsAuthenticated(Dictionary<string, string> settings)
        => !string.IsNullOrWhiteSpace(settings.GetValueOrDefault("instagram_user_id"))
           && !string.IsNullOrWhiteSpace(settings.GetValueOrDefault("access_token"));

    public async Task AuthenticateAsync(Dictionary<string, string> settings, IAuthUI ui, CancellationToken ct)
    {
        var username = await service.ValidateAccountAsync(
            GetApiVersion(settings),
            Required(settings, "instagram_user_id"),
            Required(settings, "access_token"),
            ct);
        await ui.ShowMessageAsync($"Авторизация Instagram подтверждена: @{username}");
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(
        Dictionary<string, string> settings,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        => Task.FromResult<MediaDto?>(null);

    public Task<MediaDto> DownloadAsync(string videoId, Dictionary<string, string> settings, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Instagram-плагин поддерживает только публикацию.");

    public Task<UploadResult> UpdateAsync(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        => Task.FromResult(new UploadResult
        {
            Status = MediaStatusHelper.GetById(MediaStatus.Error),
            Message = "Изменение опубликованных Reels пока не поддерживается.",
        });

    public Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Удаление Reels пока не поддерживается.");

    private static string BuildCaption(MediaDto media)
    {
        var caption = string.IsNullOrWhiteSpace(media.Description)
            ? media.Title
            : $"{media.Title}\n{media.Description}";
        return caption.Length <= 2200 ? caption : caption[..2200];
    }

    private static string GetApiVersion(Dictionary<string, string> settings)
    {
        var value = settings.GetValueOrDefault("api_version", DefaultApiVersion).Trim();
        return value.StartsWith('v') ? value : $"v{value}";
    }

    private static string Required(Dictionary<string, string> settings, string key)
        => !string.IsNullOrWhiteSpace(settings.GetValueOrDefault(key))
            ? settings[key].Trim()
            : throw new InvalidOperationException($"Не заполнена настройка {key}.");
}
