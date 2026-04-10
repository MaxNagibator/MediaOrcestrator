using MediaOrcestrator.Domain;
using SkiaSharp;
using DrawingColor = System.Drawing.Color;

namespace MediaOrcestrator.Runner;

public sealed class CoverTemplateForm : Form
{
    private readonly CoverGenerator _coverGenerator;
    private readonly CoverTemplateStore _store;

    private readonly ComboBox _uiProfilesCombo;
    private readonly TextBox _uiTemplatePathTextBox;
    private readonly PictureBox _uiPreview;
    private readonly NumericUpDown _uiStartNumber;
    private readonly NumericUpDown _uiSampleNumber;
    private readonly RadioButton _uiSequentialRadio;
    private readonly RadioButton _uiTitleRegexRadio;
    private readonly TextBox _uiTitleRegexTextBox;

    private readonly ListBox _uiLayersList;
    private readonly Button _uiRemoveLayerButton;

    private readonly GroupBox _uiLayerGroup;
    private readonly TextBox _uiLayerTextBox;
    private readonly ComboBox _uiFontFamily;
    private readonly NumericUpDown _uiFontSize;
    private readonly NumericUpDown _uiStrokeWidth;
    private readonly Button _uiFillColorButton;
    private readonly Button _uiStrokeColorButton;
    private readonly Label _uiPositionLabel;

    private readonly List<MutableLayer> _layers = [];

    private string? _templatePath;
    private bool _suppressPreview;
    private bool _suppressLayerEdits;
    private int _prevSelectedLayerIndex = -1;

    public CoverTemplateForm(CoverGenerator coverGenerator, CoverTemplateStore store, CoverTemplate? initial)
    {
        _coverGenerator = coverGenerator;
        _store = store;

        Text = "Шаблон обложки";
        Size = new(1080, 760);
        MinimumSize = new(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(10),
            ColumnCount = 2,
            RowCount = 2,
        };

        rootLayout.ColumnStyles.Add(new(SizeType.Percent, 58));
        rootLayout.ColumnStyles.Add(new(SizeType.Percent, 42));
        rootLayout.RowStyles.Add(new(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new(SizeType.AutoSize));

        var previewGroup = new GroupBox
        {
            Text = "Превью (клик — задать позицию выбранного слоя)",
            Dock = DockStyle.Fill,
        };

        var previewLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };

        previewLayout.RowStyles.Add(new(SizeType.Percent, 100));
        previewLayout.RowStyles.Add(new(SizeType.AutoSize));

        _uiPreview = new()
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = DrawingColor.Black,
            Cursor = Cursors.Cross,
        };

        _uiPreview.MouseDown += OnPreviewMouseDown;

        _uiPositionLabel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Text = "Слой не выбран",
        };

        previewLayout.Controls.Add(_uiPreview, 0, 0);
        previewLayout.Controls.Add(_uiPositionLabel, 0, 1);
        previewGroup.Controls.Add(previewLayout);
        rootLayout.Controls.Add(previewGroup, 0, 0);

        var settingsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoScroll = true,
        };

        settingsPanel.RowStyles.Add(new(SizeType.AutoSize));
        settingsPanel.RowStyles.Add(new(SizeType.AutoSize));
        settingsPanel.RowStyles.Add(new(SizeType.AutoSize));
        settingsPanel.RowStyles.Add(new(SizeType.Percent, 100));

        // ── Группа: профили ──────────────────────────────────────────
        var profilesGroup = new GroupBox
        {
            Text = "Профиль",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };

        var profilesLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(8),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };

        _uiProfilesCombo = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180,
        };

        var uiLoadProfileButton = new Button
        {
            Text = "Загрузить",
            AutoSize = true,
        };

        uiLoadProfileButton.Click += (_, _) => LoadSelectedProfile();

        var uiSaveAsProfileButton = new Button
        {
            Text = "Сохранить как...",
            AutoSize = true,
        };

        uiSaveAsProfileButton.Click += (_, _) => SaveAsProfile();

        var uiDeleteProfileButton = new Button
        {
            Text = "Удалить",
            AutoSize = true,
        };

        uiDeleteProfileButton.Click += (_, _) => DeleteSelectedProfile();

        profilesLayout.Controls.Add(_uiProfilesCombo);
        profilesLayout.Controls.Add(uiLoadProfileButton);
        profilesLayout.Controls.Add(uiSaveAsProfileButton);
        profilesLayout.Controls.Add(uiDeleteProfileButton);
        profilesGroup.Controls.Add(profilesLayout);
        settingsPanel.Controls.Add(profilesGroup, 0, 0);

        // ── Группа: шаблон и нумерация ───────────────────────────────
        var templateGroup = new GroupBox
        {
            Text = "Шаблон и нумерация",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };

        var templateLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new(8),
            AutoSize = true,
        };

        templateLayout.ColumnStyles.Add(new(SizeType.AutoSize));
        templateLayout.ColumnStyles.Add(new(SizeType.Percent, 100));

        for (var i = 0; i < 7; i++)
        {
            templateLayout.RowStyles.Add(new(SizeType.AutoSize));
        }

        templateLayout.Controls.Add(new Label { Text = "Файл:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
        };

        pathPanel.ColumnStyles.Add(new(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new(SizeType.AutoSize));

        _uiTemplatePathTextBox = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
        };

        var uiBrowseButton = new Button
        {
            Text = "Обзор...",
            AutoSize = true,
        };

        uiBrowseButton.Click += (_, _) => BrowseTemplate();
        pathPanel.Controls.Add(_uiTemplatePathTextBox, 0, 0);
        pathPanel.Controls.Add(uiBrowseButton, 1, 0);
        templateLayout.Controls.Add(pathPanel, 1, 0);

        templateLayout.Controls.Add(new Label { Text = "Режим №:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);

        var modeLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };

        _uiSequentialRadio = new()
        {
            Text = "Последовательно",
            AutoSize = true,
            Checked = true,
        };

        _uiTitleRegexRadio = new()
        {
            Text = "Из названия",
            AutoSize = true,
        };

        _uiSequentialRadio.CheckedChanged += (_, _) =>
        {
            if (!_uiSequentialRadio.Checked)
            {
                return;
            }

            _uiTitleRegexRadio.Checked = false;
            UpdateNumberModeUi();
            UpdatePreview();
        };

        _uiTitleRegexRadio.CheckedChanged += (_, _) =>
        {
            if (!_uiTitleRegexRadio.Checked)
            {
                return;
            }

            _uiSequentialRadio.Checked = false;
            UpdateNumberModeUi();
            UpdatePreview();
        };

        modeLayout.Controls.Add(_uiSequentialRadio);
        modeLayout.Controls.Add(_uiTitleRegexRadio);
        templateLayout.Controls.Add(modeLayout, 1, 1);

        templateLayout.Controls.Add(new Label { Text = "Начальный №:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);

        _uiStartNumber = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 99999,
            Value = 1,
        };

        _uiStartNumber.ValueChanged += (_, _) => UpdatePreview();
        templateLayout.Controls.Add(_uiStartNumber, 1, 2);

        templateLayout.Controls.Add(new Label { Text = "Regex:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);

        _uiTitleRegexTextBox = new()
        {
            Dock = DockStyle.Fill,
            Text = CoverTemplate.DefaultTitleRegex,
        };

        _uiTitleRegexTextBox.TextChanged += (_, _) => UpdatePreview();
        templateLayout.Controls.Add(_uiTitleRegexTextBox, 1, 3);

        templateLayout.Controls.Add(new Label { Text = "Образец №:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);

        _uiSampleNumber = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 99999,
            Value = 12,
        };

        _uiSampleNumber.ValueChanged += (_, _) => UpdatePreview();
        templateLayout.Controls.Add(_uiSampleNumber, 1, 4);

        var uiHintLabel = new Label
        {
            Text = "В тексте слоя пишите {number} — будет подставлен расчётный номер.",
            AutoSize = true,
            ForeColor = DrawingColor.Gray,
            MaximumSize = new(360, 0),
        };

        templateLayout.SetColumnSpan(uiHintLabel, 2);
        templateLayout.Controls.Add(uiHintLabel, 0, 5);

        templateGroup.Controls.Add(templateLayout);
        settingsPanel.Controls.Add(templateGroup, 0, 1);

        // ── Группа: список слоёв ─────────────────────────────────────
        var layersGroup = new GroupBox
        {
            Text = "Слои",
            Dock = DockStyle.Fill,
            Height = 130,
        };

        var layersLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(8),
            ColumnCount = 2,
            RowCount = 1,
        };

        layersLayout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layersLayout.ColumnStyles.Add(new(SizeType.AutoSize));

        _uiLayersList = new()
        {
            Dock = DockStyle.Fill,
        };

        _uiLayersList.SelectedIndexChanged += (_, _) => OnLayerSelectionChanged();

        var layersButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
        };

        var uiAddLayerButton = new Button
        {
            Text = "Добавить",
            AutoSize = true,
            Width = 90,
        };

        uiAddLayerButton.Click += (_, _) => AddLayer();

        _uiRemoveLayerButton = new()
        {
            Text = "Удалить",
            AutoSize = true,
            Width = 90,
            Enabled = false,
        };

        _uiRemoveLayerButton.Click += (_, _) => RemoveSelectedLayer();

        layersButtons.Controls.Add(uiAddLayerButton);
        layersButtons.Controls.Add(_uiRemoveLayerButton);

        layersLayout.Controls.Add(_uiLayersList, 0, 0);
        layersLayout.Controls.Add(layersButtons, 1, 0);
        layersGroup.Controls.Add(layersLayout);
        settingsPanel.Controls.Add(layersGroup, 0, 2);

        // ── Группа: редактор текущего слоя ───────────────────────────
        _uiLayerGroup = new()
        {
            Text = "Текущий слой",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false,
        };

        var layerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(8),
            ColumnCount = 2,
            RowCount = 6,
            AutoSize = true,
        };

        layerLayout.ColumnStyles.Add(new(SizeType.AutoSize));
        layerLayout.ColumnStyles.Add(new(SizeType.Percent, 100));

        for (var i = 0; i < 6; i++)
        {
            layerLayout.RowStyles.Add(new(SizeType.AutoSize));
        }

        layerLayout.Controls.Add(new Label { Text = "Текст:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);

        _uiLayerTextBox = new()
        {
            Dock = DockStyle.Fill,
            Text = "{number}",
        };

        _uiLayerTextBox.TextChanged += (_, _) => OnLayerFieldChanged(layer => layer.TextTemplate = _uiLayerTextBox.Text);
        // Ярлык в ListBox обновляем только когда редактирование завершено — иначе ListBox.Items[idx] = ...
        // пересоздаёт элемент, сбрасывает выделение и крадёт фокус с текстбокса на каждом нажатии.
        _uiLayerTextBox.Leave += (_, _) => RefreshLayerListLabel(_uiLayersList.SelectedIndex);
        layerLayout.Controls.Add(_uiLayerTextBox, 1, 0);

        layerLayout.Controls.Add(new Label { Text = "Шрифт:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);

        _uiFontFamily = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };

        foreach (var family in FontFamily.Families.OrderBy(f => f.Name))
        {
            _uiFontFamily.Items.Add(family.Name);
        }

        _uiFontFamily.SelectedIndexChanged += (_, _) => OnLayerFieldChanged(layer => layer.FontFamily = _uiFontFamily.SelectedItem?.ToString() ?? "Arial");
        layerLayout.Controls.Add(_uiFontFamily, 1, 1);

        layerLayout.Controls.Add(new Label { Text = "Размер (% выс.):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);

        _uiFontSize = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 25m,
        };

        _uiFontSize.ValueChanged += (_, _) => OnLayerFieldChanged(layer => layer.FontSizeRatio = (float)_uiFontSize.Value / 100f);
        layerLayout.Controls.Add(_uiFontSize, 1, 2);

        layerLayout.Controls.Add(new Label { Text = "Обводка (% выс.):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);

        _uiStrokeWidth = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 10,
            DecimalPlaces = 2,
            Increment = 0.1m,
            Value = 1.0m,
        };

        _uiStrokeWidth.ValueChanged += (_, _) => OnLayerFieldChanged(layer => layer.StrokeWidthRatio = (float)_uiStrokeWidth.Value / 100f);
        layerLayout.Controls.Add(_uiStrokeWidth, 1, 3);

        layerLayout.Controls.Add(new Label { Text = "Заливка:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);

        _uiFillColorButton = new()
        {
            Dock = DockStyle.Fill,
            Text = "",
            Height = 26,
            BackColor = DrawingColor.White,
            FlatStyle = FlatStyle.Flat,
        };

        _uiFillColorButton.Click += (_, _) => PickColor(true);
        layerLayout.Controls.Add(_uiFillColorButton, 1, 4);

        layerLayout.Controls.Add(new Label { Text = "Цвет обводки:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);

        _uiStrokeColorButton = new()
        {
            Dock = DockStyle.Fill,
            Text = "",
            Height = 26,
            BackColor = DrawingColor.Black,
            FlatStyle = FlatStyle.Flat,
        };

        _uiStrokeColorButton.Click += (_, _) => PickColor(false);
        layerLayout.Controls.Add(_uiStrokeColorButton, 1, 5);

        _uiLayerGroup.Controls.Add(layerLayout);
        settingsPanel.Controls.Add(_uiLayerGroup, 0, 3);

        rootLayout.Controls.Add(settingsPanel, 1, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new(0, 8, 0, 0),
        };

        var uiCancelButton = new Button
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
        };

        var uiOkButton = new Button { Text = "OK" };
        uiOkButton.Click += (_, _) => OnApply();

        buttonPanel.Controls.Add(uiCancelButton);
        buttonPanel.Controls.Add(uiOkButton);

        rootLayout.SetColumnSpan(buttonPanel, 2);
        rootLayout.Controls.Add(buttonPanel, 0, 1);

        Controls.Add(rootLayout);
        CancelButton = uiCancelButton;
        AcceptButton = uiOkButton;

        if (initial != null)
        {
            ApplyInitial(initial);
        }
        else
        {
            // Дефолтный шаблон с одним слоем-номером.
            _layers.Add(MutableLayer.FromDomain(CoverTemplate.DefaultNumberLayer));
            RefreshLayersList();
            _uiLayersList.SelectedIndex = 0;
        }

        UpdateNumberModeUi();
        PopulateProfiles();
    }

    public CoverTemplate? Result { get; private set; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _uiPreview.Image?.Dispose();
        _uiPreview.Image = null;
        base.OnFormClosed(e);
    }

    private void OnPreviewMouseDown(object? sender, MouseEventArgs e)
    {
        if (_uiPreview.Image == null)
        {
            return;
        }

        var layer = GetSelectedLayer();

        if (layer == null)
        {
            return;
        }

        var rect = GetZoomedImageRect();

        if (!rect.Contains(e.Location))
        {
            return;
        }

        layer.TextX = Math.Clamp((e.X - rect.X) / rect.Width, 0f, 1f);
        layer.TextY = Math.Clamp((e.Y - rect.Y) / rect.Height, 0f, 1f);
        UpdatePositionLabel();
        UpdatePreview();
    }

    private void ApplyInitial(CoverTemplate initial)
    {
        _suppressPreview = true;
        _suppressLayerEdits = true;

        _templatePath = initial.TemplatePath;
        _uiTemplatePathTextBox.Text = initial.TemplatePath;
        _uiStartNumber.Value = Math.Clamp(initial.StartNumber, (int)_uiStartNumber.Minimum, (int)_uiStartNumber.Maximum);
        _uiSampleNumber.Value = Math.Clamp(initial.StartNumber, (int)_uiSampleNumber.Minimum, (int)_uiSampleNumber.Maximum);
        _uiTitleRegexTextBox.Text = string.IsNullOrWhiteSpace(initial.TitleRegexPattern) ? CoverTemplate.DefaultTitleRegex : initial.TitleRegexPattern;
        _uiSequentialRadio.Checked = initial.NumberMode == CoverNumberMode.Sequential;
        _uiTitleRegexRadio.Checked = initial.NumberMode == CoverNumberMode.TitleRegex;

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

        if (_uiLayersList.Items.Count > 0)
        {
            _uiLayersList.SelectedIndex = 0;
        }

        UpdateNumberModeUi();
        UpdatePreview();
    }

    private void UpdateNumberModeUi()
    {
        var sequential = _uiSequentialRadio.Checked;
        _uiStartNumber.Enabled = sequential;
        _uiTitleRegexTextBox.Enabled = !sequential;
    }

    private void OnLayerSelectionChanged()
    {
        var currentIndex = _uiLayersList.SelectedIndex;

        // Обновляем ярлык у слоя, с которого ушли — вдруг в текстбоксе были несохранённые правки.
        if (_prevSelectedLayerIndex >= 0 && _prevSelectedLayerIndex != currentIndex)
        {
            RefreshLayerListLabel(_prevSelectedLayerIndex);
        }

        _prevSelectedLayerIndex = currentIndex;

        var layer = GetSelectedLayer();

        if (layer == null)
        {
            _uiLayerGroup.Enabled = false;
            _uiRemoveLayerButton.Enabled = false;
            _uiPositionLabel.Text = "Слой не выбран";
            return;
        }

        _uiLayerGroup.Enabled = true;
        _uiRemoveLayerButton.Enabled = _layers.Count > 1;

        _suppressLayerEdits = true;
        _uiLayerTextBox.Text = layer.TextTemplate;

        var familyIndex = _uiFontFamily.Items.IndexOf(layer.FontFamily);
        _uiFontFamily.SelectedIndex = familyIndex >= 0 ? familyIndex : Math.Max(0, _uiFontFamily.Items.IndexOf("Impact"));

        _uiFontSize.Value = (decimal)Math.Clamp(layer.FontSizeRatio * 100f, (float)_uiFontSize.Minimum, (float)_uiFontSize.Maximum);
        _uiStrokeWidth.Value = (decimal)Math.Clamp(layer.StrokeWidthRatio * 100f, (float)_uiStrokeWidth.Minimum, (float)_uiStrokeWidth.Maximum);
        _uiFillColorButton.BackColor = layer.FillColor;
        _uiStrokeColorButton.BackColor = layer.StrokeColor;
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
        var idx = _uiLayersList.SelectedIndex;
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
        _uiLayersList.SelectedIndex = _layers.Count - 1;
        UpdatePreview();
    }

    private void RemoveSelectedLayer()
    {
        var idx = _uiLayersList.SelectedIndex;

        if (idx < 0 || _layers.Count <= 1)
        {
            return;
        }

        _layers.RemoveAt(idx);
        RefreshLayersList();
        _uiLayersList.SelectedIndex = Math.Min(idx, _layers.Count - 1);
        UpdatePreview();
    }

    private void RefreshLayersList()
    {
        var preserved = _uiLayersList.SelectedIndex;
        _uiLayersList.BeginUpdate();
        _uiLayersList.Items.Clear();

        for (var i = 0; i < _layers.Count; i++)
        {
            _uiLayersList.Items.Add($"{i + 1}. {_layers[i].TextTemplate}");
        }

        _uiLayersList.EndUpdate();

        if (preserved >= 0 && preserved < _layers.Count)
        {
            _uiLayersList.SelectedIndex = preserved;
        }
    }

    private void RefreshLayerListLabel(int idx)
    {
        if (idx < 0 || idx >= _layers.Count || idx >= _uiLayersList.Items.Count)
        {
            return;
        }

        var newLabel = $"{idx + 1}. {_layers[idx].TextTemplate}";

        if (Equals(_uiLayersList.Items[idx], newLabel))
        {
            return;
        }

        _uiLayersList.Items[idx] = newLabel;
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
        _uiTemplatePathTextBox.Text = dialog.FileName;
        UpdatePreview();
    }

    private void PopulateProfiles(string? select = null)
    {
        _uiProfilesCombo.Items.Clear();

        foreach (var name in _store.List())
        {
            _uiProfilesCombo.Items.Add(name);
        }

        if (!string.IsNullOrEmpty(select))
        {
            var idx = _uiProfilesCombo.Items.IndexOf(select);

            if (idx >= 0)
            {
                _uiProfilesCombo.SelectedIndex = idx;
            }
        }
    }

    private void LoadSelectedProfile()
    {
        var name = _uiProfilesCombo.SelectedItem?.ToString();

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

        ApplyInitial(loaded);
    }

    private void SaveAsProfile()
    {
        using var dialog = new InputDialog("Имя профиля:", "Сохранение профиля", _uiProfilesCombo.Text);

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

        if (_uiProfilesCombo.Items.Contains(name))
        {
            var confirm = MessageBox.Show($"Профиль '{name}' уже существует. Перезаписать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        _store.Save(name, BuildTemplate());
        PopulateProfiles(name);
    }

    private void DeleteSelectedProfile()
    {
        var name = _uiProfilesCombo.SelectedItem?.ToString();

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
            _uiFillColorButton.BackColor = dialog.Color;
        }
        else
        {
            layer.StrokeColor = dialog.Color;
            _uiStrokeColorButton.BackColor = dialog.Color;
        }

        UpdatePreview();
    }

    private void UpdatePositionLabel()
    {
        var layer = GetSelectedLayer();
        _uiPositionLabel.Text = layer == null
            ? "Слой не выбран"
            : $"Позиция слоя: X={layer.TextX:F2}, Y={layer.TextY:F2}";
    }

    private RectangleF GetZoomedImageRect()
    {
        if (_uiPreview.Image == null)
        {
            return RectangleF.Empty;
        }

        var imgW = (float)_uiPreview.Image.Width;
        var imgH = (float)_uiPreview.Image.Height;
        var ctrlW = (float)_uiPreview.ClientSize.Width;
        var ctrlH = (float)_uiPreview.ClientSize.Height;

        var scale = Math.Min(ctrlW / imgW, ctrlH / imgH);
        var w = imgW * scale;
        var h = imgH * scale;
        var x = (ctrlW - w) / 2f;
        var y = (ctrlH - h) / 2f;
        return new(x, y, w, h);
    }

    private CoverTemplate BuildTemplate()
    {
        var mode = _uiTitleRegexRadio.Checked ? CoverNumberMode.TitleRegex : CoverNumberMode.Sequential;

        return new(_templatePath ?? string.Empty,
            (int)_uiStartNumber.Value,
            mode,
            _uiTitleRegexTextBox.Text,
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
            _uiPreview.Image?.Dispose();
            _uiPreview.Image = null;
            return;
        }

        try
        {
            var template = BuildTemplate();
            using var skBitmap = _coverGenerator.Render(template, (int)_uiSampleNumber.Value);
            using var skImage = SKImage.FromBitmap(skBitmap);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 90);
            using var ms = new MemoryStream(data.ToArray());
            using var sourceBitmap = new Bitmap(ms);

            _uiPreview.Image?.Dispose();
            _uiPreview.Image = new Bitmap(sourceBitmap);
        }
        catch (Exception ex)
        {
            _uiPositionLabel.Text = $"Ошибка превью: {ex.Message}";
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
