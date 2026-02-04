using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Rutube;

public class RutubeChannel : ISourceType
{
    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "Rutube";

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

    public Task<MediaDto> Download(string videoId, Dictionary<string, string> settings)
    {
        Console.WriteLine("я загрузил брат");
        throw new NotImplementedException();
    }

    public Task<string> Upload(MediaDto media, Dictionary<string, string> settings)
    {
        Console.WriteLine("я загрузил брат " + media.Title);
        return Task.FromResult("rutube_id_placeholder");
    }
}

//public class RutubeMedia : Media
//{
//    public string Title { get; set; }
//    public string Description { get; set; }

//    public string Id => throw new NotImplementedException();
//}
