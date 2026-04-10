using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MediaOrcestrator.Runner;

public sealed class CoverTemplateStore(SettingsManager settingsManager, ILogger<CoverTemplateStore> logger)
{
    private const string LastTemplateName = "last";
    private const string FileExtension = ".json";

    private readonly string _baseDirectory = Path.Combine(settingsManager.SettingsDirectory, "templates", "covers");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    public CoverTemplate? LoadLast()
    {
        return Load(LastTemplateName);
    }

    public void SaveLast(CoverTemplate template)
    {
        Save(LastTemplateName, template);
    }

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
            return dto?.ToDomain();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось загрузить шаблон обложки '{Name}' из {Path}", name, path);
            return null;
        }
    }

    public void Save(string name, CoverTemplate template)
    {
        try
        {
            Directory.CreateDirectory(_baseDirectory);
            var dto = CoverTemplateDto.FromDomain(template);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            File.WriteAllText(GetPath(name), json);
            logger.LogDebug("Шаблон обложки '{Name}' сохранён", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось сохранить шаблон обложки '{Name}'", name);
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
            .Where(n => !string.Equals(n, LastTemplateName, StringComparison.OrdinalIgnoreCase))
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
            logger.LogDebug("Профиль шаблона '{Name}' удалён", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось удалить профиль шаблона '{Name}'", name);
        }
    }

    private string GetPath(string name)
    {
        var safeName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_baseDirectory, safeName + FileExtension);
    }

    private sealed record CoverTextLayerDto(
        string TextTemplate,
        float TextX,
        float TextY,
        float FontSizeRatio,
        string FontFamily,
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
