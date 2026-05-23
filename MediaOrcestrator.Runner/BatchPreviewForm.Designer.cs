namespace MediaOrcestrator.Runner;

partial class BatchPreviewForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        uiMainLayout = new TableLayoutPanel();

        uiSourceGroup = new GroupBox();
        uiSourcePanelLayout = new TableLayoutPanel();
        uiDonorLayout = new FlowLayoutPanel();
        uiFromSourceRadio = new RadioButton();
        uiDonorComboBox = new ComboBox();
        uiFileLayout = new FlowLayoutPanel();
        uiFromFileRadio = new RadioButton();
        uiFilePathTextBox = new TextBox();
        uiBrowseButton = new Button();
        uiTemplateLayout = new FlowLayoutPanel();
        uiFromTemplateRadio = new RadioButton();
        uiTemplateButton = new Button();
        uiProfileCombo = new ComboBox();
        uiCoverThumbnail = new PictureBox();

        uiTargetsGroup = new GroupBox();
        uiTargetsPanel = new FlowLayoutPanel();
        uiTargetsAllLink = new LinkLabel();
        uiTargetsNoneLink = new LinkLabel();

        uiRowSelectPanel = new FlowLayoutPanel();
        uiRowSelectLabel = new Label();
        uiRowAllLink = new LinkLabel();
        uiRowNoneLink = new LinkLabel();

        uiGridLogSplit = new SplitContainer();
        uiResultGrid = new DataGridView();
        uiApplyColumn = new DataGridViewCheckBoxColumn();
        uiTitleColumn = new DataGridViewTextBoxColumn();
        uiTargetColumn = new DataGridViewTextBoxColumn();
        uiStatusColumn = new DataGridViewTextBoxColumn();

        uiLogBox = new RichTextBox();

        uiStatusPanel = new TableLayoutPanel();
        uiProgressBar = new ProgressBar();
        uiStatusLabel = new Label();

        uiButtonPanel = new FlowLayoutPanel();
        uiCancelButton = new Button();
        uiApplyButton = new Button();

        uiMainLayout.SuspendLayout();
        uiSourceGroup.SuspendLayout();
        uiSourcePanelLayout.SuspendLayout();
        uiDonorLayout.SuspendLayout();
        uiFileLayout.SuspendLayout();
        uiTemplateLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiCoverThumbnail).BeginInit();
        uiTargetsGroup.SuspendLayout();
        uiRowSelectPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiGridLogSplit).BeginInit();
        uiGridLogSplit.Panel1.SuspendLayout();
        uiGridLogSplit.Panel2.SuspendLayout();
        uiGridLogSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiResultGrid).BeginInit();
        uiStatusPanel.SuspendLayout();
        uiButtonPanel.SuspendLayout();
        SuspendLayout();

        //
        // uiMainLayout
        //
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiSourceGroup, 0, 0);
        uiMainLayout.Controls.Add(uiTargetsGroup, 0, 1);
        uiMainLayout.Controls.Add(uiRowSelectPanel, 0, 2);
        uiMainLayout.Controls.Add(uiGridLogSplit, 0, 3);
        uiMainLayout.Controls.Add(uiStatusPanel, 0, 4);
        uiMainLayout.Controls.Add(uiButtonPanel, 0, 5);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(10);
        uiMainLayout.RowCount = 6;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.TabIndex = 0;

        //
        // uiGridLogSplit
        //
        uiGridLogSplit.Dock = DockStyle.Fill;
        uiGridLogSplit.Name = "uiGridLogSplit";
        uiGridLogSplit.Orientation = Orientation.Horizontal;
        uiGridLogSplit.Size = new Size(600, 420);
        uiGridLogSplit.Panel1.Controls.Add(uiResultGrid);
        uiGridLogSplit.Panel2.Controls.Add(uiLogBox);
        uiGridLogSplit.Panel1MinSize = 120;
        uiGridLogSplit.Panel2MinSize = 80;
        uiGridLogSplit.SplitterDistance = 260;
        uiGridLogSplit.SplitterWidth = 6;
        uiGridLogSplit.Margin = new Padding(3, 3, 3, 3);
        uiGridLogSplit.TabIndex = 2;

        //
        // uiSourceGroup
        //
        uiSourceGroup.AutoSize = true;
        uiSourceGroup.Controls.Add(uiSourcePanelLayout);
        uiSourceGroup.Dock = DockStyle.Fill;
        uiSourceGroup.Name = "uiSourceGroup";
        uiSourceGroup.TabIndex = 0;
        uiSourceGroup.TabStop = false;
        uiSourceGroup.Text = "Источник превью";

        //
        // uiSourcePanelLayout
        //
        uiSourcePanelLayout.AutoSize = true;
        uiSourcePanelLayout.ColumnCount = 1;
        uiSourcePanelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiSourcePanelLayout.Controls.Add(uiDonorLayout, 0, 0);
        uiSourcePanelLayout.Controls.Add(uiFileLayout, 0, 1);
        uiSourcePanelLayout.Controls.Add(uiTemplateLayout, 0, 2);
        uiSourcePanelLayout.Dock = DockStyle.Fill;
        uiSourcePanelLayout.Name = "uiSourcePanelLayout";
        uiSourcePanelLayout.RowCount = 3;
        uiSourcePanelLayout.RowStyles.Add(new RowStyle());
        uiSourcePanelLayout.RowStyles.Add(new RowStyle());
        uiSourcePanelLayout.RowStyles.Add(new RowStyle());
        uiSourcePanelLayout.TabIndex = 0;

        //
        // uiDonorLayout
        //
        uiDonorLayout.AutoSize = true;
        uiDonorLayout.Controls.Add(uiFromSourceRadio);
        uiDonorLayout.Controls.Add(uiDonorComboBox);
        uiDonorLayout.Dock = DockStyle.Fill;
        uiDonorLayout.FlowDirection = FlowDirection.LeftToRight;
        uiDonorLayout.Name = "uiDonorLayout";
        uiDonorLayout.TabIndex = 0;
        uiDonorLayout.WrapContents = false;

        //
        // uiFromSourceRadio
        //
        uiFromSourceRadio.AutoSize = true;
        uiFromSourceRadio.Checked = true;
        uiFromSourceRadio.Margin = new Padding(3, 6, 3, 3);
        uiFromSourceRadio.Name = "uiFromSourceRadio";
        uiFromSourceRadio.TabIndex = 0;
        uiFromSourceRadio.TabStop = true;
        uiFromSourceRadio.Text = "Из источника:";
        uiFromSourceRadio.UseVisualStyleBackColor = true;
        uiFromSourceRadio.CheckedChanged += OnFromSourceCheckedChanged;

        //
        // uiDonorComboBox
        //
        uiDonorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        uiDonorComboBox.Margin = new Padding(6, 3, 3, 3);
        uiDonorComboBox.Name = "uiDonorComboBox";
        uiDonorComboBox.Size = new Size(260, 23);
        uiDonorComboBox.TabIndex = 1;
        uiDonorComboBox.SelectedIndexChanged += OnDonorComboSelectedIndexChanged;

        //
        // uiFileLayout
        //
        uiFileLayout.AutoSize = true;
        uiFileLayout.Controls.Add(uiFromFileRadio);
        uiFileLayout.Controls.Add(uiFilePathTextBox);
        uiFileLayout.Controls.Add(uiBrowseButton);
        uiFileLayout.Dock = DockStyle.Fill;
        uiFileLayout.FlowDirection = FlowDirection.LeftToRight;
        uiFileLayout.Name = "uiFileLayout";
        uiFileLayout.TabIndex = 1;
        uiFileLayout.WrapContents = false;

        //
        // uiFromFileRadio
        //
        uiFromFileRadio.AutoSize = true;
        uiFromFileRadio.Margin = new Padding(3, 6, 3, 3);
        uiFromFileRadio.Name = "uiFromFileRadio";
        uiFromFileRadio.TabIndex = 0;
        uiFromFileRadio.Text = "Из файла:";
        uiFromFileRadio.UseVisualStyleBackColor = true;
        uiFromFileRadio.CheckedChanged += OnFromFileCheckedChanged;

        //
        // uiFilePathTextBox
        //
        uiFilePathTextBox.Margin = new Padding(6, 3, 3, 3);
        uiFilePathTextBox.Name = "uiFilePathTextBox";
        uiFilePathTextBox.ReadOnly = true;
        uiFilePathTextBox.Size = new Size(210, 23);
        uiFilePathTextBox.TabIndex = 1;

        //
        // uiBrowseButton
        //
        uiBrowseButton.AutoSize = true;
        uiBrowseButton.Margin = new Padding(3, 2, 3, 3);
        uiBrowseButton.Name = "uiBrowseButton";
        uiBrowseButton.TabIndex = 2;
        uiBrowseButton.Text = "Обзор...";
        uiBrowseButton.UseVisualStyleBackColor = true;
        uiBrowseButton.Click += OnBrowseButtonClick;

        //
        // uiTemplateLayout
        //
        uiTemplateLayout.AutoSize = true;
        uiTemplateLayout.Controls.Add(uiFromTemplateRadio);
        uiTemplateLayout.Controls.Add(uiTemplateButton);
        uiTemplateLayout.Controls.Add(uiProfileCombo);
        uiTemplateLayout.Controls.Add(uiCoverThumbnail);
        uiTemplateLayout.Dock = DockStyle.Fill;
        uiTemplateLayout.FlowDirection = FlowDirection.LeftToRight;
        uiTemplateLayout.Name = "uiTemplateLayout";
        uiTemplateLayout.TabIndex = 2;
        uiTemplateLayout.WrapContents = false;

        //
        // uiFromTemplateRadio
        //
        uiFromTemplateRadio.AutoSize = true;
        uiFromTemplateRadio.Margin = new Padding(3, 26, 3, 3);
        uiFromTemplateRadio.Name = "uiFromTemplateRadio";
        uiFromTemplateRadio.TabIndex = 0;
        uiFromTemplateRadio.Text = "Из шаблона:";
        uiFromTemplateRadio.UseVisualStyleBackColor = true;
        uiFromTemplateRadio.CheckedChanged += OnFromTemplateCheckedChanged;

        //
        // uiTemplateButton
        //
        uiTemplateButton.AutoSize = true;
        uiTemplateButton.Margin = new Padding(6, 24, 3, 3);
        uiTemplateButton.Name = "uiTemplateButton";
        uiTemplateButton.TabIndex = 1;
        uiTemplateButton.Text = "Настроить...";
        uiTemplateButton.UseVisualStyleBackColor = true;
        uiTemplateButton.Click += OnTemplateButtonClick;

        //
        // uiProfileCombo
        //
        uiProfileCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        uiProfileCombo.Margin = new Padding(6, 24, 3, 3);
        uiProfileCombo.Name = "uiProfileCombo";
        uiProfileCombo.Size = new Size(140, 23);
        uiProfileCombo.TabIndex = 2;
        uiProfileCombo.SelectedIndexChanged += OnProfileComboSelectedIndexChanged;

        //
        // uiCoverThumbnail
        //
        uiCoverThumbnail.BackColor = Color.Black;
        uiCoverThumbnail.BorderStyle = BorderStyle.FixedSingle;
        uiCoverThumbnail.Margin = new Padding(6, 3, 0, 3);
        uiCoverThumbnail.Name = "uiCoverThumbnail";
        uiCoverThumbnail.Size = new Size(124, 70);
        uiCoverThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        uiCoverThumbnail.TabIndex = 3;
        uiCoverThumbnail.TabStop = false;

        //
        // uiTargetsGroup
        //
        uiTargetsGroup.AutoSize = true;
        uiTargetsGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiTargetsGroup.Controls.Add(uiTargetsPanel);
        uiTargetsGroup.Dock = DockStyle.Fill;
        uiTargetsGroup.Name = "uiTargetsGroup";
        uiTargetsGroup.TabIndex = 1;
        uiTargetsGroup.TabStop = false;
        uiTargetsGroup.Text = "Куда загрузить";

        //
        // uiTargetsPanel
        //
        uiTargetsPanel.AutoSize = true;
        uiTargetsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiTargetsPanel.Dock = DockStyle.Fill;
        uiTargetsPanel.FlowDirection = FlowDirection.LeftToRight;
        uiTargetsPanel.Margin = new Padding(3, 3, 3, 3);
        uiTargetsPanel.Name = "uiTargetsPanel";
        uiTargetsPanel.WrapContents = true;
        uiTargetsPanel.TabIndex = 0;

        //
        // uiTargetsAllLink
        //
        uiTargetsAllLink.AutoSize = true;
        uiTargetsAllLink.Name = "uiTargetsAllLink";
        uiTargetsAllLink.Text = "все";
        uiTargetsAllLink.Margin = new Padding(12, 6, 4, 3);
        uiTargetsAllLink.LinkClicked += (s, e) => SetAllTargets(true);

        //
        // uiTargetsNoneLink
        //
        uiTargetsNoneLink.AutoSize = true;
        uiTargetsNoneLink.Name = "uiTargetsNoneLink";
        uiTargetsNoneLink.Text = "ни одного";
        uiTargetsNoneLink.Margin = new Padding(4, 6, 4, 3);
        uiTargetsNoneLink.LinkClicked += (s, e) => SetAllTargets(false);

        //
        // uiRowSelectPanel
        //
        uiRowSelectPanel.AutoSize = true;
        uiRowSelectPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiRowSelectPanel.Dock = DockStyle.Fill;
        uiRowSelectPanel.FlowDirection = FlowDirection.LeftToRight;
        uiRowSelectPanel.Margin = new Padding(3, 6, 3, 2);
        uiRowSelectPanel.Name = "uiRowSelectPanel";
        uiRowSelectPanel.WrapContents = false;
        uiRowSelectPanel.TabIndex = 1;
        uiRowSelectPanel.Controls.Add(uiRowSelectLabel);
        uiRowSelectPanel.Controls.Add(uiRowAllLink);
        uiRowSelectPanel.Controls.Add(uiRowNoneLink);

        //
        // uiRowSelectLabel
        //
        uiRowSelectLabel.AutoSize = true;
        uiRowSelectLabel.Name = "uiRowSelectLabel";
        uiRowSelectLabel.Text = "Строки:";
        uiRowSelectLabel.Margin = new Padding(0, 3, 4, 3);

        //
        // uiRowAllLink
        //
        uiRowAllLink.AutoSize = true;
        uiRowAllLink.Name = "uiRowAllLink";
        uiRowAllLink.Text = "выбрать все";
        uiRowAllLink.Margin = new Padding(4, 3, 4, 3);
        uiRowAllLink.LinkClicked += (s, e) => SetAllRows(true);

        //
        // uiRowNoneLink
        //
        uiRowNoneLink.AutoSize = true;
        uiRowNoneLink.Name = "uiRowNoneLink";
        uiRowNoneLink.Text = "снять все";
        uiRowNoneLink.Margin = new Padding(4, 3, 4, 3);
        uiRowNoneLink.LinkClicked += (s, e) => SetAllRows(false);

        //
        // uiResultGrid
        //
        uiResultGrid.AllowUserToAddRows = false;
        uiResultGrid.AllowUserToDeleteRows = false;
        uiResultGrid.AllowUserToResizeRows = false;
        uiResultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        uiResultGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        uiResultGrid.Columns.AddRange(new DataGridViewColumn[]
        {
            uiApplyColumn,
            uiTitleColumn,
            uiTargetColumn,
            uiStatusColumn,
        });
        uiResultGrid.Dock = DockStyle.Fill;
        uiResultGrid.Margin = new Padding(0);
        uiResultGrid.Name = "uiResultGrid";
        uiResultGrid.RowHeadersVisible = false;
        uiResultGrid.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
        uiResultGrid.MultiSelect = false;
        uiResultGrid.TabIndex = 0;
        uiResultGrid.CellValueChanged += uiResultGrid_CellValueChanged;
        uiResultGrid.CurrentCellDirtyStateChanged += uiResultGrid_CurrentCellDirtyStateChanged;

        //
        // uiApplyColumn
        //
        uiApplyColumn.HeaderText = "";
        uiApplyColumn.Name = "uiApplyColumn";
        uiApplyColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        uiApplyColumn.Width = 32;
        uiApplyColumn.Resizable = DataGridViewTriState.False;
        uiApplyColumn.ToolTipText = "Применять обновление превью к этой строке";

        //
        // uiTitleColumn
        //
        uiTitleColumn.HeaderText = "Название";
        uiTitleColumn.Name = "uiTitleColumn";
        uiTitleColumn.ReadOnly = true;
        uiTitleColumn.FillWeight = 110F;

        //
        // uiTargetColumn
        //
        uiTargetColumn.HeaderText = "Площадка";
        uiTargetColumn.Name = "uiTargetColumn";
        uiTargetColumn.ReadOnly = true;
        uiTargetColumn.FillWeight = 80F;

        //
        // uiStatusColumn
        //
        uiStatusColumn.HeaderText = "Статус";
        uiStatusColumn.Name = "uiStatusColumn";
        uiStatusColumn.ReadOnly = true;
        uiStatusColumn.FillWeight = 70F;

        //
        // uiLogBox
        //
        uiLogBox.BackColor = SystemColors.Window;
        uiLogBox.BorderStyle = BorderStyle.FixedSingle;
        uiLogBox.DetectUrls = false;
        uiLogBox.Dock = DockStyle.Fill;
        uiLogBox.HideSelection = false;
        uiLogBox.Name = "uiLogBox";
        uiLogBox.ReadOnly = true;
        uiLogBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        uiLogBox.TabStop = false;
        uiLogBox.Margin = new Padding(0);

        //
        // uiStatusPanel
        //
        uiStatusPanel.AutoSize = true;
        uiStatusPanel.ColumnCount = 2;
        uiStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
        uiStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiStatusPanel.Controls.Add(uiProgressBar, 0, 0);
        uiStatusPanel.Controls.Add(uiStatusLabel, 1, 0);
        uiStatusPanel.Dock = DockStyle.Fill;
        uiStatusPanel.Name = "uiStatusPanel";
        uiStatusPanel.RowCount = 1;
        uiStatusPanel.RowStyles.Add(new RowStyle());
        uiStatusPanel.TabIndex = 3;

        //
        // uiProgressBar
        //
        uiProgressBar.Dock = DockStyle.Fill;
        uiProgressBar.Name = "uiProgressBar";
        uiProgressBar.TabIndex = 0;
        uiProgressBar.Visible = false;
        uiProgressBar.MarqueeAnimationSpeed = 30;

        //
        // uiStatusLabel
        //
        uiStatusLabel.AutoSize = true;
        uiStatusLabel.Dock = DockStyle.Fill;
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Text = "";
        uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        uiStatusLabel.Margin = new Padding(8, 0, 3, 0);

        //
        // uiButtonPanel
        //
        uiButtonPanel.AutoSize = true;
        uiButtonPanel.Controls.Add(uiCancelButton);
        uiButtonPanel.Controls.Add(uiApplyButton);
        uiButtonPanel.Dock = DockStyle.Fill;
        uiButtonPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonPanel.Name = "uiButtonPanel";
        uiButtonPanel.TabIndex = 4;

        //
        // uiCancelButton
        //
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Size = new Size(100, 27);
        uiCancelButton.TabIndex = 0;
        uiCancelButton.Text = "Закрыть";
        uiCancelButton.UseVisualStyleBackColor = true;
        uiCancelButton.Click += uiCancelButton_Click;

        //
        // uiApplyButton
        //
        uiApplyButton.Enabled = false;
        uiApplyButton.Name = "uiApplyButton";
        uiApplyButton.Size = new Size(160, 27);
        uiApplyButton.TabIndex = 1;
        uiApplyButton.Text = "Применить";
        uiApplyButton.UseVisualStyleBackColor = true;
        uiApplyButton.Click += uiApplyButton_Click;

        //
        // BatchPreviewForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(880, 640);
        Controls.Add(uiMainLayout);
        MinimumSize = new Size(720, 540);
        Name = "BatchPreviewForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Обновление превью";

        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        uiSourceGroup.ResumeLayout(false);
        uiSourceGroup.PerformLayout();
        uiSourcePanelLayout.ResumeLayout(false);
        uiSourcePanelLayout.PerformLayout();
        uiDonorLayout.ResumeLayout(false);
        uiDonorLayout.PerformLayout();
        uiFileLayout.ResumeLayout(false);
        uiFileLayout.PerformLayout();
        uiTemplateLayout.ResumeLayout(false);
        uiTemplateLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiCoverThumbnail).EndInit();
        uiTargetsGroup.ResumeLayout(false);
        uiTargetsGroup.PerformLayout();
        uiTargetsPanel.ResumeLayout(false);
        uiTargetsPanel.PerformLayout();
        uiRowSelectPanel.ResumeLayout(false);
        uiRowSelectPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiResultGrid).EndInit();
        uiGridLogSplit.Panel1.ResumeLayout(false);
        uiGridLogSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)uiGridLogSplit).EndInit();
        uiGridLogSplit.ResumeLayout(false);
        uiStatusPanel.ResumeLayout(false);
        uiStatusPanel.PerformLayout();
        uiButtonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiMainLayout;

    private GroupBox uiSourceGroup;
    private TableLayoutPanel uiSourcePanelLayout;
    private FlowLayoutPanel uiDonorLayout;
    private RadioButton uiFromSourceRadio;
    private ComboBox uiDonorComboBox;
    private FlowLayoutPanel uiFileLayout;
    private RadioButton uiFromFileRadio;
    private TextBox uiFilePathTextBox;
    private Button uiBrowseButton;
    private FlowLayoutPanel uiTemplateLayout;
    private RadioButton uiFromTemplateRadio;
    private Button uiTemplateButton;
    private ComboBox uiProfileCombo;
    private PictureBox uiCoverThumbnail;

    private GroupBox uiTargetsGroup;
    private FlowLayoutPanel uiTargetsPanel;
    private LinkLabel uiTargetsAllLink;
    private LinkLabel uiTargetsNoneLink;

    private FlowLayoutPanel uiRowSelectPanel;
    private Label uiRowSelectLabel;
    private LinkLabel uiRowAllLink;
    private LinkLabel uiRowNoneLink;

    private SplitContainer uiGridLogSplit;
    private DataGridView uiResultGrid;
    private DataGridViewCheckBoxColumn uiApplyColumn;
    private DataGridViewTextBoxColumn uiTitleColumn;
    private DataGridViewTextBoxColumn uiTargetColumn;
    private DataGridViewTextBoxColumn uiStatusColumn;

    private RichTextBox uiLogBox;

    private TableLayoutPanel uiStatusPanel;
    private ProgressBar uiProgressBar;
    private Label uiStatusLabel;

    private FlowLayoutPanel uiButtonPanel;
    private Button uiCancelButton;
    private Button uiApplyButton;
}
