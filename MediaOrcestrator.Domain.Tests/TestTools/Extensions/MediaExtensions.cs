namespace MediaOrcestrator.Domain.Tests.TestTools.Extensions;

public static class MediaExtensions
{
    public static MediaSourceLink? LinkTo(this Media media, Source source)
    {
        return media.Sources.SingleOrDefault(x => x.SourceId == source.Id);
    }
}
