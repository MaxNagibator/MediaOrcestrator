namespace MediaOrcestrator.Domain;

public sealed class ParallelSyncExecutor
{
    private readonly List<SyncIntent> _rootIntents;
    private readonly Dictionary<(string FromId, string ToId), RelationChannel> _channels;
    private int _pending;

    public ParallelSyncExecutor(IEnumerable<SyncIntent> rootIntents, IEnumerable<SourceSyncRelation> relations)
    {
        _rootIntents = rootIntents.ToList();
        _channels = new();

        foreach (var relation in relations)
        {
            var key = (relation.FromId, relation.ToId);
            if (!_channels.ContainsKey(key))
            {
                _channels[key] = new(relation);
            }
        }
    }

    public Task ExecuteAsync(
        Func<SyncIntent, Task<SyncRunOutcome>> executeFunc,
        CancellationToken ct,
        Action<SyncIntent, Exception>? onError = null)
    {
        foreach (var intent in _rootIntents)
        {
            EnqueueIntent(intent);
        }

        if (_pending == 0)
        {
            return Task.CompletedTask;
        }

        var consumerTasks = _channels.Values.Select(channel =>
                channel.RunAsync(async intent =>
                {
                    try
                    {
                        var outcome = await executeFunc(intent);
                        if (outcome == SyncRunOutcome.Transferred)
                        {
                            EnqueueNextIntents(intent);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke(intent, ex);
                    }
                    finally
                    {
                        DecrementPending();
                    }
                }, ct))
            .ToList();

        return Task.WhenAll(consumerTasks);
    }

    private void EnqueueIntent(SyncIntent intent)
    {
        var key = (intent.Relation.FromId, intent.Relation.ToId);
        if (!_channels.TryGetValue(key, out var channel))
        {
            return;
        }

        Interlocked.Increment(ref _pending);
        channel.Enqueue(intent);
    }

    private void EnqueueNextIntents(SyncIntent intent)
    {
        foreach (var next in intent.NextIntents.Where(next => next.IsSelected))
        {
            EnqueueIntent(next);
        }
    }

    private void DecrementPending()
    {
        if (Interlocked.Decrement(ref _pending) != 0)
        {
            return;
        }

        foreach (var channel in _channels.Values)
        {
            channel.Complete();
        }
    }
}
