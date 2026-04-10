using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public class BatchPreviewForm : Form
{
    private readonly List<Media> _medias;
    private readonly BatchPreviewService _service;
    private bool _hasSuccess;

    private readonly RadioButton _uiFromSourceRadio;
    private readonly RadioButton _uiFromFileRadio;
    private readonly ComboBox _uiDonorComboBox;
    private readonly TextBox _uiFilePathTextBox;
    private readonly Button _uiBrowseButton;
    private readonly CheckedListBox _uiTargetsListBox;
    private readonly DataGridView _uiResultGrid;
    private readonly Button _uiApplyButton;
    private readonly Label _uiStatusLabel;

    private List<Source> _donors = [];
    private List<Source> _targets = [];

    public BatchPreviewForm(List<Media> medias, BatchPreviewService service)
    {
        _medias = medias;
        _service = service;

        Text = $"Обновление превью ({medias.Count} видео)";
        Size = new(750, 550);
        MinimumSize = new(600, 450);
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

        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
        };

        topLayout.ColumnStyles.Add(new(SizeType.Percent, 55));
        topLayout.ColumnStyles.Add(new(SizeType.Percent, 45));

        var sourcePanel = new GroupBox
        {
            Text = "Источник превью",
            Dock = DockStyle.Fill,
            AutoSize = true,
        };

        var sourcePanelLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
        };

        var donorLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        _uiFromSourceRadio = new()
        {
            Text = "Из источника:",
            AutoSize = true,
            Checked = true,
        };

        _uiDonorComboBox = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 250,
        };

        donorLayout.Controls.Add(_uiFromSourceRadio);
        donorLayout.Controls.Add(_uiDonorComboBox);

        var fileLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        _uiFromFileRadio = new()
        {
            Text = "Из файла:",
            AutoSize = true,
        };

        _uiFilePathTextBox = new()
        {
            Width = 200,
            ReadOnly = true,
        };

        _uiBrowseButton = new()
        {
            Text = "Обзор...",
            AutoSize = true,
        };

        _uiBrowseButton.Click += (_, _) => BrowseFile();

        fileLayout.Controls.Add(_uiFromFileRadio);
        fileLayout.Controls.Add(_uiFilePathTextBox);
        fileLayout.Controls.Add(_uiBrowseButton);

        sourcePanelLayout.Controls.Add(donorLayout, 0, 0);
        sourcePanelLayout.Controls.Add(fileLayout, 0, 1);
        sourcePanel.Controls.Add(sourcePanelLayout);

        topLayout.Controls.Add(sourcePanel, 0, 0);

        var targetsGroup = new GroupBox
        {
            Text = "Куда загрузить",
            Dock = DockStyle.Fill,
        };

        _uiTargetsListBox = new()
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
        };

        _uiTargetsListBox.ItemCheck += (_, _) =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(UpdatePreview);
            }
        };

        targetsGroup.Controls.Add(_uiTargetsListBox);
        topLayout.Controls.Add(targetsGroup, 1, 0);

        mainLayout.Controls.Add(topLayout, 0, 0);

        _uiResultGrid = new()
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

        _uiResultGrid.Columns.Add("Title", "Название");
        _uiResultGrid.Columns.Add("Target", "Источник");
        _uiResultGrid.Columns.Add("Status", "Статус");
        _uiResultGrid.Columns["Status"]!.FillWeight = 60;

        mainLayout.Controls.Add(_uiResultGrid, 0, 1);

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

        var uiCancelButton = new Button
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

        buttonPanel.Controls.Add(uiCancelButton);
        buttonPanel.Controls.Add(_uiApplyButton);

        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);

        CancelButton = uiCancelButton;

        _uiFromSourceRadio.CheckedChanged += (_, _) => OnModeChanged();
        _uiFromFileRadio.CheckedChanged += (_, _) => OnModeChanged();
        _uiDonorComboBox.SelectedIndexChanged += (_, _) => OnDonorChanged();

        PopulateDonors();
        OnModeChanged();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult is DialogResult.Cancel or DialogResult.None)
        {
            DialogResult = _hasSuccess ? DialogResult.OK : DialogResult.Cancel;
        }

        base.OnFormClosing(e);
    }

    private void PopulateDonors()
    {
        _donors = _service.GetAvailableDonors(_medias);
        _uiDonorComboBox.Items.Clear();

        foreach (var donor in _donors)
        {
            _uiDonorComboBox.Items.Add(donor.TitleFull);
        }

        if (_uiDonorComboBox.Items.Count > 0)
        {
            _uiDonorComboBox.SelectedIndex = 0;
        }
    }

    private void PopulateTargets(Source? excludeDonor)
    {
        _targets = _service.GetAvailableTargets(_medias, excludeDonor);
        _uiTargetsListBox.Items.Clear();

        for (var i = 0; i < _targets.Count; i++)
        {
            _uiTargetsListBox.Items.Add(_targets[i].TitleFull);
            _uiTargetsListBox.SetItemChecked(i, true);
        }

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        _uiResultGrid.Rows.Clear();
        var targets = GetSelectedTargets();

        foreach (var media in _medias)
        {
            foreach (var target in targets)
            {
                var hasLink = media.Sources.Any(s => s.SourceId == target.Id);
                if (!hasLink)
                {
                    continue;
                }

                var row = _uiResultGrid.Rows.Add(media.Title, target.TitleFull, "Ожидание");
                _uiResultGrid.Rows[row].Tag = RowKey(media.Id, target.Id);
                _uiResultGrid.Rows[row].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        var hasSource = _uiFromSourceRadio.Checked
            ? _uiDonorComboBox.SelectedIndex >= 0
            : !string.IsNullOrEmpty(_uiFilePathTextBox.Text);

        _uiApplyButton.Enabled = hasSource && _uiResultGrid.Rows.Count > 0;
        _uiStatusLabel.Text = _uiResultGrid.Rows.Count > 0
            ? $"Запланировано: {_uiResultGrid.Rows.Count}"
            : "";
    }

    private static string RowKey(string mediaId, string sourceId)
    {
        return $"{mediaId}|{sourceId}";
    }

    private void OnProgressReport(BatchPreviewResult result)
    {
        var key = RowKey(result.Media.Id, result.Target.Id);
        var statusText = result.Success ? "Готово" : $"Ошибка: {result.ErrorMessage}";
        var color = result.Success ? Color.DarkGreen : Color.DarkRed;

        DataGridViewRow? matchingRow = null;

        foreach (DataGridViewRow gridRow in _uiResultGrid.Rows)
        {
            if (gridRow.Tag is string rowKey && rowKey == key)
            {
                matchingRow = gridRow;
                break;
            }
        }

        if (matchingRow != null)
        {
            matchingRow.Cells["Status"].Value = statusText;
            matchingRow.DefaultCellStyle.ForeColor = color;
        }
        else
        {
            var row = _uiResultGrid.Rows.Add(result.Media.Title, result.Target.TitleFull, statusText);
            _uiResultGrid.Rows[row].Tag = key;
            _uiResultGrid.Rows[row].DefaultCellStyle.ForeColor = color;
        }
    }

    private void OnModeChanged()
    {
        _uiDonorComboBox.Enabled = _uiFromSourceRadio.Checked;

        _uiFilePathTextBox.Enabled = _uiFromFileRadio.Checked;
        _uiBrowseButton.Enabled = _uiFromFileRadio.Checked;

        if (_uiFromSourceRadio.Checked)
        {
            OnDonorChanged();
        }
        else
        {
            PopulateTargets(null);
        }
    }

    private void OnDonorChanged()
    {
        var selectedDonor = GetSelectedDonor();
        PopulateTargets(selectedDonor);
    }

    private Source? GetSelectedDonor()
    {
        var index = _uiDonorComboBox.SelectedIndex;
        return index >= 0 && index < _donors.Count ? _donors[index] : null;
    }

    private List<Source> GetSelectedTargets()
    {
        var selected = new List<Source>();

        for (var i = 0; i < _uiTargetsListBox.Items.Count; i++)
        {
            if (_uiTargetsListBox.GetItemChecked(i))
            {
                selected.Add(_targets[i]);
            }
        }

        return selected;
    }

    private void BrowseFile()
    {
        using var dialog = new OpenFileDialog();
        dialog.Title = "Выберите файл превью";
        dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp|Все файлы|*.*";

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _uiFilePathTextBox.Text = dialog.FileName;
        UpdatePreview();
    }

    private async Task ApplyAsync()
    {
        var donor = _uiFromSourceRadio.Checked ? GetSelectedDonor() : null;
        var localFilePath = _uiFromFileRadio.Checked ? _uiFilePathTextBox.Text : null;
        var targets = GetSelectedTargets();

        _uiApplyButton.Enabled = false;
        _uiFromSourceRadio.Enabled = false;
        _uiFromFileRadio.Enabled = false;
        _uiDonorComboBox.Enabled = false;
        _uiFilePathTextBox.Enabled = false;
        _uiBrowseButton.Enabled = false;
        _uiTargetsListBox.Enabled = false;
        _uiStatusLabel.Text = "Обновление превью...";

        try
        {
            var progress = new Progress<BatchPreviewResult>(OnProgressReport);

            var results = await Task.Run(() =>
                _service.ApplyAsync(_medias, donor, targets, localFilePath, progress, CancellationToken.None));

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;
            _uiStatusLabel.Text = $"Готово: {successCount} успешно, {failCount} с ошибками";

            _hasSuccess = successCount > 0;
        }
        catch (Exception ex)
        {
            _uiStatusLabel.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            _uiFromSourceRadio.Enabled = true;
            _uiFromFileRadio.Enabled = true;
            _uiTargetsListBox.Enabled = true;
            _uiDonorComboBox.Enabled = _uiFromSourceRadio.Checked;
            _uiFilePathTextBox.Enabled = _uiFromFileRadio.Checked;
            _uiBrowseButton.Enabled = _uiFromFileRadio.Checked;
            _uiApplyButton.Enabled = true;
        }
    }
}
