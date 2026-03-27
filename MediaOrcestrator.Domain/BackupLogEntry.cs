namespace MediaOrcestrator.Domain;

public record BackupLogEntry(string FileName, DateTime CreatedAt, BackupTrigger Trigger, long SizeBytes);
