using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrcestrator.Core.Services
{
    public interface IMediaSource
    {
        string Name { get; }

        IAsyncEnumerable<IMedia> GetMedia();

        IMedia GetMediaById();
        void Upload(IMedia media);
        IMedia Download();
        ChannelType ChannelType { get; }
    }

    public enum ChannelType
    {
        OnlyDownload = 1,
        OnlyUpload = 2,
        Full = 3,
    }

    public interface IMedia
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
    }
}
