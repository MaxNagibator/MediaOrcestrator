namespace MediaOrcestrator.Youtube;

internal sealed record YoutubeDownloadSettings(bool DownloadStreams, string? StateDirectory = null)
{
    public const string DownloadStreamsKey = "download_streams";

    public static YoutubeDownloadSettings FromSettings(IReadOnlyDictionary<string, string> settings)
    {
        var stateDirectory = settings.GetValueOrDefault("_system_state_path");

        if (!settings.TryGetValue(DownloadStreamsKey, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return new(true, stateDirectory);
        }

        if (!bool.TryParse(value, out var downloadStreams))
        {
            throw new InvalidOperationException($"Некорректное значение настройки «скачивать трансляции»: {value}");
        }

        return new(downloadStreams, stateDirectory);
    }
}
