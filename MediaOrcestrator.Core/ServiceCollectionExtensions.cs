using MediaOrcestrator.Core.Configurations;
using MediaOrcestrator.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using YoutubeExplode;

namespace MediaOrcestrator.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYoutubeChannelDownloader(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DownloadOptions>(configuration.GetSection(nameof(DownloadOptions)))
            .Configure<FFmpegOptions>(configuration.GetSection(nameof(FFmpegOptions)))
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog();
            })
            .AddSingleton<ILoggerFactory>(SerilogFactory.Init)
            .AddSingleton<VideoDownloaderService>()
            .AddSingleton<DirectoryService>()
            .AddSingleton<YoutubeClient>()
            .AddSingleton<DownloadService>()
            .AddSingleton<IYoutubeService, YoutubeService>()
            .AddSingleton<IVideoConverter, FFmpegConverter>()
            .AddSingleton<IPictureDownloader, PictureDownloader>()
            .AddSingleton<FFmpeg>()
            .AddSingleton<HttpClient>()
            .AddSingleton<ChannelService>();

        return services;
    }
}
