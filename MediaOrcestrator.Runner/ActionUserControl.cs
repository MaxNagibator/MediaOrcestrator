using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public sealed partial class ActionUserControl : UserControl
{
    private const int BaseHeight = 42;
    private const int HeightWithSubtitle = 58;
    private static readonly Color CanceledBackColor = Color.Gainsboro;
    private static readonly Color CanceledForeColor = Color.DimGray;
    private static readonly Color StatusErrorColor = Color.IndianRed;
    private static readonly Color SucceededBackColor = Color.FromArgb(0xF1, 0xF8, 0xF1);
    private static readonly Color SucceededAccent = Color.FromArgb(0x2E, 0x8B, 0x57);
    private static readonly Color SucceededForeColor = Color.FromArgb(0x55, 0x6B, 0x55);
    private static readonly Color FailedBackColor = Color.FromArgb(0xFB, 0xEF, 0xEF);

    private static readonly Color SyncAccent = Color.FromArgb(0x2F, 0x6F, 0xB0);
    private static readonly Color DownloadAccent = Color.FromArgb(0x2E, 0x8B, 0x57);
    private static readonly Color UploadAccent = Color.FromArgb(0xC8, 0x86, 0x16);
    private static readonly Color TransferAccent = Color.FromArgb(0x6A, 0x5A, 0xCD);
    private static readonly Color CommentsAccent = Color.FromArgb(0x00, 0x96, 0x88);
    private static readonly Color ConvertAccent = Color.FromArgb(0x9C, 0x27, 0xB0);
    private static readonly Color DeleteAccent = Color.FromArgb(0xA0, 0x39, 0x2E);
    private static readonly Color MetadataAccent = Color.FromArgb(0x8D, 0x6E, 0x63);
    private static readonly Color PublishAccent = Color.FromArgb(0xD2, 0x69, 0x1E);
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

    private void uiDetailsButton_Click(object sender, EventArgs e)
    {
        if (_action == null)
        {
            return;
        }

        var error = _action.Error;
        if (string.IsNullOrEmpty(error))
        {
            return;
        }

        using var form = new TaskErrorForm(_action.Name, error);
        form.ShowDialog(this);
    }

    private static Color AccentFor(ActionKind kind)
    {
        return kind switch
        {
            ActionKind.Sync => SyncAccent,
            ActionKind.Download => DownloadAccent,
            ActionKind.Upload => UploadAccent,
            ActionKind.Transfer => TransferAccent,
            ActionKind.Comments => CommentsAccent,
            ActionKind.Convert => ConvertAccent,
            ActionKind.Delete => DeleteAccent,
            ActionKind.Metadata => MetadataAccent,
            ActionKind.Publish => PublishAccent,
            _ => OtherAccent,
        };
    }

    private void UpdateStatus()
    {
        if (_action == null)
        {
            return;
        }

        var state = _action.State;
        var status = _action.Status;
        uiNameLabel.Text = _action.Name;
        uiStatusLabel.Text = status;

        var subtitle = _action.Subtitle;
        var isCompleted = state is ActionState.Succeeded or ActionState.Failed or ActionState.Cancelled;
        if (isCompleted)
        {
            var duration = ActionFormatting.FormatDuration(_action.Duration);
            subtitle = subtitle.Length > 0 ? $"{subtitle} · {duration}" : duration;
        }

        var hasSubtitle = subtitle.Length > 0;
        uiSubtitleLabel.Text = subtitle;
        uiSubtitleLabel.Visible = hasSubtitle;

        var targetHeight = LogicalToDeviceUnits(hasSubtitle ? HeightWithSubtitle : BaseHeight);
        if (Height != targetHeight)
        {
            Height = targetHeight;
        }

        if (state == ActionState.Cancelled || _isCanceled)
        {
            BackColor = CanceledBackColor;
            uiAccentStrip.BackColor = CanceledForeColor;
            uiNameLabel.ForeColor = CanceledForeColor;
            uiStatusLabel.ForeColor = CanceledForeColor;
            uiSubtitleLabel.ForeColor = CanceledForeColor;
            HideProgress();
            uiCancelButton.Visible = false;
            uiDetailsButton.Visible = false;
            return;
        }

        switch (state)
        {
            case ActionState.Failed:
                BackColor = FailedBackColor;
                uiAccentStrip.BackColor = StatusErrorColor;
                uiNameLabel.ForeColor = _defaultNameForeColor;
                uiStatusLabel.ForeColor = StatusErrorColor;
                uiSubtitleLabel.ForeColor = _defaultSubtitleForeColor;
                HideProgress();
                uiCancelButton.Visible = false;
                uiDetailsButton.Visible = !string.IsNullOrEmpty(_action.Error);
                return;

            case ActionState.Succeeded:
                BackColor = SucceededBackColor;
                uiAccentStrip.BackColor = SucceededAccent;
                uiNameLabel.ForeColor = _defaultNameForeColor;
                uiStatusLabel.ForeColor = SucceededForeColor;
                uiSubtitleLabel.ForeColor = _defaultSubtitleForeColor;
                HideProgress();
                uiCancelButton.Visible = false;
                uiDetailsButton.Visible = false;
                return;
        }

        BackColor = _defaultBackColor;
        uiNameLabel.ForeColor = _defaultNameForeColor;
        uiSubtitleLabel.ForeColor = _defaultSubtitleForeColor;
        uiCancelButton.Visible = true;
        uiDetailsButton.Visible = false;

        uiAccentStrip.BackColor = AccentFor(_action.Kind);
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
