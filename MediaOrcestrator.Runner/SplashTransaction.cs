using Serilog;
using System.Diagnostics;

namespace MediaOrcestrator.Runner;

public interface ISplashSpanFactory
{
    ISplashSpan StartSpan(string name);
}

public interface ISplashSpan : IDisposable, ISplashSpanFactory
{
    string Name { get; }
}

public static class Splash
{
    private static readonly AsyncLocal<ISplashSpanFactory?> _current = new();

    public static ISplashSpanFactory Current => _current.Value ?? NullSplashSpan.Instance;

    internal static IDisposable Push(ISplashSpanFactory factory)
    {
        var previous = _current.Value;
        _current.Value = factory;
        return new Scope(previous);
    }

    private sealed class Scope(ISplashSpanFactory? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _current.Value = previous;
        }
    }
}

public sealed class SplashTransaction : IDisposable, ISplashSpanFactory
{
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new();
    private readonly object _lock = new();
    private readonly List<SplashSpan> _active = [];
    private readonly int _expectedSpans;
    private readonly string _name;
    private readonly IDisposable _ambientScope;
    private readonly ILogger? _logger;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private SplashForm? _form;
    private int _completedRootSpans;
    private bool _closed;

    public SplashTransaction(string version, string name, int expectedSpans, ILogger? logger = null)
    {
        _name = name;
        _expectedSpans = Math.Max(1, expectedSpans);
        _logger = logger;

        _uiThread = new(() =>
        {
            _form = new();
            _form.SetVersion(version);
            _form.SetMaxSteps(_expectedSpans);
            _form.SetStatus(name);
            _form.HandleCreated += (_, _) => _ready.Set();
            _form.FormClosed += (_, _) => _ready.Set();
            Application.Run(_form);
        })
        {
            Name = "SplashUI",
            IsBackground = true,
        };

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _ready.Wait(TimeSpan.FromSeconds(5));

        _ambientScope = Splash.Push(this);
    }

    public ISplashSpan StartSpan(string name)
    {
        return StartSpanInternal(null, name);
    }

    public void Dispose()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _ambientScope.Dispose();
        _stopwatch.Stop();
        _logger?.Information("Splash транзакция: {Name} — {Elapsed:0} мс", _name, _stopwatch.Elapsed.TotalMilliseconds);

        var form = _form;

        if (form is { IsDisposed: false })
        {
            try
            {
                form.BeginInvoke(form.Close);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        _uiThread.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }

    internal SplashSpan StartSpanInternal(SplashSpan? parent, string name)
    {
        var span = new SplashSpan(this, parent, name, Stopwatch.StartNew());

        lock (_lock)
        {
            _active.Add(span);
        }

        UpdateUi();
        return span;
    }

    internal void EndSpan(SplashSpan span)
    {
        lock (_lock)
        {
            if (!_active.Remove(span))
            {
                return;
            }

            if (span.Parent == null && _completedRootSpans < _expectedSpans)
            {
                _completedRootSpans++;
            }
        }

        if (_logger != null)
        {
            var elapsedMs = span.Elapsed.TotalMilliseconds;
            var path = BuildPath(span);

            if (elapsedMs >= 2000)
            {
                _logger.Warning("Splash span: {Path} — {Elapsed:0} мс (медленно)", path, elapsedMs);
            }
            else
            {
                _logger.Debug("Splash span: {Path} — {Elapsed:0} мс", path, elapsedMs);
            }
        }

        UpdateUi();
    }

    private static string BuildPath(SplashSpan span)
    {
        var parts = new Stack<string>();
        var current = span;

        while (current != null)
        {
            parts.Push(current.Name);
            current = current.Parent;
        }

        return string.Join(" › ", parts);
    }

    private void UpdateUi()
    {
        string status;
        int completed;

        lock (_lock)
        {
            status = _active.Count > 0
                ? string.Join(" › ", _active.Select(static s => s.Name))
                : _name;

            completed = _completedRootSpans;
        }

        Marshal(form =>
        {
            form.SetStatus(status);
            form.SetProgress(completed);
        });
    }

    private void Marshal(Action<SplashForm> action)
    {
        var form = _form;

        if (form == null || form.IsDisposed)
        {
            return;
        }

        try
        {
            form.BeginInvoke(action, form);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}

public sealed class SplashSpan : ISplashSpan
{
    private readonly SplashTransaction _transaction;
    private readonly Stopwatch _stopwatch;
    private readonly IDisposable _ambientScope;
    private bool _disposed;

    internal SplashSpan(SplashTransaction transaction, SplashSpan? parent, string name, Stopwatch stopwatch)
    {
        _transaction = transaction;
        _stopwatch = stopwatch;
        Parent = parent;
        Name = name;
        _ambientScope = Splash.Push(this);
    }

    public string Name { get; }
    public SplashSpan? Parent { get; }
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public ISplashSpan StartSpan(string name)
    {
        return _transaction.StartSpanInternal(this, name);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopwatch.Stop();
        _ambientScope.Dispose();
        _transaction.EndSpan(this);
    }
}

public sealed class NullSplashSpan : ISplashSpan, ISplashSpanFactory
{
    public static readonly NullSplashSpan Instance = new();

    private NullSplashSpan()
    {
    }

    public string Name => string.Empty;

    public ISplashSpan StartSpan(string name)
    {
        return this;
    }

    public void Dispose()
    {
    }
}
