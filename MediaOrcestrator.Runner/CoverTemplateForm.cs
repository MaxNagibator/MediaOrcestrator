using MediaOrcestrator.Domain;
using SkiaSharp;
using DrawingColor = System.Drawing.Color;

namespace MediaOrcestrator.Runner;

public partial class CoverTemplateForm : Form
{
    private readonly CoverGenerator _coverGenerator;
    private readonly CoverTemplateStore _store;

    private readonly List<MutableLayer> _layers = [];

    private string? _templatePath;
    private string? _currentProfileName;
    private bool _suppressPreview;
    private bool _suppressLayerEdits;
    private int _prevSelectedLayerIndex = -1;
    private bool _isDraggingLayer;

    public CoverTemplateForm()
    {
        _coverGenerator = null!;
        _store = null!;
        InitializeComponent();
        PopulateFontFamilies();
    }

    public CoverTemplateForm(CoverGenerator coverGenerator, CoverTemplateStore store, CoverTemplate? initial, string? initialProfileName = null) : this()
    {
        _coverGenerator = coverGenerator;
        _store = store;
        _currentProfileName = initialProfileName;

        Text = FormatTitle(_currentProfileName);

        if (initial != null)
        {
            ApplyInitial(initial);
        }
        else
        {
            _layers.Add(MutableLayer.FromDomain(CoverTemplate.DefaultNumberLayer));
            RefreshLayersList();
            uiLayersList.SelectedIndex = 0;
        }

        UpdateNumberModeUi();
        PopulateProfiles();
    }

    public CoverTemplate? Result { get; private set; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        uiPreview.Image?.Dispose();
        uiPreview.Image = null;
        base.OnFormClosed(e);
    }

    private void OnPreviewMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || uiPreview.Image == null)
        {
            return;
        }

        if (!TryApplyDragPosition(e.Location))
        {
            return;
        }

        _isDraggingLayer = true;
        uiPreview.Capture = true;
    }

    private void OnPreviewMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDraggingLayer)
        {
            return;
        }

        TryApplyDragPosition(e.Location);
    }

    private void OnPreviewMouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isDraggingLayer)
        {
            return;
        }

        _isDraggingLayer = false;
        uiPreview.Capture = false;
    }

    private void uiBrowseButton_Click(object? sender, EventArgs e)
    {
        BrowseTemplate();
    }

    private void uiSequentialRadio_CheckedChanged(object? sender, EventArgs e)
    {
        if (!uiSequentialRadio.Checked)
        {
            return;
        }

        uiTitleRegexRadio.Checked = false;
        UpdateNumberModeUi();
        UpdatePreview();
    }

    private void uiTitleRegexRadio_CheckedChanged(object? sender, EventArgs e)
    {
        if (!uiTitleRegexRadio.Checked)
        {
            return;
        }

        uiSequentialRadio.Checked = false;
        UpdateNumberModeUi();
        UpdatePreview();
    }

    private void uiStartNumber_ValueChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void uiTitleRegexTextBox_TextChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void uiSampleNumber_ValueChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void uiLayersList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        OnLayerSelectionChanged();
    }

    private void uiLayerTextBox_TextChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer => layer.TextTemplate = uiLayerTextBox.Text);
    }

    private void uiLayerTextBox_Leave(object? sender, EventArgs e)
    {
        RefreshLayerListLabel(uiLayersList.SelectedIndex);
    }

    private void uiFontFamily_SelectedIndexChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer => layer.FontFamily = uiFontFamily.SelectedItem?.ToString() ?? "Arial");
    }

    private void uiFontSize_ValueChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer => layer.FontSizeRatio = (float)uiFontSize.Value / 100f);
    }

    private void uiStrokeWidth_ValueChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer => layer.StrokeWidthRatio = (float)uiStrokeWidth.Value / 100f);
    }

    private void uiFillColorButton_Click(object? sender, EventArgs e)
    {
        PickColor(true);
    }

    private void uiStrokeColorButton_Click(object? sender, EventArgs e)
    {
        PickColor(false);
    }

    private void uiAddLayerButton_Click(object? sender, EventArgs e)
    {
        AddLayer();
    }

    private void uiRemoveLayerButton_Click(object? sender, EventArgs e)
    {
        RemoveSelectedLayer();
    }

    private void uiMoveLayerUpButton_Click(object? sender, EventArgs e)
    {
        MoveSelectedLayer(-1);
    }

    private void uiMoveLayerDownButton_Click(object? sender, EventArgs e)
    {
        MoveSelectedLayer(1);
    }

    private void uiLoadProfileButton_Click(object? sender, EventArgs e)
    {
        LoadSelectedProfile();
    }

    private void uiSaveAsProfileButton_Click(object? sender, EventArgs e)
    {
        SaveAsProfile();
    }

    private void uiDeleteProfileButton_Click(object? sender, EventArgs e)
    {
        DeleteSelectedProfile();
    }

    private void uiOkButton_Click(object? sender, EventArgs e)
    {
        OnApply();
    }

    private void uiHelpButton_Click(object? sender, EventArgs e)
    {
        ShowHelp();
    }

    private static string FormatTitle(string? profileName)
    {
        return string.IsNullOrEmpty(profileName)
            ? "Шаблон обложки"
            : $"Шаблон обложки — {profileName}";
    }

    private void PopulateFontFamilies()
    {
        uiFontFamily.Items.Clear();

        foreach (var family in FontFamily.Families.OrderBy(f => f.Name))
        {
            uiFontFamily.Items.Add(family.Name);
        }
    }

    private bool TryApplyDragPosition(Point location)
    {
        var layer = GetSelectedLayer();

        if (layer == null)
        {
            return false;
        }

        var rect = GetZoomedImageRect();

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return false;
        }

        layer.TextX = Math.Clamp((location.X - rect.X) / rect.Width, 0f, 1f);
        layer.TextY = Math.Clamp((location.Y - rect.Y) / rect.Height, 0f, 1f);
        UpdatePositionLabel();
        UpdatePreview();
        return true;
    }

    private void ApplyInitial(CoverTemplate initial)
    {
        _suppressPreview = true;
        _suppressLayerEdits = true;

        _templatePath = initial.TemplatePath;
        SetTemplatePathDisplay(initial.TemplatePath);
        uiStartNumber.Value = Math.Clamp(initial.StartNumber, (int)uiStartNumber.Minimum, (int)uiStartNumber.Maximum);
        uiSampleNumber.Value = Math.Clamp(initial.StartNumber, (int)uiSampleNumber.Minimum, (int)uiSampleNumber.Maximum);
        uiTitleRegexTextBox.Text = string.IsNullOrWhiteSpace(initial.TitleRegexPattern) ? CoverTemplate.DefaultTitleRegex : initial.TitleRegexPattern;
        uiSequentialRadio.Checked = initial.NumberMode == CoverNumberMode.Sequential;
        uiTitleRegexRadio.Checked = initial.NumberMode == CoverNumberMode.TitleRegex;

        _layers.Clear();

        foreach (var layer in initial.Layers)
        {
            _layers.Add(MutableLayer.FromDomain(layer));
        }

        if (_layers.Count == 0)
        {
            _layers.Add(MutableLayer.FromDomain(CoverTemplate.DefaultNumberLayer));
        }

        RefreshLayersList();

        _suppressLayerEdits = false;
        _suppressPreview = false;

        if (uiLayersList.Items.Count > 0)
        {
            uiLayersList.SelectedIndex = 0;
        }

        UpdateNumberModeUi();
        UpdatePreview();
    }

    private void UpdateNumberModeUi()
    {
        var sequential = uiSequentialRadio.Checked;
        uiStartNumber.Enabled = sequential;
        uiTitleRegexTextBox.Enabled = !sequential;
    }

    private void OnLayerSelectionChanged()
    {
        var currentIndex = uiLayersList.SelectedIndex;

        if (_prevSelectedLayerIndex >= 0 && _prevSelectedLayerIndex != currentIndex)
        {
            RefreshLayerListLabel(_prevSelectedLayerIndex);
        }

        _prevSelectedLayerIndex = currentIndex;

        var layer = GetSelectedLayer();

        if (layer == null)
        {
            uiLayerGroup.Enabled = false;
            uiRemoveLayerButton.Enabled = false;
            uiMoveLayerUpButton.Enabled = false;
            uiMoveLayerDownButton.Enabled = false;
            uiPositionLabel.Text = "Слой не выбран";
            return;
        }

        uiLayerGroup.Enabled = true;
        uiRemoveLayerButton.Enabled = _layers.Count > 1;
        uiMoveLayerUpButton.Enabled = currentIndex > 0;
        uiMoveLayerDownButton.Enabled = currentIndex >= 0 && currentIndex < _layers.Count - 1;

        _suppressLayerEdits = true;
        uiLayerTextBox.Text = layer.TextTemplate;

        var familyIndex = uiFontFamily.Items.IndexOf(layer.FontFamily);
        uiFontFamily.SelectedIndex = familyIndex >= 0 ? familyIndex : Math.Max(0, uiFontFamily.Items.IndexOf("Impact"));

        uiFontSize.Value = (decimal)Math.Clamp(layer.FontSizeRatio * 100f, (float)uiFontSize.Minimum, (float)uiFontSize.Maximum);
        uiStrokeWidth.Value = (decimal)Math.Clamp(layer.StrokeWidthRatio * 100f, (float)uiStrokeWidth.Minimum, (float)uiStrokeWidth.Maximum);
        uiFillColorButton.BackColor = layer.FillColor;
        uiStrokeColorButton.BackColor = layer.StrokeColor;
        _suppressLayerEdits = false;

        UpdatePositionLabel();
    }

    private void OnLayerFieldChanged(Action<MutableLayer> apply)
    {
        if (_suppressLayerEdits)
        {
            return;
        }

        var layer = GetSelectedLayer();

        if (layer == null)
        {
            return;
        }

        apply(layer);
        UpdatePreview();
    }

    private MutableLayer? GetSelectedLayer()
    {
        var idx = uiLayersList.SelectedIndex;
        return idx >= 0 && idx < _layers.Count ? _layers[idx] : null;
    }

    private void AddLayer()
    {
        var newLayer = MutableLayer.FromDomain(CoverTemplate.DefaultNumberLayer with
        {
            TextTemplate = "Текст",
            TextX = 0.5f,
            TextY = 0.2f,
        });

        _layers.Add(newLayer);
        RefreshLayersList();
        uiLayersList.SelectedIndex = _layers.Count - 1;
        UpdatePreview();
    }

    private void RemoveSelectedLayer()
    {
        var idx = uiLayersList.SelectedIndex;

        if (idx < 0 || _layers.Count <= 1)
        {
            return;
        }

        _layers.RemoveAt(idx);
        RefreshLayersList();
        uiLayersList.SelectedIndex = Math.Min(idx, _layers.Count - 1);
        UpdatePreview();
    }

    private void MoveSelectedLayer(int direction)
    {
        var idx = uiLayersList.SelectedIndex;
        var target = idx + direction;

        if (idx < 0 || target < 0 || target >= _layers.Count)
        {
            return;
        }

        (_layers[idx], _layers[target]) = (_layers[target], _layers[idx]);
        RefreshLayersList();
        uiLayersList.SelectedIndex = target;
        UpdatePreview();
    }

    private void RefreshLayersList()
    {
        var preserved = uiLayersList.SelectedIndex;
        uiLayersList.BeginUpdate();
        uiLayersList.Items.Clear();

        for (var i = 0; i < _layers.Count; i++)
        {
            uiLayersList.Items.Add($"{i + 1}. {_layers[i].TextTemplate}");
        }

        uiLayersList.EndUpdate();

        if (preserved >= 0 && preserved < _layers.Count)
        {
            uiLayersList.SelectedIndex = preserved;
        }
    }

    private void RefreshLayerListLabel(int idx)
    {
        if (idx < 0 || idx >= _layers.Count || idx >= uiLayersList.Items.Count)
        {
            return;
        }

        var newLabel = $"{idx + 1}. {_layers[idx].TextTemplate}";

        if (Equals(uiLayersList.Items[idx], newLabel))
        {
            return;
        }

        uiLayersList.Items[idx] = newLabel;
    }

    private void BrowseTemplate()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите файл шаблона обложки",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp|Все файлы|*.*",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _templatePath = dialog.FileName;
        SetTemplatePathDisplay(dialog.FileName);
        UpdatePreview();
    }

    private void SetTemplatePathDisplay(string? fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            uiTemplatePathTextBox.Text = string.Empty;
            uiToolTip.SetToolTip(uiTemplatePathTextBox, string.Empty);
            return;
        }

        uiTemplatePathTextBox.Text = Path.GetFileName(fullPath);
        uiToolTip.SetToolTip(uiTemplatePathTextBox, fullPath);
    }

    private void PopulateProfiles(string? select = null)
    {
        uiProfilesCombo.Items.Clear();

        foreach (var name in _store.List())
        {
            uiProfilesCombo.Items.Add(name);
        }

        if (!string.IsNullOrEmpty(select))
        {
            var idx = uiProfilesCombo.Items.IndexOf(select);

            if (idx >= 0)
            {
                uiProfilesCombo.SelectedIndex = idx;
            }
        }
    }

    private void LoadSelectedProfile()
    {
        var name = uiProfilesCombo.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var loaded = _store.Load(name);

        if (loaded == null)
        {
            MessageBox.Show($"Не удалось загрузить профиль '{name}'", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentProfileName = name;
        Text = FormatTitle(_currentProfileName);
        ApplyInitial(loaded);
    }

    private void SaveAsProfile()
    {
        using var dialog = new InputDialog("Имя профиля:", "Сохранение профиля", _currentProfileName ?? string.Empty);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = dialog.InputText?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (string.IsNullOrEmpty(_templatePath) || !File.Exists(_templatePath))
        {
            MessageBox.Show("Выберите файл шаблона", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (uiProfilesCombo.Items.Contains(name))
        {
            var confirm = MessageBox.Show($"Профиль '{name}' уже существует. Перезаписать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        _store.Save(name, BuildTemplate());
        _currentProfileName = name;
        Text = FormatTitle(_currentProfileName);
        PopulateProfiles(name);
    }

    private void DeleteSelectedProfile()
    {
        var name = uiProfilesCombo.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var confirm = MessageBox.Show($"Удалить профиль '{name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _store.Delete(name);

        if (string.Equals(name, _currentProfileName, StringComparison.OrdinalIgnoreCase))
        {
            _currentProfileName = null;
            Text = FormatTitle(null);
        }

        PopulateProfiles();
    }

    private void PickColor(bool fill)
    {
        var layer = GetSelectedLayer();

        if (layer == null)
        {
            return;
        }

        using var dialog = new ColorDialog
        {
            Color = fill ? layer.FillColor : layer.StrokeColor,
            FullOpen = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (fill)
        {
            layer.FillColor = dialog.Color;
            uiFillColorButton.BackColor = dialog.Color;
        }
        else
        {
            layer.StrokeColor = dialog.Color;
            uiStrokeColorButton.BackColor = dialog.Color;
        }

        UpdatePreview();
    }

    private void UpdatePositionLabel()
    {
        var layer = GetSelectedLayer();
        uiPositionLabel.Text = layer == null
            ? "Слой не выбран"
            : $"Позиция слоя: X={layer.TextX:F2}, Y={layer.TextY:F2}";
    }

    private RectangleF GetZoomedImageRect()
    {
        if (uiPreview.Image == null)
        {
            return RectangleF.Empty;
        }

        var imgW = (float)uiPreview.Image.Width;
        var imgH = (float)uiPreview.Image.Height;
        var ctrlW = (float)uiPreview.ClientSize.Width;
        var ctrlH = (float)uiPreview.ClientSize.Height;

        var scale = Math.Min(ctrlW / imgW, ctrlH / imgH);
        var w = imgW * scale;
        var h = imgH * scale;
        var x = (ctrlW - w) / 2f;
        var y = (ctrlH - h) / 2f;
        return new(x, y, w, h);
    }

    private CoverTemplate BuildTemplate()
    {
        var mode = uiTitleRegexRadio.Checked ? CoverNumberMode.TitleRegex : CoverNumberMode.Sequential;

        return new(_templatePath ?? string.Empty,
            (int)uiStartNumber.Value,
            mode,
            uiTitleRegexTextBox.Text,
            _layers.Select(l => l.ToDomain()).ToList());
    }

    private void UpdatePreview()
    {
        if (_suppressPreview)
        {
            return;
        }

        if (string.IsNullOrEmpty(_templatePath) || !File.Exists(_templatePath))
        {
            uiPreview.Image?.Dispose();
            uiPreview.Image = null;
            return;
        }

        try
        {
            var template = BuildTemplate();
            using var skBitmap = _coverGenerator.Render(template, (int)uiSampleNumber.Value);
            using var skImage = SKImage.FromBitmap(skBitmap);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 90);
            using var ms = new MemoryStream(data.ToArray());
            using var sourceBitmap = new Bitmap(ms);

            uiPreview.Image?.Dispose();
            uiPreview.Image = new Bitmap(sourceBitmap);
        }
        catch (Exception ex)
        {
            uiPositionLabel.Text = $"Ошибка превью: {ex.Message}";
        }
    }

    private void OnApply()
    {
        if (string.IsNullOrEmpty(_templatePath) || !File.Exists(_templatePath))
        {
            MessageBox.Show("Выберите файл шаблона", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_layers.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы один слой текста", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = BuildTemplate();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowHelp()
    {
        DocumentationForm.ShowAppDoc(this, "Генератор обложек — справка", "covers.md");
    }

    private sealed class MutableLayer
    {
        public string TextTemplate { get; set; } = "{number}";
        public float TextX { get; set; } = 0.5f;
        public float TextY { get; set; } = 0.5f;
        public float FontSizeRatio { get; set; } = 0.25f;
        public string FontFamily { get; set; } = "Impact";
        public DrawingColor FillColor { get; set; } = DrawingColor.White;
        public DrawingColor StrokeColor { get; set; } = DrawingColor.Black;
        public float StrokeWidthRatio { get; set; } = 0.01f;

        public static MutableLayer FromDomain(CoverTextLayer layer)
        {
            return new()
            {
                TextTemplate = layer.TextTemplate,
                TextX = layer.TextX,
                TextY = layer.TextY,
                FontSizeRatio = layer.FontSizeRatio,
                FontFamily = layer.FontFamily,
                FillColor = DrawingColor.FromArgb(layer.FillColor.Red, layer.FillColor.Green, layer.FillColor.Blue),
                StrokeColor = DrawingColor.FromArgb(layer.StrokeColor.Red, layer.StrokeColor.Green, layer.StrokeColor.Blue),
                StrokeWidthRatio = layer.StrokeWidthRatio,
            };
        }

        public CoverTextLayer ToDomain()
        {
            return new(TextTemplate,
                TextX,
                TextY,
                FontSizeRatio,
                FontFamily,
                new(FillColor.R, FillColor.G, FillColor.B),
                new(StrokeColor.R, StrokeColor.G, StrokeColor.B),
                StrokeWidthRatio);
        }
    }
}
