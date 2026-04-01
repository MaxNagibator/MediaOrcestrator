namespace MediaOrcestrator.Domain;

public record ReleaseInfo
{
    public required string TagName { get; init; }
    public string? AssetUrl { get; init; }
    public string? AssetName { get; init; }
    public long AssetSize { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public string? Body { get; init; }
}
