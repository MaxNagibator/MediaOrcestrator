using LiteDB;
using MediaOrcestrator.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.RichTextBoxForms.Themes;

namespace MediaOrcestrator.Runner;

file static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // TODO: Выглядит не очень
        var logControl = new RichTextBox();
        logControl.BackColor = SystemColors.Window;
        logControl.Dock = DockStyle.Fill;
        logControl.Font = new("Cascadia Mono", 10.8F, FontStyle.Regular, GraphicsUnit.Point);
        logControl.Location = new(0, 0);
        logControl.Name = "uiLogControl";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .WriteTo.RichTextBox(logControl, ThemePresets.Literate)
            .CreateLogger();

        try
        {
            Log.Information("Приложение запускается...");
            var services = new ServiceCollection();
            services.AddSingleton(logControl);
            ConfigureServices(services);

            using var serviceProvider = services.BuildServiceProvider();
            var mainForm = serviceProvider.GetRequiredService<MainForm>();

            var orcestrator = serviceProvider.GetRequiredService<Orcestrator>();
            Task.Run(async () =>
            {
                await GoGo(orcestrator);
            });
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Приложение не смогло запуститься корректно");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task GoGo(Orcestrator orcestrator)
    {
        var sources = orcestrator.GetSources();

        while (true)
        {
            await orcestrator.GetStorageFullInfo();
            // todo делаем бич вариант, потом распараллелим
            var relations = orcestrator.GetRelations();
            foreach (var rel in relations)
            {
                var medias = orcestrator.GetMedias();
                foreach (var media in medias)
                {
                    var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
                    var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
                    if(fromSource != null && toSource == null)
                    {
                        var tempMedia = await rel.From.Type.Download(fromSource.ExternalId, rel.From.Settings);
                        tempMedia.Id = media.Id;
                        var externalId = await rel.To.Type.Upload(tempMedia, rel.To.Settings);

                        var toMediaSource = new MediaSourceLink
                        {
                            MediaId = media.Id,
                            Media = media,
                            ExternalId = externalId,
                            Status = "OK",
                            SourceId = rel.To.Id,
                        };

                        media.Sources.Add(toMediaSource);
                        orcestrator.UpdateMedia(media);
                    }
                }
            }
            Thread.Sleep(3600_000);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        services.AddSingleton<LiteDatabase>(_ => new(@"MyData.db"));
        services.AddSingleton<PluginManager>();
        services.AddSingleton<Orcestrator>();
        services.AddTransient<MainForm>();

        services.AddTransient<SourceControl>();
        services.AddTransient<RelationControl>();
    }
}
