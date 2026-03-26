using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Domain;

public class ToolVersionDetector(ILogger<ToolVersionDetector> logger)
{
    public async Task<string?> GetInstalledVersionAsync(
        string? toolPath,
        ToolDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        if (toolPath is null || !File.Exists(toolPath))
        {
            return null;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = descriptor.VersionCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                return "unknown";
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var token = timeoutCts.Token;

            var output = await process.StandardOutput.ReadToEndAsync(token);
            var errorOutput = await process.StandardError.ReadToEndAsync(token);

            await process.WaitForExitAsync(token);

            var fullOutput = string.IsNullOrWhiteSpace(output) ? errorOutput : output;
            fullOutput = fullOutput.Trim();

            if (descriptor.VersionPattern is null)
            {
                return fullOutput.Split('\n')[0].Trim();
            }

            var match = Regex.Match(fullOutput, descriptor.VersionPattern);
            return match.Success ? match.Groups[1].Value : "unknown";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить версию '{Name}'", descriptor.Name);
            return "unknown";
        }
    }

    public static bool IsUpdateAvailable(string? installedVersion, string? latestVersion)
    {
        if (latestVersion is null)
        {
            return false;
        }

        if (installedVersion is null)
        {
            return true;
        }

        return !VersionsEqual(installedVersion, latestVersion);
    }

    public static string NormalizeTagVersion(string tag, string? versionTagPattern)
    {
        if (versionTagPattern is null)
        {
            return tag.StartsWith('v') ? tag[1..] : tag;
        }

        var match = Regex.Match(tag, versionTagPattern);
        return match.Success ? match.Groups[1].Value : tag;
    }

    private static bool VersionsEqual(string version1, string version2)
    {
        var v1 = NormalizeVersion(version1);
        var v2 = NormalizeVersion(version2);

        if (string.Equals(v1, v2, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Version.TryParse(v1, out var semVer1) && Version.TryParse(v2, out var semVer2))
        {
            return semVer1 == semVer2;
        }

        var normalized1 = v1.Replace("-", "").Replace(".", "");
        var normalized2 = v2.Replace("-", "").Replace(".", "");
        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string version)
    {
        version = version.Trim();
        return version.StartsWith('v') ? version[1..] : version;
    }
}
