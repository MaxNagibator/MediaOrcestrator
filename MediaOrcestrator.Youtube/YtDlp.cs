using CliWrap;
using CliWrap.Exceptions;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Youtube;

public sealed record YtDlpProgress(int PartNumber, double Progress);

public sealed record YtDlpVideoInfo(
    string Id,
    string Title,
    string? Description,
    string? Uploader,
    TimeSpan? Duration,
    long? ViewCount,
    DateTimeOffset? UploadDate,
    string? Thumbnail,
    IReadOnlyList<string> Tags);

public sealed partial class YtDlp(string path, string ffmpegPath, string jsRuntime = "none", string? jsRuntimeDir = null, string cookiePath = "")
{
    public async Task<YtDlpVideoInfo?> DownloadAsync(
        string url,
        string outputPath,
        IProgress<YtDlpProgress>? progress = null,
        long? rateLimitBytes = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "-f", "bestvideo+bestaudio/best",
            "--merge-output-format", "mp4",
            "--newline",
            "--no-colors",
            "--ffmpeg-location", ffmpegPath,
            "--write-thumbnail",
            "--convert-thumbnails", "jpg",
            "--write-info-json",
            "-o", outputPath,
        };

        if (rateLimitBytes.HasValue)
        {
            arguments.Add("--limit-rate");
            arguments.Add(rateLimitBytes.Value.ToString());
        }

        if (!string.IsNullOrEmpty(cookiePath))
        {
            arguments.Add("--cookies");
            arguments.Add(cookiePath);
        }

        if (!string.Equals(jsRuntime, "none", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add("--js-runtimes");
            arguments.Add(jsRuntime);
        }

        arguments.Add(url);

        var partNumber = 0;

        Progress<double> internalProgress = new(percent =>
        {
            progress?.Report(new(partNumber > 0 ? partNumber : 1, percent));
        });

        await ExecuteAsync(arguments, internalProgress, OutputCallback, cancellationToken: cancellationToken);

        var infoJsonPath = Path.ChangeExtension(outputPath, ".info.json");
        if (!File.Exists(infoJsonPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(infoJsonPath, cancellationToken);
            return ParseVideoInfo(json);
        }
        catch (JsonException)
        {
            return null;
        }

        void OutputCallback(string line)
        {
            if (!line.StartsWith("[download] Destination:", StringComparison.Ordinal))
            {
                return;
            }

            partNumber++;
            progress?.Report(new(partNumber, 0));
        }
    }

    public async Task<YtDlpVideoInfo?> GetVideoInfoAsync(string url, CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "--dump-single-json",
            "--skip-download",
            "--no-warnings",
            "--no-colors",
        };

        if (!string.IsNullOrEmpty(cookiePath))
        {
            arguments.Add("--cookies");
            arguments.Add(cookiePath);
        }

        if (!string.Equals(jsRuntime, "none", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add("--js-runtimes");
            arguments.Add(jsRuntime);
        }

        arguments.Add(url);

        StringBuilder stdOut = new();
        await ExecuteAsync(arguments, stdOutBuffer: stdOut, cancellationToken: cancellationToken);

        return stdOut.Length == 0 ? null : ParseVideoInfo(stdOut.ToString());
    }

    private static YtDlpVideoInfo? ParseVideoInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = TryGetString(root, "id") ?? "";
        var title = TryGetString(root, "title") ?? "";
        var description = TryGetString(root, "description");
        var uploader = TryGetString(root, "uploader") ?? TryGetString(root, "channel");
        var thumbnail = TryGetString(root, "thumbnail");

        TimeSpan? duration = null;
        if (root.TryGetProperty("duration", out var durElement) && durElement.ValueKind == JsonValueKind.Number)
        {
            duration = TimeSpan.FromSeconds(durElement.GetDouble());
        }

        long? viewCount = null;
        if (root.TryGetProperty("view_count", out var viewsElement) && viewsElement.ValueKind == JsonValueKind.Number)
        {
            viewCount = viewsElement.GetInt64();
        }

        DateTimeOffset? uploadDate = null;
        if (root.TryGetProperty("timestamp", out var tsElement) && tsElement.ValueKind == JsonValueKind.Number)
        {
            uploadDate = DateTimeOffset.FromUnixTimeSeconds(tsElement.GetInt64());
        }
        else
        {
            var uploadDateStr = TryGetString(root, "upload_date");
            if (!string.IsNullOrEmpty(uploadDateStr)
                && DateTimeOffset.TryParseExact(uploadDateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                uploadDate = parsed;
            }
        }

        IReadOnlyList<string> tags = [];
        if (root.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
        {
            tags = tagsElement.EnumerateArray()
                .Select(t => t.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .Cast<string>()
                .ToArray();
        }

        return new(id, title, description, uploader, duration, viewCount, uploadDate, thumbnail,
            tags);
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static PipeTarget CreateProgressRouter(IProgress<double> progress)
    {
        return PipeTarget.ToDelegate(line =>
        {
            var match = ProgressRegex().Match(line);
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var percent))
            {
                progress.Report((percent / 100.0).Clamp(0, 1));
            }
        });
    }

    [GeneratedRegex(@"\[download\]\s+(\d+\.?\d*)%")]
    private static partial Regex ProgressRegex();

    private async ValueTask ExecuteAsync(
        IEnumerable<string> arguments,
        IProgress<double>? progress = null,
        Action<string>? outputCallback = null,
        StringBuilder? stdOutBuffer = null,
        CancellationToken cancellationToken = default)
    {
        var argumentsList = arguments.ToList();
        var commandString = $"{path} {string.Join(" ", argumentsList.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";

        StringBuilder stdErrBuffer = new();

        var stdOutPipe = PipeTarget.Merge(progress?.Pipe(CreateProgressRouter) ?? PipeTarget.Null,
            outputCallback != null ? PipeTarget.ToDelegate(outputCallback) : PipeTarget.Null,
            stdOutBuffer != null ? PipeTarget.ToStringBuilder(stdOutBuffer) : PipeTarget.Null);

        var stdErrPipe = PipeTarget.ToStringBuilder(stdErrBuffer);

        try
        {
            var command = Cli.Wrap(path)
                .WithArguments(arguments)
                .WithEnvironmentVariables(env =>
                {
                    if (jsRuntimeDir is null)
                    {
                        return;
                    }

                    var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    env.Set("PATH", $"{jsRuntimeDir};{currentPath}");
                })
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            await command.ExecuteAsync(cancellationToken);
        }
        catch (CommandExecutionException exception)
        {
            var message = $"""
                           Ошибка выполнения yt-dlp.

                           Команда:
                           {commandString}

                           Вывод ошибок:
                           {stdErrBuffer}
                           """;

            throw new InvalidOperationException(message, exception);
        }
    }
}
