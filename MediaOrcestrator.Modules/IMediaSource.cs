namespace MediaOrcestrator.Modules;

public interface IMediaSource
{
    string Name { get; }
    ChannelType ChannelType { get; }

    IEnumerable<SourceSettings> SettingsKeys { get; }

    IAsyncEnumerable<IMedia> GetMedia(Dictionary<string, string> settings);

    IMedia GetMediaById();
    void Upload(IMedia media);
    IMedia Download();
}

public class SourceSettings
{
    public string Key { get; set; }
    public string Title { get; set; }
    public bool IsRequired { get; set; }
}

public enum ChannelType
{
    OnlyDownload = 1,
    OnlyUpload = 2,
    Full = 3,
}

public interface IMedia
{
    string Id { get; }
    string Title { get; }
    string Description { get; }
}
