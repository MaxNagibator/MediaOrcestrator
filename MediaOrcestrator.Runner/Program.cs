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
            try
            {
                Log.CloseAndFlush();
            }
            catch
            {
                // Игнорируем ошибки при закрытии логов, так как приложение все равно завершается
            }
        }
    }

    /// <summary>
    /// Тут лопатит по кд и синхронизирует пока синхронизируется.
    /// </summary>
    /// <param name="orcestrator"></param>
    /// <returns></returns>
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
            // todo делаем бич вариант, потом распараллелим, возможно (ну типо почему бы и не синхкать две связи rutube -> hdd,   youtube -> vkvideo в параллель
            var relations = orcestrator.GetRelations();

            var relationPower = new Dictionary<int, int>();
            for (var i = 0; i < relations.Count; i++)
            {
                relationPower[i] = GetPower(relations[i], relations);

                // тесты бы на эту дрисню конечно юнитовые
                int GetPower(SourceSyncRelation rel, List<SourceSyncRelation> allrel)
                {
                    var power = 1;
                    var otherRelations = allrel
                        .Where(x => x.FromId != rel.FromId && x.ToId != rel.ToId)
                        .Where(x => x.FromId == rel.ToId)
                        .ToList();

                    foreach (var otherRelation in otherRelations)
                    {
                        power += GetPower(otherRelation, otherRelations);
                    }

                    return power;
                }
            }

            relations = relations.Select((x, i) => new { relation = x, power = relationPower[i] })
                .OrderByDescending(x => x.power)
                .Select(x => x.relation)
                .ToList();

            logger.Information("Найдено связей: {RelationCount}", relations.Count);

            foreach (var processRelation in relations)
            {
                var medias = orcestrator.GetMedias();
                logger.Debug("Проверка связи {Relation} для {MediaCount} медиа", processRelation, medias.Count);

                foreach (var media in medias)
                {
                    await ProcessMedia(orcestrator, logger, relations, processRelation, media);
                }
            }

            var delay = TimeSpan.FromHours(1);
            var nextRun = DateTime.Now.Add(delay);
            logger.Information("Цикл завершен. Следующий запуск в {NextRunTime}", nextRun);
            await Task.Delay(delay);
        }
    }

    private static async Task ProcessMedia(
        Orcestrator orcestrator,
        Serilog.ILogger logger,
        List<SourceSyncRelation> relations,
        SourceSyncRelation processRelation,
        Media media)
    {
        var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == processRelation.From.Id);
        var toSource = media.Sources.FirstOrDefault(x => x.SourceId == processRelation.To.Id);
        if (fromSource != null && toSource == null)
        {
            logger.Information("Синхронизация медиа {Media} по связи {Relation}", media, processRelation);
            var success = false;
            try
            {
                await orcestrator.TransferByRelation(media, processRelation, fromSource.ExternalId);
                success = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Не удалось синхронизировать медиа {Media} по связи {Relation}", media, processRelation);
            }

            if (success)
            {
                // если успешно скачали, чекаем следующие цепочки сразу.
                // например есть youtube - hdd (скачали), сразу же обработаем связь hdd - rutube
                var otherRelations = relations
                    // поскольку мы выкидываем обработанную связь, бесконечной рекурсии не будет, даже если кто то настроет по кругу хранилища
                    .Where(x => x.FromId != processRelation.FromId && x.ToId != processRelation.ToId)
                    .Where(x => x.FromId == processRelation.ToId)
                    .ToList();
                // playlist / next mutanti / next motivation sportphaza

                foreach (var otherRelation in otherRelations)
                {
                    // ??? mb Wait.All ? чё б и не параллелить vk и рутуб допустим, канал же резиновый допустим
                    // допустим есть связь тупая   y -> hdd   hdd -> vk   hdd -> rutube      vk -> zalupa     rutube -> zalupa ? будет ли баг?
                    // да вроде не, хотя начнут в залупу одновременно качать, сами ебланы хули, мьютекс на to? усложнение преждевременное, потом сделаем мб
                    // todo Wait.All хуярить будем когда то следующую туду сделать
                    // todo есть шанс конфликта синка   a -> c   b -> c  d -> c (три раза не надо качать в параллель)
                    await ProcessMedia(orcestrator, logger, otherRelations, otherRelation, media);
                }
            }
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
