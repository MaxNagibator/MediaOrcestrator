namespace MediaOrcestrator.Youtube;

internal interface IYoutubeVideoInfoReader
{
    Task<YtDlpVideoInfo?> GetVideoInfoAsync(string url, CancellationToken cancellationToken = default);
}
