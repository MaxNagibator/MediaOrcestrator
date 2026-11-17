namespace MediaOrcestrator.Modules;

public sealed class MediaDownloadSkippedException(string message) : Exception(message);
