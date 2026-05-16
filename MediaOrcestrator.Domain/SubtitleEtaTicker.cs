using System.Diagnostics;

namespace MediaOrcestrator.Domain;

public sealed class SubtitleEtaTicker(ActionHolder.RunningAction action, ProgressEtaEstimator estimator)
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);

    private readonly Stopwatch _sinceStart = Stopwatch.StartNew();
    private readonly object _lock = new();

    private long _lastEmitTicks = -1;
    private int _lastWholePercent = -1;

    public void Report(double percent)
    {
        var wholePercent = (int)Math.Clamp(percent, 0, 100);
        var elapsed = _sinceStart.Elapsed;

        string subtitle;

        lock (_lock)
        {
            var sinceEmit = _lastEmitTicks < 0
                ? TimeSpan.MaxValue
                : TimeSpan.FromTicks(elapsed.Ticks - _lastEmitTicks);

            var percentMoved = wholePercent != _lastWholePercent;
            if (!percentMoved && sinceEmit < MinInterval)
            {
                return;
            }

            _lastEmitTicks = elapsed.Ticks;
            _lastWholePercent = wholePercent;

            var remaining = estimator.Update(percent, elapsed);
            subtitle = ProgressTextFormatter.FormatEta(remaining);
        }

        action.Subtitle = subtitle;
    }
}
