using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace MediaOrcestrator.Domain;

public sealed class CoverGenerator(ILogger<CoverGenerator> logger) : IDisposable
{
    private readonly object _cacheLock = new();
    private string? _cachedPath;
    private DateTime _cachedMtime;
    private SKBitmap? _cachedBitmap;

    public string Generate(CoverTemplate template, int number, string outputDir)
    {
        using var bitmap = Render(template, number);
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, $"cover_{number}.png");

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 95);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);

        logger.LogDebug("Сгенерирована обложка №{Number} → {Path}", number, outputPath);
        return outputPath;
    }

    public SKBitmap Render(CoverTemplate template, int number)
    {
        var bitmap = CopyCachedBackground(template.TemplatePath);

        using var canvas = new SKCanvas(bitmap);

        foreach (var layer in template.Layers)
        {
            DrawLayer(canvas, bitmap.Width, bitmap.Height, layer, number);
        }

        logger.LogTrace("Отрисована обложка №{Number} ({Width}×{Height}, слоёв: {Layers})", number, bitmap.Width, bitmap.Height, template.Layers.Count);
        return bitmap;
    }

    public void Dispose()
    {
        lock (_cacheLock)
        {
            _cachedBitmap?.Dispose();
            _cachedBitmap = null;
            _cachedPath = null;
        }
    }

    private static void DrawLayer(SKCanvas canvas, int width, int height, CoverTextLayer layer, int number)
    {
        var text = ResolveText(layer.TextTemplate, number);

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var fontSize = height * layer.FontSizeRatio;
        var strokeWidth = height * layer.StrokeWidthRatio;

        var skiaStyle = ToSkiaStyle(layer.FontStyle);
        var ownedTypeface = SKTypeface.FromFamilyName(layer.FontFamily, skiaStyle);
        var typeface = ownedTypeface ?? SKTypeface.Default;

        try
        {
            using var fillPaint = new SKPaint
            {
                Color = layer.FillColor,
                IsAntialias = true,
                Typeface = typeface,
                TextSize = fontSize,
                TextAlign = SKTextAlign.Center,
                Style = SKPaintStyle.Fill,
            };

            var x = width * layer.TextX;
            var metrics = fillPaint.FontMetrics;
            var y = height * layer.TextY - (metrics.Ascent + metrics.Descent) / 2f;

            if (strokeWidth > 0)
            {
                using var strokePaint = new SKPaint
                {
                    Color = layer.StrokeColor,
                    IsAntialias = true,
                    Typeface = typeface,
                    TextSize = fontSize,
                    TextAlign = SKTextAlign.Center,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = strokeWidth,
                    StrokeJoin = SKStrokeJoin.Round,
                };

                canvas.DrawText(text, x, y, strokePaint);
            }

            canvas.DrawText(text, x, y, fillPaint);
        }
        finally
        {
            ownedTypeface?.Dispose();
        }
    }

    private static SKFontStyle ToSkiaStyle(CoverFontStyle style)
    {
        return style switch
        {
            CoverFontStyle.Regular => SKFontStyle.Normal,
            CoverFontStyle.Italic => SKFontStyle.Italic,
            CoverFontStyle.BoldItalic => SKFontStyle.BoldItalic,
            _ => SKFontStyle.Bold,
        };
    }

    private static string ResolveText(string template, int number)
    {
        return string.IsNullOrEmpty(template) ? string.Empty : template.Replace("{number}", number.ToString());
    }

    private SKBitmap CopyCachedBackground(string templatePath)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("Шаблон обложки не найден", templatePath);
        }

        var mtime = File.GetLastWriteTimeUtc(templatePath);

        lock (_cacheLock)
        {
            if (_cachedBitmap == null
                || !string.Equals(_cachedPath, templatePath, StringComparison.OrdinalIgnoreCase)
                || _cachedMtime != mtime)
            {
                var decoded = SKBitmap.Decode(templatePath)
                              ?? throw new InvalidOperationException($"Не удалось декодировать шаблон: {templatePath}");

                _cachedBitmap?.Dispose();
                _cachedBitmap = decoded;
                _cachedPath = templatePath;
                _cachedMtime = mtime;
            }

            return _cachedBitmap.Copy()
                   ?? throw new InvalidOperationException($"Не удалось скопировать декодированный шаблон: {templatePath}");
        }
    }
}
