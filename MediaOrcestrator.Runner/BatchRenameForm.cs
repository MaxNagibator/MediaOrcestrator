using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

// TODO: Переделать на дизайнер. Не удобно постоянно на студию переключаться для вёрстки
public class BatchRenameForm : Form
{
    private readonly List<Media> _medias;
    private readonly Orcestrator _orcestrator;

    private readonly TextBox _uiFindTextBox;
    private readonly TextBox _uiReplaceTextBox;
    private readonly Button _uiPreviewButton;
    private readonly DataGridView _uiPreviewGrid;
    private readonly Button _uiApplyButton;
    private readonly Button _uiCancelButton;
    private readonly Label _uiStatusLabel;

    public BatchRenameForm(List<Media> medias, Orcestrator orcestrator)
    {
        _medias = medias;
        _orcestrator = orcestrator;

        Text = $"Пакетное переименование ({medias.Count} видео)";
        Size = new(700, 500);
        MinimumSize = new(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(10),
            RowCount = 4,
            ColumnCount = 1,
        };

        mainLayout.RowStyles.Add(new(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new(SizeType.AutoSize));

        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 2,
        };

        inputPanel.ColumnStyles.Add(new(SizeType.AutoSize));
        inputPanel.ColumnStyles.Add(new(SizeType.Percent, 100));
        inputPanel.ColumnStyles.Add(new(SizeType.AutoSize));

        inputPanel.Controls.Add(new Label
        {
            Text = "Найти:",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
        }, 0, 0);

        _uiFindTextBox = new()
        {
            Dock = DockStyle.Fill,
        };

        inputPanel.Controls.Add(_uiFindTextBox, 1, 0);

        inputPanel.Controls.Add(new Label
        {
            Text = "Заменить:",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
        }, 0, 1);

        _uiReplaceTextBox = new()
        {
            Dock = DockStyle.Fill,
        };

        inputPanel.Controls.Add(_uiReplaceTextBox, 1, 1);

        _uiPreviewButton = new()
        {
            Text = "Обновить превью",
            AutoSize = true,
        };

        _uiPreviewButton.Click += (_, _) => UpdatePreview();
        inputPanel.Controls.Add(_uiPreviewButton, 2, 0);
        inputPanel.SetRowSpan(_uiPreviewButton, 2);

        mainLayout.Controls.Add(inputPanel, 0, 0);

        _uiPreviewGrid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
        };

        _uiPreviewGrid.Columns.Add("OldTitle", "Было");
        _uiPreviewGrid.Columns.Add("NewTitle", "Стало");
        _uiPreviewGrid.Columns.Add("Status", "Статус");
        _uiPreviewGrid.Columns["Status"]!.FillWeight = 50;

        mainLayout.Controls.Add(_uiPreviewGrid, 0, 1);

        _uiStatusLabel = new()
        {
            Text = "",
            Dock = DockStyle.Fill,
            AutoSize = true,
        };

        mainLayout.Controls.Add(_uiStatusLabel, 0, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
        };

        _uiCancelButton = new()
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
        };

        _uiApplyButton = new()
        {
            Text = "Применить",
            Enabled = false,
        };

        _uiApplyButton.Click += async (_, _) => await ApplyAsync();

        buttonPanel.Controls.Add(_uiCancelButton);
        buttonPanel.Controls.Add(_uiApplyButton);

        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);

        CancelButton = _uiCancelButton;
        AcceptButton = _uiPreviewButton;
    }

    private void UpdatePreview()
    {
        var find = _uiFindTextBox.Text;
        _uiPreviewGrid.Rows.Clear();

        if (string.IsNullOrEmpty(find))
        {
            _uiApplyButton.Enabled = false;
            return;
        }

        var hasChanges = false;

        foreach (var media in _medias)
        {
            var oldTitle = media.Title;
            var newTitle = oldTitle.Replace(find, _uiReplaceTextBox.Text);

            if (oldTitle == newTitle)
            {
                var row = _uiPreviewGrid.Rows.Add(oldTitle, oldTitle, "(без изменений)");
                _uiPreviewGrid.Rows[row].DefaultCellStyle.ForeColor = Color.Gray;
            }
            else
            {
                _uiPreviewGrid.Rows.Add(oldTitle, newTitle, "");
                hasChanges = true;
            }
        }

        _uiApplyButton.Enabled = hasChanges;
    }

    private async Task ApplyAsync()
    {
        var find = _uiFindTextBox.Text;
        var replace = _uiReplaceTextBox.Text;

        _uiApplyButton.Enabled = false;
        _uiPreviewButton.Enabled = false;
        _uiFindTextBox.Enabled = false;
        _uiReplaceTextBox.Enabled = false;
        _uiStatusLabel.Text = "Применение изменений...";

        try
        {
            var results = await Task.Run(() =>
                _orcestrator.BatchRenameAsync(_medias, find, replace, CancellationToken.None));

            _uiPreviewGrid.Rows.Clear();

            foreach (var result in results)
            {
                var statusText = result.Success ? "Готово" : $"Ошибка: {result.ErrorMessage}";
                var row = _uiPreviewGrid.Rows.Add(result.OldTitle, result.NewTitle, statusText);

                if (!result.Success)
                {
                    _uiPreviewGrid.Rows[row].DefaultCellStyle.ForeColor = Color.DarkRed;
                }
                else
                {
                    _uiPreviewGrid.Rows[row].DefaultCellStyle.ForeColor = Color.DarkGreen;
                }
            }

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);
            _uiStatusLabel.Text = $"Готово: {successCount} успешно, {failCount} с ошибками";

            DialogResult = successCount > 0 ? DialogResult.OK : DialogResult.None;
        }
        catch (Exception ex)
        {
            _uiStatusLabel.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            _uiPreviewButton.Enabled = true;
            _uiFindTextBox.Enabled = true;
            _uiReplaceTextBox.Enabled = true;
        }
    }
}
