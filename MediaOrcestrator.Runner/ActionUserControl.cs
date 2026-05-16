using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public sealed partial class ActionUserControl : UserControl
{
    private const int BaseHeight = 42;
    private const int HeightWithSubtitle = 58;
    private static readonly Color CanceledBackColor = Color.Gainsboro;
    private static readonly Color CanceledForeColor = Color.DimGray;
    private static readonly Color StatusErrorColor = Color.IndianRed;

    private static readonly Color SyncAccent = Color.FromArgb(0x2F, 0x6F, 0xB0);
    private static readonly Color DownloadAccent = Color.FromArgb(0x2E, 0x8B, 0x57);
    private static readonly Color UploadAccent = Color.FromArgb(0xC8, 0x86, 0x16);
    private static readonly Color TransferAccent = Color.FromArgb(0x6A, 0x5A, 0xCD);
    private static readonly Color OtherAccent = Color.FromArgb(0x9E, 0x9E, 0x9E);

    private readonly Color _defaultBackColor;
    private readonly Color _defaultNameForeColor;
    private readonly Color _defaultStatusForeColor;
    private readonly Color _defaultSubtitleForeColor;

    private ActionHolder.RunningAction? _action;
    private bool _isCanceled;

    public ActionUserControl()
    {
        InitializeComponent();

        _defaultBackColor = BackColor;
        _defaultNameForeColor = uiNameLabel.ForeColor;
        _defaultStatusForeColor = uiStatusLabel.ForeColor;
        _defaultSubtitleForeColor = uiSubtitleLabel.ForeColor;

        Disposed += OnDisposed;
    }

    private enum ActionKind
    {
        Sync,
        Download,
        Upload,
        Transfer,
        Other,
    }

    public void SetAction(ActionHolder.RunningAction action)
    {
        if (_action != null)
        {
            _action.Changed -= OnActionChanged;
        }

        _action = action;
        _action.Changed += OnActionChanged;
        UpdateStatus();
    }

    private void OnActionChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(UpdateStatus);
            return;
        }

        UpdateStatus();
    }

    private void OnDisposed(object? sender, EventArgs e)
    {
        if (_action == null)
        {
            return;
        }

        _action.Changed -= OnActionChanged;
        _action = null;
    }

    private void uiCancelButton_Click(object sender, EventArgs e)
    {
        if (_action == null)
        {
            return;
        }

        _isCanceled = true;
        uiCancelButton.Enabled = false;
        UpdateStatus();
        _action.Cancel();
    }

    private static ActionKind ClassifyKind(string name)
    {
        if (name.StartsWith("Синхронизация", StringComparison.Ordinal))
        {
            return ActionKind.Sync;
        }

        if (name.StartsWith("Загрузка", StringComparison.Ordinal))
        {
            return ActionKind.Download;
        }

        if (name.StartsWith("Заливка", StringComparison.Ordinal))
        {
            return ActionKind.Upload;
        }

        if (name.StartsWith("Передача", StringComparison.Ordinal))
        {
            return ActionKind.Transfer;
        }

        return ActionKind.Other;
    }

    private static Color AccentFor(ActionKind kind)
    {
        return kind switch
        {
            ActionKind.Sync => SyncAccent,
            ActionKind.Download => DownloadAccent,
            ActionKind.Upload => UploadAccent,
            ActionKind.Transfer => TransferAccent,
            _ => OtherAccent,
        };
    }

    private void UpdateStatus()
    {
        if (_action == null)
        {
            return;
        }

        var status = _action.Status;
        uiNameLabel.Text = _action.Name;
        uiStatusLabel.Text = status;

        var subtitle = _action.Subtitle;
        var hasSubtitle = subtitle.Length > 0;
        uiSubtitleLabel.Text = subtitle;
        uiSubtitleLabel.Visible = hasSubtitle;

        var targetHeight = LogicalToDeviceUnits(hasSubtitle ? HeightWithSubtitle : BaseHeight);
        if (Height != targetHeight)
        {
            Height = targetHeight;
        }

        var isCanceled = _isCanceled || status.StartsWith("Отмен", StringComparison.Ordinal);
        if (isCanceled)
        {
            BackColor = CanceledBackColor;
            uiAccentStrip.BackColor = CanceledForeColor;
            uiNameLabel.ForeColor = CanceledForeColor;
            uiStatusLabel.ForeColor = CanceledForeColor;
            uiSubtitleLabel.ForeColor = CanceledForeColor;
            HideProgress();
            uiCancelButton.Visible = false;
            return;
        }

        BackColor = _defaultBackColor;
        uiNameLabel.ForeColor = _defaultNameForeColor;
        uiSubtitleLabel.ForeColor = _defaultSubtitleForeColor;
        uiCancelButton.Visible = true;

        if (status.StartsWith("Ошибк", StringComparison.Ordinal))
        {
            uiAccentStrip.BackColor = StatusErrorColor;
            uiStatusLabel.ForeColor = StatusErrorColor;
            HideProgress();
            uiCancelButton.Visible = false;
            return;
        }

        uiAccentStrip.BackColor = AccentFor(ClassifyKind(_action.Name));
        uiStatusLabel.ForeColor = _defaultStatusForeColor;

        var progressMax = _action.ProgressMax;
        if (progressMax <= 0)
        {
            ShowMarquee();

            var count = _action.ProgressValue;
            if (count > 0)
            {
                uiProgressLabel.Visible = true;
                uiProgressLabel.Text = count.ToString();
            }
            else
            {
                uiProgressLabel.Visible = false;
            }

            return;
        }

        var progressValue = Math.Clamp(_action.ProgressValue, 0, progressMax);
        ShowBlocks(progressMax, progressValue);
        uiProgressLabel.Visible = true;
        uiProgressLabel.Text = progressMax == 100
            ? $"{progressValue} %"
            : $"{progressValue} / {progressMax}";
    }

    private void HideProgress()
    {
        if (uiProgressBar.Style != ProgressBarStyle.Blocks)
        {
            uiProgressBar.Style = ProgressBarStyle.Blocks;
        }

        uiProgressBar.Visible = false;
        uiProgressLabel.Visible = false;
    }

    private void ShowMarquee()
    {
        if (uiProgressBar.Style != ProgressBarStyle.Marquee)
        {
            uiProgressBar.Style = ProgressBarStyle.Marquee;
        }

        uiProgressBar.Visible = true;
    }

    private void ShowBlocks(int max, int value)
    {
        if (uiProgressBar.Style != ProgressBarStyle.Blocks)
        {
            uiProgressBar.Style = ProgressBarStyle.Blocks;
        }

        if (uiProgressBar.Maximum != max)
        {
            uiProgressBar.Maximum = max;
        }

        if (uiProgressBar.Value != value)
        {
            uiProgressBar.Value = value;
        }

        uiProgressBar.Visible = true;
    }
}
