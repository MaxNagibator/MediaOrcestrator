namespace MediaOrcestrator.Runner;

//TODO: Подумать
public sealed class MediaGridRowDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public Dictionary<string, string> PlatformStatuses { get; set; } = new();
}
