using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaOrcestrator.Domain;

public class ToolInstaller(IReleaseProvider releaseProvider, ILogger<ToolInstaller> logger)
{
    public async Task<string?> InstallAsync(
        string toolName,
        ToolDescriptor descriptor,
        string toolsRoot,
        string? currentPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var toolDir = Path.Combine(toolsRoot, toolName);
        var backupDir = toolDir + ".old";

        EnsureToolNotRunning(toolName, currentPath, toolDir);

        var release = await releaseProvider.GetLatestReleaseAsync(
            descriptor.GitHubRepo, descriptor.AssetPattern, cancellationToken);

        if (release?.AssetUrl is null)
        {
            throw new InvalidOperationException($"Не найден файл для скачивания инструмента '{toolName}'");
        }

        var tempDir = Path.Combine(toolsRoot, ".temp", toolName);

        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            Directory.CreateDirectory(tempDir);

            var downloadPath = Path.Combine(tempDir, release.AssetName ?? $"{toolName}.download");

            logger.LogInformation("Скачивание {Tool} из {Url}...", toolName, release.AssetUrl);

            await releaseProvider.DownloadAssetAsync(release.AssetUrl, downloadPath, progress, cancellationToken);

            SwapDirectories(toolDir, backupDir, tempDir, downloadPath, descriptor);

            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
            }

            return FindExecutableInToolDir(toolDir, descriptor);
        }
        catch
        {
            if (Directory.Exists(backupDir) && !Directory.Exists(toolDir))
            {
                Directory.Move(backupDir, toolDir);
                logger.LogWarning("Откат '{Name}' к предыдущей версии", toolName);
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    public static string? FindExecutableInToolDir(string toolDir, ToolDescriptor descriptor)
    {
        if (!Directory.Exists(toolDir))
        {
            return null;
        }

        if (descriptor.ArchiveExecutablePath is not null)
        {
            return ArchiveExtractor.FindExecutable(toolDir, descriptor.ArchiveExecutablePath)
                   ?? ArchiveExtractor.FindExecutable(toolDir, Path.GetFileName(descriptor.ArchiveExecutablePath));
        }

        var exeName = descriptor.AssetPattern.Contains('*')
            ? null
            : descriptor.AssetPattern;

        if (exeName is null)
        {
            var searchPattern = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "*.exe" : "*";
            var exeFiles = Directory.GetFiles(toolDir, searchPattern);
            return exeFiles.Length == 1 ? exeFiles[0] : null;
        }

        var path = Path.Combine(toolDir, exeName);
        return File.Exists(path) ? path : null;
    }

    private static void SwapDirectories(
        string toolDir,
        string backupDir,
        string tempDir,
        string downloadPath,
        ToolDescriptor descriptor)
    {
        if (descriptor.ArchiveType != ArchiveType.None)
        {
            var extractDir = Path.Combine(tempDir, "extracted");
            ArchiveExtractor.Extract(downloadPath, extractDir, descriptor.ArchiveType);
            File.Delete(downloadPath);

            BackupExisting(toolDir, backupDir);

            if (descriptor.ArchiveExecutablePath is not null)
            {
                var exePath = ArchiveExtractor.FindExecutable(extractDir, descriptor.ArchiveExecutablePath);

                if (exePath is null)
                {
                    throw new FileNotFoundException(
                        $"Исполняемый файл не найден в архиве по паттерну '{descriptor.ArchiveExecutablePath}'");
                }

                var exeDir = Path.GetDirectoryName(exePath)!;
                Directory.Move(exeDir, toolDir);
            }
            else
            {
                Directory.Move(extractDir, toolDir);
            }
        }
        else
        {
            BackupExisting(toolDir, backupDir);

            Directory.CreateDirectory(toolDir);
            var destFile = Path.Combine(toolDir, Path.GetFileName(downloadPath));
            File.Move(downloadPath, destFile);
        }
    }

    private static void BackupExisting(string toolDir, string backupDir)
    {
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }

        if (Directory.Exists(toolDir))
        {
            Directory.Move(toolDir, backupDir);
        }
    }

    private static void EnsureToolNotRunning(string toolName, string? currentPath, string toolDir)
    {
        if (currentPath is null || !File.Exists(currentPath))
        {
            return;
        }

        var exeName = Path.GetFileNameWithoutExtension(currentPath);
        var processes = Process.GetProcessesByName(exeName);

        foreach (var process in processes)
        {
            try
            {
                if (process.MainModule?.FileName?.StartsWith(toolDir, StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new InvalidOperationException(
                        $"Инструмент '{toolName}' сейчас используется (PID {process.Id}). Дождитесь завершения операции перед обновлением.");
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
