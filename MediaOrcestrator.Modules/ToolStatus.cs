namespace MediaOrcestrator.Modules;

public record ToolStatus
{
    public required string Name { get; init; }
    public string? InstalledVersion { get; init; }
    public string? LatestVersion { get; init; }
    public bool UpdateAvailable { get; init; }
    public string? ResolvedPath { get; init; }
    public DateTimeOffset? LastChecked { get; init; }
}
