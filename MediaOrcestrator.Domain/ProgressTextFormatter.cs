namespace MediaOrcestrator.Domain;

public static class ProgressTextFormatter
{
    private const int SecondsInMinute = 60;
    private const int SecondsInHour = 60 * 60;

    public static string FormatEta(TimeSpan? remaining)
    {
        if (remaining is not { } value || value < TimeSpan.Zero)
        {
            return "оценка...";
        }

        return $"осталось ~{FormatDurationApprox(value)}";
    }

    public static string FormatDurationApprox(TimeSpan duration)
    {
        var totalSeconds = (long)Math.Ceiling(duration.TotalSeconds);
        if (totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        if (totalSeconds < SecondsInMinute)
        {
            return $"{totalSeconds} с";
        }

        if (totalSeconds < SecondsInHour)
        {
            var minutes = (long)Math.Ceiling(totalSeconds / (double)SecondsInMinute);
            return $"{minutes} мин";
        }

        var hours = totalSeconds / SecondsInHour;
        var restMinutes = totalSeconds % SecondsInHour / SecondsInMinute;
        return restMinutes == 0
            ? $"{hours} ч"
            : $"{hours} ч {restMinutes} мин";
    }
}
