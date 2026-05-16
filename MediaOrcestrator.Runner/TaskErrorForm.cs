using System.Diagnostics;

namespace MediaOrcestrator.Runner;

public partial class TaskErrorForm : Form
{
    private string _taskName = string.Empty;
    private string _errorText = string.Empty;

    public TaskErrorForm()
    {
        InitializeComponent();
    }

    public TaskErrorForm(string taskName, string errorText)
        : this()
    {
        SetError(taskName, errorText);
    }

    public void SetError(string taskName, string errorText)
    {
        _taskName = taskName ?? string.Empty;
        _errorText = errorText ?? string.Empty;

        if (!IsHandleCreated)
        {
            return;
        }

        ApplyError();
    }

    private void TaskErrorForm_Load(object? sender, EventArgs e)
    {
        ApplyError();
    }

    private void uiCopyButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(_errorText.Length > 0 ? _errorText : " ");
            uiCopyButton.Text = "Скопировано";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Не удалось скопировать: {ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void uiOpenLogButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            var todayLog = Path.Combine(logsDirectory, $"log-{DateTime.Now:yyyyMMdd}.txt");

            if (File.Exists(todayLog))
            {
                Process.Start(new ProcessStartInfo(todayLog)
                {
                    UseShellExecute = true,
                });

                return;
            }

            Directory.CreateDirectory(logsDirectory);
            Process.Start(new ProcessStartInfo(logsDirectory)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Не удалось открыть лог: {ex.Message}",
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

    private void ApplyError()
    {
        uiTitleLabel.Text = _taskName.Length > 0
            ? $"Задача «{_taskName}» завершилась с ошибкой"
            : "Задача завершилась с ошибкой";

        uiErrorTextBox.Text = _errorText;
        uiErrorTextBox.SelectionStart = 0;
        uiErrorTextBox.SelectionLength = 0;
    }
}
