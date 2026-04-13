using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaOrcestrator.Runner;

public sealed record ErrorReportPayload(
    string IssueUrl,
    string FullReport,
    string LogClipboard,
    string Title,
    string Summary);

public sealed class ErrorReportService(ILogger<ErrorReportService> logger)
{
    private const string IssueRepo = "MaxNagibator/MediaOrcestrator";
    private const int MaxEncodedUrlLength = 7000;
    private const int MaxLogLines = 150;

    private const string SessionStartMarker = "Приложение запускается";

    public ErrorReportPayload Build(Exception? exception, string? userContext)
    {
        try
        {
            return BuildCore(exception, userContext);
        }
        catch (Exception buildEx)
        {
            logger.LogError(buildEx, "Не удалось собрать отчёт об ошибке");

            var fallbackTitle = exception != null
                ? $"[bug] {exception.GetType().Name}"
                : "[report] Без описания";

            var fallbackSummary = exception?.Message ?? userContext ?? "Без описания";
            var fallbackBody =
                $"> ⚠️ При сборе отчёта произошла ошибка: {buildEx.Message}\n\n" + $"### Исходное исключение\n\n```\n{exception?.ToString() ?? "(нет)"}\n```";

            var fallbackUrl = BuildIssueUrl(fallbackTitle, fallbackBody);
            return new(fallbackUrl, fallbackBody, fallbackBody, fallbackTitle, fallbackSummary);
        }
    }

    private static string BuildIssueUrl(string title, string body)
    {
        var prefix = $"https://github.com/{IssueRepo}/issues/new?title={Uri.EscapeDataString(title)}&body=";
        var budget = MaxEncodedUrlLength - prefix.Length;
        var encodedBody = Uri.EscapeDataString(body);

        while (encodedBody.Length > budget && body.Length > 100)
        {
            var newLen = Math.Max(100, (int)(body.Length * 0.8));
            body = Truncate(body, newLen);
            encodedBody = Uri.EscapeDataString(body);
        }

        return prefix + encodedBody;
    }

    private static string BuildTitle(Exception? exception, string? userContext)
    {
        if (exception != null)
        {
            var typeName = exception.GetType().Name;
            var head = exception.Message.Length > 80 ? exception.Message[..80] + "…" : exception.Message;
            return $"[bug] {typeName}: {head}";
        }

        if (!string.IsNullOrWhiteSpace(userContext))
        {
            var head = userContext.Length > 80 ? userContext[..80] + "…" : userContext;
            return $"[report] {head}";
        }

        return "[report] Без описания";
    }

    private static string BuildMetadata()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        var sb = new StringBuilder();
        sb.AppendLine($"**Версия:** {version}");
        sb.AppendLine($"**ОС:** {RuntimeInformation.OSDescription}");
        sb.AppendLine($"**.NET:** {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"**Архитектура:** {RuntimeInformation.ProcessArchitecture}");
        sb.Append($"**Время (UTC):** {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        return sb.ToString();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "\n…(обрезано)";
    }

    private ErrorReportPayload BuildCore(Exception? exception, string? userContext)
    {
        var title = BuildTitle(exception, userContext);
        var summary = exception?.Message ?? userContext ?? "Без описания";

        var metadata = BuildMetadata();
        var stackTrace = exception?.ToString() ?? "(без исключения — проактивный репорт)";
        var logTail = ReadLogTail();
        var userNote = string.IsNullOrWhiteSpace(userContext)
            ? string.Empty
            : $"""
               ### Контекст от пользователя

               {userContext}


               """;

        var logSection = $"""
                          ### Лог сессии

                          ```
                          {logTail}
                          ```
                          """;

        var fullReport =
            $"""
             {userNote}### Метаданные

             {metadata}

             ### Исключение

             ```
             {stackTrace}
             ```

             {logSection}
             """;

        var urlBody =
            $"""
             {userNote}### Метаданные

             {metadata}

             ### Исключение

             ```
             {Truncate(stackTrace, 2000)}
             ```

             ⬇ Вставьте лог сессии из буфера обмена (Ctrl+V):


             """;

        var issueUrl = BuildIssueUrl(title, urlBody);

        return new(issueUrl, fullReport, logSection, title, summary);
    }

    private string ReadLogTail()
    {
        try
        {
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");

            if (!Directory.Exists(logsDir))
            {
                return "(папка logs отсутствует)";
            }

            var latest = new DirectoryInfo(logsDir)
                .EnumerateFiles("log-*.txt")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latest == null)
            {
                return "(лог-файлы не найдены)";
            }

            using var stream = new FileStream(latest.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var allLines = new List<string>();

            while (reader.ReadLine() is { } line)
            {
                allLines.Add(line);
            }

            if (allLines.Count == 0)
            {
                return "(лог пуст)";
            }

            var sessionStartIndex = -1;
            for (var i = allLines.Count - 1; i >= 0; i--)
            {
                if (!allLines[i].Contains(SessionStartMarker, StringComparison.Ordinal))
                {
                    continue;
                }

                sessionStartIndex = i;
                break;
            }

            var tailStart = Math.Max(0, allLines.Count - MaxLogLines);
            var skipCount = sessionStartIndex >= 0
                ? Math.Max(sessionStartIndex, tailStart)
                : tailStart;

            return string.Join(Environment.NewLine, allLines.Skip(skipCount));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось прочитать хвост лога для отчёта об ошибке");
            return $"(не удалось прочитать лог: {ex.Message})";
        }
    }
}
