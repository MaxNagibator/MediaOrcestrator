using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Modules;

public static class UploadProgressLogger
{
    public static IProgress<double> CreateBucketed(ILogger logger, string title, int bucketCount = 10, IProgress<UploadProgress>? external = null)
    {
        if (bucketCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bucketCount), "Количество корзин должно быть положительным");
        }

        var lockObject = new object();
        var lastReportedBucket = -1;

        return new Progress<double>(fraction =>
        {
            external?.Report(new(Math.Clamp(fraction, 0.0, 1.0) * 100));

            lock (lockObject)
            {
                var bucket = (int)Math.Floor(Math.Clamp(fraction, 0.0, 1.0) * bucketCount);

                if (bucket <= lastReportedBucket)
                {
                    return;
                }

                lastReportedBucket = bucket;
                logger.LogInformation("Прогресс загрузки '{Title}': {Percent:P0}", title, fraction);
            }
        });
    }
}
