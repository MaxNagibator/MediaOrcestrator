namespace MediaOrcestrator.Domain;

public interface IReleaseProvider
{
    Task<ReleaseInfo?> GetLatestReleaseAsync(string repo, string assetPattern, CancellationToken cancellationToken = default);
    Task DownloadAssetAsync(string url, string targetPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
