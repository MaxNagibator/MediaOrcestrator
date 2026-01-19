using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using MediaOrcestrator.Core;
using MediaOrcestrator.Core.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile("appsettings.Development.json", true, true)
    .Build();

var services = new ServiceCollection()
    .AddYoutubeChannelDownloader(configuration);

var serviceProvider = services.BuildServiceProvider();
var service = serviceProvider.GetRequiredService<ChannelService>();

var channelUrl = configuration["Channel:Url"];

if (string.IsNullOrWhiteSpace(channelUrl))
{
    throw new InvalidOperationException("URL канала не настроен. Пожалуйста, укажите 'Channel:Url' в appsettings.");
}

var path1 = "..\\..\\..\\..\\ModuleBuilds";


var scanner = new InterfaceScanner();
var myInterfaceType = typeof(IMediaSource); // Пример интерфейса
var implementations = scanner.FindImplementations(path1, myInterfaceType);
foreach (var x in implementations)
{
    try
    {
        var aaaa = (IMediaSource)Activator.CreateInstance(x.Type);
        aaaa.Download();
    }
    catch
    {

    }
}


await service.DownloadVideosAsync(channelUrl);

Log.CloseAndFlush();
