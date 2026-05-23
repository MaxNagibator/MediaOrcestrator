using MediaOrcestrator.Domain;
using SkiaSharp;
using System.Text.RegularExpressions;
using DrawingColor = System.Drawing.Color;

namespace MediaOrcestrator.Runner;

public partial class CoverTemplateForm : Form
{
    private static readonly (string Label, CoverFontStyle Style)[] FontStyleOptions =
    [
        ("Обычный", CoverFontStyle.Regular),
        ("Жирный", CoverFontStyle.Bold),
        ("Курсив", CoverFontStyle.Italic),
        ("Жирный курсив", CoverFontStyle.BoldItalic),
    ];

    private readonly CoverGenerator _coverGenerator;
    private readonly CoverTemplateStore _store;

    private readonly List<MutableLayer> _layers = [];

    private string? _templatePath;
    private bool _suppressPreview;
    private bool _suppressLayerEdits;
    private int _prevSelectedLayerIndex = -1;
    private bool _isDraggingLayer;

    private string? _regexWarning;
    private string? _templateSizeWarning;

    public CoverTemplateForm()
    {
        _coverGenerator = null!;
        _store = null!;
        InitializeComponent();
        PopulateFontFamilies();
        PopulateFontStyles();
    }

    public CoverTemplateForm(CoverGenerator coverGenerator, CoverTemplateStore store, CoverTemplate? initial, string? initialProfileName = null) : this()
    {
        _coverGenerator = coverGenerator;
        _store = store;
        CurrentProfileName = initialProfileName;

        Text = FormatTitle(CurrentProfileName);

        if (initial != null)
        {
            ApplyInitial(initial);
        }
        else
        {
            _layers.Add(MutableLayer.FromDomain(CoverTemplate.DefaultNumberLayer));
            RefreshLayersList();
            uiLayersList.SelectedIndex = 0;
            uiTitleRegexTextBox.Text = CoverTemplate.DefaultTitleRegex;
        }

        UpdateNumberModeUi();
        UpdateSaveButtonState();
    }

    public string? CurrentProfileName { get; private set; }

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
        ValidateRegex();
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
        ValidateRegex();
        UpdatePreview();
    }

    private void uiStartNumber_ValueChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void uiTitleRegexTextBox_TextChanged(object? sender, EventArgs e)
    {
        ValidateRegex();
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

    private void uiFontStyle_SelectedIndexChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer => layer.FontStyle = GetSelectedFontStyle());
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

    private void uiFillAlpha_ValueChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer =>
        {
            var c = layer.FillColor;
            layer.FillColor = DrawingColor.FromArgb((int)uiFillAlpha.Value, c.R, c.G, c.B);
        });

        uiFillColorButton.Invalidate();
    }

    private void uiStrokeAlpha_ValueChanged(object? sender, EventArgs e)
    {
        OnLayerFieldChanged(layer =>
        {
            var c = layer.StrokeColor;
            layer.StrokeColor = DrawingColor.FromArgb((int)uiStrokeAlpha.Value, c.R, c.G, c.B);
        });

        uiStrokeColorButton.Invalidate();
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

    private void uiSaveProfileButton_Click(object? sender, EventArgs e)
    {
        if (SaveCurrentProfile())
        {
            Close();
        }
    }

    private void uiSaveAsProfileButton_Click(object? sender, EventArgs e)
    {
        if (SaveAsProfile())
        {
            Close();
        }
    }

    private void uiFontFamily_Leave(object? sender, EventArgs e)
    {
        var entered = uiFontFamily.Text;

        if (string.IsNullOrEmpty(entered))
        {
            return;
        }

        var idx = uiFontFamily.FindStringExact(entered);

        if (idx >= 0)
        {
            if (uiFontFamily.SelectedIndex != idx)
            {
                uiFontFamily.SelectedIndex = idx;
            }

            return;
        }

        var layer = GetSelectedLayer();
        var fallback = layer?.FontFamily ?? "Impact";
        var fallbackIdx = uiFontFamily.FindStringExact(fallback);
        uiFontFamily.SelectedIndex = fallbackIdx >= 0 ? fallbackIdx : Math.Max(0, uiFontFamily.Items.Count - 1);
    }

    private void uiFillColorButton_Paint(object? sender, PaintEventArgs e)
    {
        PaintColorSwatch(e, GetSelectedLayer()?.FillColor ?? DrawingColor.White);
    }

    private void uiStrokeColorButton_Paint(object? sender, PaintEventArgs e)
    {
        PaintColorSwatch(e, GetSelectedLayer()?.StrokeColor ?? DrawingColor.Black);
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

    private static void PaintColorSwatch(PaintEventArgs e, DrawingColor color)
    {
        var bounds = e.ClipRectangle;

        if (color.A < 255)
        {
            const int tile = 6;

            using var darkBrush = new SolidBrush(DrawingColor.FromArgb(204, 204, 204));
            using var lightBrush = new SolidBrush(DrawingColor.White);

            for (var y = bounds.Top; y < bounds.Bottom; y += tile)
            {
                for (var x = bounds.Left; x < bounds.Right; x += tile)
                {
                    var brush = (x / tile + y / tile & 1) == 0 ? lightBrush : darkBrush;
                    var width = Math.Min(tile, bounds.Right - x);
                    var height = Math.Min(tile, bounds.Bottom - y);
                    e.Graphics.FillRectangle(brush, x, y, width, height);
                }
            }
        }

        using var paintBrush = new SolidBrush(color);
        e.Graphics.FillRectangle(paintBrush, bounds);
    }

    private static CoverFontStyle GetFontStyleAt(int index)
    {
        return index >= 0 && index < FontStyleOptions.Length ? FontStyleOptions[index].Style : CoverFontStyle.Bold;
    }

    private static int IndexOfFontStyle(CoverFontStyle style)
    {
        for (var i = 0; i < FontStyleOptions.Length; i++)
        {
            if (FontStyleOptions[i].Style == style)
            {
                return i;
            }
        }

        return IndexOfFontStyle(CoverFontStyle.Bold);
    }

    private void PopulateFontFamilies()
    {
        uiFontFamily.Items.Clear();

        foreach (var family in SKFontManager.Default.GetFontFamilies()
                     .Where(f => !string.IsNullOrWhiteSpace(f))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            uiFontFamily.Items.Add(family);
        }
    }

    private void PopulateFontStyles()
    {
        uiFontStyle.Items.Clear();

        foreach (var (label, _) in FontStyleOptions)
        {
            uiFontStyle.Items.Add(label);
        }
    }

    private CoverFontStyle GetSelectedFontStyle()
    {
        return GetFontStyleAt(uiFontStyle.SelectedIndex);
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
        UpdateTemplateSizeWarning(initial.TemplatePath);
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
        ValidateRegex();
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

        uiFontStyle.SelectedIndex = IndexOfFontStyle(layer.FontStyle);
        uiFontSize.Value = (decimal)Math.Clamp(layer.FontSizeRatio * 100f, (float)uiFontSize.Minimum, (float)uiFontSize.Maximum);
        uiStrokeWidth.Value = (decimal)Math.Clamp(layer.StrokeWidthRatio * 100f, (float)uiStrokeWidth.Minimum, (float)uiStrokeWidth.Maximum);
        uiFillAlpha.Value = layer.FillColor.A;
        uiStrokeAlpha.Value = layer.StrokeColor.A;
        uiFillColorButton.Invalidate();
        uiStrokeColorButton.Invalidate();
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
        UpdateTemplateSizeWarning(dialog.FileName);
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

    private void UpdateTemplateSizeWarning(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            _templateSizeWarning = null;
            RefreshWarning();
            return;
        }

        try
        {
            using var codec = SKCodec.Create(path);

            if (codec == null)
            {
                _templateSizeWarning = null;
            }
            else
            {
                var info = codec.Info;
                _templateSizeWarning = info.Width < 1280 || info.Height < 720
                    ? $"Шаблон {info.Width}×{info.Height} — мал для большинства площадок (рекомендуется ≥ 1280×720)."
                    : null;
            }
        }
        catch
        {
            _templateSizeWarning = null;
        }

        RefreshWarning();
    }

    private void ValidateRegex()
    {
        if (!uiTitleRegexRadio.Checked)
        {
            _regexWarning = null;
            RefreshWarning();
            return;
        }

        var pattern = string.IsNullOrWhiteSpace(uiTitleRegexTextBox.Text)
            ? CoverTemplate.DefaultTitleRegex
            : uiTitleRegexTextBox.Text;

        try
        {
            _ = new Regex(pattern);
            _regexWarning = null;
        }
        catch (ArgumentException ex)
        {
            _regexWarning = "Невалидный regex: " + ex.Message;
        }

        RefreshWarning();
    }

    private void RefreshWarning()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(_templateSizeWarning))
        {
            parts.Add(_templateSizeWarning);
        }

        if (!string.IsNullOrEmpty(_regexWarning))
        {
            parts.Add(_regexWarning);
        }

        if (parts.Count == 0)
        {
            uiWarningLabel.Visible = false;
            uiWarningLabel.Text = string.Empty;
        }
        else
        {
            uiWarningLabel.Text = string.Join(Environment.NewLine, parts);
            uiWarningLabel.Visible = true;
        }

        UpdateSaveButtonState();
    }

    private bool SaveCurrentProfile()
    {
        if (string.IsNullOrEmpty(CurrentProfileName))
        {
            return false;
        }

        if (!CanSaveTemplate(out var template))
        {
            return false;
        }

        if (!_store.Save(CurrentProfileName, template))
        {
            MessageBox.Show($"Не удалось сохранить профиль '{CurrentProfileName}'. Подробности в логах.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    private bool SaveAsProfile()
    {
        using var dialog = new InputDialog("Имя профиля:", "Сохранение профиля", CurrentProfileName ?? string.Empty);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        var rawName = dialog.InputText?.Trim();

        if (string.IsNullOrEmpty(rawName))
        {
            return false;
        }

        var name = _store.Sanitize(rawName);

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Имя профиля содержит только запрещённые символы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!string.Equals(name, rawName, StringComparison.Ordinal))
        {
            MessageBox.Show($"Имя приведено к '{name}' (убраны запрещённые в имени файла символы).", "Имя профиля", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        if (!CanSaveTemplate(out var template))
        {
            return false;
        }

        if (_store.Exists(name))
        {
            var confirm = MessageBox.Show($"Профиль '{name}' уже существует. Перезаписать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return false;
            }
        }

        if (!_store.Save(name, template))
        {
            MessageBox.Show($"Не удалось сохранить профиль '{name}'. Подробности в логах.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        CurrentProfileName = name;
        Text = FormatTitle(CurrentProfileName);
        UpdateSaveButtonState();
        return true;
    }

    private bool CanSaveTemplate(out CoverTemplate template)
    {
        template = null!;

        if (string.IsNullOrEmpty(_templatePath) || !File.Exists(_templatePath))
        {
            MessageBox.Show("Выберите файл шаблона", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_layers.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы один слой текста", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!string.IsNullOrEmpty(_regexWarning))
        {
            MessageBox.Show(_regexWarning, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        template = BuildTemplate();
        return true;
    }

    private void UpdateSaveButtonState()
    {
        uiSaveProfileButton.Enabled = !string.IsNullOrEmpty(CurrentProfileName) && string.IsNullOrEmpty(_regexWarning);
    }

    private void PickColor(bool fill)
    {
        var layer = GetSelectedLayer();

        if (layer == null)
        {
            return;
        }

        var current = fill ? layer.FillColor : layer.StrokeColor;
        var existingAlpha = current.A;

        using var dialog = new ColorDialog
        {
            Color = DrawingColor.FromArgb(current.R, current.G, current.B),
            FullOpen = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var picked = dialog.Color;
        var withAlpha = DrawingColor.FromArgb(existingAlpha, picked.R, picked.G, picked.B);

        if (fill)
        {
            layer.FillColor = withAlpha;
            uiFillColorButton.Invalidate();
        }
        else
        {
            layer.StrokeColor = withAlpha;
            uiStrokeColorButton.Invalidate();
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

            uiPreview.Image?.Dispose();
            uiPreview.Image = SkiaInterop.ToBitmap(skBitmap);
        }
        catch (Exception ex)
        {
            uiPositionLabel.Text = $"Ошибка превью: {ex.Message}";
        }
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
        public CoverFontStyle FontStyle { get; set; } = CoverFontStyle.Bold;
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
                FontStyle = layer.FontStyle,
                FillColor = DrawingColor.FromArgb(layer.FillColor.Alpha, layer.FillColor.Red, layer.FillColor.Green, layer.FillColor.Blue),
                StrokeColor = DrawingColor.FromArgb(layer.StrokeColor.Alpha, layer.StrokeColor.Red, layer.StrokeColor.Green, layer.StrokeColor.Blue),
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
                FontStyle,
                new(FillColor.R, FillColor.G, FillColor.B, FillColor.A),
                new(StrokeColor.R, StrokeColor.G, StrokeColor.B, StrokeColor.A),
                StrokeWidthRatio);
        }
    }
}
