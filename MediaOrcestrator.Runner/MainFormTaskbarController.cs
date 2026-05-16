using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace MediaOrcestrator.Runner;

public sealed class MainFormTaskbarController(
    WindowsTaskbarProgress taskbar,
    ActionHolder actionHolder,
    ILogger<MainFormTaskbarController> logger)
{
    private SynchronizationContext? _uiContext;
    private ISynchronizeInvoke? _uiInvoke;

    private IntPtr _hwnd;
    private bool _subscribed;
    private bool _disposed;
    private bool _marshalWarned;

    public void Attach(IntPtr hwnd, ISynchronizeInvoke uiInvoke)
    {
        if (_disposed)
        {
            return;
        }

        _uiContext = SynchronizationContext.Current;
        _uiInvoke = uiInvoke;

        _hwnd = hwnd;
        taskbar.Attach(hwnd);

        if (!_subscribed)
        {
            actionHolder.Changed += OnActionsChanged;
            _subscribed = true;
        }

        Recalculate();
    }

    public void OnTaskbarButtonCreated()
    {
        if (_disposed || _hwnd == IntPtr.Zero)
        {
            return;
        }

        taskbar.Attach(_hwnd);
        Recalculate();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_subscribed)
        {
            actionHolder.Changed -= OnActionsChanged;
            _subscribed = false;
        }

        taskbar.Dispose();
    }

    private void OnActionsChanged(object? sender, EventArgs e)
    {
        if (_disposed || taskbar.IsUnavailable)
        {
            return;
        }

        if (_uiContext != null && _uiContext == SynchronizationContext.Current)
        {
            Recalculate();
            return;
        }

        if (_uiContext != null)
        {
            _uiContext.Post(_ => Recalculate(), null);
            return;
        }

        if (_uiInvoke is { InvokeRequired: true })
        {
            try
            {
                _uiInvoke.BeginInvoke(new Action(Recalculate), null);
            }
            catch (ObjectDisposedException)
            {
                // Форма уже закрывается
            }
            catch (InvalidOperationException)
            {
                // Хэндл ещё/уже не создан
            }

            return;
        }

        if (_uiInvoke is { InvokeRequired: false })
        {
            Recalculate();
            return;
        }

        if (_marshalWarned)
        {
            return;
        }

        _marshalWarned = true;
        logger.LogWarning("Нет UI-контекста для обновления панели задач, индикатор отключён");
    }

    private void Recalculate()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var active = actionHolder.Snapshot();

            if (active.Count > 0)
            {
                var determinate = active.Where(a => a.ProgressMax > 0).ToArray();

                if (determinate.Length == 0)
                {
                    taskbar.Apply(TaskbarProgressState.Indeterminate, 0, active.Count);
                    return;
                }

                var avg = determinate.Average(a =>
                    Math.Clamp(a.ProgressValue, 0, a.ProgressMax) / (double)a.ProgressMax * 100.0);

                var percent = (int)Math.Round(avg, MidpointRounding.AwayFromZero);
                taskbar.Apply(TaskbarProgressState.Normal, percent, active.Count);
                return;
            }

            var hasUnclearedFailures = actionHolder
                .CompletedSnapshot()
                .Any(a => a.State == ActionState.Failed);

            taskbar.Apply(hasUnclearedFailures ? TaskbarProgressState.Error : TaskbarProgressState.NoProgress,
                0,
                0);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось пересчитать состояние панели задач");
        }
    }
}
