using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MediaOrcestrator.Runner;

public sealed class CoverTemplateStore(SettingsManager settingsManager, ILogger<CoverTemplateStore> logger)
{
    private const string FileExtension = ".json";

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".bmp",
        ".gif",
    };

    private readonly string _baseDirectory = Path.Combine(settingsManager.SettingsDirectory, "templates", "covers");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    public CoverTemplate? Load(string name)
    {
        var path = GetPath(name);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<CoverTemplateDto>(json);
            var template = dto?.ToDomain();

            if (template == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(template.TemplatePath) && File.Exists(template.TemplatePath))
            {
                return template;
            }

            var backup = FindTemplateBackup(name);

            if (backup != null)
            {
                logger.LogInformation("Исходный шаблон '{Original}' не найден, используется резервная копия '{Backup}'", template.TemplatePath, backup);
                return template with { TemplatePath = backup };
            }

            return template;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось загрузить шаблон обложки '{Name}' из {Path}", name, path);
            return null;
        }
    }

    public bool Save(string name, CoverTemplate template)
    {
        try
        {
            Directory.CreateDirectory(_baseDirectory);
            var dto = CoverTemplateDto.FromDomain(template);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            File.WriteAllText(GetPath(name), json);

            TrySaveBackup(name, template.TemplatePath);

            logger.LogDebug("Шаблон обложки '{Name}' сохранён", name);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось сохранить шаблон обложки '{Name}'", name);
            return false;
        }
    }

    public IEnumerable<string> List()
    {
        if (!Directory.Exists(_baseDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(_baseDirectory, "*" + FileExtension)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void Delete(string name)
    {
        var path = GetPath(name);

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);

            foreach (var backup in EnumerateBackups(name))
            {
                try
                {
                    File.Delete(backup);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Не удалось удалить копию шаблона '{Path}'", backup);
                }
            }

            logger.LogDebug("Профиль шаблона '{Name}' удалён", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось удалить профиль шаблона '{Name}'", name);
        }
    }

    private static string SanitizeName(string name)
    {
        return string.Concat(name.Split(Path.GetInvalidFileNameChars()));
    }

    private void TrySaveBackup(string name, string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        var ext = Path.GetExtension(sourcePath);

        if (string.IsNullOrEmpty(ext) || !ImageExtensions.Contains(ext))
        {
            ext = ".png";
        }

        var safeName = SanitizeName(name);
        var backupPath = Path.Combine(_baseDirectory, safeName + ext);

        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(backupPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var stale in EnumerateBackups(name))
        {
            if (!string.Equals(stale, backupPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(stale);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Не удалось удалить устаревшую копию шаблона '{Path}'", stale);
                }
            }
        }

        try
        {
            File.Copy(sourcePath, backupPath, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить копию шаблона '{Name}'", name);
        }
    }

    private string? FindTemplateBackup(string name)
    {
        return EnumerateBackups(name).FirstOrDefault();
    }

    private IEnumerable<string> EnumerateBackups(string name)
    {
        if (!Directory.Exists(_baseDirectory))
        {
            yield break;
        }

        var safeName = SanitizeName(name);

        foreach (var file in Directory.EnumerateFiles(_baseDirectory, safeName + ".*"))
        {
            var ext = Path.GetExtension(file);

            if (string.Equals(ext, FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ImageExtensions.Contains(ext))
            {
                yield return file;
            }
        }
    }

    private string GetPath(string name)
    {
        return Path.Combine(_baseDirectory, SanitizeName(name) + FileExtension);
    }

    private sealed record CoverTextLayerDto(
        string TextTemplate,
        float TextX,
        float TextY,
        float FontSizeRatio,
        string FontFamily,
        CoverFontStyle FontStyle,
        uint FillColorArgb,
        uint StrokeColorArgb,
        float StrokeWidthRatio)
    {
        public static CoverTextLayerDto FromDomain(CoverTextLayer layer)
        {
            return new(layer.TextTemplate,
                layer.TextX,
                layer.TextY,
                layer.FontSizeRatio,
                layer.FontFamily,
                layer.FontStyle,
                (uint)layer.FillColor,
                (uint)layer.StrokeColor,
                layer.StrokeWidthRatio);
        }

        public CoverTextLayer ToDomain()
        {
            return new(TextTemplate,
                TextX,
                TextY,
                FontSizeRatio,
                FontFamily,
                FontStyle,
                new(FillColorArgb),
                new(StrokeColorArgb),
                StrokeWidthRatio);
        }
    }

    private sealed record CoverTemplateDto(
        string TemplatePath,
        int StartNumber,
        CoverNumberMode NumberMode,
        string TitleRegexPattern,
        List<CoverTextLayerDto> Layers)
    {
        public static CoverTemplateDto FromDomain(CoverTemplate template)
        {
            return new(template.TemplatePath,
                template.StartNumber,
                template.NumberMode,
                template.TitleRegexPattern,
                template.Layers.Select(CoverTextLayerDto.FromDomain).ToList());
        }

        public CoverTemplate ToDomain()
        {
            return new(TemplatePath,
                StartNumber,
                NumberMode,
                TitleRegexPattern,
                Layers.Select(l => l.ToDomain()).ToList());
        }
    }
}
