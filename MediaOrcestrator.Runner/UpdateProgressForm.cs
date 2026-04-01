namespace MediaOrcestrator.Runner;

public sealed class UpdateProgressForm : Form
{
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Button _cancelButton;
    private CancellationTokenSource? _cts;

    public CancellationToken CancellationToken => (_cts ??= new()).Token;

    public UpdateProgressForm()
    {
        Text = "Обновление приложения";
        Size = new(420, 150);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        _statusLabel = new()
        {
            Text = "Скачивание обновления...",
            Location = new(12, 15),
            Size = new(380, 20),
        };

        _progressBar = new()
        {
            Location = new(12, 40),
            Size = new(380, 25),
            Minimum = 0,
            Maximum = 100,
        };

        _cancelButton = new()
        {
            Text = "Отмена",
            Location = new(317, 75),
            Size = new(75, 25),
        };

        _cancelButton.Click += (_, _) =>
        {
            _cts?.Cancel();
            Close();
        };

        Controls.AddRange([_statusLabel, _progressBar, _cancelButton]);
    }

    public void UpdateProgress(double progress)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateProgress(progress));
            return;
        }

        _progressBar.Value = (int)(progress * 100);
        _statusLabel.Text = $"Скачивание обновления... {progress:P0}";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        base.OnFormClosing(e);
    }
}
