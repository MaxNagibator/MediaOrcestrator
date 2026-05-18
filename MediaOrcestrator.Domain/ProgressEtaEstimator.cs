namespace MediaOrcestrator.Domain;

public sealed class ProgressEtaEstimator(double smoothingFactor = 0.25)
{
    private readonly double _smoothing = Math.Clamp(smoothingFactor, 0.01, 1.0);

    private double _smoothedRemainingSeconds = -1;
    private int _samples;

    public TimeSpan? Update(double percent, TimeSpan elapsed)
    {
        if (percent < 2 || percent >= 100 || elapsed <= TimeSpan.Zero)
        {
            return null;
        }

        var fraction = Math.Clamp(percent / 100.0, 0, 1);
        var rawRemaining = elapsed.TotalSeconds / fraction - elapsed.TotalSeconds;
        if (rawRemaining < 0 || double.IsNaN(rawRemaining) || double.IsInfinity(rawRemaining))
        {
            return null;
        }

        _smoothedRemainingSeconds = _smoothedRemainingSeconds < 0
            ? rawRemaining
            : _smoothing * rawRemaining + (1 - _smoothing) * _smoothedRemainingSeconds;

        _samples++;

        return _samples < 3 ? null : TimeSpan.FromSeconds(_smoothedRemainingSeconds);
    }
}
