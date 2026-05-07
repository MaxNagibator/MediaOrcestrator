using LiteDB;
using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Domain.Merging;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.RichTextBoxForms.Themes;
using System.Text;
using ILogger = Serilog.ILogger;

namespace MediaOrcestrator.Runner;

file static class Program
{
    private static IServiceProvider? _runningServiceProvider;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        RegisterGlobalExceptionHandlers();

        // TODO: Выглядит не очень
        var logControl = new RichTextBox();
        logControl.BackColor = SystemColors.Window;
        logControl.Dock = DockStyle.Fill;
        logControl.Font = new("Cascadia Mono", 10.8F, FontStyle.Regular, GraphicsUnit.Point);
        logControl.Location = new(0, 0);
        logControl.Name = "uiLogControl";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .WriteTo.File("logs/log-.txt",
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                encoding: Encoding.UTF8)
            .WriteTo.RichTextBox(logControl,
                ThemePresets.Literate,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var settingsManager = new SettingsManager("settings.txt");

            var pluginPath = settingsManager.GetStringValue("plugin_path");
            if (pluginPath == null)
            {
                var result = InputMessageBox.Show("Введите путь до папки с плугинами, или оставьте системный", "Важная настройка", "ModuleBuilds", InputBrowseMode.Folder);
                if (result == null)
                {
                    MessageBox.Show("Так нельзя, закрываюсь");
                    return;
                }

                pluginPath = result;
                settingsManager.SetValue("plugin_path", result);
            }

            var databasePath = settingsManager.GetStringValue("database_path");
            if (databasePath == null)
            {
                var result = InputMessageBox.Show("Введите путь до базы данных, или оставьте системный", "Важная настройка", "MyData.db", InputBrowseMode.File);
                if (result == null)
                {
                    MessageBox.Show("Так нельзя, закрываюсь");
                    return;
                }

                databasePath = result;
                settingsManager.SetValue("database_path", result);
            }

            var tempPath = settingsManager.GetStringValue("temp_path");
            if (tempPath == null)
            {
                var result = InputMessageBox.Show("Введите путь до временной папки для загрузок", "Важная настройка", "temp", InputBrowseMode.Folder);
                if (result == null)
                {
                    MessageBox.Show("Так нельзя, закрываюсь");
                    return;
                }

                tempPath = result;
                settingsManager.SetValue("temp_path", result);
            }

            var statePath = settingsManager.GetStringValue("state_path");
            if (statePath == null)
            {
                var result = InputMessageBox.Show("Введите путь до папки состояния источников (куки, сессии)", "Важная настройка", "state", InputBrowseMode.Folder);
                if (result == null)
                {
                    MessageBox.Show("Так нельзя, закрываюсь");
                    return;
                }

                statePath = result;
                settingsManager.SetValue("state_path", result);
            }

            Log.Information("Приложение запускается...");
            CheckUpdaterLog();

            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "—";
            const int ExpectedStartupSpans = 10;
            using var splash = new SplashTransaction(version, "Запуск приложения", ExpectedStartupSpans, Log.ForContext<SplashTransaction>());

            var services = new ServiceCollection();

            using (splash.StartSpan("Подготовка сервисов..."))
            {
                services.AddSingleton(logControl);
                services.AddSingleton(settingsManager);
                ConfigureServices(services, databasePath);

                services.AddSingleton(sp =>
                    new TempManager(tempPath,
                        sp.GetRequiredService<LiteDatabase>(),
                        sp.GetRequiredService<ILogger<TempManager>>()));

                services.AddSingleton(sp =>
                    new StateManager(statePath,
                        sp.GetRequiredService<LiteDatabase>(),
                        sp.GetRequiredService<ILogger<StateManager>>()));
            }

            using var serviceProvider = services.BuildServiceProvider();
            _runningServiceProvider = serviceProvider;

            var orcestrator = serviceProvider.GetRequiredService<Orcestrator>();

            using (splash.StartSpan("Загрузка плагинов..."))
            {
                orcestrator.Init(pluginPath);
            }

            using (splash.StartSpan("Резервное копирование базы данных..."))
            {
                var backupService = serviceProvider.GetRequiredService<DatabaseBackupService>();
                backupService.ValidateLog();
                backupService.Backup(BackupTrigger.Startup);
                backupService.StartScheduled(TimeSpan.FromHours(6));
            }

            using (splash.StartSpan("Подготовка временных файлов..."))
            {
                var tempManager = serviceProvider.GetRequiredService<TempManager>();
                tempManager.MigrateOldTempPaths();
                tempManager.CleanAll();
            }

            using (splash.StartSpan("Подготовка состояния источников..."))
            {
                var stateManager = serviceProvider.GetRequiredService<StateManager>();
                stateManager.MigrateLegacyStatePaths();
            }

            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            mainForm.StartupCompleted = splash.Dispose;

            Task.Run(async () =>
            {
                await GoGo(orcestrator);
            });

            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Приложение не смогло запуститься корректно");
            ShowErrorReport(ex);
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

    private static void RegisterGlobalExceptionHandlers()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Необработанное исключение в UI-потоке");
            ShowErrorReport(args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is not Exception ex)
            {
                return;
            }

            Log.Fatal(ex, "Необработанное исключение вне UI-потока");
            ShowErrorReport(ex);
        };
    }

    private static void ShowErrorReport(Exception ex)
    {
        try
        {
            var service = _runningServiceProvider?.GetService<ErrorReportService>()
                          ?? BuildFallbackErrorReportService();

            using var form = new ErrorReportForm(service, ex);
            form.ShowDialog();
        }
        catch (Exception reportEx)
        {
            Log.Error(reportEx, "Не удалось показать ErrorReportForm");
            MessageBox.Show(ex.ToString(),
                "Критическая ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static ErrorReportService BuildFallbackErrorReportService()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
        return new(loggerFactory.CreateLogger<ErrorReportService>());
    }

    /// <summary>
    /// Тут лопатит по кд и синхронизирует пока синхронизируется.
    /// </summary>
    /// <param name="orcestrator"></param>
    /// <returns></returns>
    private static async Task GoGo(Orcestrator orcestrator)
    {
        //await orcestrator.GetStorageFullInfo();
        return;

        var logger = Log.Logger;
        logger.Information("Цикл GoGo запущен");
        var sources = orcestrator.GetSources();
        logger.Debug("Найдено источников: {SourceCount}", sources.Count);

        while (true)
        {
            logger.Information("Обновление полной информации о хранилище...");
            await orcestrator.GetStorageFullInfo(false);
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
        ILogger logger,
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
                await orcestrator.TransferByRelation(media, processRelation);
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

    private static void CheckUpdaterLog()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "updater.log");

        if (!File.Exists(logPath))
        {
            return;
        }

        try
        {
            var content = File.ReadAllText(logPath);

            if (content.Contains("ОШИБКА"))
            {
                Log.Warning("Обнаружены ошибки в updater.log:\n{Content}", content);
                MessageBox.Show("При последнем обновлении возникли ошибки. Подробности в updater.log.",
                    "Предупреждение",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                Log.Information("Обновление прошло успешно (updater.log)");
            }

            File.Delete(logPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Не удалось прочитать updater.log");
        }
    }

    private static void ConfigureServices(IServiceCollection services, string databasePath)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        services.AddSingleton<LiteDatabase>(_ => new(databasePath));
        services.AddSingleton<DatabaseBackupService>(sp =>
            new(sp.GetRequiredService<LiteDatabase>(),
                databasePath,
                sp.GetRequiredService<ILogger<DatabaseBackupService>>()));

        services.AddSingleton<PluginManager>();
        services.AddHttpClient("GitHub", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MediaOrcestrator/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("Preview", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MediaOrcestrator/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<GitHubReleaseProvider>();
        services.AddSingleton<IReleaseProvider>(sp => sp.GetRequiredService<GitHubReleaseProvider>());
        services.AddSingleton<ToolVersionDetector>();
        services.AddSingleton<ToolInstaller>();
        services.AddSingleton<ToolManager>(sp =>
            new(Path.Combine(AppContext.BaseDirectory, "tools"),
                sp.GetRequiredService<IReleaseProvider>(),
                sp.GetRequiredService<ToolVersionDetector>(),
                sp.GetRequiredService<ToolInstaller>(),
                sp.GetRequiredService<ILogger<ToolManager>>()));

        services.AddSingleton<IToolPathProvider>(sp => sp.GetRequiredService<ToolManager>());
        services.AddSingleton<VideoTranscoder>();
        services.AddSingleton<AppUpdateManager>(sp =>
        {
            var settingsManager = sp.GetRequiredService<SettingsManager>();
            var updateRepo = settingsManager.GetStringValue("update_repo") ?? BuildConstants.DefaultUpdateRepo;
            return new(sp.GetRequiredService<IReleaseProvider>(),
                updateRepo,
                sp.GetRequiredService<ILogger<AppUpdateManager>>());
        });

        services.AddSingleton<ErrorReportService>();

        services.AddSingleton<Orcestrator>();
        services.AddSingleton<CommentsRepository>();
        services.AddSingleton<CommentsService>();
        services.AddSingleton<MediaMergeService>();
        services.AddSingleton<SyncRetryRunner>();
        services.AddSingleton<ActionHolder>();
        services.AddSingleton<BatchRenameService>();
        services.AddSingleton<CoverGenerator>();
        services.AddSingleton<CoverTemplateStore>();
        services.AddSingleton<BatchPreviewService>();
        services.AddSingleton<SyncPlanner>();
        services.AddTransient<MainForm>();

        services.AddTransient<SourceControl>();
        services.AddTransient<RelationControl>();
        services.AddTransient<SyncTreeControl>();
        services.AddTransient<PublishControl>();
    }
}

file static class InputMessageBox
{
    public static string? Show(string prompt, string title = "Ввод данных", string defaultValue = "", InputBrowseMode browseMode = InputBrowseMode.None)
    {
        using var dialog = new InputDialog(prompt, title, defaultValue, browseMode);
        return dialog.ShowDialog() == DialogResult.OK ? dialog.InputText : null;
    }
}
