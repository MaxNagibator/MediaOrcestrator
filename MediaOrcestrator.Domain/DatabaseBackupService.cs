using LiteDB;
using Microsoft.Extensions.Logging;
using STJ = System.Text.Json;

namespace MediaOrcestrator.Domain;

// TODO: Механизмы удаления осознано опушены, так как требуют утверждения данжен-мастером
public class DatabaseBackupService(LiteDatabase db, string databasePath, ILogger<DatabaseBackupService> logger) : IDisposable
{
    private static readonly STJ.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _backupDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(databasePath))!, "backups");
    private readonly string _databaseName = Path.GetFileNameWithoutExtension(databasePath);
    private readonly object _logLock = new();

    private Timer? _timer;

    public void Backup(BackupTrigger trigger)
    {
        try
        {
            Directory.CreateDirectory(_backupDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var triggerName = trigger.ToString().ToLowerInvariant();
            var backupFileName = $"{_databaseName}_{timestamp}_{triggerName}.db";
            var backupPath = Path.Combine(_backupDir, backupFileName);
            var fullDatabasePath = Path.GetFullPath(databasePath);

            lock (db)
            {
                db.Checkpoint();
                File.Copy(fullDatabasePath, backupPath);
            }

            var entry = new BackupLogEntry(backupFileName, DateTime.Now, trigger, new FileInfo(backupPath).Length);
            AppendToLog(entry);

            logger.LogInformation("Бэкап создан: {BackupFileName} ({SizeBytes} байт, триггер: {Trigger})",
                backupFileName, entry.SizeBytes, triggerName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании бэкапа базы данных (триггер: {Trigger})", trigger);
        }
    }

    public void ValidateLog()
    {
        lock (_logLock)
        {
            var logPath = Path.Combine(_backupDir, "backup-log.json");

            if (!File.Exists(logPath))
            {
                return;
            }

            var entries = ReadLog();
            var validEntries = entries.Where(e => File.Exists(Path.Combine(_backupDir, e.FileName))).ToList();

            if (validEntries.Count == entries.Count)
            {
                return;
            }

            logger.LogWarning("Удалено {Count} записей из журнала бэкапов (файлы не найдены)", entries.Count - validEntries.Count);
            WriteLog(validEntries);
        }
    }

    public void StartScheduled(TimeSpan interval)
    {
        _timer = new(_ => Backup(BackupTrigger.Scheduled), null, interval, interval);
        logger.LogInformation("Запланировано автоматическое резервное копирование каждые {Hours} ч", interval.TotalHours);
    }

    public void Dispose()
    {
        using var waitHandle = new ManualResetEvent(false);

        if (_timer != null)
        {
            _timer.Dispose(waitHandle);
            waitHandle.WaitOne();
        }

        GC.SuppressFinalize(this);
    }

    private List<BackupLogEntry> ReadLog()
    {
        var logPath = Path.Combine(_backupDir, "backup-log.json");

        if (!File.Exists(logPath))
        {
            return [];
        }

        var json = File.ReadAllText(logPath);
        return STJ.JsonSerializer.Deserialize<List<BackupLogEntry>>(json, JsonOptions) ?? [];
    }

    private void WriteLog(List<BackupLogEntry> entries)
    {
        var logPath = Path.Combine(_backupDir, "backup-log.json");
        var json = STJ.JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(logPath, json);
    }

    private void AppendToLog(BackupLogEntry entry)
    {
        lock (_logLock)
        {
            var entries = ReadLog();
            entries.Add(entry);
            WriteLog(entries);
        }
    }
}
