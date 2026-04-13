using System.Diagnostics;

namespace MediaOrcestrator.Runner;

public partial class ErrorReportForm : Form
{
    private readonly ErrorReportService? _errorReportService;
    private readonly Exception? _exception;
    private readonly string? _userContext;
    private ErrorReportPayload? _payload;

    public ErrorReportForm()
    {
        InitializeComponent();
    }

    public ErrorReportForm(ErrorReportService errorReportService, Exception? exception = null, string? userContext = null)
    {
        _errorReportService = errorReportService;
        _exception = exception;
        _userContext = userContext;

        InitializeComponent();
    }

    private void ErrorReportForm_Load(object? sender, EventArgs e)
    {
        if (_errorReportService == null)
        {
            return;
        }

        _payload = _errorReportService.Build(_exception, _userContext);

        uiTitleLabel.Text = _exception != null ? "Произошла ошибка" : "Сообщить о проблеме";
        uiSummaryLabel.Text = _payload.Summary;
        uiDetailsTextBox.Text = _payload.FullReport;
        uiDetailsTextBox.SelectionStart = 0;
        uiDetailsTextBox.SelectionLength = 0;
    }

    private void uiOpenIssueButton_Click(object? sender, EventArgs e)
    {
        if (_payload == null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(_payload.LogClipboard);
            Process.Start(new ProcessStartInfo(_payload.IssueUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть браузер: {ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void uiCopyButton_Click(object? sender, EventArgs e)
    {
        if (_payload == null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(_payload.FullReport);
            uiCopyButton.Text = "Скопировано";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось скопировать: {ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void uiCloseButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
