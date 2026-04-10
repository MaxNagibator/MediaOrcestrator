using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace MediaOrcestrator.Domain;

public sealed class CoverGenerator(ILogger<CoverGenerator> logger)
{
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
        if (!File.Exists(template.TemplatePath))
        {
            throw new FileNotFoundException("Шаблон обложки не найден", template.TemplatePath);
        }

        var bitmap = SKBitmap.Decode(template.TemplatePath)
                     ?? throw new InvalidOperationException($"Не удалось декодировать шаблон: {template.TemplatePath}");

        using var canvas = new SKCanvas(bitmap);

        foreach (var layer in template.Layers)
        {
            DrawLayer(canvas, bitmap.Width, bitmap.Height, layer, number);
        }

        logger.LogTrace("Отрисована обложка №{Number} ({Width}×{Height}, слоёв: {Layers})", number, bitmap.Width, bitmap.Height, template.Layers.Count);
        return bitmap;
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

        var ownedTypeface = SKTypeface.FromFamilyName(layer.FontFamily, SKFontStyle.Bold);
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

    private static string ResolveText(string template, int number)
    {
        return string.IsNullOrEmpty(template) ? string.Empty : template.Replace("{number}", number.ToString());
    }
}
