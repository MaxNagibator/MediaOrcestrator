namespace MediaOrcestrator.Modules;

public class MediaDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string DataPath { get; set; }
    public string PreviewPath { get; set; }

    public List<MetadataItem>? Metadata { get; set; }

    public string TempDataPath { get; set; } // todo это отсюда уберём
    public string TempPreviewPath { get; set; }

    public bool IsExcluded { get; set; }
}
