using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MediaOrcestrator.Domain;

public class ActionHolder(ILogger<ActionHolder> logger)
{
    private readonly ConcurrentDictionary<Guid, RunningAction> _actions = new();
    private readonly AsyncLocal<RunningAction?> _ambientParent = new();
    private long _seq;

    public event EventHandler? Changed;

    public IReadOnlyList<RunningAction> Snapshot()
    {
        var all = _actions.Values.ToArray();
        var byParent = all
            .Where(a => a.ParentId.HasValue)
            .ToLookup(a => a.ParentId!.Value);

        var present = all.Select(a => a.Id).ToHashSet();
        var result = new List<RunningAction>(all.Length);

        var roots = all
            .Where(a => a.ParentId == null || !present.Contains(a.ParentId.Value))
            .OrderBy(a => a.Order);

        foreach (var root in roots)
        {
            AddSubtree(root);
        }

        return result;

        void AddSubtree(RunningAction node)
        {
            result.Add(node);
            foreach (var child in byParent[node.Id].OrderBy(c => c.Order))
            {
                AddSubtree(child);
            }
        }
    }

    public IDisposable BeginScope(RunningAction parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var previous = _ambientParent.Value;
        _ambientParent.Value = parent;
        return new ScopeReset(this, previous);
    }

    public RunningAction Register(string name, string status, int progressMax, CancellationTokenSource ctx, string? subtitle = null)
    {
        var id = Guid.NewGuid();
        var parent = _ambientParent.Value;
        var act = new RunningAction
        {
            Id = id,
            Name = name,
            Subtitle = subtitle ?? string.Empty,
            Status = status,
            ProgressMax = progressMax,
            CancellationTokenSource = ctx,
            Holder = this,
            ParentId = parent?.Id,
            Depth = parent == null ? 0 : parent.Depth + 1,
            Order = Interlocked.Increment(ref _seq),
        };

        _actions.TryAdd(id, act);
        logger.LogInformation("Action registered: {Id} {Name} parent={ParentId}", id, name, act.ParentId);
        OnChanged();
        return act;
    }

    public void SetStatus(Guid id, string value)
    {
        if (_actions.TryGetValue(id, out var act))
        {
            act.Status = value;
        }
    }

    public void ProgressPlus(Guid id)
    {
        if (_actions.TryGetValue(id, out var act))
        {
            act.IncrementProgress();
        }
    }

    public void SetProgress(Guid id, int value)
    {
        if (_actions.TryGetValue(id, out var act))
        {
            act.SetProgress(value);
        }
    }

    public void Cancel(Guid id)
    {
        if (!_actions.TryRemove(id, out var act))
        {
            return;
        }

        logger.LogWarning("Action cancelled: {Id} {Name}", act.Id, act.Name);

        try
        {
            act.CancellationTokenSource.Cancel();
        }
        finally
        {
            act.CancellationTokenSource.Dispose();
        }

        foreach (var child in _actions.Values.Where(a => a.ParentId == id).ToArray())
        {
            child.Cancel();
        }

        OnChanged();
    }

    internal void Remove(Guid id)
    {
        if (!_actions.TryRemove(id, out var act))
        {
            return;
        }

        logger.LogInformation("Action finished: {Id} {Name} status={Status}", act.Id, act.Name, act.Status);
        act.CancellationTokenSource.Dispose();
        OnChanged();
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public class RunningAction
    {
        private string _status = string.Empty;
        private int _progressValue;
        private int _progressMax;
        private int _terminal;

        public event EventHandler? Changed;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Subtitle { get; set; } = string.Empty;
        public Guid? ParentId { get; internal set; }
        public int Depth { get; internal set; }

        public string Status
        {
            get => Volatile.Read(ref _status);
            set
            {
                Volatile.Write(ref _status, value);
                OnChanged();
            }
        }

        public int ProgressValue
        {
            get => Volatile.Read(ref _progressValue);
            private set => Volatile.Write(ref _progressValue, value);
        }

        public int ProgressMax
        {
            get => Volatile.Read(ref _progressMax);
            set
            {
                Volatile.Write(ref _progressMax, value);
                OnChanged();
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ActionHolder Holder { get; internal set; }

        public void Cancel()
        {
            if (Interlocked.CompareExchange(ref _terminal, 1, 0) != 0)
            {
                return;
            }

            Status = "Отменено";
            Holder.Cancel(Id);
        }

        public void ProgressPlus()
        {
            IncrementProgress();
        }

        public void SetProgress(int value)
        {
            ProgressValue = value;
            OnChanged();
        }

        public void Finish(string? finalStatus = null)
        {
            if (Interlocked.CompareExchange(ref _terminal, 1, 0) != 0)
            {
                return;
            }

            Status = finalStatus ?? "Выполнено";
            Holder.Remove(Id);
        }

        internal void IncrementProgress()
        {
            Interlocked.Increment(ref _progressValue);
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal long Order { get; set; }
    }

    private sealed class ScopeReset(ActionHolder holder, RunningAction? previous) : IDisposable
    {
        public void Dispose()
        {
            holder._ambientParent.Value = previous;
        }
    }
}
