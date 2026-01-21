using MediaOrcestrator.Core.Services;

namespace MediaOrcestrator.HardDiskDrive
{
    public class HardDiskDriveChannel : IMediaSource
    {
        public ChannelType ChannelType => ChannelType.OnlyUpload;

        public string Name => "HardDiskDrive";

        public IMedia[] GetMedia()
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

        IAsyncEnumerable<IMedia> IMediaSource.GetMedia()
        {
            throw new NotImplementedException();
        }
    }

    public class HardDiskDriveMedia : IMedia
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public string Id => throw new NotImplementedException();
    }
}
