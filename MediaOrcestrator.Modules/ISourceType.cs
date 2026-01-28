namespace MediaOrcestrator.Modules;

public interface ISourceType
{
    string Name { get; }
    SyncDirection ChannelType { get; }

    IEnumerable<SourceSettings> SettingsKeys { get; }

    IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings);

    MediaDto GetMediaById();
    void Upload(MediaDto media);
    MediaDto Download();
}

public class SourceSettings
{
    public string Key { get; set; }
    public string Title { get; set; }
    public bool IsRequired { get; set; }
}

public enum SyncDirection
{
    OnlyDownload = 1,
    OnlyUpload = 2,
    Full = 3,
}
