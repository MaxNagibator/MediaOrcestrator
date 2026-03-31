namespace MediaOrcestrator.Modules;

public interface ISourceType
{
    string Name { get; }

    SyncDirection ChannelType { get; }

    IEnumerable<SourceSettings> SettingsKeys { get; }

    Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        return Task.FromResult(new List<SettingOption>());
    }

    Task ConvertAsync(
        int typeId,
        string externalId,
        Dictionary<string, string> settings,
        IProgress<ConvertProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        return ConvertAsync(typeId, externalId, settings, cancellationToken);
    }

    ConvertAvailability CheckConvertAvailability(int typeId, MediaDto media)
    {
        return new(false, "Конвертация не поддерживается");
    }

    /// <summary>
    /// Самые свежие в самом начале.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, CancellationToken cancellationToken = default);

    Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
    Task<UploadResult> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
    // todo name postfix Async
    Task<MediaDto> Download(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
    Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
    Task<UploadResult> Update(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken);

    Uri? GetExternalUri(string externalId, Dictionary<string, string> settings) => null;

    ConvertType[] GetAvailabelConvertTypes();
    Task ConvertAsync(int typeId, string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
}

public class ConvertType
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public record ConvertAvailability(bool IsAvailable, string? Reason);

public record ConvertProgress(double Percent, string FileName);

public enum SettingType
{
    None = 0,
    Text = 1,
    Dropdown = 2,
}


/// <summary>
/// Результат загрузки.
/// </summary>
public class UploadResult
{
    /// <summary>
    /// Статус загрузки.
    /// </summary>
    public MediaStatus Status { get; set; }

    /// <summary>
    /// Дополнительный текст к статусу.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Идинетификатор загруженного медиа в источнике.
    /// </summary>
    public string? Id { get; set; }
}

public class SourceSettings
{
    public required string Key { get; set; }
    public required string Title { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public SettingType Type { get; set; } = SettingType.Text;
    public List<SettingOption>? Options { get; set; }
}

public class SettingOption
{
    public required string Value { get; set; }
    public required string Label { get; set; }
}

public enum SyncDirection
{
    OnlyDownload = 1,
    OnlyUpload = 2,
    Full = 3,
}
