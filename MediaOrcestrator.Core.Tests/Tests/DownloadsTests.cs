namespace MediaOrcestrator.Core.Tests;

public class DownloadsTests : BaseTests
{
    /// <summary>
    /// Создаём тестовые данные билдером,
    /// TestYoutubeService возвращает их как будто от YouTube API,
    /// ChannelService работает с этими данными.
    /// isDownload: false - только сканируем канал без скачивания (без FFmpeg)
    /// </summary>
    [Test]
    public async Task КаналаИспользуетТестовыеДанныеДляСканирования()
    {
        const string Video1Id = "TestVideo01";
        const string Video2Id = "TestVideo02";

        var channel = _client.WithChannel()
            .SetName("TestChannel")
            .SetUrl("https://www.youtube.com/@test_channel");

        var video1 = channel.WithVideo()
            .SetId(Video1Id)
            .SetName("Первое видео");

        var video2 = channel.WithVideo()
            .SetId(Video2Id)
            .SetName("Второе видео");

        _client.Save();

        await GetChannelService().DownloadVideosAsync(channel.Url);

        using var _ = Assert.Multiple();

        var channelDir = Path.Combine(_tempPath, channel.Name);
        await Assert.That(Directory.Exists(channelDir)).IsTrue();

        var dataFile = Path.Combine(channelDir, "data.json");
        await Assert.That(File.Exists(dataFile)).IsTrue();

        var dataContent = await File.ReadAllTextAsync(dataFile);

        await Assert.That(dataContent).Contains(video1.Id);
        await Assert.That(dataContent).Contains(video1.Name);

        await Assert.That(dataContent).Contains(video2.Id);
        await Assert.That(dataContent).Contains(video2.Name);
    }

    [Test]
    public async Task СкачатьВидео()
    {
        var channel = _client.WithChannel()
            .SetName("TestChannel")
            .SetUrl("https://www.youtube.com/@test_channel")
            .WithVideo()
            .SetName("Единственное видео")
            .Channel;

        _client.Save();

        await GetChannelService().DownloadVideosAsync(channel.Url);

        using var _ = Assert.Multiple();

        var channelDir = Path.Combine(_tempPath, channel.Name);
        await Assert.That(Directory.Exists(channelDir)).IsTrue();

        var dataFile = Path.Combine(channelDir, "data.json");
        await Assert.That(File.Exists(dataFile)).IsTrue();

        var videoFile = Path.Combine(channelDir, "videos", $"{channel.Videos[0].Id}.mp4");
        await Assert.That(File.Exists(videoFile)).IsTrue();

        var videoMetadataFile = Path.Combine(channelDir, "videos", $"{channel.Videos[0].Id}.json");
        await Assert.That(File.Exists(videoMetadataFile)).IsTrue();

        var thumbnailFile = Path.Combine(channelDir, "videos", $"{channel.Videos[0].Id}_thumbnail.jpg");
        await Assert.That(File.Exists(thumbnailFile)).IsTrue();
    }
}
