using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Domain;

public static class CoverNumberResolver
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    public static int Resolve(CoverTemplate template, string? title, int index, ILogger? logger = null)
    {
        var fallback = template.StartNumber + index;

        if (template.NumberMode != CoverNumberMode.TitleRegex)
        {
            return fallback;
        }

        var pattern = string.IsNullOrWhiteSpace(template.TitleRegexPattern)
            ? CoverTemplate.DefaultTitleRegex
            : template.TitleRegexPattern;

        if (string.IsNullOrEmpty(title))
        {
            logger?.LogWarning("Не удалось извлечь номер: пустой Title, использован {Fallback}", fallback);
            return fallback;
        }

        try
        {
            var match = Regex.Match(title, pattern, RegexOptions.None, RegexTimeout);

            if (match.Success)
            {
                var captured = match.Groups.Count > 1 && match.Groups[1].Success ? match.Groups[1].Value : match.Value;

                if (int.TryParse(captured, out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            logger?.LogWarning(ex, "Регулярка для номера зависла на '{Title}', использован {Fallback}", title, fallback);
            return fallback;
        }
        catch (ArgumentException ex)
        {
            logger?.LogWarning(ex, "Невалидная регулярка номера '{Pattern}', использован {Fallback}", pattern, fallback);
            return fallback;
        }

        logger?.LogWarning("Не удалось извлечь номер из '{Title}' по '{Pattern}', использован {Fallback}", title, pattern, fallback);
        return fallback;
    }
}
