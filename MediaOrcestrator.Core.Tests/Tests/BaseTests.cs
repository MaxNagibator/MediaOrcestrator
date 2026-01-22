using MediaOrcestrator.Core.Services;
using MediaOrcestrator.Core.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediaOrcestrator.Core.Tests;

public class BaseTests
{
    protected static string _baseTempPath = Path.Combine(Path.GetTempPath(), "MediaOrcestrator.Core");
    private static int _tempPathNumber;
    protected TestYoutubeDataClient _client = null!;
    protected string _tempPath = null!;

    [Before(Assembly)]
    public static void BeforeAssembly()
    {
        try
        {
            if (Directory.Exists(_baseTempPath))
            {
                Directory.Delete(_baseTempPath, true);
            }
        }
        catch (IOException)
        {
        }
    }

    [Before(Test)]
    public void Setup()
    {
        _client = new();
        var currentNumber = Interlocked.Increment(ref _tempPathNumber);
        _tempPath = Path.Combine(_baseTempPath, "YoutubeTests_" + currentNumber);
        Directory.CreateDirectory(_tempPath);
    }

    [After(Test)]
    public void TearDown()
    {
        _client.Clear();

        // TODO: Подумать
        try
        {
            if (Directory.Exists(_tempPath))
            {
                // Directory.Delete(_tempPath, true);
            }
        }
        catch (IOException)
        {
        }
    }

    public ChannelService GetChannelService()
    {
        var testYoutubeService = new TestYoutubeService(_client.Storage);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DownloadOptions:MaxDownloadsPerRun"] = "10",
                ["DownloadOptions:VideoFolderPath"] = _tempPath,
            })
            .Build();

        var services = new ServiceCollection()
            .AddYoutubeChannelDownloader(configuration)
            .AddSingleton<IYoutubeService>(testYoutubeService)
            .AddSingleton<IVideoConverter, TestVideoConverter>()
            .AddSingleton<IPictureDownloader, TestPictureDownloader>();

        var provider = services.BuildServiceProvider();
        var channelService = provider.GetRequiredService<ChannelService>();
        return channelService;
    }
}
