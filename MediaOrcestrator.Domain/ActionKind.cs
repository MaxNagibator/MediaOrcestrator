namespace MediaOrcestrator.Domain;

public enum ActionKind
{
    None = 0,
    Sync = 1,
    Download = 2,
    Upload = 3,
    Transfer = 4,
    Comments = 5,
    Other = 6,
}
