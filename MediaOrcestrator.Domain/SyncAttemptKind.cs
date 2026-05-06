namespace MediaOrcestrator.Domain;

public enum SyncAttemptKind
{
    None = 0,
    Started = 1,
    Succeeded = 2,
    FailedRetrying = 3,
    FailedFinal = 4,
    NonRetriable = 5,
    Joined = 6,
}
