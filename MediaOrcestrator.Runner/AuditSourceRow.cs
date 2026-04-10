using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public enum AuditSyncMode
{
    None = 0,
    Full = 1,
    Quick = 2,
    New = 3,
}

public sealed class AuditSyncRequestedEventArgs(Source source, AuditSyncMode mode) : EventArgs
{
    public Source Source { get; } = source;
    public AuditSyncMode Mode { get; } = mode;
}

public partial class AuditSourceRow : UserControl
{
    public AuditSourceRow()
    {
        InitializeComponent();
        uiRowToolTip.SetToolTip(uiFullSyncButton, "Полное перечитывание всех медиа источника");
        uiRowToolTip.SetToolTip(uiQuickSyncButton, "Загружает список медиа без метаданных");
        uiRowToolTip.SetToolTip(uiNewSyncButton, "Синхронизирует до первого уже известного медиа");
    }

    public event EventHandler<AuditSyncRequestedEventArgs>? SyncRequested;

    public Source? Source { get; private set; }

    public void SetSource(Source source)
    {
        Source = source;
        uiTitleLabel.Text = string.IsNullOrWhiteSpace(source.Title) ? source.TitleFull : source.Title;
        uiTypeLabel.Text = source.TypeId;
        uiProgressLabel.Text = "—";
        SetLastSync(source.LastSyncedAt);
    }

    public void SetLastSync(DateTime? lastSyncedAt)
    {
        if (lastSyncedAt == null)
        {
            uiLastSyncLabel.Text = "не синхронизировано";
            return;
        }

        var local = lastSyncedAt.Value.ToLocalTime();
        uiLastSyncLabel.Text = $"синхр.: {local:dd.MM.yyyy HH:mm}";
    }

    public void SetBusy(bool busy)
    {
        uiFullSyncButton.Enabled = !busy;
        uiQuickSyncButton.Enabled = !busy;
        uiNewSyncButton.Enabled = !busy;
    }

    public void ReportProgress(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(ReportProgress), message);
            return;
        }

        uiProgressLabel.Text = message;
    }

    public void ResetProgress()
    {
        uiProgressLabel.Text = "—";
    }

    private void uiFullSyncButton_Click(object sender, EventArgs e)
    {
        Raise(AuditSyncMode.Full);
    }

    private void uiQuickSyncButton_Click(object sender, EventArgs e)
    {
        Raise(AuditSyncMode.Quick);
    }

    private void uiNewSyncButton_Click(object sender, EventArgs e)
    {
        Raise(AuditSyncMode.New);
    }

    private void Raise(AuditSyncMode mode)
    {
        if (Source == null)
        {
            return;
        }

        SyncRequested?.Invoke(this, new(Source, mode));
    }
}
