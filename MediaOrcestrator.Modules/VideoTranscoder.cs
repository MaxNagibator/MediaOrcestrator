using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
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

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

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

    public async Task<VideoFrameSize?> GetVideoFrameSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            logger.LogWarning("Отсутствует ffprobe для определения размера кадра");
            return null;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                logger.LogWarning("ffprobe завершился с кодом {ExitCode} для файла: {FilePath}", process.ExitCode, filePath);
                return null;
            }

            var parts = output.Trim().Split('x');

            if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
            {
                return new VideoFrameSize(width, height);
            }

            logger.LogWarning("Неожиданный вывод ffprobe: '{Output}' для файла: {FilePath}", output.Trim(), filePath);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить размер кадра видео через ffprobe: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<TimeSpan?> GetVideoDurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            logger.LogWarning("Отсутствует ffprobe для определения длительности видео");
            return null;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v error -show_entries format=duration -of csv=p=0 \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                logger.LogWarning("ffprobe завершился с кодом {ExitCode} для файла: {FilePath}", process.ExitCode, filePath);
                return null;
            }

            if (double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }

            logger.LogWarning("Неожиданный вывод ffprobe (длительность): '{Output}' для файла: {FilePath}", output.Trim(), filePath);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить длительность видео через ffprobe: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<string?> GetVideoCodecAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            return null;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v quiet -print_format json -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                logger.LogWarning("ffprobe завершился с кодом {ExitCode} для файла: {FilePath}", process.ExitCode, filePath);
                return null;
            }

            using var doc = JsonDocument.Parse(output);

            if (!doc.RootElement.TryGetProperty("streams", out var streams))
            {
                return null;
            }

            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = stream.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;

                if (codecType == "video" && stream.TryGetProperty("codec_name", out var codecName))
                {
                    return codecName.GetString();
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить кодек видео через ffprobe: {FilePath}", filePath);
            return null;
        }
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
