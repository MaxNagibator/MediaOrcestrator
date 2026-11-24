namespace MediaOrcestrator.Instagram;

public sealed class InstagramOptions
{
    public TimeSpan UploadTimeout { get; set; } = TimeSpan.FromHours(2);
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ProcessingPollInterval { get; set; } = TimeSpan.FromSeconds(10);
}
