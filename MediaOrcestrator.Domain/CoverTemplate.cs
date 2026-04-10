using SkiaSharp;

namespace MediaOrcestrator.Domain;

public enum CoverNumberMode
{
    Sequential = 0,
    TitleRegex = 1,
}

public sealed record CoverTextLayer(
    string TextTemplate,
    float TextX,
    float TextY,
    float FontSizeRatio,
    string FontFamily,
    SKColor FillColor,
    SKColor StrokeColor,
    float StrokeWidthRatio);

public sealed record CoverTemplate(
    string TemplatePath,
    int StartNumber,
    CoverNumberMode NumberMode,
    string TitleRegexPattern,
    List<CoverTextLayer> Layers)
{
    public const string DefaultTitleRegex = @"#?(\d+)";

    public static CoverTextLayer DefaultNumberLayer { get; } = new("{number}",
        0.5f,
        0.5f,
        0.25f,
        "Impact",
        new(255, 255, 255),
        new(0, 0, 0),
        0.01f);
}
