using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class UpdateForm : Form
{
    private readonly Func<IProgress<double>, CancellationToken, Task<string>>? _downloader;
    private readonly string _releaseNotesMarkdown = "";
    private CancellationTokenSource? _cts;

    public UpdateForm()
    {
        InitializeComponent();
    }

    public UpdateForm(AppUpdateInfo update, Func<IProgress<double>, CancellationToken, Task<string>> downloader) : this()
    {
        _downloader = downloader;
        _releaseNotesMarkdown = update.ReleaseNotes ?? "";

        uiVersionLabel.Text = $"Доступна новая версия {update.Version}";
        uiSizeLabel.Text = $"Размер: {FormatSize(update.Size)}";
    }

    public string? DownloadedZipPath { get; private set; }

    public Exception? DownloadError { get; private set; }

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

    private void UpdateForm_Load(object? sender, EventArgs e)
    {
        uiReleaseNotesBrowser.DocumentText = DocumentationForm.RenderMarkdown(StripWhatsChangedHeading(_releaseNotesMarkdown), string.Empty);
    }

    private async void uiUpdateButton_Click(object? sender, EventArgs e)
    {
        if (_downloader is null)
        {
            return;
        }

        _cts = new();

        uiLaterButton.Visible = false;
        uiUpdateButton.Visible = false;
        uiCancelButton.Visible = true;
        uiStatusLabel.Visible = true;
        uiProgressBar.Visible = true;
        uiStatusLabel.Text = "Скачивание обновления...";

        AcceptButton = null;
        CancelButton = uiCancelButton;

        var progress = new Progress<double>(UpdateProgress);

        try
        {
            DownloadedZipPath = await Task.Run(() => _downloader(progress, _cts.Token));
            DialogResult = DialogResult.OK;
        }
        catch (OperationCanceledException)
        {
            DialogResult = DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            DownloadError = ex;
            DialogResult = DialogResult.Abort;
        }
    }

    private void uiCancelButton_Click(object? sender, EventArgs e)
    {
        uiCancelButton.Enabled = false;
        _cts?.Cancel();
    }

    private static string StripWhatsChangedHeading(string markdown)
    {
        var lines = markdown.Split('\n');
        var start = 0;

        while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        if (start < lines.Length && lines[start].TrimEnd('\r').Trim().Equals("## What's Changed", StringComparison.OrdinalIgnoreCase))
        {
            start++;
        }

        return string.Join('\n', lines, start, lines.Length - start);
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} Б",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} КБ",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} МБ",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} ГБ",
        };
    }

    private void UpdateProgress(double value)
    {
        uiProgressBar.Value = Math.Clamp((int)(value * 100), 0, 100);
        uiStatusLabel.Text = $"Скачивание обновления... {value:P0}";
    }
}
