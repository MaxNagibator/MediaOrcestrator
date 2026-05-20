using CliWrap;
using CliWrap.Exceptions;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Youtube;

internal sealed record YtDlpProgress(int PartNumber, double Progress);

internal sealed record YtDlpVideoInfo(
    string Id,
    string Title,
    string? Description,
    string? Uploader,
    TimeSpan? Duration,
    long? ViewCount,
    DateTimeOffset? UploadDate,
    string? Thumbnail,
    IReadOnlyList<string> Tags);

internal sealed partial class YtDlp(string path, string ffmpegPath, string jsRuntime = "none", string? jsRuntimeDir = null, string cookiePath = "")
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
            await using var stream = File.OpenRead(infoJsonPath);
            var info = await JsonSerializer.DeserializeAsync(stream, YoutubeJsonContext.Default.YtDlpInfoJson, cancellationToken);
            return info is null ? null : MapVideoInfo(info);
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

    public async Task<YtDlpVideoInfo?> GetVideoInfoAsync(
        string url,
        CancellationToken cancellationToken = default)
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

        using var stdOut = new MemoryStream();
        await ExecuteAsync(arguments, stdOutStream: stdOut, cancellationToken: cancellationToken);

        if (stdOut.Length == 0)
        {
            return null;
        }

        stdOut.Position = 0;
        var info = await JsonSerializer.DeserializeAsync(stdOut, YoutubeJsonContext.Default.YtDlpInfoJson, cancellationToken);
        return info is null ? null : MapVideoInfo(info);
    }

    public async Task<IReadOnlyList<YtDlpCommentJson>> GetCommentsAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "--dump-single-json",
            "--skip-download",
            "--write-comments",
            "--extractor-args", "youtube:max_comments=all,all,all,all",
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

        using var stdOut = new MemoryStream();
        var stdErr = new StringBuilder();
        await ExecuteAsync(arguments, stdOutStream: stdOut, stdErrBuilder: stdErr, cancellationToken: cancellationToken);

        if (stdOut.Length == 0)
        {
            var stderrSummary = stdErr.Length == 0 ? "(stderr пустой)" : stdErr.ToString().Trim();
            throw new InvalidOperationException($"yt-dlp вернул пустой ответ при получении комментариев для {url}. stderr: {stderrSummary}");
        }

        stdOut.Position = 0;
        var info = await JsonSerializer.DeserializeAsync(stdOut, YoutubeJsonContext.Default.YtDlpInfoJson, cancellationToken);
        return info?.Comments ?? [];
    }

    private static YtDlpVideoInfo MapVideoInfo(YtDlpInfoJson info)
    {
        var duration = info.Duration.HasValue
            ? TimeSpan.FromSeconds(info.Duration.Value)
            : (TimeSpan?)null;

        DateTimeOffset? uploadDate = null;
        if (info.Timestamp.HasValue)
        {
            uploadDate = DateTimeOffset.FromUnixTimeSeconds(info.Timestamp.Value);
        }
        else if (!string.IsNullOrEmpty(info.UploadDate)
                 && DateTimeOffset.TryParseExact(info.UploadDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            uploadDate = parsed;
        }

        IReadOnlyList<string> tags = info.Tags ?? [];

        return new(info.Id ?? "",
            info.Title ?? "",
            info.Description,
            info.Uploader ?? info.Channel,
            duration,
            info.ViewCount,
            uploadDate,
            info.Thumbnail,
            tags);
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
        Stream? stdOutStream = null,
        StringBuilder? stdErrBuilder = null,
        CancellationToken cancellationToken = default)
    {
        var argumentsList = arguments.ToList();
        var commandString = $"{path} {string.Join(" ", argumentsList.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}";

        var stdErrBuffer = stdErrBuilder ?? new StringBuilder();

        var stdOutPipe = PipeTarget.Merge(progress?.Pipe(CreateProgressRouter) ?? PipeTarget.Null,
            outputCallback != null ? PipeTarget.ToDelegate(outputCallback) : PipeTarget.Null,
            stdOutStream != null ? PipeTarget.ToStream(stdOutStream) : PipeTarget.Null);

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
