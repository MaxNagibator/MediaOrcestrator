using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace MediaOrcestrator.Domain;

public sealed class AppUpdateManager(
    IReleaseProvider releaseProvider,
    string updateRepo,
    ILogger<AppUpdateManager> logger)
{
    private const string AssetPattern = "MediaOrcestrator-v*.zip";

    private AppUpdateInfo? _cachedUpdate;
    private DateTimeOffset _lastChecked;

    public Version CurrentVersion { get; } = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.Now - _lastChecked < TimeSpan.FromHours(1) && _cachedUpdate is not null)
        {
            return _cachedUpdate;
        }

        try
        {
            var release = await releaseProvider.GetLatestReleaseAsync(updateRepo, AssetPattern, cancellationToken);

            if (release?.AssetUrl is null)
            {
                logger.LogDebug("Нет доступных релизов для {Repo}", updateRepo);
                _lastChecked = DateTimeOffset.Now;
                _cachedUpdate = null;
                return null;
            }

            var tagVersion = release.TagName.StartsWith('v') ? release.TagName[1..] : release.TagName;

            if (!Version.TryParse(tagVersion, out var latestVersion) || latestVersion <= CurrentVersion)
            {
                logger.LogDebug("Текущая версия {Current} актуальна (последняя: {Latest})",
                    CurrentVersion, tagVersion);

                _lastChecked = DateTimeOffset.Now;
                _cachedUpdate = null;
                return null;
            }

            _cachedUpdate = new(tagVersion,
                release.AssetUrl,
                release.Body ?? "",
                release.AssetSize);

            _lastChecked = DateTimeOffset.Now;

            logger.LogInformation("Доступно обновление: {Version}", tagVersion);
            return _cachedUpdate;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось проверить обновления приложения");
            return null;
        }
    }

    public async Task<string> DownloadUpdateAsync(
        AppUpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var backupsDir = Path.Combine(AppContext.BaseDirectory, "backups");
        var zipName = $"MediaOrcestrator-v{update.Version}.zip";
        var backupZipPath = Path.Combine(backupsDir, zipName);

        if (File.Exists(backupZipPath) && new FileInfo(backupZipPath).Length == update.Size)
        {
            logger.LogInformation("Обновление {Version} уже скачано: {Path}", update.Version, backupZipPath);
            return backupZipPath;
        }

        Directory.CreateDirectory(backupsDir);

        var tempPath = backupZipPath + ".downloading";

        logger.LogInformation("Скачивание обновления {Version}...", update.Version);
        await releaseProvider.DownloadAssetAsync(update.DownloadUrl, tempPath, progress, cancellationToken);

        File.Move(tempPath, backupZipPath, true);
        logger.LogInformation("Обновление скачано: {Path}", backupZipPath);

        return backupZipPath;
    }

    public void ApplyUpdate(string zipPath)
    {
        var appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var updaterPath = Path.Combine(appDir, "Updater.exe");

        if (!File.Exists(updaterPath))
        {
            throw new FileNotFoundException("Updater.exe не найден в папке приложения", updaterPath);
        }

        var pid = Environment.ProcessId;
        var currentVersion = CurrentVersion.ToString(3);

        var args = $"--zip \"{zipPath}\" --target \"{appDir}\" --pid {pid} --restart MediaOrcestrator.Runner.exe --current-version {currentVersion}";

        logger.LogInformation("Запуск Updater: {Args}", args);

        Process.Start(new ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments = args,
            UseShellExecute = true,
        });

        Environment.Exit(0);
    }
}
