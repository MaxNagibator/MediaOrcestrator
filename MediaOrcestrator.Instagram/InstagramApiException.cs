using System.Net;

namespace MediaOrcestrator.Instagram;

public sealed class InstagramApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
