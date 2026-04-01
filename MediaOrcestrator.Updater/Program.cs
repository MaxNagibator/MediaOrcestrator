using System.Diagnostics;
using System.IO.Compression;

namespace MediaOrcestrator.Updater;

file static class Program
{
    private static readonly string[] ExcludeFromBackup =
    [
        "settings.txt",
        "tools",
        "backups",
        "logs",
        "updater.log",
    ];

    private static readonly string[] SelfFiles =
    [
        "Updater.exe",
        "Updater.dll",
        "Updater.runtimeconfig.json",
        "Updater.deps.json",
    ];

    private static int Main(string[] args)
    {
        var logPath = "updater.log";

        try
        {
            var arguments = ParseArguments(args);
            var zipPath = arguments["zip"];
            var targetDir = arguments["target"].TrimEnd('\\', '/');
            var pid = int.Parse(arguments["pid"]);
            var restart = arguments["restart"];
            var currentVersion = arguments["current-version"];

            logPath = Path.Combine(targetDir, "updater.log");
            Log(logPath, $"Updater запущен. Обновление до версии из {Path.GetFileName(zipPath)}");

            WaitForProcessExit(pid, logPath);

            var backupDir = Path.Combine(targetDir, "backups", $"v{currentVersion}");
            BackupCurrentVersion(targetDir, backupDir, logPath);

            ExtractUpdate(zipPath, targetDir, logPath);

            // CleanupTempFiles(zipPath, logPath);

            Log(logPath, "Обновление завершено успешно. Запуск приложения...");

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(targetDir, restart),
                WorkingDirectory = targetDir,
                UseShellExecute = true,
            });

            return 0;
        }
        catch (Exception ex)
        {
            Log(logPath, $"ОШИБКА: {ex.Message}\n{ex.StackTrace}");

            try
            {
                var arguments = ParseArguments(args);
                var targetDir = arguments["target"].TrimEnd('\\', '/');
                var currentVersion = arguments["current-version"];
                var backupDir = Path.Combine(targetDir, "backups", $"v{currentVersion}");

                if (Directory.Exists(backupDir))
                {
                    Log(logPath, "Откат из бэкапа...");
                    RestoreFromBackup(backupDir, targetDir, logPath);
                    Log(logPath, "Откат завершён.");
                }

                var restart = arguments["restart"];
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(targetDir, restart),
                    WorkingDirectory = targetDir,
                    UseShellExecute = true,
                });
            }
            catch (Exception rollbackEx)
            {
                Log(logPath, $"ОШИБКА ОТКАТА: {rollbackEx.Message}");
            }

            return 1;
        }
    }

    private static void WaitForProcessExit(int pid, string logPath)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            Log(logPath, $"Ожидание завершения процесса {pid}...");

            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                Log(logPath, $"Процесс {pid} не завершился, принудительное завершение...");
                process.Kill();
                process.WaitForExit(TimeSpan.FromSeconds(5));
            }
        }
        catch (ArgumentException)
        {
            Log(logPath, $"Процесс {pid} уже завершён.");
        }
    }

    private static void BackupCurrentVersion(string targetDir, string backupDir, string logPath)
    {
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }

        Directory.CreateDirectory(backupDir);
        Log(logPath, $"Создание бэкапа в {backupDir}...");

        foreach (var file in Directory.GetFiles(targetDir))
        {
            var fileName = Path.GetFileName(file);

            if (ShouldExclude(fileName))
            {
                continue;
            }

            File.Copy(file, Path.Combine(backupDir, fileName), true);
        }

        foreach (var dir in Directory.GetDirectories(targetDir))
        {
            var dirName = Path.GetFileName(dir);

            if (ShouldExclude(dirName))
            {
                continue;
            }

            CopyDirectory(dir, Path.Combine(backupDir, dirName));
        }

        Log(logPath, "Бэкап создан.");
    }

    private static void ExtractUpdate(string zipPath, string targetDir, string logPath)
    {
        Log(logPath, $"Распаковка {Path.GetFileName(zipPath)} в {targetDir}...");

        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var destPath = Path.Combine(targetDir, entry.FullName);
            var destDir = Path.GetDirectoryName(destPath);

            if (destDir is not null && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (SelfFiles.Any(f => entry.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
            {
                Log(logPath, $"Пропуск собственного файла: {entry.Name}");
                continue;
            }

            entry.ExtractToFile(destPath, true);
        }

        Log(logPath, "Распаковка завершена.");
    }

    private static void RestoreFromBackup(string backupDir, string targetDir, string logPath)
    {
        foreach (var file in Directory.GetFiles(backupDir))
        {
            var fileName = Path.GetFileName(file);

            if (SelfFiles.Any(f => fileName.Equals(f, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            File.Copy(file, Path.Combine(targetDir, fileName), true);
        }

        foreach (var dir in Directory.GetDirectories(backupDir))
        {
            var destPath = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, destPath);
        }

        Log(logPath, "Файлы восстановлены из бэкапа.");
    }

    private static void CleanupTempFiles(string zipPath, string logPath)
    {
        try
        {
            var tempDir = Path.GetDirectoryName(zipPath);

            if (tempDir is not null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            Log(logPath, $"Не удалось удалить временные файлы: {ex.Message}");
        }
    }

    private static bool ShouldExclude(string name)
    {
        return ExcludeFromBackup.Any(e => name.Equals(e, StringComparison.OrdinalIgnoreCase) || name.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            result[args[i][2..]] = args[i + 1];
            i++;
        }

        return result;
    }

    private static void Log(string logPath, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        Console.WriteLine(line);

        try
        {
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
