namespace MediaOrcestrator.Runner;

internal static class ActionFormatting
{
    public static string FormatDuration(TimeSpan duration)
    {
        var totalSeconds = (int)Math.Round(duration.TotalSeconds);
        if (totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        if (totalSeconds < 60)
        {
            return $"за {totalSeconds} с";
        }

        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        if (minutes < 60)
        {
            return seconds == 0
                ? $"за {minutes} мин"
                : $"за {minutes} мин {seconds} с";
        }

        var hours = minutes / 60;
        minutes %= 60;
        return minutes == 0
            ? $"за {hours} ч"
            : $"за {hours} ч {minutes} мин";
    }
}
