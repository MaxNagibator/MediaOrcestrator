using MediaOrcestrator.Domain;
using SkiaSharp;
using DrawingColor = System.Drawing.Color;

namespace MediaOrcestrator.Runner;

public sealed class CoverTemplateForm : Form
{
    private readonly CoverGenerator _coverGenerator;

    private readonly TextBox _uiTemplatePathTextBox;
    private readonly PictureBox _uiPreview;
    private readonly NumericUpDown _uiStartNumber;
    private readonly NumericUpDown _uiSampleNumber;
    private readonly NumericUpDown _uiFontSize;
    private readonly NumericUpDown _uiStrokeWidth;
    private readonly ComboBox _uiFontFamily;
    private readonly Button _uiFillColorButton;
    private readonly Button _uiStrokeColorButton;
    private readonly Label _uiPositionLabel;

    private string? _templatePath;
    private float _textX = 0.5f;
    private float _textY = 0.5f;
    private DrawingColor _fillColor = DrawingColor.White;
    private DrawingColor _strokeColor = DrawingColor.Black;
    private bool _suppressPreview;

    public CoverTemplateForm(CoverGenerator coverGenerator, CoverTemplate? initial)
    {
        _coverGenerator = coverGenerator;

        Text = "Шаблон обложки";
        Size = new(960, 620);
        MinimumSize = new(780, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(10),
            ColumnCount = 2,
            RowCount = 2,
        };

        rootLayout.ColumnStyles.Add(new(SizeType.Percent, 60));
        rootLayout.ColumnStyles.Add(new(SizeType.Percent, 40));
        rootLayout.RowStyles.Add(new(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new(SizeType.AutoSize));

        var previewGroup = new GroupBox
        {
            Text = "Превью (клик — задать позицию номера)",
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
            Text = "Позиция: X=0.50, Y=0.50",
        };

        previewLayout.Controls.Add(_uiPreview, 0, 0);
        previewLayout.Controls.Add(_uiPositionLabel, 0, 1);
        previewGroup.Controls.Add(previewLayout);
        rootLayout.Controls.Add(previewGroup, 0, 0);

        var settingsGroup = new GroupBox
        {
            Text = "Параметры",
            Dock = DockStyle.Fill,
        };

        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(8),
            ColumnCount = 2,
            RowCount = 9,
            AutoSize = true,
        };

        settingsLayout.ColumnStyles.Add(new(SizeType.AutoSize));
        settingsLayout.ColumnStyles.Add(new(SizeType.Percent, 100));

        for (var i = 0; i < 9; i++)
        {
            settingsLayout.RowStyles.Add(new(SizeType.AutoSize));
        }

        settingsLayout.Controls.Add(new Label { Text = "Файл шаблона:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);

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
        settingsLayout.Controls.Add(pathPanel, 1, 0);

        settingsLayout.Controls.Add(new Label { Text = "Начальный №:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);

        _uiStartNumber = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 99999,
            Value = 1,
        };

        _uiStartNumber.ValueChanged += (_, _) => UpdatePreview();
        settingsLayout.Controls.Add(_uiStartNumber, 1, 1);

        settingsLayout.Controls.Add(new Label { Text = "Образец №:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);

        _uiSampleNumber = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 99999,
            Value = 12,
        };

        _uiSampleNumber.ValueChanged += (_, _) => UpdatePreview();
        settingsLayout.Controls.Add(_uiSampleNumber, 1, 2);

        settingsLayout.Controls.Add(new Label { Text = "Шрифт:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);

        _uiFontFamily = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };

        foreach (var family in FontFamily.Families.OrderBy(f => f.Name))
        {
            _uiFontFamily.Items.Add(family.Name);
        }

        var defaultFont = _uiFontFamily.Items.IndexOf("Impact");

        if (defaultFont < 0)
        {
            defaultFont = _uiFontFamily.Items.IndexOf("Arial Black");
        }

        _uiFontFamily.SelectedIndex = defaultFont >= 0 ? defaultFont : 0;
        _uiFontFamily.SelectedIndexChanged += (_, _) => UpdatePreview();
        settingsLayout.Controls.Add(_uiFontFamily, 1, 3);

        settingsLayout.Controls.Add(new Label { Text = "Размер шрифта (% выс.):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);

        _uiFontSize = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 25m,
        };

        _uiFontSize.ValueChanged += (_, _) => UpdatePreview();
        settingsLayout.Controls.Add(_uiFontSize, 1, 4);

        settingsLayout.Controls.Add(new Label { Text = "Толщина обводки (% выс.):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);

        _uiStrokeWidth = new()
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 10,
            DecimalPlaces = 2,
            Increment = 0.1m,
            Value = 1.0m,
        };

        _uiStrokeWidth.ValueChanged += (_, _) => UpdatePreview();
        settingsLayout.Controls.Add(_uiStrokeWidth, 1, 5);

        settingsLayout.Controls.Add(new Label { Text = "Цвет заливки:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);

        _uiFillColorButton = new()
        {
            Dock = DockStyle.Fill,
            Text = "",
            Height = 26,
            BackColor = _fillColor,
            FlatStyle = FlatStyle.Flat,
        };

        _uiFillColorButton.Click += (_, _) => PickColor(true);
        settingsLayout.Controls.Add(_uiFillColorButton, 1, 6);

        settingsLayout.Controls.Add(new Label { Text = "Цвет обводки:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 7);

        _uiStrokeColorButton = new()
        {
            Dock = DockStyle.Fill,
            Text = "",
            Height = 26,
            BackColor = _strokeColor,
            FlatStyle = FlatStyle.Flat,
        };

        _uiStrokeColorButton.Click += (_, _) => PickColor(false);
        settingsLayout.Controls.Add(_uiStrokeColorButton, 1, 7);

        var uiHintLabel = new Label
        {
            Text = "Размеры в % от высоты картинки — обложка корректно отрисуется на любом разрешении.",
            AutoSize = true,
            ForeColor = DrawingColor.Gray,
            MaximumSize = new(280, 0),
        };

        settingsLayout.Controls.Add(uiHintLabel, 1, 8);

        settingsGroup.Controls.Add(settingsLayout);
        rootLayout.Controls.Add(settingsGroup, 1, 0);

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

        var rect = GetZoomedImageRect();

        if (!rect.Contains(e.Location))
        {
            return;
        }

        _textX = Math.Clamp((e.X - rect.X) / rect.Width, 0f, 1f);
        _textY = Math.Clamp((e.Y - rect.Y) / rect.Height, 0f, 1f);
        UpdatePreview();
    }

    private void ApplyInitial(CoverTemplate initial)
    {
        _suppressPreview = true;
        _templatePath = initial.TemplatePath;
        _uiTemplatePathTextBox.Text = initial.TemplatePath;
        _textX = initial.TextX;
        _textY = initial.TextY;
        _uiStartNumber.Value = Math.Clamp(initial.StartNumber, (int)_uiStartNumber.Minimum, (int)_uiStartNumber.Maximum);
        _uiSampleNumber.Value = Math.Clamp(initial.StartNumber, (int)_uiSampleNumber.Minimum, (int)_uiSampleNumber.Maximum);

        var familyIndex = _uiFontFamily.Items.IndexOf(initial.FontFamily);

        if (familyIndex >= 0)
        {
            _uiFontFamily.SelectedIndex = familyIndex;
        }

        _uiFontSize.Value = (decimal)Math.Clamp(initial.FontSizeRatio * 100f, (float)_uiFontSize.Minimum, (float)_uiFontSize.Maximum);
        _uiStrokeWidth.Value = (decimal)Math.Clamp(initial.StrokeWidthRatio * 100f, (float)_uiStrokeWidth.Minimum, (float)_uiStrokeWidth.Maximum);
        _fillColor = DrawingColor.FromArgb(initial.FillColor.Red, initial.FillColor.Green, initial.FillColor.Blue);
        _strokeColor = DrawingColor.FromArgb(initial.StrokeColor.Red, initial.StrokeColor.Green, initial.StrokeColor.Blue);
        _uiFillColorButton.BackColor = _fillColor;
        _uiStrokeColorButton.BackColor = _strokeColor;
        _suppressPreview = false;
        UpdatePreview();
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

    private void PickColor(bool fill)
    {
        using var dialog = new ColorDialog
        {
            Color = fill ? _fillColor : _strokeColor,
            FullOpen = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (fill)
        {
            _fillColor = dialog.Color;
            _uiFillColorButton.BackColor = dialog.Color;
        }
        else
        {
            _strokeColor = dialog.Color;
            _uiStrokeColorButton.BackColor = dialog.Color;
        }

        UpdatePreview();
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
        var family = _uiFontFamily.SelectedItem?.ToString() ?? "Arial";

        return new(_templatePath ?? string.Empty,
            (int)_uiStartNumber.Value,
            _textX,
            _textY,
            (float)_uiFontSize.Value / 100f,
            family,
            new(_fillColor.R, _fillColor.G, _fillColor.B),
            new(_strokeColor.R, _strokeColor.G, _strokeColor.B),
            (float)_uiStrokeWidth.Value / 100f);
    }

    private void UpdatePreview()
    {
        if (_suppressPreview)
        {
            return;
        }

        _uiPositionLabel.Text = $"Позиция: X={_textX:F2}, Y={_textY:F2}";

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

        Result = BuildTemplate();
        DialogResult = DialogResult.OK;
        Close();
    }
}
