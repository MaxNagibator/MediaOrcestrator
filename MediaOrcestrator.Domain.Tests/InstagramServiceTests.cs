using MediaOrcestrator.Instagram;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace MediaOrcestrator.Domain.Tests;

public sealed class InstagramServiceTests
{
    [Test]
    public async Task PublishReelAsync_UsesResumableUploadAndPublishesContainer()
    {
        var requests = new List<(HttpMethod Method, Uri Uri, string? Authorization)>();
        var responses = new Queue<HttpResponseMessage>(
        [
            Json("{\"id\":\"container-1\"}"),
            Json("{\"success\":true}"),
            Json("{\"status_code\":\"FINISHED\"}"),
            Json("{\"id\":\"media-1\"}"),
        ]);
        using var client = new HttpClient(new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!, request.Headers.Authorization?.ToString()));
            return responses.Dequeue();
        }));
        var service = new InstagramService(client, Options.Create(new InstagramOptions
        {
            ProcessingPollInterval = TimeSpan.FromMilliseconds(1),
            ProcessingTimeout = TimeSpan.FromSeconds(1),
        }));
        var filePath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(filePath, [1, 2, 3]);

        try
        {
            var result = await service.PublishReelAsync(
                "v27.0", "ig-1", "secret-token", filePath, "caption", true, null, CancellationToken.None);

            Assert.That(result, Is.EqualTo("media-1"));
            Assert.That(requests, Has.Count.EqualTo(4));
            Assert.That(requests[0].Uri.AbsoluteUri, Does.EndWith("/v27.0/ig-1/media"));
            Assert.That(requests[1].Uri.Host, Is.EqualTo("rupload.facebook.com"));
            Assert.That(requests[1].Authorization, Is.EqualTo("OAuth secret-token"));
            Assert.That(requests[2].Uri.Query, Does.Contain("status_code"));
            Assert.That(requests[3].Uri.AbsoluteUri, Does.EndWith("/v27.0/ig-1/media_publish"));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Test]
    public void Channel_IsUploadOnly()
    {
        var channel = new InstagramChannel(null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<InstagramChannel>.Instance);
        Assert.That(channel.ChannelType, Is.EqualTo(MediaOrcestrator.Modules.SyncDirection.OnlyUpload));
    }

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(value, Encoding.UTF8, "application/json"),
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
