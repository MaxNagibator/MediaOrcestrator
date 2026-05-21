using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Rutube;

public interface IRutubeServiceFactory
{
    RutubeService Create(
        string cookieString,
        string studioCsrfToken,
        string playerCsrfToken);
}

public sealed class RutubeServiceFactory(IHttpClientFactory httpClientFactory, ILogger<RutubeService> logger) : IRutubeServiceFactory
{
    public const string ApiClientName = "Rutube.Api";
    public const string UploadClientName = "Rutube.Upload";

    public RutubeService Create(
        string cookieString,
        string studioCsrfToken,
        string playerCsrfToken)
    {
        var apiClient = httpClientFactory.CreateClient(ApiClientName);
        var uploadClient = httpClientFactory.CreateClient(UploadClientName);
        return new(apiClient, uploadClient, cookieString, studioCsrfToken, playerCsrfToken, logger);
    }
}
