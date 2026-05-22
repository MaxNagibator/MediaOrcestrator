using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Modules;

// TODO: Более красиво обыграть для общего ffmpeg
public sealed partial class VideoTranscoder(IToolPathProvider toolPathProvider, ILogger<VideoTranscoder> logger)
{
    private string? _h264Encoder;

    public async Task<string> GetH264EncoderAsync()
    {
        if (_h264Encoder != null)
        {
            return _h264Encoder;
        }

        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg);
        if (ffmpegPath == null)
        {
            _h264Encoder = "libx264";
            return _h264Encoder;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-encoders",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _h264Encoder = "libx264";
                return _h264Encoder;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = await stdoutTask;
            await stderrTask;

            _h264Encoder = output.Contains("h264_nvenc") ? "h264_nvenc" : "libx264";
            logger.LogInformation("Выбран H264 кодек: {Encoder}", _h264Encoder);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить доступные кодеки, используем libx264");
            _h264Encoder = "libx264";
        }

        return _h264Encoder;
    }

    public Task<bool> TranscodeVp9ToH264Async(
        string inputPath,
        string outputPath,
        TimeSpan totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        return TranscodeToH264Async(inputPath, outputPath, totalDuration, progress, cancellationToken);
    }

    public Task<bool> TranscodeAv1ToH264Async(
        string inputPath,
        string outputPath,
        TimeSpan totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        return TranscodeToH264Async(inputPath, outputPath, totalDuration, progress, cancellationToken);
    }

    public Task<bool> TranscodeH264ToVp9Async(
        string inputPath,
        string outputPath,
        TimeSpan totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var arguments = $"-y -i \"{inputPath}\" -c:v libvpx-vp9 -b:v 0 -crf 30 -deadline good -cpu-used 2 -c:a libopus \"{outputPath}\"";
        return RunFfmpegAsync(arguments, inputPath, outputPath, totalDuration, progress, cancellationToken);
    }

    public async Task<VideoFrameSize> GetVideoFrameSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var output = await RunFfprobeAsync($"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{filePath}\"",
            filePath,
            "определение размера кадра видео",
            cancellationToken);

        var parts = output.Split('x');

        if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
        {
            return new(width, height);
        }

        logger.LogWarning("ffprobe не вернул размер кадра для файла {FilePath}: '{Output}'", filePath, output);

        throw new NonRetriableException(output.Length == 0
            ? "В файле не найдена видеодорожка — определить размер кадра невозможно"
            : $"Не удалось разобрать размер кадра из вывода ffprobe: '{output}'");
    }

    public async Task<TimeSpan> GetVideoDurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var output = await RunFfprobeAsync($"-v error -show_entries format=duration -of csv=p=0 \"{filePath}\"",
            filePath,
            "определение длительности видео",
            cancellationToken);

        if (double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        logger.LogWarning("ffprobe не вернул длительность для файла {FilePath}: '{Output}'", filePath, output);

        throw new NonRetriableException($"Не удалось определить длительность видео из вывода ffprobe: '{output}'");
    }

    public async Task<string?> GetVideoCodecAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var output = await RunFfprobeAsync($"-v error -select_streams v:0 -show_entries stream=codec_name -of csv=p=0 \"{filePath}\"",
            filePath,
            "определение кодека видео",
            cancellationToken);

        if (output.Length != 0)
        {
            return output;
        }

        logger.LogWarning("ffprobe не нашёл видеодорожку в файле {FilePath}", filePath);

        return null;
    }

    private static bool TryParseFFmpegTime(string line, out double seconds)
    {
        seconds = 0;
        var match = FFmpegTimeRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        seconds = int.Parse(match.Groups[1].Value) * 3600
                  + int.Parse(match.Groups[2].Value) * 60
                  + int.Parse(match.Groups[3].Value)
                  + int.Parse(match.Groups[4].Value) / 100.0;

        return true;
    }

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})")]
    private static partial Regex FFmpegTimeRegex();

    private async Task<string> RunFfprobeAsync(
        string arguments,
        string filePath,
        string purpose,
        CancellationToken cancellationToken)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            logger.LogWarning("ffprobe не найден ({Purpose}): {FilePath}", purpose, filePath);

            throw new NonRetriableException($"ffprobe не установлен — {purpose} невозможно. Установите ffmpeg через панель управления инструментами.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = ffprobePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        Process? process;

        try
        {
            process = Process.Start(psi);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Не удалось запустить ffprobe ({Purpose}): {FilePath}", purpose, filePath);

            throw new NonRetriableException($"Не удалось запустить ffprobe — {purpose} невозможно: {ex.Message}", ex);
        }

        if (process is null)
        {
            logger.LogWarning("Process.Start вернул null для ffprobe ({Purpose}): {FilePath}", purpose, filePath);

            throw new NonRetriableException($"Не удалось запустить ffprobe — {purpose} невозможно");
        }

        using (process)
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var stdout = (await stdoutTask).Trim();
            var stderr = (await stderrTask).Trim();

            if (process.ExitCode == 0)
            {
                return stdout;
            }

            var details = stderr.Length == 0 ? "ffprobe не сообщил подробностей" : stderr;

            logger.LogWarning("ffprobe завершился с кодом {ExitCode} ({Purpose}) для файла {FilePath}: {Stderr}",
                process.ExitCode, purpose, filePath, details);

            throw new NonRetriableException($"ffprobe завершился с ошибкой (код {process.ExitCode}) — {purpose} невозможно: {details}");
        }
    }

    private async Task<bool> TranscodeToH264Async(
        string inputPath,
        string outputPath,
        TimeSpan totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var h264Encoder = await GetH264EncoderAsync();
        var preset = h264Encoder == "h264_nvenc" ? "slow" : "medium";
        var arguments = $"-y -i \"{inputPath}\" -c:v {h264Encoder} -preset {preset} -c:a copy \"{outputPath}\"";

        return await RunFfmpegAsync(arguments, inputPath, outputPath, totalDuration, progress, cancellationToken);
    }

    private async Task<bool> RunFfmpegAsync(
        string ffmpegArguments,
        string inputPath,
        string outputPath,
        TimeSpan totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg);
        if (ffmpegPath is null)
        {
            logger.LogError("ffmpeg не найден, конвертация невозможна");
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = ffmpegArguments,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                logger.LogError("Не удалось запустить ffmpeg процесс");
                return false;
            }

            await using (cancellationToken.Register(ForceStop))
            {
                var totalSeconds = totalDuration.TotalSeconds;
                var stderrBuilder = new StringBuilder();

                while (await process.StandardError.ReadLineAsync(cancellationToken) is { } line)
                {
                    stderrBuilder.AppendLine(line);

                    if (!(totalSeconds > 0) || !TryParseFFmpegTime(line, out var currentSeconds))
                    {
                        continue;
                    }

                    var percent = Math.Min(currentSeconds / totalSeconds * 100, 100);
                    progress?.Report(percent);
                }

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    logger.LogInformation("ffmpeg конвертация завершена успешно: {OutputPath}", outputPath);
                    return true;
                }

                logger.LogWarning("ffmpeg завершился с кодом {ExitCode} для файла: {FilePath}. Stderr: {Stderr}",
                    process.ExitCode, inputPath, stderrBuilder.ToString());

                return false;
            }

            // TODO: Костыль
            void ForceStop()
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сконвертировать видео через ffmpeg: {FilePath}", inputPath);
            return false;
        }
    }
}
