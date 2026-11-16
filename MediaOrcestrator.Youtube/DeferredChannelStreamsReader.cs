namespace MediaOrcestrator.Youtube;

internal sealed class DeferredChannelStreamsReader(Func<IYoutubeChannelStreamsReader> createReader) : IYoutubeChannelStreamsReader
{
    public Task<IReadOnlySet<string>> GetChannelStreamIdsAsync(string channelId, CancellationToken cancellationToken = default)
    {
        return createReader().GetChannelStreamIdsAsync(channelId, cancellationToken);
    }
}
