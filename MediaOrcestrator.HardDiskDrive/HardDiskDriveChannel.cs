using MediaOrcestrator.Modules;

namespace MediaOrcestrator.HardDiskDrive;

public class HardDiskDriveChannel : ISourceType
{
    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "HardDiskDrive";

    public IEnumerable<SourceSettings> SettingsKeys { get; }

    public MediaDto[] GetMedia()
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings)
    {
        throw new NotImplementedException();
    }

    public MediaDto GetMediaById()
    {
        throw new NotImplementedException();
    }

    public MediaDto Download()
    {
        Console.WriteLine("я загрузил брат");
        throw new NotImplementedException();
    }

    public void Upload(MediaDto media)
    {
        Console.WriteLine("я загрузил брат " + media.Title);
    }
}

public class HardDiskDriveMedia : MediaDto
{
    public string Title { get; set; }
    public string Description { get; set; }

    public string Id => throw new NotImplementedException();
}
