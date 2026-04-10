using SkiaSharp;

namespace MediaOrcestrator.Domain;

public sealed record CoverTemplate(
    string TemplatePath,
    int StartNumber,
    float TextX,
    float TextY,
    float FontSizeRatio,
    string FontFamily,
    SKColor FillColor,
    SKColor StrokeColor,
    float StrokeWidthRatio);
