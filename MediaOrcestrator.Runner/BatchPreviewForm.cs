using MediaOrcestrator.Domain;
using SkiaSharp;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Runner;

public partial class BatchPreviewForm : Form
{
    private readonly List<Media> _medias;
    private readonly BatchPreviewService _service;
    private readonly CoverGenerator _coverGenerator;
    private readonly CoverTemplateStore _coverTemplateStore;

    private bool _hasSuccess;
    private List<Source> _donors = [];
    private List<Source> _targets = [];
    private CoverTemplate? _coverTemplate;
    private string? _currentProfileName;
    private bool _suppressProfileComboEvents;

    public BatchPreviewForm()
    {
        _medias = [];
        _service = null!;
        _coverGenerator = null!;
        _coverTemplateStore = null!;
        InitializeComponent();
    }

    public BatchPreviewForm(List<Media> medias, BatchPreviewService service, CoverGenerator coverGenerator, CoverTemplateStore coverTemplateStore) : this()
    {
        _medias = medias;
        _service = service;
        _coverGenerator = coverGenerator;
        _coverTemplateStore = coverTemplateStore;
        _coverTemplate = coverTemplateStore.LoadLast();

        Text = $"Обновление превью ({medias.Count} видео)";

        PopulateDonors();
        OnModeChanged();
        RefreshProfilesCombo();
        RefreshCoverThumbnail();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult is DialogResult.Cancel or DialogResult.None)
        {
            DialogResult = _hasSuccess ? DialogResult.OK : DialogResult.Cancel;
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        uiCoverThumbnail.Image?.Dispose();
        uiCoverThumbnail.Image = null;
        base.OnFormClosed(e);
    }

    private void OnFromSourceCheckedChanged(object? sender, EventArgs e)
    {
        if (!uiFromSourceRadio.Checked)
        {
            return;
        }

        uiFromFileRadio.Checked = false;
        uiFromTemplateRadio.Checked = false;
        OnModeChanged();
    }

    private void OnFromFileCheckedChanged(object? sender, EventArgs e)
    {
        if (!uiFromFileRadio.Checked)
        {
            return;
        }

        uiFromSourceRadio.Checked = false;
        uiFromTemplateRadio.Checked = false;
        OnModeChanged();
    }

    private void OnFromTemplateCheckedChanged(object? sender, EventArgs e)
    {
        if (!uiFromTemplateRadio.Checked)
        {
            return;
        }

        uiFromSourceRadio.Checked = false;
        uiFromFileRadio.Checked = false;
        OnModeChanged();
    }

    private void OnDonorComboSelectedIndexChanged(object? sender, EventArgs e)
    {
        OnDonorChanged();
    }

    private void OnBrowseButtonClick(object? sender, EventArgs e)
    {
        BrowseFile();
    }

    private void OnTemplateButtonClick(object? sender, EventArgs e)
    {
        OpenTemplateEditor();
    }

    private void OnProfileComboSelectedIndexChanged(object? sender, EventArgs e)
    {
        OnProfileComboChanged();
    }

    private void OnTargetsItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (IsHandleCreated)
        {
            BeginInvoke(UpdatePreview);
        }
    }

    private async void OnApplyButtonClick(object? sender, EventArgs e)
    {
        await ApplyAsync();
    }

    private static string RowKey(string mediaId, string sourceId)
    {
        return $"{mediaId}|{sourceId}";
    }

    private void RefreshCoverThumbnail()
    {
        uiCoverThumbnail.Image?.Dispose();
        uiCoverThumbnail.Image = null;

        if (_coverTemplate == null || string.IsNullOrEmpty(_coverTemplate.TemplatePath) || !File.Exists(_coverTemplate.TemplatePath))
        {
            return;
        }

        try
        {
            var sampleNumber = ResolveSampleNumber(_coverTemplate);
            using var skBitmap = _coverGenerator.Render(_coverTemplate, sampleNumber);
            using var skImage = SKImage.FromBitmap(skBitmap);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 90);
            using var ms = new MemoryStream(data.ToArray());
            using var sourceBitmap = new Bitmap(ms);
            uiCoverThumbnail.Image = new Bitmap(sourceBitmap);
        }
        catch
        {
        }
    }

    private int ResolveSampleNumber(CoverTemplate template)
    {
        if (template.NumberMode != CoverNumberMode.TitleRegex || _medias.Count == 0)
        {
            return template.StartNumber;
        }

        var title = _medias[0].Title;

        if (string.IsNullOrEmpty(title))
        {
            return template.StartNumber;
        }

        var pattern = string.IsNullOrWhiteSpace(template.TitleRegexPattern)
            ? CoverTemplate.DefaultTitleRegex
            : template.TitleRegexPattern;

        try
        {
            var match = Regex.Match(title, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));

            if (match.Success)
            {
                var captured = match.Groups.Count > 1 && match.Groups[1].Success ? match.Groups[1].Value : match.Value;

                if (int.TryParse(captured, out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (ArgumentException)
        {
        }
        catch (RegexMatchTimeoutException)
        {
        }

        return template.StartNumber;
    }

    private void RefreshProfilesCombo()
    {
        _suppressProfileComboEvents = true;
        uiProfileCombo.Items.Clear();
        uiProfileCombo.Items.Add("— выбрать профиль —");

        foreach (var name in _coverTemplateStore.List())
        {
            uiProfileCombo.Items.Add(name);
        }

        uiProfileCombo.SelectedIndex = 0;
        _suppressProfileComboEvents = false;
    }

    private void OnProfileComboChanged()
    {
        if (_suppressProfileComboEvents)
        {
            return;
        }

        var idx = uiProfileCombo.SelectedIndex;

        if (idx <= 0)
        {
            return;
        }

        var name = uiProfileCombo.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var loaded = _coverTemplateStore.Load(name);

        if (loaded == null)
        {
            MessageBox.Show($"Не удалось загрузить профиль '{name}'", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _coverTemplate = loaded;
        _currentProfileName = name;
        _coverTemplateStore.SaveLast(loaded);
        RefreshCoverThumbnail();
        UpdatePreview();
    }

    private void PopulateDonors()
    {
        _donors = _service.GetAvailableDonors(_medias);
        uiDonorComboBox.Items.Clear();

        foreach (var donor in _donors)
        {
            uiDonorComboBox.Items.Add(donor.TitleFull);
        }

        if (uiDonorComboBox.Items.Count > 0)
        {
            uiDonorComboBox.SelectedIndex = 0;
        }
    }

    private void PopulateTargets(Source? excludeDonor)
    {
        _targets = _service.GetAvailableTargets(_medias, excludeDonor);
        uiTargetsListBox.Items.Clear();

        for (var i = 0; i < _targets.Count; i++)
        {
            uiTargetsListBox.Items.Add(_targets[i].TitleFull);
            uiTargetsListBox.SetItemChecked(i, true);
        }

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        uiResultGrid.Rows.Clear();
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

                var row = uiResultGrid.Rows.Add(media.Title, target.TitleFull, "Ожидание");
                uiResultGrid.Rows[row].Tag = RowKey(media.Id, target.Id);
                uiResultGrid.Rows[row].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        var hasSource = HasSelectedSource();

        uiApplyButton.Enabled = hasSource && uiResultGrid.Rows.Count > 0;
        uiStatusLabel.Text = uiResultGrid.Rows.Count > 0
            ? $"Запланировано: {uiResultGrid.Rows.Count}"
            : "";
    }

    private bool HasSelectedSource()
    {
        if (uiFromSourceRadio.Checked)
        {
            return uiDonorComboBox.SelectedIndex >= 0;
        }

        if (uiFromFileRadio.Checked)
        {
            return !string.IsNullOrEmpty(uiFilePathTextBox.Text);
        }

        return uiFromTemplateRadio.Checked && _coverTemplate != null;
    }

    private void OnProgressReport(BatchPreviewResult result)
    {
        var key = RowKey(result.Media.Id, result.Target.Id);
        var statusText = result.Success ? "Готово" : $"Ошибка: {result.ErrorMessage}";
        var color = result.Success ? Color.DarkGreen : Color.DarkRed;

        DataGridViewRow? matchingRow = null;

        foreach (DataGridViewRow gridRow in uiResultGrid.Rows)
        {
            if (gridRow.Tag is string rowKey && rowKey == key)
            {
                matchingRow = gridRow;
                break;
            }
        }

        if (matchingRow != null)
        {
            matchingRow.Cells[uiStatusColumn.Name].Value = statusText;
            matchingRow.DefaultCellStyle.ForeColor = color;
        }
        else
        {
            var row = uiResultGrid.Rows.Add(result.Media.Title, result.Target.TitleFull, statusText);
            uiResultGrid.Rows[row].Tag = key;
            uiResultGrid.Rows[row].DefaultCellStyle.ForeColor = color;
        }
    }

    private void OnModeChanged()
    {
        uiDonorComboBox.Enabled = uiFromSourceRadio.Checked;

        uiFilePathTextBox.Enabled = uiFromFileRadio.Checked;
        uiBrowseButton.Enabled = uiFromFileRadio.Checked;

        uiTemplateButton.Enabled = uiFromTemplateRadio.Checked;

        if (uiFromSourceRadio.Checked)
        {
            OnDonorChanged();
        }
        else
        {
            PopulateTargets(null);
        }
    }

    private void OpenTemplateEditor()
    {
        using var form = new CoverTemplateForm(_coverGenerator, _coverTemplateStore, _coverTemplate, _currentProfileName);

        if (form.ShowDialog(this) != DialogResult.OK || form.Result == null)
        {
            return;
        }

        _coverTemplate = form.Result;
        _coverTemplateStore.SaveLast(_coverTemplate);
        RefreshProfilesCombo();
        RefreshCoverThumbnail();
        UpdatePreview();
    }

    private void OnDonorChanged()
    {
        var selectedDonor = GetSelectedDonor();
        PopulateTargets(selectedDonor);
    }

    private Source? GetSelectedDonor()
    {
        var index = uiDonorComboBox.SelectedIndex;
        return index >= 0 && index < _donors.Count ? _donors[index] : null;
    }

    private List<Source> GetSelectedTargets()
    {
        var selected = new List<Source>();

        for (var i = 0; i < uiTargetsListBox.Items.Count; i++)
        {
            if (uiTargetsListBox.GetItemChecked(i))
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

        uiFilePathTextBox.Text = dialog.FileName;
        UpdatePreview();
    }

    private async Task ApplyAsync()
    {
        var donor = uiFromSourceRadio.Checked ? GetSelectedDonor() : null;
        var localFilePath = uiFromFileRadio.Checked ? uiFilePathTextBox.Text : null;
        var coverTemplate = uiFromTemplateRadio.Checked ? _coverTemplate : null;
        var targets = GetSelectedTargets();

        uiApplyButton.Enabled = false;
        uiFromSourceRadio.Enabled = false;
        uiFromFileRadio.Enabled = false;
        uiFromTemplateRadio.Enabled = false;
        uiDonorComboBox.Enabled = false;
        uiFilePathTextBox.Enabled = false;
        uiBrowseButton.Enabled = false;
        uiTemplateButton.Enabled = false;
        uiTargetsListBox.Enabled = false;
        uiStatusLabel.Text = "Обновление превью...";

        try
        {
            var progress = new Progress<BatchPreviewResult>(OnProgressReport);

            var results = await Task.Run(() =>
                _service.ApplyAsync(_medias, donor, targets, localFilePath, coverTemplate, progress, CancellationToken.None));

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;
            uiStatusLabel.Text = $"Готово: {successCount} успешно, {failCount} с ошибками";

            _hasSuccess = successCount > 0;
        }
        catch (Exception ex)
        {
            uiStatusLabel.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            uiFromSourceRadio.Enabled = true;
            uiFromFileRadio.Enabled = true;
            uiFromTemplateRadio.Enabled = true;
            uiTargetsListBox.Enabled = true;
            uiDonorComboBox.Enabled = uiFromSourceRadio.Checked;
            uiFilePathTextBox.Enabled = uiFromFileRadio.Checked;
            uiBrowseButton.Enabled = uiFromFileRadio.Checked;
            uiTemplateButton.Enabled = uiFromTemplateRadio.Checked;
            uiApplyButton.Enabled = true;
        }
    }
}
