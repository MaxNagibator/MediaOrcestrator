using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Runner;

public partial class ToolsForm : Form
{
    private readonly ToolManager _toolManager;
    private List<ToolStatus> _statuses = [];

    public ToolsForm(ToolManager toolManager)
    {
        _toolManager = toolManager;
        InitializeComponent();
        checkUpdatesButton.Click += CheckUpdatesButton_Click;
        toolsDataGridView.CellClick += ToolsDataGridView_CellClick;
        LoadCurrentState();
    }

    private void LoadCurrentState()
    {
        toolsDataGridView.Rows.Clear();

        foreach (var (name, _) in _toolManager.GetRegistry())
        {
            var path = _toolManager.GetToolPath(name);
            toolsDataGridView.Rows.Add(name,
                path is not null ? "..." : "Не установлен",
                "—",
                path is not null ? "Проверьте обновления" : "Не установлен",
                path is not null ? "Проверить" : "Установить");
        }
    }

    private async void CheckUpdatesButton_Click(object? sender, EventArgs e)
    {
        checkUpdatesButton.Enabled = false;
        statusLabel.Text = "Проверка обновлений...";

        try
        {
            _statuses = await Task.Run(() => _toolManager.CheckForUpdatesAsync());
            RefreshGrid();
            statusLabel.Text = $"Проверено: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            checkUpdatesButton.Enabled = true;
        }
    }

    private void RefreshGrid()
    {
        toolsDataGridView.Rows.Clear();

        foreach (var status in _statuses)
        {
            string statusText;
            string actionText;

            if (status.InstalledVersion is null)
            {
                statusText = "Не установлен";
                actionText = "Установить";
            }
            else if (status.UpdateAvailable)
            {
                statusText = "Есть обновление";
                actionText = "Обновить";
            }
            else
            {
                statusText = "Актуально";
                actionText = "";
            }

            toolsDataGridView.Rows.Add(status.Name,
                status.InstalledVersion ?? "—",
                status.LatestVersion ?? "—",
                statusText,
                actionText);
        }
    }

    private async void ToolsDataGridView_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != colAction.Index || e.RowIndex < 0)
        {
            return;
        }

        var toolName = toolsDataGridView.Rows[e.RowIndex].Cells[colName.Index].Value?.ToString();

        if (string.IsNullOrEmpty(toolName))
        {
            return;
        }

        var buttonCell = toolsDataGridView.Rows[e.RowIndex].Cells[colAction.Index];

        if (string.IsNullOrEmpty(buttonCell.Value?.ToString()))
        {
            return;
        }

        checkUpdatesButton.Enabled = false;
        toolsDataGridView.Enabled = false;
        progressBar.Visible = true;
        progressBar.Value = 0;
        statusLabel.Text = $"Обновление {toolName}...";

        var progress = new Progress<double>(p =>
        {
            progressBar.Value = (int)(p * 100);
        });

        try
        {
            await Task.Run(() => _toolManager.UpdateToolAsync(toolName, progress));
            statusLabel.Text = $"{toolName} успешно обновлён!";
            _statuses = await Task.Run(() => _toolManager.CheckForUpdatesAsync());
            RefreshGrid();
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Ошибка обновления {toolName}: {ex.Message}";
            MessageBox.Show($"Не удалось обновить {toolName}:\n\n{ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            progressBar.Visible = false;
            checkUpdatesButton.Enabled = true;
            toolsDataGridView.Enabled = true;
        }
    }
}
