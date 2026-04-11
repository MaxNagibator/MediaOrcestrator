namespace MediaOrcestrator.Runner;

public partial class UpdateProgressForm : Form
{
    private CancellationTokenSource? _cts;

    public UpdateProgressForm()
    {
        InitializeComponent();
    }

    public CancellationToken CancellationToken => (_cts ??= new()).Token;

    public void UpdateProgress(double progress)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateProgress(progress));
            return;
        }

        uiProgressBar.Value = (int)(progress * 100);
        uiStatusLabel.Text = $"Скачивание обновления... {progress:P0}";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _cts?.Dispose();
        _cts = null;
        base.OnFormClosed(e);
    }

    private void uiCancelButton_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        Close();
    }
}
