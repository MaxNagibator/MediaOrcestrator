namespace MediaOrcestrator.Domain;

public sealed class SyncIntent
{
    public Media Media { get; set; }
    public Source From { get; set; }
    public Source To { get; set; }
    public SourceSyncRelation Relation { get; set; }

    public List<SyncIntent> NextIntents { get; set; } = [];

    public bool IsSelected { get; set; } = true;

    public override string ToString()
    {
        return $"{Media.Title}: {From.TypeId} -> {To.TypeId}";
    }
}
