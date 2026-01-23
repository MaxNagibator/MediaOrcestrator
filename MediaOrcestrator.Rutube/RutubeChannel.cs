using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Rutube;

public class RutubeChannel : IMediaSource
{
    public ChannelType ChannelType => ChannelType.OnlyUpload;

    public string Name => "Rutube";

    public IEnumerable<SourceSettings> SettingsKeys { get; }

    public IMedia[] GetMedia()
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IMedia> GetMedia(Dictionary<string, string> settings)
    {
        throw new NotImplementedException();
    }

    public IMedia GetMediaById()
    {
        throw new NotImplementedException();
    }

    public IMedia Download()
    {
        Console.WriteLine("я загрузил брат");
        throw new NotImplementedException();
    }

    public void Upload(IMedia media)
    {
        Console.WriteLine("я загрузил брат " + media.Title);
    }
}

public class RutubeMedia : IMedia
{
    public string Title { get; set; }
    public string Description { get; set; }

    public string Id => throw new NotImplementedException();
}
