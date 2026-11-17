namespace MediaOrcestrator.Youtube;

internal interface IYoutubeChannelStreamsReader
{
    Task<IReadOnlySet<string>> GetChannelStreamIdsAsync(string channelId, CancellationToken cancellationToken = default);
}
