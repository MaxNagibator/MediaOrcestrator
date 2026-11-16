using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MediaOrcestrator.Domain;

public sealed class SyncRetryRunner(
    Orcestrator orcestrator,
    ILogger<SyncRetryRunner> logger)
{
    private const int DefaultMaxAttempts = 50;

    private readonly Dictionary<(string MediaId, string ToSourceId), Task<SyncRunOutcome>> _inflight = new();
    private readonly object _lock = new();

    public async Task<SyncRunOutcome> RunAsync(
        Media media,
        SourceSyncRelation relation,
        IProgress<SyncAttemptStatus>? progress = null,
        int maxAttempts = DefaultMaxAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogDebug("SyncRetryRunner.RunAsync запрошен: {Media} → {To}, maxAttempts={Max}",
            media.Title,
            relation.To.TitleFull,
            maxAttempts);

        var key = (media.Id, relation.To.Id);

        Task<SyncRunOutcome> runTask;
        bool isLeader;
        int inflightCount;

        lock (_lock)
        {
            if (_inflight.TryGetValue(key, out var existing))
            {
                runTask = existing;
                isLeader = false;
            }
            else
            {
                runTask = Task.Run(() => ExecuteAsync(media, relation, progress, maxAttempts, cancellationToken),
                    cancellationToken);

                _inflight[key] = runTask;
                isLeader = true;
            }

            inflightCount = _inflight.Count;
        }

        if (isLeader)
        {
            logger.LogDebug("In-flight регистрация: {Media} → {To} (всего активных: {Count})",
                media.Title,
                relation.To.TitleFull,
                inflightCount);

            try
            {
                return await runTask;
            }
            finally
            {
                int remaining;
                lock (_lock)
                {
                    _inflight.Remove(key);
                    remaining = _inflight.Count;
                }

                logger.LogDebug("In-flight снятие: {Media} → {To} (осталось активных: {Count})",
                    media.Title,
                    relation.To.TitleFull,
                    remaining);
            }
        }

        logger.LogInformation("Синхронизация {Media} → {To} уже выполняется, вызов присоединён к существующей операции (всего активных: {Count})",
            media.Title,
            relation.To.TitleFull,
            inflightCount);

        // TODO: лидерский CT «утекает» в followers - если лидер отменит свой CT,
        // runTask упадёт с OCE и follower получит её же, хотя его собственный CT не трогали.
        // Для честной развязки нужен ref-counted linked CTS или полностью отвязанный inner task.
        progress?.Report(new(SyncAttemptKind.Joined, 0, maxAttempts));
        var outcome = await runTask.WaitAsync(cancellationToken);

        logger.LogDebug("Follower {Media} → {To}: ожидание лидерской операции снято",
            media.Title,
            relation.To.TitleFull);

        return outcome;
    }

    private static TimeSpan GetDelayBeforeAttempt(int nextAttemptNumber)
    {
        return nextAttemptNumber switch
        {
            <= 1 => TimeSpan.Zero,
            2 => TimeSpan.FromMinutes(1),
            <= 4 => TimeSpan.FromMinutes(5),
            <= 9 => TimeSpan.FromMinutes(30),
            _ => TimeSpan.FromHours(1),
        };
    }

    private async Task<SyncRunOutcome> ExecuteAsync(
        Media media,
        SourceSyncRelation relation,
        IProgress<SyncAttemptStatus>? progress,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Попытка {Attempt}/{Max}: {Media} → {To} — старт",
                attempt,
                maxAttempts,
                media.Title,
                relation.To.TitleFull);

            progress?.Report(new(SyncAttemptKind.Started, attempt, maxAttempts));

            try
            {
                await orcestrator.TransferByRelation(media, relation, cancellationToken);

                logger.LogDebug("Попытка {Attempt}/{Max}: {Media} → {To} — успех",
                    attempt,
                    maxAttempts,
                    media.Title,
                    relation.To.TitleFull);

                progress?.Report(new(SyncAttemptKind.Succeeded, attempt, maxAttempts));
                return SyncRunOutcome.Transferred;
            }
            catch (MediaDownloadSkippedException ex)
            {
                logger.LogDebug("Попытка {Attempt}/{Max}: {Media} → {To} – пропуск: {Message}",
                    attempt,
                    maxAttempts,
                    media.Title,
                    relation.To.TitleFull,
                    ex.Message);

                progress?.Report(new(SyncAttemptKind.Skipped, attempt, maxAttempts, Error: ex));
                return SyncRunOutcome.Skipped;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var isNonRetriable = ex is NonRetriableException;
                var isFinal = isNonRetriable || attempt >= maxAttempts;

                if (isFinal)
                {
                    var kind = isNonRetriable ? SyncAttemptKind.NonRetriable : SyncAttemptKind.FailedFinal;

                    logger.LogDebug(ex,
                        "Попытка {Attempt}/{Max}: {Media} → {To} — финал ({Kind}): {Message}",
                        attempt,
                        maxAttempts,
                        media.Title,
                        relation.To.TitleFull,
                        kind,
                        ex.Message);

                    progress?.Report(new(kind, attempt, maxAttempts, Error: ex));
                    throw;
                }

                var delay = GetDelayBeforeAttempt(attempt + 1);
                var nextAt = DateTimeOffset.Now.Add(delay);

                logger.LogWarning(ex,
                    "Попытка {Attempt}/{Max} синхронизации {Media} → {To} не удалась: {Message}. Следующая через {Delay}",
                    attempt,
                    maxAttempts,
                    media.Title,
                    relation.To.TitleFull,
                    ex.Message,
                    delay);

                progress?.Report(new(SyncAttemptKind.FailedRetrying,
                    attempt,
                    maxAttempts,
                    delay,
                    nextAt,
                    ex));

                logger.LogDebug("Попытка {Attempt}/{Max}: {Media} → {To} — ожидание {Delay} перед следующей попыткой",
                    attempt,
                    maxAttempts,
                    media.Title,
                    relation.To.TitleFull,
                    delay);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new UnreachableException();
    }
}
