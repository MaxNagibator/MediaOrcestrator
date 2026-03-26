using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaOrcestrator.Domain;

public class GitHubReleaseProvider(IHttpClientFactory httpClientFactory, ILogger<GitHubReleaseProvider> logger) : IReleaseProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<ReleaseInfo?> GetLatestReleaseAsync(string repo, string assetPattern, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("GitHub");

        try
        {
            var release = await TryGetReleaseAsync(client, $"https://api.github.com/repos/{repo}/releases/latest", cancellationToken);

            if (release is not null && !release.Assets.Any(a => GlobMatcher.IsMatch(a.Name, assetPattern)))
            {
                release = null;
            }

            if (release is null)
            {
                var releases = await client.GetFromJsonAsync<List<GitHubRelease>>($"https://api.github.com/repos/{repo}/releases",
                    JsonOptions,
                    cancellationToken);

                release = releases?.FirstOrDefault(r => !r.Prerelease && r.Assets.Any(a => GlobMatcher.IsMatch(a.Name, assetPattern)));
            }

            if (release is null)
            {
                logger.LogWarning("Релизы не найдены для {Repo}", repo);
                return null;
            }

            var matchingAsset = release.Assets.FirstOrDefault(a => GlobMatcher.IsMatch(a.Name, assetPattern));

            return new()
            {
                TagName = release.TagName,
                AssetUrl = matchingAsset?.BrowserDownloadUrl,
                AssetName = matchingAsset?.Name,
                AssetSize = matchingAsset?.Size ?? 0,
                PublishedAt = release.PublishedAt,
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            logger.LogWarning("Превышен лимит запросов GitHub API для {Repo}", repo);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Не удалось проверить релизы для {Repo}", repo);
            return null;
        }
    }

    public async Task DownloadAssetAsync(string url, string targetPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("GitHub");

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var buffer = new byte[81920];
        long bytesRead = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(targetPath);

        int read;

        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesRead += read;

            if (totalBytes > 0)
            {
                progress?.Report((double)bytesRead / totalBytes);
            }
        }
    }

    private async Task<GitHubRelease?> TryGetReleaseAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GitHubRelease>(JsonOptions, cancellationToken);
    }

    private sealed record GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; init; }

        [JsonPropertyName("assets")]
        public List<GithubAsset> Assets { get; init; } = [];
    }

    private sealed record GithubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public int Size { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; } = string.Empty;
    }
}
