using LiteDB;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public sealed class StateManager(string stateRoot, LiteDatabase db, ILogger<StateManager> logger)
{
    private static readonly LegacyStateMapping[] LegacyMappings =
    [
        new("Rutube", "auth_state_path", LegacyMigrationKind.SingleFile, "auth_state"),
        new("Youtube", "auth_state_path", LegacyMigrationKind.SingleFile, "auth_state"),
        new("Youtube", "token_path", LegacyMigrationKind.GoogleOAuthDir, "oauth"),
        new("VkVideo", "auth_state_path", LegacyMigrationKind.SingleFile, "auth_state"),
        new("Telegram", "session_path", LegacyMigrationKind.SingleFile, "telegram.session"),
    ];

    private enum LegacyMigrationKind
    {
        SingleFile = 0,
        GoogleOAuthDir = 1,
    }

    public string StateRoot => stateRoot;

    public string GetSourceStatePath(string sourceId)
    {
        var path = Path.Combine(stateRoot, sourceId);
        Directory.CreateDirectory(path);
        return path;
    }

    public void MigrateLegacyStatePaths()
    {
        var sources = db.GetCollection<Source>("sources").FindAll().ToList();
        var dirtySources = sources.Where(TryMigrateSource).ToList();

        if (dirtySources.Count == 0)
        {
            return;
        }

        var collection = db.GetCollection<Source>("sources");
        foreach (var source in dirtySources)
        {
            collection.Update(source);
        }

        logger.LogInformation("Миграция состояния: обновлены настройки {Count} источников", dirtySources.Count);
    }

    public void CleanSource(string sourceId)
    {
        var path = Path.Combine(stateRoot, sourceId);
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, true);
            logger.LogInformation("Удалена директория состояния: {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось удалить директорию состояния {Path}", path);
        }
    }

    private static bool IsYoutubeLegacyJsonFormat(string typeId, string path)
    {
        if (!string.Equals(typeId, "Youtube", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var reader = new StreamReader(path);
            int ch;
            while ((ch = reader.Read()) != -1)
            {
                if (!char.IsWhiteSpace((char)ch))
                {
                    return (char)ch == '{';
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private bool TryMigrateSource(Source source)
    {
        var dirty = false;

        foreach (var mapping in LegacyMappings)
        {
            if (!string.Equals(mapping.TypeId, source.TypeId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!source.Settings.TryGetValue(mapping.LegacyKey, out var legacyValue) || string.IsNullOrWhiteSpace(legacyValue))
            {
                continue;
            }

            if (!TryMigrateMapping(source, mapping, legacyValue))
            {
                continue;
            }

            source.Settings.Remove(mapping.LegacyKey);
            dirty = true;
        }

        return dirty;
    }

    private bool TryMigrateMapping(Source source, LegacyStateMapping mapping, string legacyValue)
    {
        return mapping.Kind switch
        {
            LegacyMigrationKind.SingleFile => TryCopyLegacyStateFile(source, mapping.TargetName, legacyValue),
            LegacyMigrationKind.GoogleOAuthDir => TryCopyLegacyOAuthDir(source, mapping.TargetName, legacyValue),
            _ => false,
        };
    }

    private bool TryCopyLegacyStateFile(Source source, string targetFileName, string legacyPath)
    {
        try
        {
            var targetDir = GetSourceStatePath(source.Id);
            var targetPath = Path.Combine(targetDir, targetFileName);

            if (!File.Exists(legacyPath))
            {
                logger.LogWarning("Миграция состояния: исходный файл не найден {Path} (источник {SourceId})", legacyPath, source.Id);
                return true;
            }

            if (File.Exists(targetPath))
            {
                return true;
            }

            if (IsYoutubeLegacyJsonFormat(source.TypeId, legacyPath))
            {
                logger.LogWarning("Миграция состояния YouTube: файл {Path} сохранён в старом формате Playwright JSON и не поддерживается новым yt-dlp flow. Копирование пропущено — выполните повторную авторизацию YouTube через кнопку «Авторизация»",
                    legacyPath);

                return true;
            }

            File.Copy(legacyPath, targetPath);
            logger.LogInformation("Миграция состояния: {Old} → {New}", legacyPath, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось мигрировать состояние {Old} для источника {SourceId}", legacyPath, source.Id);
            return false;
        }
    }

    private bool TryCopyLegacyOAuthDir(Source source, string targetSubdir, string legacyTokenPath)
    {
        var legacyDir = Path.GetDirectoryName(Path.GetFullPath(legacyTokenPath));
        if (string.IsNullOrEmpty(legacyDir) || !Directory.Exists(legacyDir))
        {
            logger.LogWarning("Миграция OAuth-токена: исходная директория не найдена для {Path} (источник {SourceId})", legacyTokenPath, source.Id);
            return true;
        }

        try
        {
            var targetDir = Path.Combine(GetSourceStatePath(source.Id), targetSubdir);
            Directory.CreateDirectory(targetDir);

            var copied = 0;
            foreach (var file in Directory.EnumerateFiles(legacyDir, "Google.Apis.Auth.OAuth2.Responses.*"))
            {
                var name = Path.GetFileName(file);
                var destination = Path.Combine(targetDir, name);
                if (File.Exists(destination))
                {
                    continue;
                }

                File.Copy(file, destination);
                copied++;
            }

            if (copied > 0)
            {
                logger.LogInformation("Миграция OAuth-токена: скопировано {Count} файлов из {Old} в {New}", copied, legacyDir, targetDir);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось мигрировать OAuth-токен из {Old} для источника {SourceId}", legacyDir, source.Id);
            return false;
        }
    }

    private sealed record LegacyStateMapping(string TypeId, string LegacyKey, LegacyMigrationKind Kind, string TargetName);
}
