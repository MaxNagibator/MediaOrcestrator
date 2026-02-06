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
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .WriteTo.RichTextBox(logControl,
                ThemePresets.Literate,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
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
            orcestrator.Init();
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
        await orcestrator.GetStorageFullInfo();
        return;

        var logger = Log.Logger;
        logger.Information("Цикл GoGo запущен");
        var sources = orcestrator.GetSources();
        logger.Debug("Найдено источников: {SourceCount}", sources.Count);

        while (true)
        {
            logger.Information("Обновление полной информации о хранилище...");
            await orcestrator.GetStorageFullInfo();
            // todo делаем бич вариант, потом распараллелим
            var relations = orcestrator.GetRelations();
            logger.Information("Найдено связей: {RelationCount}", relations.Count);

            foreach (var rel in relations)
            {
                var medias = orcestrator.GetMedias();
                logger.Debug("Проверка связи {Relation} для {MediaCount} медиа", rel, medias.Count);

                foreach (var media in medias)
                {
                    var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
                    var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
                    if (fromSource != null && toSource == null)
                    {
                        logger.Information("Синхронизация медиа {Media} по связи {Relation}", media, rel);
                        try
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
                            logger.Information("Успешно синхронизировано медиа {Media} в {ToSource}. ExternalId: {ExternalId}", media, rel.To, externalId);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Не удалось синхронизировать медиа {Media} по связи {Relation}", media, rel);
                        }
                    }
                }
            }

            var delay = TimeSpan.FromHours(1);
            var nextRun = DateTime.Now.Add(delay);
            logger.Information("Цикл завершен. Следующий запуск в {NextRunTime}", nextRun);
            await Task.Delay(delay);
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
