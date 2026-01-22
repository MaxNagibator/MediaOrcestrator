namespace MediaOrcestrator.Core.Services;

public interface IMediaSource
{
    string Name { get; }
    ChannelType ChannelType { get; }

    IAsyncEnumerable<IMedia> GetMedia();

    IMedia GetMediaById();
    void Upload(IMedia media);
    IMedia Download();
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
