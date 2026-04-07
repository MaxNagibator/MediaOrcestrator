using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Youtube;

internal static class RetryHelper
{
    public static async Task<T?> ExecuteAsync<T>(
        Func<Task<T?>> action,
        int maxRetries,
        int delayMs,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                logger.LogWarning(ex, "Попытка {RetryCount}/{MaxRetries} не удалась. Повтор через {DelayMs}мс", retryCount, maxRetries, delayMs);

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        return await action();
    }
}
