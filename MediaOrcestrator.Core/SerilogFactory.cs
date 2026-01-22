using MediaOrcestrator.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Extensions.Logging;

namespace MediaOrcestrator.Core;

public static class SerilogFactory
{
    public static SerilogLoggerFactory Init(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<DownloadOptions>>();
        var logPath = Path.Combine(options.Value.VideoFolderPath, "logs", "verbose.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}]({SourceContext}) {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Logger = logger;
        return new(logger);
    }
}
