namespace MediaOrcestrator.Domain;

public sealed record AppUpdateInfo(string Version, string DownloadUrl, string ReleaseNotes, long Size);
