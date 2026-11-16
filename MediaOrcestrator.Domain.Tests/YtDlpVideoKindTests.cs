using MediaOrcestrator.Youtube;
using System.Text.Json;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class YtDlpVideoKindTests
{
    [TestCase("{\"live_status\":\"not_live\"}", YoutubeVideoKind.Video)]
    [TestCase("{\"live_status\":\"is_live\"}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"live_status\":\"is_upcoming\"}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"live_status\":\"was_live\"}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"live_status\":\"post_live\"}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"is_live\":true}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"was_live\":true}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"is_live\":false,\"was_live\":false}", YoutubeVideoKind.Video)]
    [TestCase("{\"live_status\":\"not_live\",\"was_live\":true}", YoutubeVideoKind.Livestream)]
    [TestCase("{\"live_status\":\"is_live\",\"is_live\":false,\"was_live\":false}", YoutubeVideoKind.Livestream)]
    [TestCase("{}", YoutubeVideoKind.None)]
    [TestCase("{\"live_status\":null}", YoutubeVideoKind.None)]
    [TestCase("{\"is_live\":false}", YoutubeVideoKind.None)]
    [TestCase("{\"live_status\":\"future_status\",\"is_live\":false,\"was_live\":false}", YoutubeVideoKind.None)]
    public void Метаданные_по_статусу_и_признакам_определяют_тип_видео(string json, YoutubeVideoKind expected)
    {
        var info = JsonSerializer.Deserialize(json, YoutubeJsonContext.Default.YtDlpInfoJson)!;

        var video = YtDlp.MapVideoInfo(info);

        Assert.That(video.Kind, Is.EqualTo(expected));
    }
}
