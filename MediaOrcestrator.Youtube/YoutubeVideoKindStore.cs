namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeVideoKindStore
{
    public const string FileName = "video_kinds.txt";

    private readonly Dictionary<string, Dictionary<string, YoutubeVideoKind>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public YoutubeVideoKind Get(string? stateDirectory, string videoId)
    {
        if (string.IsNullOrEmpty(stateDirectory))
        {
            return YoutubeVideoKind.None;
        }

        lock (_lock)
        {
            return Load(stateDirectory).GetValueOrDefault(videoId);
        }
    }

    public void Save(string? stateDirectory, string videoId, YoutubeVideoKind kind)
    {
        if (string.IsNullOrEmpty(stateDirectory) || kind == YoutubeVideoKind.None)
        {
            return;
        }

        lock (_lock)
        {
            var kinds = Load(stateDirectory);
            if (kinds.TryGetValue(videoId, out var known) && known == kind)
            {
                return;
            }

            Directory.CreateDirectory(stateDirectory);
            File.AppendAllLines(Path.Combine(stateDirectory, FileName), [$"{videoId}	{kind}"]);
            kinds[videoId] = kind;
        }
    }

    private Dictionary<string, YoutubeVideoKind> Load(string stateDirectory)
    {
        if (_cache.TryGetValue(stateDirectory, out var kinds))
        {
            return kinds;
        }

        kinds = new(StringComparer.Ordinal);
        var path = Path.Combine(stateDirectory, FileName);
        if (File.Exists(path))
        {
            foreach (var parts in File.ReadLines(path).Select(line => line.Split('	')))
            {
                if (parts is [{ Length: > 0 }, _]
                    && Enum.TryParse<YoutubeVideoKind>(parts[1], out var kind)
                    && kind != YoutubeVideoKind.None)
                {
                    kinds[parts[0]] = kind;
                }
            }
        }

        _cache[stateDirectory] = kinds;
        return kinds;
    }
}
