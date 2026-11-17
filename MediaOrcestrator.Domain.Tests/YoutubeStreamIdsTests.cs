using MediaOrcestrator.Youtube;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using System.Text.Json;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class YoutubeStreamIdsTests
{
    private const string ChannelId = "UCtest0000000000000000000";

    [Test]
    public async Task Плейлист_трансляций_при_недоступности_уступает_вкладке_трансляций()
    {
        var service = CreateServiceWithUnavailablePlaylists();
        var fallback = Substitute.For<IYoutubeChannelStreamsReader>();
        IReadOnlySet<string> fromTab = new HashSet<string> { "stream-1" };
        fallback.GetChannelStreamIdsAsync(ChannelId, Arg.Any<CancellationToken>()).Returns(fromTab);

        var streamIds = await service.GetStreamIdsAsync(ChannelId, fallback, CancellationToken.None);

        Assert.That(streamIds, Is.SameAs(fromTab));
    }

    [Test]
    public async Task Плейлист_трансляций_при_сбое_вкладки_останавливает_сканирование()
    {
        var service = CreateServiceWithUnavailablePlaylists();
        var fallback = Substitute.For<IYoutubeChannelStreamsReader>();
        var failure = new InvalidOperationException("yt-dlp failed");
        fallback.GetChannelStreamIdsAsync(ChannelId, Arg.Any<CancellationToken>()).ThrowsAsync(failure);

        Exception? error = null;
        try
        {
            await service.GetStreamIdsAsync(ChannelId, fallback, CancellationToken.None);
        }
        catch (Exception exception)
        {
            error = exception;
        }

        Assert.That(error, Is.SameAs(failure));
    }

    [TestCase("ERROR: [youtube:tab] UCtest: This channel does not have a streams tab", true)]
    [TestCase("ERROR: [youtube:tab] UCtest: HTTP Error 429: Too Many Requests", false)]
    [TestCase("", false)]
    public void Вкладка_трансляций_отсутствует_только_по_явному_ответу_ytdlp(string stdErr, bool expected)
    {
        Assert.That(YtDlp.IsMissingStreamsTab(stdErr), Is.EqualTo(expected));
    }

    [TestCase("""{"id":"UCtest","entries":[{"id":"stream-1"},{"id":null},{"id":"stream-2"}]}""", new[] { "stream-1", "stream-2" })]
    [TestCase("""{"id":"UCtest","entries":[]}""", new string[0])]
    public void Вкладка_трансляций_разбирается_из_плоского_списка_ytdlp(string json, string[] expected)
    {
        var playlist = JsonSerializer.Deserialize(json, YoutubeJsonContext.Default.YtDlpPlaylistJson)!;

        Assert.That(YtDlp.ParseStreamIds(playlist), Is.EquivalentTo(expected));
    }

    [TestCase("{}")]
    [TestCase("""{"id":"UCtest","entries":null}""")]
    public void Вкладка_трансляций_без_списка_записей_останавливает_сканирование(string json)
    {
        var playlist = JsonSerializer.Deserialize(json, YoutubeJsonContext.Default.YtDlpPlaylistJson)!;

        var parse = () => { YtDlp.ParseStreamIds(playlist); };

        Assert.That(parse, Throws.TypeOf<InvalidOperationException>());
    }

    private static YoutubeExplodeReadService CreateServiceWithUnavailablePlaylists()
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new EmptyResponseHandler()));
        return new(httpClientFactory, NullLogger<YoutubeExplodeReadService>.Instance);
    }

    private sealed class EmptyResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }
}
