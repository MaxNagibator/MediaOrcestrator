namespace MediaOrcestrator.Domain;

/// <summary>
/// Связи синхронизациимежду хранилищами
/// </summary>
public class SourceSyncRelation
{
    public Source To { get; set; }
    public Source From { get; set; }
}
