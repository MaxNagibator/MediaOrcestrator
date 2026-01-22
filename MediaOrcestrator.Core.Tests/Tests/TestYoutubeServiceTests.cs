namespace MediaOrcestrator.Core.Tests;

public class TestYoutubeServiceTests : BaseTests
{
    [Test]
    public async Task ВозвращаетКаналИзХранилищаПоUrl()
    {
        const string ValidChannelId = "UC1234567890123456789012";

        var channel = _client.WithChannel()
            .SetId(ValidChannelId)
            .SetName("это мой канал. он мне принадлежит")
            .SetUrl("https://www.youtube.com/@my_channel");

        _client.Save();

        var service = new TestYoutubeService(_client.Storage);

        var result = await service.GetChannel("https://www.youtube.com/@my_channel");

        await Assert.That(result).IsNotNull();

        using var _ = Assert.Multiple();
        {
            await Assert.That(result.Id.Value).EqualTo(channel.Id);
            await Assert.That(result.Title).EqualTo(channel.Name);
        }
    }

    [Test]
    public async Task ВозвращаетВидеоКаналаИзХранилища()
    {
        var channel = _client.WithChannel()
            .SetName("МаксимДваЯйца")
            .SetUrl("https://www.youtube.com/@cooking");

        var topVideo1 = channel.WithVideo().SetName("Рецепт хрючева из красного булдака");
        var topVideo2 = channel.WithVideo().SetName("Пробую царских наполеон");
        _client.Save();

        var service = new TestYoutubeService(_client.Storage);

        var videoTitles = new List<string>();
        await foreach (var video in service.GetUploadsAsync(channel.Url))
        {
            videoTitles.Add(video.Title);
        }

        using var _ = Assert.Multiple();
        {
            await Assert.That(videoTitles.Count).IsEqualTo(2);
            await Assert.That(videoTitles).Contains(topVideo1.Name);
            await Assert.That(videoTitles).Contains(topVideo2.Name);
        }
    }

    [Test]
    public async Task ВозвращаетМетаданныеВидеоИзХранилища()
    {
        // TODO: Подумать над генератором валидных id
        const string ValidVideoId = "CustomVid01";

        var channel = _client.WithChannel();
        var video = channel.WithVideo()
            .SetId(ValidVideoId)
            .SetName("Неправильно рассчитал скорость")
            .SetDescription("полика жалко(")
            .SetDuration(TimeSpan.FromMinutes(45))
            .SetViewCount(500000)
            .SetLikeCount(25000)
            .SetKeywords("tutorial", "real-live");

        _client.Save();

        var service = new TestYoutubeService(_client.Storage);

        var result = await service.GetVideoAsync(video.Url);

        using var _ = Assert.Multiple();
        {
            await Assert.That(result.Id.Value).EqualTo(video.Id);
            await Assert.That(result.Title).EqualTo(video.Name);
            await Assert.That(result.Description).EqualTo(video.Description);
            await Assert.That(result.Duration).EqualTo(video.Duration);
            await Assert.That(result.Engagement.ViewCount).EqualTo(video.ViewCount);
            await Assert.That(result.Engagement.LikeCount).EqualTo(video.LikeCount);
            await Assert.That(result.Keywords).Contains(video.Keywords[0]);
            await Assert.That(result.Keywords).Contains(video.Keywords[^1]);
        }
    }

    [Test]
    public async Task ОтслеживаетСкачивания()
    {
        var service = new TestYoutubeService(_client.Storage);
        var filePath = Path.Combine(_tempPath, "test.mp4");

        var manifest = await service.GetStreamManifestAsync("any_url");
        await service.DownloadAsync(manifest.Streams[0], filePath, null, CancellationToken.None);

        using var _ = Assert.Multiple();
        {
            await Assert.That(service.DownloadCallCount).EqualTo(1);
            await Assert.That(service.WasFileDownloaded(filePath)).IsTrue();
        }
    }
}
