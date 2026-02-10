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

    IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings);

    MediaDto GetMediaById();
    Task<string> Upload(MediaDto media, Dictionary<string, string> settings);
    Task<MediaDto> Download(string videoId, Dictionary<string, string> settings);
}

public enum SettingType
{
    None = 0,
    Text = 1,
    Dropdown = 2,
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
