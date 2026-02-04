using CliWrap;
using CliWrap.Exceptions;
using MediaOrcestrator.Core.Extensions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Youtube;

public sealed record YtDlpProgress(int PartNumber, double Progress);

public sealed partial class YtDlp(string path)
{
    public async Task DownloadAsync(string url, string outputPath, IProgress<YtDlpProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var arguments = new[]
        {
            "-f", "bestvideo+bestaudio/best",
            "--merge-output-format", "mp4",
            "--newline",
            "--no-colors",
            "-o", outputPath,
            url,
        };

        var partNumber = 0;

        Progress<double> internalProgress = new(percent =>
        {
            progress?.Report(new(partNumber > 0 ? partNumber : 1, percent));
        });

        await ExecuteAsync(arguments, internalProgress, OutputCallback, cancellationToken);
        return;

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

    public async ValueTask ExecuteAsync(IEnumerable<string> arguments, IProgress<double>? progress = null, Action<string>? outputCallback = null, CancellationToken cancellationToken = default)
    {
        StringBuilder stdErrBuffer = new();

        var stdOutPipe = PipeTarget.Merge(progress?.Pipe(CreateProgressRouter) ?? PipeTarget.Null,
            outputCallback != null ? PipeTarget.ToDelegate(outputCallback) : PipeTarget.Null);

        var stdErrPipe = PipeTarget.ToStringBuilder(stdErrBuffer);

        try
        {
            var command = Cli.Wrap(path)
                .WithArguments(arguments)
                .WithStandardOutputPipe(stdOutPipe)
                .WithStandardErrorPipe(stdErrPipe);

            await command.ExecuteAsync(cancellationToken);
        }
        catch (CommandExecutionException exception)
        {
            var message = $"""
                           yt-dlp command-line tool failed with an error.

                           Standard error:
                           {stdErrBuffer}
                           """;

            throw new InvalidOperationException(message, exception);
        }
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
}
