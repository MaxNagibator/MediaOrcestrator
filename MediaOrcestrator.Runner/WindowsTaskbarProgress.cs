using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MediaOrcestrator.Runner;

public enum TaskbarProgressState
{
    NoProgress = 0,
    Indeterminate = 1,
    Normal = 2,
    Error = 4,
    Paused = 8,
}

public sealed class WindowsTaskbarProgress(ILogger<WindowsTaskbarProgress> logger) : IDisposable
{
    private readonly object _lock = new();

    private ITaskbarList3? _taskbar;
    private IntPtr _hwnd;
    private bool _failed;
    private bool _disposed;

    private TaskbarProgressState _lastState = TaskbarProgressState.NoProgress;
    private int _lastPercent = -1;
    private int _lastBadgeCount = -1;

    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        // ITaskbarList
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        // ITaskbarList3
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TaskbarProgressState tbpFlags);
        void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        void UnregisterTab(IntPtr hwndTab);
        void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
        void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
        void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string? pszDescription);
        void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string? pszTip);
        void SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);
    }

    public void Attach(IntPtr hwnd)
    {
        if (_disposed || _failed)
        {
            return;
        }

        lock (_lock)
        {
            _hwnd = hwnd;

            try
            {
                _taskbar = (ITaskbarList3)new TaskbarInstance();
                _taskbar.HrInit();
            }
            catch (Exception ex)
            {
                MarkFailed(ex);
                return;
            }

            _lastState = TaskbarProgressState.NoProgress;
            _lastPercent = -1;
            _lastBadgeCount = -1;
        }
    }

    public void Apply(TaskbarProgressState state, int percent, int activeCount)
    {
        if (_disposed || _failed)
        {
            return;
        }

        lock (_lock)
        {
            if (_taskbar == null || _hwnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                if (state != _lastState)
                {
                    _taskbar.SetProgressState(_hwnd, state);
                    _lastState = state;

                    if (state != TaskbarProgressState.Normal)
                    {
                        _lastPercent = -1;
                    }
                }

                if (state == TaskbarProgressState.Normal)
                {
                    var clamped = Math.Clamp(percent, 0, 100);
                    if (clamped != _lastPercent)
                    {
                        _taskbar.SetProgressValue(_hwnd, (ulong)clamped, 100UL);
                        _lastPercent = clamped;
                    }
                }

                ApplyBadge(activeCount);
            }
            catch (Exception ex)
            {
                MarkFailed(ex);
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                if (_taskbar != null && _hwnd != IntPtr.Zero)
                {
                    _taskbar.SetProgressState(_hwnd, TaskbarProgressState.NoProgress);
                    _taskbar.SetOverlayIcon(_hwnd, IntPtr.Zero, null);
                }
            }
            catch
            {
                // Окно уже разрушено или explorer недоступен
            }

            if (_taskbar != null)
            {
                try
                {
                    Marshal.FinalReleaseComObject(_taskbar);
                }
                catch
                {
                    // COM-объект мог быть уже освобождён.
                }

                _taskbar = null;
            }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static IntPtr CreateBadgeIconHandle(int count)
    {
        const int Size = 32;
        var text = count >= 10 ? "9+" : count.ToString();

        using var bitmap = new Bitmap(Size, Size);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            graphics.Clear(Color.Transparent);

            using (var background = new SolidBrush(Color.FromArgb(0xD3, 0x2F, 0x2F)))
            {
                graphics.FillEllipse(background, 1, 1, Size - 2, Size - 2);
            }

            using (var border = new Pen(Color.White, 2f))
            {
                graphics.DrawEllipse(border, 1, 1, Size - 3, Size - 3);
            }

            var fontSize = text.Length > 1 ? 14f : 18f;
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            graphics.DrawString(text, font, textBrush, new RectangleF(0, 0, Size, Size), format);
        }

        return bitmap.GetHicon();
    }

    private void ApplyBadge(int activeCount)
    {
        if (activeCount == _lastBadgeCount)
        {
            return;
        }

        if (activeCount <= 0)
        {
            _taskbar!.SetOverlayIcon(_hwnd, IntPtr.Zero, null);
            _lastBadgeCount = activeCount;
            return;
        }

        var iconHandle = IntPtr.Zero;
        Icon? icon = null;
        try
        {
            iconHandle = CreateBadgeIconHandle(activeCount);
            if (iconHandle == IntPtr.Zero)
            {
                return;
            }

            icon = Icon.FromHandle(iconHandle);
            _taskbar!.SetOverlayIcon(_hwnd, icon.Handle, $"Активных задач: {activeCount}");
            _lastBadgeCount = activeCount;
        }
        finally
        {
            icon?.Dispose();
            if (iconHandle != IntPtr.Zero)
            {
                DestroyIcon(iconHandle);
            }
        }
    }

    private void MarkFailed(Exception ex)
    {
        if (_failed)
        {
            return;
        }

        _failed = true;
        _taskbar = null;
        logger.LogWarning(ex, "Интеграция с панелью задач Windows недоступна, индикатор прогресса отключён");
    }

    [ComImport]
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    private class TaskbarInstance;
}
