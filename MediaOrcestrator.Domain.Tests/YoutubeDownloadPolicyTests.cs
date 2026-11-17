using MediaOrcestrator.Modules;
using MediaOrcestrator.Youtube;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class YoutubeDownloadPolicyTests
{
    private const string VideoId = "test-video";
    private const string VideoUrl = "https://www.youtube.com/watch?v=test-video";
    private readonly YoutubeDownloadPolicy _policy = new(new());
    private string _stateDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _stateDirectory = Path.Combine(Path.GetTempPath(), TestRandom.GetString("youtube-state"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_stateDirectory))
        {
            Directory.Delete(_stateDirectory, true);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("true")]
    [TestCase("TRUE")]
    public async Task Проверка_при_разрешённых_трансляциях_не_запрашивает_метаданные(string? value)
    {
        var reader = Substitute.For<IYoutubeVideoInfoReader>();
        var settings = ParseSettings(value);

        await _policy.EnsureAllowedAsync(settings, reader, VideoId, CancellationToken.None);

        await reader.DidNotReceiveWithAnyArgs().GetVideoInfoAsync(null!, CancellationToken.None);
    }

    [TestCase("false")]
    [TestCase(" FALSE ")]
    public async Task Проверка_при_запрете_трансляций_разрешает_обычное_видео(string value)
    {
        var reader = CreateReader(YoutubeVideoKind.Video);
        using var cancellation = new CancellationTokenSource();

        await _policy.EnsureAllowedAsync(ParseSettings(value), reader, VideoId, cancellation.Token);

        await reader.Received(1).GetVideoInfoAsync(VideoUrl, cancellation.Token);
    }

    [Test]
    public async Task Проверка_при_запрете_трансляций_сообщает_о_пропуске_эфира()
    {
        var reader = CreateReader(YoutubeVideoKind.Livestream);

        var error = await CaptureAsync(() => _policy.EnsureAllowedAsync(ParseSettings("false"), reader, VideoId, CancellationToken.None));

        Assert.That(error, Is.TypeOf<MediaDownloadSkippedException>());
    }

    [TestCase(YoutubeVideoKind.None)]
    [TestCase(null)]
    public async Task Проверка_при_неизвестном_типе_не_разрешает_скачивание(YoutubeVideoKind? kind)
    {
        var reader = CreateReader(kind);

        var error = await CaptureAsync(() => _policy.EnsureAllowedAsync(ParseSettings("false"), reader, VideoId, CancellationToken.None));

        Assert.That(error, Is.TypeOf<NonRetriableException>());
    }

    [TestCase(YoutubeVideoKind.Video, null)]
    [TestCase(YoutubeVideoKind.Livestream, typeof(MediaDownloadSkippedException))]
    public async Task Проверка_повторно_берёт_тип_видео_из_состояния_источника(YoutubeVideoKind kind, Type? expectedError)
    {
        var reader = CreateReader(kind);
        var settings = new YoutubeDownloadSettings(false, _stateDirectory);
        await CaptureAsync(() => _policy.EnsureAllowedAsync(settings, reader, VideoId, CancellationToken.None));
        var restartedPolicy = new YoutubeDownloadPolicy(new());

        var error = await CaptureAsync(() => restartedPolicy.EnsureAllowedAsync(settings, reader, VideoId, CancellationToken.None));

        Assert.That(error?.GetType(), Is.EqualTo(expectedError));
        await reader.ReceivedWithAnyArgs(1).GetVideoInfoAsync(null!, CancellationToken.None);
    }

    [Test]
    public async Task Проверка_не_запоминает_неизвестный_тип_видео()
    {
        var reader = CreateReader(YoutubeVideoKind.None);
        var settings = new YoutubeDownloadSettings(false, _stateDirectory);

        await CaptureAsync(() => _policy.EnsureAllowedAsync(settings, reader, VideoId, CancellationToken.None));
        await CaptureAsync(() => _policy.EnsureAllowedAsync(settings, reader, VideoId, CancellationToken.None));

        await reader.ReceivedWithAnyArgs(2).GetVideoInfoAsync(null!, CancellationToken.None);
    }

    [Test]
    public async Task Проверка_при_сбое_метаданных_сохраняет_ошибку()
    {
        var reader = Substitute.For<IYoutubeVideoInfoReader>();
        var failure = new HttpRequestException("metadata failed");
        reader.GetVideoInfoAsync(VideoUrl, CancellationToken.None).ThrowsAsync(failure);

        var error = await CaptureAsync(() => _policy.EnsureAllowedAsync(ParseSettings("false"), reader, VideoId, CancellationToken.None));

        Assert.That(error, Is.SameAs(failure));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Проверка_при_отмене_не_запрашивает_метаданные(bool downloadStreams)
    {
        var reader = Substitute.For<IYoutubeVideoInfoReader>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var error = await CaptureAsync(() => _policy.EnsureAllowedAsync(new(downloadStreams), reader, VideoId, cancellation.Token));

        Assert.That(error, Is.InstanceOf<OperationCanceledException>());
        await reader.DidNotReceiveWithAnyArgs().GetVideoInfoAsync(null!, CancellationToken.None);
    }

    [TestCase("no")]
    [TestCase("0")]
    [TestCase("invalid")]
    public void Настройки_при_некорректном_значении_не_снимают_запрет(string value)
    {
        var parse = () => { ParseSettings(value); };

        Assert.That(parse, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Модуль_регистрирует_политику_и_настройку_в_существующем_контракте()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IToolPathProvider>());
        new YoutubeModule().Register(services);
        using var provider = services.BuildServiceProvider();

        var source = provider.GetRequiredService<ISourceType>();
        var setting = source.SettingsKeys.Single(x => x.Key == YoutubeDownloadSettings.DownloadStreamsKey);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ParseSettings(setting.DefaultValue).DownloadStreams, Is.True);
            Assert.That(setting.Type, Is.EqualTo(SettingType.Dropdown));
            Assert.That(setting.Options!.Select(x => ParseSettings(x.Value).DownloadStreams), Is.EquivalentTo(new[] { true, false }));
        }
    }

    private static YoutubeDownloadSettings ParseSettings(string? value)
    {
        Dictionary<string, string> settings = [];
        if (value is not null)
        {
            settings[YoutubeDownloadSettings.DownloadStreamsKey] = value;
        }

        return YoutubeDownloadSettings.FromSettings(settings);
    }

    private static IYoutubeVideoInfoReader CreateReader(YoutubeVideoKind? kind)
    {
        var reader = Substitute.For<IYoutubeVideoInfoReader>();
        YtDlpVideoInfo? info = kind.HasValue
            ? new("test-video", "Видео", null, null, null, null, null, null,
                [], kind.Value)
            : null;

        reader.GetVideoInfoAsync(VideoUrl, Arg.Any<CancellationToken>()).Returns(info);
        return reader;
    }

    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
