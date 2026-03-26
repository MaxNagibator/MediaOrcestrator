namespace MediaOrcestrator.Modules;

public record ToolDescriptor
{
    public required string Name { get; init; }
    public required string GitHubRepo { get; init; }
    public required string AssetPattern { get; init; }
    public required string VersionCommand { get; init; }
    public string? VersionPattern { get; init; }
    public string? VersionTagPattern { get; init; }
    public string? ArchiveExecutablePath { get; init; }
    public ArchiveType ArchiveType { get; init; } = ArchiveType.None;
    public IReadOnlyList<string>? CompanionExecutables { get; init; }
}
