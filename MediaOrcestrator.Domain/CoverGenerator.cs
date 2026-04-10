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
        DrawNumber(canvas, bitmap.Width, bitmap.Height, template, number);
        logger.LogTrace("Отрисована обложка №{Number} ({Width}×{Height})", number, bitmap.Width, bitmap.Height);
        return bitmap;
    }

    private static void DrawNumber(SKCanvas canvas, int width, int height, CoverTemplate template, int number)
    {
        var text = number.ToString();
        var fontSize = height * template.FontSizeRatio;
        var strokeWidth = height * template.StrokeWidthRatio;

        var ownedTypeface = SKTypeface.FromFamilyName(template.FontFamily, SKFontStyle.Bold);
        var typeface = ownedTypeface ?? SKTypeface.Default;

        try
        {
            using var fillPaint = new SKPaint
            {
                Color = template.FillColor,
                IsAntialias = true,
                Typeface = typeface,
                TextSize = fontSize,
                TextAlign = SKTextAlign.Center,
                Style = SKPaintStyle.Fill,
            };

            using var strokePaint = new SKPaint
            {
                Color = template.StrokeColor,
                IsAntialias = true,
                Typeface = typeface,
                TextSize = fontSize,
                TextAlign = SKTextAlign.Center,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth,
                StrokeJoin = SKStrokeJoin.Round,
            };

            var x = width * template.TextX;
            var metrics = fillPaint.FontMetrics;
            var y = height * template.TextY - (metrics.Ascent + metrics.Descent) / 2f;

            canvas.DrawText(text, x, y, strokePaint);
            canvas.DrawText(text, x, y, fillPaint);
        }
        finally
        {
            ownedTypeface?.Dispose();
        }
    }
}
