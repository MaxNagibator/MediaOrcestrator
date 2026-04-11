using DrawingColor = System.Drawing.Color;

namespace MediaOrcestrator.Runner;

partial class CoverTemplateForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        uiRootLayout = new TableLayoutPanel();
        uiPreviewGroup = new GroupBox();
        uiPreviewLayout = new TableLayoutPanel();
        uiPreview = new PictureBox();
        uiPositionLabel = new Label();
        uiSettingsPanel = new TableLayoutPanel();
        uiProfilesGroup = new GroupBox();
        uiProfilesLayout = new FlowLayoutPanel();
        uiProfilesCombo = new ComboBox();
        uiLoadProfileButton = new Button();
        uiSaveAsProfileButton = new Button();
        uiDeleteProfileButton = new Button();
        uiTemplateGroup = new GroupBox();
        uiTemplateLayout = new TableLayoutPanel();
        uiFileLabel = new Label();
        uiPathPanel = new TableLayoutPanel();
        uiTemplatePathTextBox = new TextBox();
        uiBrowseButton = new Button();
        uiModeLabelControl = new Label();
        uiModeLayout = new FlowLayoutPanel();
        uiSequentialRadio = new RadioButton();
        uiTitleRegexRadio = new RadioButton();
        uiStartNumberLabel = new Label();
        uiStartNumber = new NumericUpDown();
        uiRegexLabel = new Label();
        uiTitleRegexTextBox = new TextBox();
        uiSampleLabel = new Label();
        uiSampleNumber = new NumericUpDown();
        uiHintLabel = new Label();
        uiLayersGroup = new GroupBox();
        uiLayersLayout = new TableLayoutPanel();
        uiLayersList = new ListBox();
        uiLayersButtons = new FlowLayoutPanel();
        uiAddLayerButton = new Button();
        uiRemoveLayerButton = new Button();
        uiMoveLayerUpButton = new Button();
        uiMoveLayerDownButton = new Button();
        uiLayerGroup = new GroupBox();
        uiLayerLayout = new TableLayoutPanel();
        uiLayerTextLabel = new Label();
        uiLayerTextBox = new TextBox();
        uiLayerFontLabel = new Label();
        uiFontFamily = new ComboBox();
        uiLayerSizeLabel = new Label();
        uiFontSize = new NumericUpDown();
        uiLayerStrokeLabel = new Label();
        uiStrokeWidth = new NumericUpDown();
        uiLayerFillLabel = new Label();
        uiFillColorButton = new Button();
        uiLayerStrokeColorLabel = new Label();
        uiStrokeColorButton = new Button();
        uiButtonPanel = new FlowLayoutPanel();
        uiCancelButton = new Button();
        uiOkButton = new Button();
        uiHelpButton = new Button();
        uiToolTip = new ToolTip(components);
        uiRootLayout.SuspendLayout();
        uiPreviewGroup.SuspendLayout();
        uiPreviewLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreview).BeginInit();
        uiSettingsPanel.SuspendLayout();
        uiProfilesGroup.SuspendLayout();
        uiProfilesLayout.SuspendLayout();
        uiTemplateGroup.SuspendLayout();
        uiTemplateLayout.SuspendLayout();
        uiPathPanel.SuspendLayout();
        uiModeLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiStartNumber).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiSampleNumber).BeginInit();
        uiLayersGroup.SuspendLayout();
        uiLayersLayout.SuspendLayout();
        uiLayersButtons.SuspendLayout();
        uiLayerGroup.SuspendLayout();
        uiLayerLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiFontSize).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiStrokeWidth).BeginInit();
        uiButtonPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiRootLayout
        //
        uiRootLayout.ColumnCount = 2;
        uiRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        uiRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        uiRootLayout.Controls.Add(uiPreviewGroup, 0, 0);
        uiRootLayout.Controls.Add(uiSettingsPanel, 1, 0);
        uiRootLayout.Controls.Add(uiButtonPanel, 0, 1);
        uiRootLayout.SetColumnSpan(uiButtonPanel, 2);
        uiRootLayout.Dock = DockStyle.Fill;
        uiRootLayout.Name = "uiRootLayout";
        uiRootLayout.Padding = new Padding(10);
        uiRootLayout.RowCount = 2;
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiRootLayout.RowStyles.Add(new RowStyle());
        uiRootLayout.TabIndex = 0;
        //
        // uiPreviewGroup
        //
        uiPreviewGroup.Controls.Add(uiPreviewLayout);
        uiPreviewGroup.Dock = DockStyle.Fill;
        uiPreviewGroup.Name = "uiPreviewGroup";
        uiPreviewGroup.TabIndex = 0;
        uiPreviewGroup.TabStop = false;
        uiPreviewGroup.Text = "Превью (клик — задать позицию выбранного слоя)";
        //
        // uiPreviewLayout
        //
        uiPreviewLayout.ColumnCount = 1;
        uiPreviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiPreviewLayout.Controls.Add(uiPreview, 0, 0);
        uiPreviewLayout.Controls.Add(uiPositionLabel, 0, 1);
        uiPreviewLayout.Dock = DockStyle.Fill;
        uiPreviewLayout.Name = "uiPreviewLayout";
        uiPreviewLayout.RowCount = 2;
        uiPreviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiPreviewLayout.RowStyles.Add(new RowStyle());
        uiPreviewLayout.TabIndex = 0;
        //
        // uiPreview
        //
        uiPreview.BackColor = DrawingColor.Black;
        uiPreview.Cursor = Cursors.Cross;
        uiPreview.Dock = DockStyle.Fill;
        uiPreview.Name = "uiPreview";
        uiPreview.SizeMode = PictureBoxSizeMode.Zoom;
        uiPreview.TabIndex = 0;
        uiPreview.TabStop = false;
        uiPreview.MouseDown += OnPreviewMouseDown;
        uiPreview.MouseMove += OnPreviewMouseMove;
        uiPreview.MouseUp += OnPreviewMouseUp;
        //
        // uiPositionLabel
        //
        uiPositionLabel.AutoSize = true;
        uiPositionLabel.Dock = DockStyle.Fill;
        uiPositionLabel.Name = "uiPositionLabel";
        uiPositionLabel.TabIndex = 1;
        uiPositionLabel.Text = "Слой не выбран";
        //
        // uiSettingsPanel
        //
        uiSettingsPanel.AutoScroll = true;
        uiSettingsPanel.ColumnCount = 1;
        uiSettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiSettingsPanel.Controls.Add(uiProfilesGroup, 0, 0);
        uiSettingsPanel.Controls.Add(uiTemplateGroup, 0, 1);
        uiSettingsPanel.Controls.Add(uiLayersGroup, 0, 2);
        uiSettingsPanel.Controls.Add(uiLayerGroup, 0, 3);
        uiSettingsPanel.Dock = DockStyle.Fill;
        uiSettingsPanel.Name = "uiSettingsPanel";
        uiSettingsPanel.RowCount = 4;
        uiSettingsPanel.RowStyles.Add(new RowStyle());
        uiSettingsPanel.RowStyles.Add(new RowStyle());
        uiSettingsPanel.RowStyles.Add(new RowStyle());
        uiSettingsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiSettingsPanel.TabIndex = 1;
        //
        // uiProfilesGroup
        //
        uiProfilesGroup.AutoSize = true;
        uiProfilesGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiProfilesGroup.Controls.Add(uiProfilesLayout);
        uiProfilesGroup.Dock = DockStyle.Fill;
        uiProfilesGroup.Name = "uiProfilesGroup";
        uiProfilesGroup.TabIndex = 0;
        uiProfilesGroup.TabStop = false;
        uiProfilesGroup.Text = "Профиль";
        //
        // uiProfilesLayout
        //
        uiProfilesLayout.AutoSize = true;
        uiProfilesLayout.Controls.Add(uiProfilesCombo);
        uiProfilesLayout.Controls.Add(uiLoadProfileButton);
        uiProfilesLayout.Controls.Add(uiSaveAsProfileButton);
        uiProfilesLayout.Controls.Add(uiDeleteProfileButton);
        uiProfilesLayout.Dock = DockStyle.Fill;
        uiProfilesLayout.FlowDirection = FlowDirection.LeftToRight;
        uiProfilesLayout.Name = "uiProfilesLayout";
        uiProfilesLayout.Padding = new Padding(8);
        uiProfilesLayout.TabIndex = 0;
        //
        // uiProfilesCombo
        //
        uiProfilesCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        uiProfilesCombo.Name = "uiProfilesCombo";
        uiProfilesCombo.Size = new Size(180, 23);
        uiProfilesCombo.TabIndex = 0;
        //
        // uiLoadProfileButton
        //
        uiLoadProfileButton.AutoSize = true;
        uiLoadProfileButton.Name = "uiLoadProfileButton";
        uiLoadProfileButton.TabIndex = 1;
        uiLoadProfileButton.Text = "Загрузить";
        uiLoadProfileButton.UseVisualStyleBackColor = true;
        uiLoadProfileButton.Click += uiLoadProfileButton_Click;
        //
        // uiSaveAsProfileButton
        //
        uiSaveAsProfileButton.AutoSize = true;
        uiSaveAsProfileButton.Name = "uiSaveAsProfileButton";
        uiSaveAsProfileButton.TabIndex = 2;
        uiSaveAsProfileButton.Text = "Сохранить как...";
        uiSaveAsProfileButton.UseVisualStyleBackColor = true;
        uiSaveAsProfileButton.Click += uiSaveAsProfileButton_Click;
        //
        // uiDeleteProfileButton
        //
        uiDeleteProfileButton.AutoSize = true;
        uiDeleteProfileButton.Name = "uiDeleteProfileButton";
        uiDeleteProfileButton.TabIndex = 3;
        uiDeleteProfileButton.Text = "Удалить";
        uiDeleteProfileButton.UseVisualStyleBackColor = true;
        uiDeleteProfileButton.Click += uiDeleteProfileButton_Click;
        //
        // uiTemplateGroup
        //
        uiTemplateGroup.AutoSize = true;
        uiTemplateGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiTemplateGroup.Controls.Add(uiTemplateLayout);
        uiTemplateGroup.Dock = DockStyle.Fill;
        uiTemplateGroup.Name = "uiTemplateGroup";
        uiTemplateGroup.TabIndex = 1;
        uiTemplateGroup.TabStop = false;
        uiTemplateGroup.Text = "Шаблон и нумерация";
        //
        // uiTemplateLayout
        //
        uiTemplateLayout.AutoSize = true;
        uiTemplateLayout.ColumnCount = 2;
        uiTemplateLayout.ColumnStyles.Add(new ColumnStyle());
        uiTemplateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiTemplateLayout.Controls.Add(uiFileLabel, 0, 0);
        uiTemplateLayout.Controls.Add(uiPathPanel, 1, 0);
        uiTemplateLayout.Controls.Add(uiModeLabelControl, 0, 1);
        uiTemplateLayout.Controls.Add(uiModeLayout, 1, 1);
        uiTemplateLayout.Controls.Add(uiStartNumberLabel, 0, 2);
        uiTemplateLayout.Controls.Add(uiStartNumber, 1, 2);
        uiTemplateLayout.Controls.Add(uiRegexLabel, 0, 3);
        uiTemplateLayout.Controls.Add(uiTitleRegexTextBox, 1, 3);
        uiTemplateLayout.Controls.Add(uiSampleLabel, 0, 4);
        uiTemplateLayout.Controls.Add(uiSampleNumber, 1, 4);
        uiTemplateLayout.Controls.Add(uiHintLabel, 0, 5);
        uiTemplateLayout.SetColumnSpan(uiHintLabel, 2);
        uiTemplateLayout.Dock = DockStyle.Fill;
        uiTemplateLayout.Name = "uiTemplateLayout";
        uiTemplateLayout.Padding = new Padding(8);
        uiTemplateLayout.RowCount = 7;
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.RowStyles.Add(new RowStyle());
        uiTemplateLayout.TabIndex = 0;
        //
        // uiFileLabel
        //
        uiFileLabel.Anchor = AnchorStyles.Left;
        uiFileLabel.AutoSize = true;
        uiFileLabel.Name = "uiFileLabel";
        uiFileLabel.Text = "Файл:";
        //
        // uiPathPanel
        //
        uiPathPanel.AutoSize = true;
        uiPathPanel.ColumnCount = 2;
        uiPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiPathPanel.ColumnStyles.Add(new ColumnStyle());
        uiPathPanel.Controls.Add(uiTemplatePathTextBox, 0, 0);
        uiPathPanel.Controls.Add(uiBrowseButton, 1, 0);
        uiPathPanel.Dock = DockStyle.Fill;
        uiPathPanel.Name = "uiPathPanel";
        uiPathPanel.RowCount = 1;
        uiPathPanel.RowStyles.Add(new RowStyle());
        uiPathPanel.TabIndex = 0;
        //
        // uiTemplatePathTextBox
        //
        uiTemplatePathTextBox.Dock = DockStyle.Fill;
        uiTemplatePathTextBox.Name = "uiTemplatePathTextBox";
        uiTemplatePathTextBox.ReadOnly = true;
        uiTemplatePathTextBox.TabIndex = 0;
        //
        // uiBrowseButton
        //
        uiBrowseButton.AutoSize = true;
        uiBrowseButton.Name = "uiBrowseButton";
        uiBrowseButton.TabIndex = 1;
        uiBrowseButton.Text = "Обзор...";
        uiBrowseButton.UseVisualStyleBackColor = true;
        uiBrowseButton.Click += uiBrowseButton_Click;
        //
        // uiModeLabelControl
        //
        uiModeLabelControl.Anchor = AnchorStyles.Left;
        uiModeLabelControl.AutoSize = true;
        uiModeLabelControl.Name = "uiModeLabelControl";
        uiModeLabelControl.Text = "Режим №:";
        //
        // uiModeLayout
        //
        uiModeLayout.AutoSize = true;
        uiModeLayout.Controls.Add(uiSequentialRadio);
        uiModeLayout.Controls.Add(uiTitleRegexRadio);
        uiModeLayout.Dock = DockStyle.Fill;
        uiModeLayout.FlowDirection = FlowDirection.LeftToRight;
        uiModeLayout.Name = "uiModeLayout";
        uiModeLayout.TabIndex = 0;
        uiModeLayout.WrapContents = false;
        //
        // uiSequentialRadio
        //
        uiSequentialRadio.AutoSize = true;
        uiSequentialRadio.Checked = true;
        uiSequentialRadio.Name = "uiSequentialRadio";
        uiSequentialRadio.TabIndex = 0;
        uiSequentialRadio.TabStop = true;
        uiSequentialRadio.Text = "Последовательно";
        uiSequentialRadio.UseVisualStyleBackColor = true;
        uiSequentialRadio.CheckedChanged += uiSequentialRadio_CheckedChanged;
        //
        // uiTitleRegexRadio
        //
        uiTitleRegexRadio.AutoSize = true;
        uiTitleRegexRadio.Name = "uiTitleRegexRadio";
        uiTitleRegexRadio.TabIndex = 1;
        uiTitleRegexRadio.Text = "Из названия";
        uiTitleRegexRadio.UseVisualStyleBackColor = true;
        uiTitleRegexRadio.CheckedChanged += uiTitleRegexRadio_CheckedChanged;
        //
        // uiStartNumberLabel
        //
        uiStartNumberLabel.Anchor = AnchorStyles.Left;
        uiStartNumberLabel.AutoSize = true;
        uiStartNumberLabel.Name = "uiStartNumberLabel";
        uiStartNumberLabel.Text = "Начальный №:";
        //
        // uiStartNumber
        //
        uiStartNumber.Dock = DockStyle.Fill;
        uiStartNumber.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
        uiStartNumber.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        uiStartNumber.Name = "uiStartNumber";
        uiStartNumber.TabIndex = 0;
        uiStartNumber.Value = new decimal(new int[] { 1, 0, 0, 0 });
        uiStartNumber.ValueChanged += uiStartNumber_ValueChanged;
        //
        // uiRegexLabel
        //
        uiRegexLabel.Anchor = AnchorStyles.Left;
        uiRegexLabel.AutoSize = true;
        uiRegexLabel.Name = "uiRegexLabel";
        uiRegexLabel.Text = "Regex:";
        //
        // uiTitleRegexTextBox
        //
        uiTitleRegexTextBox.Dock = DockStyle.Fill;
        uiTitleRegexTextBox.Name = "uiTitleRegexTextBox";
        uiTitleRegexTextBox.TabIndex = 0;
        uiTitleRegexTextBox.TextChanged += uiTitleRegexTextBox_TextChanged;
        //
        // uiSampleLabel
        //
        uiSampleLabel.Anchor = AnchorStyles.Left;
        uiSampleLabel.AutoSize = true;
        uiSampleLabel.Name = "uiSampleLabel";
        uiSampleLabel.Text = "Образец №:";
        //
        // uiSampleNumber
        //
        uiSampleNumber.Dock = DockStyle.Fill;
        uiSampleNumber.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
        uiSampleNumber.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        uiSampleNumber.Name = "uiSampleNumber";
        uiSampleNumber.TabIndex = 0;
        uiSampleNumber.Value = new decimal(new int[] { 12, 0, 0, 0 });
        uiSampleNumber.ValueChanged += uiSampleNumber_ValueChanged;
        //
        // uiHintLabel
        //
        uiHintLabel.AutoSize = true;
        uiHintLabel.ForeColor = DrawingColor.Gray;
        uiHintLabel.MaximumSize = new Size(360, 0);
        uiHintLabel.Name = "uiHintLabel";
        uiHintLabel.Text = "В тексте слоя пишите {number} — будет подставлен расчётный номер.";
        //
        // uiLayersGroup
        //
        uiLayersGroup.Controls.Add(uiLayersLayout);
        uiLayersGroup.Dock = DockStyle.Fill;
        uiLayersGroup.Height = 130;
        uiLayersGroup.Name = "uiLayersGroup";
        uiLayersGroup.TabIndex = 2;
        uiLayersGroup.TabStop = false;
        uiLayersGroup.Text = "Слои";
        //
        // uiLayersLayout
        //
        uiLayersLayout.ColumnCount = 2;
        uiLayersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiLayersLayout.ColumnStyles.Add(new ColumnStyle());
        uiLayersLayout.Controls.Add(uiLayersList, 0, 0);
        uiLayersLayout.Controls.Add(uiLayersButtons, 1, 0);
        uiLayersLayout.Dock = DockStyle.Fill;
        uiLayersLayout.Name = "uiLayersLayout";
        uiLayersLayout.Padding = new Padding(8);
        uiLayersLayout.RowCount = 1;
        uiLayersLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiLayersLayout.TabIndex = 0;
        //
        // uiLayersList
        //
        uiLayersList.Dock = DockStyle.Fill;
        uiLayersList.FormattingEnabled = true;
        uiLayersList.ItemHeight = 15;
        uiLayersList.Name = "uiLayersList";
        uiLayersList.TabIndex = 0;
        uiLayersList.SelectedIndexChanged += uiLayersList_SelectedIndexChanged;
        //
        // uiLayersButtons
        //
        uiLayersButtons.AutoSize = true;
        uiLayersButtons.Controls.Add(uiAddLayerButton);
        uiLayersButtons.Controls.Add(uiRemoveLayerButton);
        uiLayersButtons.Controls.Add(uiMoveLayerUpButton);
        uiLayersButtons.Controls.Add(uiMoveLayerDownButton);
        uiLayersButtons.Dock = DockStyle.Fill;
        uiLayersButtons.FlowDirection = FlowDirection.TopDown;
        uiLayersButtons.Name = "uiLayersButtons";
        uiLayersButtons.TabIndex = 1;
        uiLayersButtons.WrapContents = false;
        //
        // uiAddLayerButton
        //
        uiAddLayerButton.AutoSize = true;
        uiAddLayerButton.Name = "uiAddLayerButton";
        uiAddLayerButton.TabIndex = 0;
        uiAddLayerButton.Text = "Добавить";
        uiAddLayerButton.UseVisualStyleBackColor = true;
        uiAddLayerButton.Width = 90;
        uiAddLayerButton.Click += uiAddLayerButton_Click;
        //
        // uiRemoveLayerButton
        //
        uiRemoveLayerButton.AutoSize = true;
        uiRemoveLayerButton.Enabled = false;
        uiRemoveLayerButton.Name = "uiRemoveLayerButton";
        uiRemoveLayerButton.TabIndex = 1;
        uiRemoveLayerButton.Text = "Удалить";
        uiRemoveLayerButton.UseVisualStyleBackColor = true;
        uiRemoveLayerButton.Width = 90;
        uiRemoveLayerButton.Click += uiRemoveLayerButton_Click;
        //
        // uiMoveLayerUpButton
        //
        uiMoveLayerUpButton.AutoSize = true;
        uiMoveLayerUpButton.Enabled = false;
        uiMoveLayerUpButton.Name = "uiMoveLayerUpButton";
        uiMoveLayerUpButton.TabIndex = 2;
        uiMoveLayerUpButton.Text = "Вверх";
        uiMoveLayerUpButton.UseVisualStyleBackColor = true;
        uiMoveLayerUpButton.Width = 90;
        uiMoveLayerUpButton.Click += uiMoveLayerUpButton_Click;
        //
        // uiMoveLayerDownButton
        //
        uiMoveLayerDownButton.AutoSize = true;
        uiMoveLayerDownButton.Enabled = false;
        uiMoveLayerDownButton.Name = "uiMoveLayerDownButton";
        uiMoveLayerDownButton.TabIndex = 3;
        uiMoveLayerDownButton.Text = "Вниз";
        uiMoveLayerDownButton.UseVisualStyleBackColor = true;
        uiMoveLayerDownButton.Width = 90;
        uiMoveLayerDownButton.Click += uiMoveLayerDownButton_Click;
        //
        // uiLayerGroup
        //
        uiLayerGroup.AutoSize = true;
        uiLayerGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiLayerGroup.Controls.Add(uiLayerLayout);
        uiLayerGroup.Dock = DockStyle.Fill;
        uiLayerGroup.Enabled = false;
        uiLayerGroup.Name = "uiLayerGroup";
        uiLayerGroup.TabIndex = 3;
        uiLayerGroup.TabStop = false;
        uiLayerGroup.Text = "Текущий слой";
        //
        // uiLayerLayout
        //
        uiLayerLayout.AutoSize = true;
        uiLayerLayout.ColumnCount = 2;
        uiLayerLayout.ColumnStyles.Add(new ColumnStyle());
        uiLayerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiLayerLayout.Controls.Add(uiLayerTextLabel, 0, 0);
        uiLayerLayout.Controls.Add(uiLayerTextBox, 1, 0);
        uiLayerLayout.Controls.Add(uiLayerFontLabel, 0, 1);
        uiLayerLayout.Controls.Add(uiFontFamily, 1, 1);
        uiLayerLayout.Controls.Add(uiLayerSizeLabel, 0, 2);
        uiLayerLayout.Controls.Add(uiFontSize, 1, 2);
        uiLayerLayout.Controls.Add(uiLayerStrokeLabel, 0, 3);
        uiLayerLayout.Controls.Add(uiStrokeWidth, 1, 3);
        uiLayerLayout.Controls.Add(uiLayerFillLabel, 0, 4);
        uiLayerLayout.Controls.Add(uiFillColorButton, 1, 4);
        uiLayerLayout.Controls.Add(uiLayerStrokeColorLabel, 0, 5);
        uiLayerLayout.Controls.Add(uiStrokeColorButton, 1, 5);
        uiLayerLayout.Dock = DockStyle.Fill;
        uiLayerLayout.Name = "uiLayerLayout";
        uiLayerLayout.Padding = new Padding(8);
        uiLayerLayout.RowCount = 6;
        uiLayerLayout.RowStyles.Add(new RowStyle());
        uiLayerLayout.RowStyles.Add(new RowStyle());
        uiLayerLayout.RowStyles.Add(new RowStyle());
        uiLayerLayout.RowStyles.Add(new RowStyle());
        uiLayerLayout.RowStyles.Add(new RowStyle());
        uiLayerLayout.RowStyles.Add(new RowStyle());
        uiLayerLayout.TabIndex = 0;
        //
        // uiLayerTextLabel
        //
        uiLayerTextLabel.Anchor = AnchorStyles.Left;
        uiLayerTextLabel.AutoSize = true;
        uiLayerTextLabel.Name = "uiLayerTextLabel";
        uiLayerTextLabel.Text = "Текст:";
        //
        // uiLayerTextBox
        //
        uiLayerTextBox.Dock = DockStyle.Fill;
        uiLayerTextBox.Name = "uiLayerTextBox";
        uiLayerTextBox.TabIndex = 0;
        uiLayerTextBox.Text = "{number}";
        uiLayerTextBox.TextChanged += uiLayerTextBox_TextChanged;
        uiLayerTextBox.Leave += uiLayerTextBox_Leave;
        //
        // uiLayerFontLabel
        //
        uiLayerFontLabel.Anchor = AnchorStyles.Left;
        uiLayerFontLabel.AutoSize = true;
        uiLayerFontLabel.Name = "uiLayerFontLabel";
        uiLayerFontLabel.Text = "Шрифт:";
        //
        // uiFontFamily
        //
        uiFontFamily.Dock = DockStyle.Fill;
        uiFontFamily.DropDownStyle = ComboBoxStyle.DropDownList;
        uiFontFamily.Name = "uiFontFamily";
        uiFontFamily.TabIndex = 0;
        uiFontFamily.SelectedIndexChanged += uiFontFamily_SelectedIndexChanged;
        //
        // uiLayerSizeLabel
        //
        uiLayerSizeLabel.Anchor = AnchorStyles.Left;
        uiLayerSizeLabel.AutoSize = true;
        uiLayerSizeLabel.Name = "uiLayerSizeLabel";
        uiLayerSizeLabel.Text = "Размер (% выс.):";
        //
        // uiFontSize
        //
        uiFontSize.DecimalPlaces = 1;
        uiFontSize.Dock = DockStyle.Fill;
        uiFontSize.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        uiFontSize.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        uiFontSize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        uiFontSize.Name = "uiFontSize";
        uiFontSize.TabIndex = 0;
        uiFontSize.Value = new decimal(new int[] { 25, 0, 0, 0 });
        uiFontSize.ValueChanged += uiFontSize_ValueChanged;
        //
        // uiLayerStrokeLabel
        //
        uiLayerStrokeLabel.Anchor = AnchorStyles.Left;
        uiLayerStrokeLabel.AutoSize = true;
        uiLayerStrokeLabel.Name = "uiLayerStrokeLabel";
        uiLayerStrokeLabel.Text = "Обводка (% выс.):";
        //
        // uiStrokeWidth
        //
        uiStrokeWidth.DecimalPlaces = 2;
        uiStrokeWidth.Dock = DockStyle.Fill;
        uiStrokeWidth.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        uiStrokeWidth.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        uiStrokeWidth.Name = "uiStrokeWidth";
        uiStrokeWidth.TabIndex = 0;
        uiStrokeWidth.Value = new decimal(new int[] { 1, 0, 0, 0 });
        uiStrokeWidth.ValueChanged += uiStrokeWidth_ValueChanged;
        //
        // uiLayerFillLabel
        //
        uiLayerFillLabel.Anchor = AnchorStyles.Left;
        uiLayerFillLabel.AutoSize = true;
        uiLayerFillLabel.Name = "uiLayerFillLabel";
        uiLayerFillLabel.Text = "Заливка:";
        //
        // uiFillColorButton
        //
        uiFillColorButton.BackColor = DrawingColor.White;
        uiFillColorButton.Dock = DockStyle.Fill;
        uiFillColorButton.FlatStyle = FlatStyle.Flat;
        uiFillColorButton.Height = 26;
        uiFillColorButton.Name = "uiFillColorButton";
        uiFillColorButton.TabIndex = 0;
        uiFillColorButton.Text = "";
        uiFillColorButton.Click += uiFillColorButton_Click;
        //
        // uiLayerStrokeColorLabel
        //
        uiLayerStrokeColorLabel.Anchor = AnchorStyles.Left;
        uiLayerStrokeColorLabel.AutoSize = true;
        uiLayerStrokeColorLabel.Name = "uiLayerStrokeColorLabel";
        uiLayerStrokeColorLabel.Text = "Цвет обводки:";
        //
        // uiStrokeColorButton
        //
        uiStrokeColorButton.BackColor = DrawingColor.Black;
        uiStrokeColorButton.Dock = DockStyle.Fill;
        uiStrokeColorButton.FlatStyle = FlatStyle.Flat;
        uiStrokeColorButton.Height = 26;
        uiStrokeColorButton.Name = "uiStrokeColorButton";
        uiStrokeColorButton.TabIndex = 0;
        uiStrokeColorButton.Text = "";
        uiStrokeColorButton.Click += uiStrokeColorButton_Click;
        //
        // uiButtonPanel
        //
        uiButtonPanel.AutoSize = true;
        uiButtonPanel.Controls.Add(uiCancelButton);
        uiButtonPanel.Controls.Add(uiOkButton);
        uiButtonPanel.Controls.Add(uiHelpButton);
        uiButtonPanel.Dock = DockStyle.Fill;
        uiButtonPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonPanel.Name = "uiButtonPanel";
        uiButtonPanel.Padding = new Padding(0, 8, 0, 0);
        uiButtonPanel.TabIndex = 2;
        //
        // uiCancelButton
        //
        uiCancelButton.DialogResult = DialogResult.Cancel;
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Size = new Size(90, 27);
        uiCancelButton.TabIndex = 0;
        uiCancelButton.Text = "Отмена";
        uiCancelButton.UseVisualStyleBackColor = true;
        //
        // uiOkButton
        //
        uiOkButton.Name = "uiOkButton";
        uiOkButton.Size = new Size(90, 27);
        uiOkButton.TabIndex = 1;
        uiOkButton.Text = "OK";
        uiOkButton.UseVisualStyleBackColor = true;
        uiOkButton.Click += uiOkButton_Click;
        //
        // uiHelpButton
        //
        uiHelpButton.AutoSize = true;
        uiHelpButton.Name = "uiHelpButton";
        uiHelpButton.TabIndex = 2;
        uiHelpButton.Text = "Справка";
        uiHelpButton.UseVisualStyleBackColor = true;
        uiHelpButton.Click += uiHelpButton_Click;
        //
        // CoverTemplateForm
        //
        AcceptButton = uiOkButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(1064, 721);
        Controls.Add(uiRootLayout);
        MinimumSize = new Size(900, 600);
        Name = "CoverTemplateForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Шаблон обложки";
        uiRootLayout.ResumeLayout(false);
        uiRootLayout.PerformLayout();
        uiPreviewGroup.ResumeLayout(false);
        uiPreviewLayout.ResumeLayout(false);
        uiPreviewLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreview).EndInit();
        uiSettingsPanel.ResumeLayout(false);
        uiSettingsPanel.PerformLayout();
        uiProfilesGroup.ResumeLayout(false);
        uiProfilesGroup.PerformLayout();
        uiProfilesLayout.ResumeLayout(false);
        uiProfilesLayout.PerformLayout();
        uiTemplateGroup.ResumeLayout(false);
        uiTemplateGroup.PerformLayout();
        uiTemplateLayout.ResumeLayout(false);
        uiTemplateLayout.PerformLayout();
        uiPathPanel.ResumeLayout(false);
        uiPathPanel.PerformLayout();
        uiModeLayout.ResumeLayout(false);
        uiModeLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiStartNumber).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiSampleNumber).EndInit();
        uiLayersGroup.ResumeLayout(false);
        uiLayersLayout.ResumeLayout(false);
        uiLayersLayout.PerformLayout();
        uiLayersButtons.ResumeLayout(false);
        uiLayersButtons.PerformLayout();
        uiLayerGroup.ResumeLayout(false);
        uiLayerGroup.PerformLayout();
        uiLayerLayout.ResumeLayout(false);
        uiLayerLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiFontSize).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiStrokeWidth).EndInit();
        uiButtonPanel.ResumeLayout(false);
        uiButtonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiRootLayout;
    private GroupBox uiPreviewGroup;
    private TableLayoutPanel uiPreviewLayout;
    private PictureBox uiPreview;
    private Label uiPositionLabel;
    private TableLayoutPanel uiSettingsPanel;
    private GroupBox uiProfilesGroup;
    private FlowLayoutPanel uiProfilesLayout;
    private ComboBox uiProfilesCombo;
    private Button uiLoadProfileButton;
    private Button uiSaveAsProfileButton;
    private Button uiDeleteProfileButton;
    private GroupBox uiTemplateGroup;
    private TableLayoutPanel uiTemplateLayout;
    private Label uiFileLabel;
    private TableLayoutPanel uiPathPanel;
    private TextBox uiTemplatePathTextBox;
    private Button uiBrowseButton;
    private Label uiModeLabelControl;
    private FlowLayoutPanel uiModeLayout;
    private RadioButton uiSequentialRadio;
    private RadioButton uiTitleRegexRadio;
    private Label uiStartNumberLabel;
    private NumericUpDown uiStartNumber;
    private Label uiRegexLabel;
    private TextBox uiTitleRegexTextBox;
    private Label uiSampleLabel;
    private NumericUpDown uiSampleNumber;
    private Label uiHintLabel;
    private GroupBox uiLayersGroup;
    private TableLayoutPanel uiLayersLayout;
    private ListBox uiLayersList;
    private FlowLayoutPanel uiLayersButtons;
    private Button uiAddLayerButton;
    private Button uiRemoveLayerButton;
    private Button uiMoveLayerUpButton;
    private Button uiMoveLayerDownButton;
    private GroupBox uiLayerGroup;
    private TableLayoutPanel uiLayerLayout;
    private Label uiLayerTextLabel;
    private TextBox uiLayerTextBox;
    private Label uiLayerFontLabel;
    private ComboBox uiFontFamily;
    private Label uiLayerSizeLabel;
    private NumericUpDown uiFontSize;
    private Label uiLayerStrokeLabel;
    private NumericUpDown uiStrokeWidth;
    private Label uiLayerFillLabel;
    private Button uiFillColorButton;
    private Label uiLayerStrokeColorLabel;
    private Button uiStrokeColorButton;
    private FlowLayoutPanel uiButtonPanel;
    private Button uiCancelButton;
    private Button uiOkButton;
    private Button uiHelpButton;
    private ToolTip uiToolTip;
}
