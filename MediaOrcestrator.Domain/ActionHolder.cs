using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MediaOrcestrator.Domain;

public class ActionHolder(ILogger<ActionHolder> logger)
{
    private const int CompletedRetention = 50;

    private readonly ConcurrentDictionary<Guid, RunningAction> _actions = new();
    private readonly List<RunningAction> _completed = [];
    private readonly object _completedLock = new();
    private readonly AsyncLocal<RunningAction?> _ambientParent = new();
    private long _seq;

    public event EventHandler? Changed;

    public int ActiveCount => _actions.Count;

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

    public IReadOnlyList<RunningAction> CompletedSnapshot()
    {
        lock (_completedLock)
        {
            return _completed
                .OrderByDescending(a => a.FinishedAt ?? DateTime.MinValue)
                .ToArray();
        }
    }

    public IDisposable BeginScope(RunningAction parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var previous = _ambientParent.Value;
        _ambientParent.Value = parent;
        return new ScopeReset(this, previous);
    }

    public RunningAction Register(string name, string status, int progressMax, CancellationTokenSource ctx, string? subtitle = null, ActionKind kind = ActionKind.Other)
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
            Kind = kind,
            State = ActionState.Running,
            StartedAt = DateTime.Now,
        };

        _actions.TryAdd(id, act);
        logger.LogInformation("Action registered: {Id} {Name} kind={Kind} parent={ParentId}", id, name, kind, act.ParentId);
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
        if (!_actions.TryGetValue(id, out var act))
        {
            return;
        }

        logger.LogWarning("Action cancelled: {Id} {Name}", act.Id, act.Name);

        try
        {
            act.CancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Токен уже освобождён другим терминальным переходом
        }

        Complete(act, ActionState.Cancelled, act.Status, null);

        foreach (var child in _actions.Values.Where(a => a.ParentId == id).ToArray())
        {
            child.Cancel();
        }
    }

    public void ClearCompleted()
    {
        bool changed;
        lock (_completedLock)
        {
            changed = _completed.Count > 0;
            _completed.Clear();
        }

        if (changed)
        {
            OnChanged();
        }
    }

    public void Remove(Guid id)
    {
        bool changed;
        lock (_completedLock)
        {
            changed = _completed.RemoveAll(a => a.Id == id) > 0;
        }

        if (changed)
        {
            OnChanged();
        }
    }

    public void Dismiss(RunningAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Remove(action.Id);
    }

    private void Complete(RunningAction act, ActionState state, string status, string? error)
    {
        if (!_actions.TryRemove(act.Id, out _))
        {
            return;
        }

        act.MarkFinished(state, status, error);

        logger.LogInformation("Action finished: {Id} {Name} state={State} status={Status}",
            act.Id, act.Name, state, act.Status);

        try
        {
            act.CancellationTokenSource.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }

        RunningAction? evicted = null;
        lock (_completedLock)
        {
            _completed.Add(act);
            if (_completed.Count > CompletedRetention)
            {
                evicted = _completed
                    .OrderBy(a => a.FinishedAt ?? DateTime.MinValue)
                    .First();

                _completed.Remove(evicted);
            }
        }

        if (evicted != null)
        {
            logger.LogInformation("Action evicted from history: {Id} {Name}", evicted.Id, evicted.Name);
        }

        OnChanged();
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public class RunningAction
    {
        private string _status = string.Empty;
        private string? _error;
        private int _progressValue;
        private int _progressMax;
        private int _state;
        private long _finishedAtTicks;
        private int _terminal;

        public event EventHandler? Changed;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Subtitle { get; set; } = string.Empty;
        public Guid? ParentId { get; internal set; }
        public int Depth { get; internal set; }
        public ActionKind Kind { get; internal set; } = ActionKind.Other;
        public DateTime StartedAt { get; internal set; }

        public DateTime? FinishedAt
        {
            get
            {
                var ticks = Interlocked.Read(ref _finishedAtTicks);
                return ticks == 0 ? null : new DateTime(ticks);
            }
        }

        public ActionState State
        {
            get => (ActionState)Volatile.Read(ref _state);
            internal set => Volatile.Write(ref _state, (int)value);
        }

        public string? Error
        {
            get => Volatile.Read(ref _error);
            private set => Volatile.Write(ref _error, value);
        }

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

        public TimeSpan Duration
        {
            get
            {
                var finished = FinishedAt ?? DateTime.Now;
                var elapsed = finished - StartedAt;
                return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
            }
        }

        public void Cancel()
        {
            if (Interlocked.CompareExchange(ref _terminal, 1, 0) != 0)
            {
                return;
            }

            Volatile.Write(ref _status, "Отменено");
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

            Holder.Complete(this, ActionState.Succeeded, finalStatus ?? "Выполнено", null);
        }

        public void Fail(string message, Exception? ex = null)
        {
            if (Interlocked.CompareExchange(ref _terminal, 1, 0) != 0)
            {
                return;
            }

            Holder.Complete(this, ActionState.Failed, message, ex?.ToString() ?? message);
        }

        public void MarkCancelled(string? finalStatus = null)
        {
            if (Interlocked.CompareExchange(ref _terminal, 1, 0) != 0)
            {
                return;
            }

            Holder.Complete(this, ActionState.Cancelled, finalStatus ?? "Отменено", null);
        }

        internal void IncrementProgress()
        {
            Interlocked.Increment(ref _progressValue);
            OnChanged();
        }

        internal void MarkFinished(ActionState state, string status, string? error)
        {
            State = state;
            Error = error;
            Interlocked.Exchange(ref _finishedAtTicks, DateTime.Now.Ticks);
            Status = status;
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
