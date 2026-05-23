using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace MediaOrcestrator.Domain;

public static class RetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxAttempts,
        ILogger logger,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, maxAttempts);
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (attempt < attempts && IsTransient(ex, cancellationToken))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

                logger.LogWarning(ex,
                    "{Operation}: попытка {Attempt}/{Max} не удалась ({ExType}: {Msg}); повтор через {Delay}",
                    operationDescription, attempt, attempts, ex.GetType().Name, ex.Message, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        int maxAttempts,
        ILogger logger,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(async ct =>
        {
            await action(ct);
            return 0;
        }, maxAttempts, logger, operationDescription, cancellationToken);
    }

    private static bool IsTransient(Exception ex, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return ex switch
        {
            NonRetriableException => false,
            TaskCanceledException => true,
            OperationCanceledException => false,
            HttpRequestException => true,
            SocketException => true,
            TimeoutException => true,
            FileNotFoundException => false,
            DirectoryNotFoundException => false,
            UnauthorizedAccessException => false,
            IOException => true,
            _ => false,
        };
    }
}
