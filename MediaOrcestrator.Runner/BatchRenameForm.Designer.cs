namespace MediaOrcestrator.Runner;

partial class BatchRenameForm
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
        uiPreviewTimer = new System.Windows.Forms.Timer(components);

        uiMainLayout = new TableLayoutPanel();
        uiInputPanel = new TableLayoutPanel();
        uiModeLabel = new Label();
        uiModeCombo = new ComboBox();
        uiIgnoreCaseCheck = new CheckBox();
        uiFindLabel = new Label();
        uiFindTextBox = new TextBox();
        uiReplaceLabel = new Label();
        uiReplaceTextBox = new TextBox();
        uiErrorLabel = new Label();
        uiSourcesLabel = new Label();
        uiSourcesPanel = new FlowLayoutPanel();
        uiSourcesAllLink = new LinkLabel();
        uiSourcesNoneLink = new LinkLabel();

        uiRowSelectPanel = new FlowLayoutPanel();
        uiRowSelectLabel = new Label();
        uiRowAllLink = new LinkLabel();
        uiRowNoneLink = new LinkLabel();
        uiStopOnErrorCheck = new CheckBox();

        uiPreviewGrid = new DataGridView();
        uiApplyColumn = new DataGridViewCheckBoxColumn();
        uiOldTitleColumn = new DataGridViewTextBoxColumn();
        uiNewTitleColumn = new DataGridViewTextBoxColumn();
        uiSourcesColumn = new DataGridViewTextBoxColumn();

        uiLogBox = new RichTextBox();

        uiStatusPanel = new TableLayoutPanel();
        uiProgressBar = new ProgressBar();
        uiStatusLabel = new Label();
        uiResetEditsLink = new LinkLabel();

        uiButtonPanel = new FlowLayoutPanel();
        uiCancelButton = new Button();
        uiApplyButton = new Button();

        uiMainLayout.SuspendLayout();
        uiInputPanel.SuspendLayout();
        uiRowSelectPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreviewGrid).BeginInit();
        uiStatusPanel.SuspendLayout();
        uiButtonPanel.SuspendLayout();
        SuspendLayout();

        //
        // uiPreviewTimer
        //
        uiPreviewTimer.Interval = 200;
        uiPreviewTimer.Tick += uiPreviewTimer_Tick;

        //
        // uiMainLayout
        //
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiInputPanel, 0, 0);
        uiMainLayout.Controls.Add(uiRowSelectPanel, 0, 1);
        uiMainLayout.Controls.Add(uiPreviewGrid, 0, 2);
        uiMainLayout.Controls.Add(uiLogBox, 0, 3);
        uiMainLayout.Controls.Add(uiStatusPanel, 0, 4);
        uiMainLayout.Controls.Add(uiButtonPanel, 0, 5);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Location = new Point(0, 0);
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(10);
        uiMainLayout.RowCount = 6;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.Size = new Size(820, 620);
        uiMainLayout.TabIndex = 0;

        //
        // uiInputPanel
        //
        uiInputPanel.AutoSize = true;
        uiInputPanel.ColumnCount = 4;
        uiInputPanel.ColumnStyles.Add(new ColumnStyle());
        uiInputPanel.ColumnStyles.Add(new ColumnStyle());
        uiInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiInputPanel.ColumnStyles.Add(new ColumnStyle());
        uiInputPanel.Controls.Add(uiModeLabel, 0, 0);
        uiInputPanel.Controls.Add(uiModeCombo, 1, 0);
        uiInputPanel.Controls.Add(uiIgnoreCaseCheck, 2, 0);
        uiInputPanel.Controls.Add(uiFindLabel, 0, 1);
        uiInputPanel.SetColumnSpan(uiFindTextBox, 3);
        uiInputPanel.Controls.Add(uiFindTextBox, 1, 1);
        uiInputPanel.Controls.Add(uiReplaceLabel, 0, 2);
        uiInputPanel.SetColumnSpan(uiReplaceTextBox, 3);
        uiInputPanel.Controls.Add(uiReplaceTextBox, 1, 2);
        uiInputPanel.SetColumnSpan(uiErrorLabel, 4);
        uiInputPanel.Controls.Add(uiErrorLabel, 0, 3);
        uiInputPanel.Controls.Add(uiSourcesLabel, 0, 4);
        uiInputPanel.SetColumnSpan(uiSourcesPanel, 3);
        uiInputPanel.Controls.Add(uiSourcesPanel, 1, 4);
        uiInputPanel.Dock = DockStyle.Fill;
        uiInputPanel.Location = new Point(13, 13);
        uiInputPanel.Name = "uiInputPanel";
        uiInputPanel.RowCount = 5;
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.TabIndex = 0;

        //
        // uiModeLabel
        //
        uiModeLabel.Anchor = AnchorStyles.Left;
        uiModeLabel.AutoSize = true;
        uiModeLabel.Name = "uiModeLabel";
        uiModeLabel.Text = "Режим:";
        uiModeLabel.Margin = new Padding(3, 6, 3, 3);

        //
        // uiModeCombo
        //
        uiModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        uiModeCombo.Name = "uiModeCombo";
        uiModeCombo.Size = new Size(180, 23);
        uiModeCombo.TabIndex = 0;
        uiModeCombo.SelectedIndexChanged += uiAnyOption_Changed;

        //
        // uiIgnoreCaseCheck
        //
        uiIgnoreCaseCheck.Anchor = AnchorStyles.Left;
        uiIgnoreCaseCheck.AutoSize = true;
        uiIgnoreCaseCheck.Name = "uiIgnoreCaseCheck";
        uiIgnoreCaseCheck.Text = "Без учёта регистра";
        uiIgnoreCaseCheck.TabIndex = 1;
        uiIgnoreCaseCheck.UseVisualStyleBackColor = true;
        uiIgnoreCaseCheck.CheckedChanged += uiAnyOption_Changed;

        //
        // uiFindLabel
        //
        uiFindLabel.Anchor = AnchorStyles.Left;
        uiFindLabel.AutoSize = true;
        uiFindLabel.Name = "uiFindLabel";
        uiFindLabel.Text = "Найти:";
        uiFindLabel.Margin = new Padding(3, 6, 3, 3);

        //
        // uiFindTextBox
        //
        uiFindTextBox.Dock = DockStyle.Fill;
        uiFindTextBox.Name = "uiFindTextBox";
        uiFindTextBox.TabIndex = 2;
        uiFindTextBox.TextChanged += uiAnyOption_Changed;

        //
        // uiReplaceLabel
        //
        uiReplaceLabel.Anchor = AnchorStyles.Left;
        uiReplaceLabel.AutoSize = true;
        uiReplaceLabel.Name = "uiReplaceLabel";
        uiReplaceLabel.Text = "Заменить:";
        uiReplaceLabel.Margin = new Padding(3, 6, 3, 3);

        //
        // uiReplaceTextBox
        //
        uiReplaceTextBox.Dock = DockStyle.Fill;
        uiReplaceTextBox.Name = "uiReplaceTextBox";
        uiReplaceTextBox.TabIndex = 3;
        uiReplaceTextBox.TextChanged += uiAnyOption_Changed;

        //
        // uiErrorLabel
        //
        uiErrorLabel.AutoSize = true;
        uiErrorLabel.Dock = DockStyle.Fill;
        uiErrorLabel.ForeColor = Color.DarkRed;
        uiErrorLabel.Name = "uiErrorLabel";
        uiErrorLabel.Text = "";
        uiErrorLabel.Margin = new Padding(3, 4, 3, 0);

        //
        // uiSourcesLabel
        //
        uiSourcesLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        uiSourcesLabel.AutoSize = true;
        uiSourcesLabel.Name = "uiSourcesLabel";
        uiSourcesLabel.Text = "Площадки:";
        uiSourcesLabel.Margin = new Padding(3, 8, 3, 3);

        //
        // uiSourcesPanel
        //
        uiSourcesPanel.AutoSize = true;
        uiSourcesPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiSourcesPanel.Dock = DockStyle.Fill;
        uiSourcesPanel.FlowDirection = FlowDirection.LeftToRight;
        uiSourcesPanel.Margin = new Padding(0, 4, 0, 0);
        uiSourcesPanel.Name = "uiSourcesPanel";
        uiSourcesPanel.WrapContents = true;
        uiSourcesPanel.TabIndex = 4;

        //
        // uiSourcesAllLink
        //
        uiSourcesAllLink.AutoSize = true;
        uiSourcesAllLink.Name = "uiSourcesAllLink";
        uiSourcesAllLink.Text = "все";
        uiSourcesAllLink.Margin = new Padding(8, 6, 4, 3);
        uiSourcesAllLink.LinkClicked += (s, e) => SetAllSources(true);

        //
        // uiSourcesNoneLink
        //
        uiSourcesNoneLink.AutoSize = true;
        uiSourcesNoneLink.Name = "uiSourcesNoneLink";
        uiSourcesNoneLink.Text = "ни одного";
        uiSourcesNoneLink.Margin = new Padding(4, 6, 4, 3);
        uiSourcesNoneLink.LinkClicked += (s, e) => SetAllSources(false);

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
        uiRowSelectPanel.Controls.Add(uiStopOnErrorCheck);

        //
        // uiStopOnErrorCheck
        //
        uiStopOnErrorCheck.AutoSize = true;
        uiStopOnErrorCheck.Name = "uiStopOnErrorCheck";
        uiStopOnErrorCheck.Text = "Останавливать при ошибке";
        uiStopOnErrorCheck.Margin = new Padding(20, 3, 4, 3);
        uiStopOnErrorCheck.UseVisualStyleBackColor = true;

        //
        // uiRowSelectLabel
        //
        uiRowSelectLabel.AutoSize = true;
        uiRowSelectLabel.Name = "uiRowSelectLabel";
        uiRowSelectLabel.Text = "Записи:";
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
        // uiPreviewGrid
        //
        uiPreviewGrid.AllowUserToAddRows = false;
        uiPreviewGrid.AllowUserToDeleteRows = false;
        uiPreviewGrid.AllowUserToResizeRows = false;
        uiPreviewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        uiPreviewGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        uiPreviewGrid.Columns.AddRange(new DataGridViewColumn[]
        {
            uiApplyColumn,
            uiOldTitleColumn,
            uiNewTitleColumn,
            uiSourcesColumn,
        });
        uiPreviewGrid.Dock = DockStyle.Fill;
        uiPreviewGrid.Name = "uiPreviewGrid";
        uiPreviewGrid.RowHeadersVisible = false;
        uiPreviewGrid.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
        uiPreviewGrid.MultiSelect = false;
        uiPreviewGrid.TabIndex = 2;
        uiPreviewGrid.CellValueChanged += uiPreviewGrid_CellValueChanged;
        uiPreviewGrid.CurrentCellDirtyStateChanged += uiPreviewGrid_CurrentCellDirtyStateChanged;

        //
        // uiApplyColumn
        //
        uiApplyColumn.HeaderText = "";
        uiApplyColumn.Name = "uiApplyColumn";
        uiApplyColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        uiApplyColumn.Width = 32;
        uiApplyColumn.Resizable = DataGridViewTriState.False;
        uiApplyColumn.ToolTipText = "Применять переименование к этому медиа";

        //
        // uiOldTitleColumn
        //
        uiOldTitleColumn.HeaderText = "Было";
        uiOldTitleColumn.Name = "uiOldTitleColumn";
        uiOldTitleColumn.ReadOnly = true;
        uiOldTitleColumn.FillWeight = 85F;
        uiOldTitleColumn.DefaultCellStyle.ForeColor = Color.Gray;

        //
        // uiNewTitleColumn
        //
        uiNewTitleColumn.HeaderText = "Стало (двойной клик — править)";
        uiNewTitleColumn.Name = "uiNewTitleColumn";
        uiNewTitleColumn.FillWeight = 125F;

        //
        // uiSourcesColumn
        //
        uiSourcesColumn.HeaderText = "Площадки";
        uiSourcesColumn.Name = "uiSourcesColumn";
        uiSourcesColumn.ReadOnly = true;
        uiSourcesColumn.FillWeight = 95F;

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
        uiLogBox.Margin = new Padding(3, 6, 3, 6);

        //
        // uiStatusPanel
        //
        uiStatusPanel.AutoSize = true;
        uiStatusPanel.ColumnCount = 3;
        uiStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
        uiStatusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiStatusPanel.ColumnStyles.Add(new ColumnStyle());
        uiStatusPanel.Controls.Add(uiProgressBar, 0, 0);
        uiStatusPanel.Controls.Add(uiStatusLabel, 1, 0);
        uiStatusPanel.Controls.Add(uiResetEditsLink, 2, 0);
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
        // uiResetEditsLink
        //
        uiResetEditsLink.Anchor = AnchorStyles.Right;
        uiResetEditsLink.AutoSize = true;
        uiResetEditsLink.Name = "uiResetEditsLink";
        uiResetEditsLink.Text = "Сбросить ручные правки";
        uiResetEditsLink.Visible = false;
        uiResetEditsLink.LinkClicked += uiResetEditsLink_LinkClicked;

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
        // BatchRenameForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(820, 620);
        Controls.Add(uiMainLayout);
        MinimumSize = new Size(620, 520);
        Name = "BatchRenameForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Пакетное переименование";

        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        uiInputPanel.ResumeLayout(false);
        uiInputPanel.PerformLayout();
        uiRowSelectPanel.ResumeLayout(false);
        uiRowSelectPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreviewGrid).EndInit();
        uiStatusPanel.ResumeLayout(false);
        uiStatusPanel.PerformLayout();
        uiButtonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Timer uiPreviewTimer;
    private TableLayoutPanel uiMainLayout;
    private TableLayoutPanel uiInputPanel;
    private Label uiModeLabel;
    private ComboBox uiModeCombo;
    private CheckBox uiIgnoreCaseCheck;
    private Label uiFindLabel;
    private TextBox uiFindTextBox;
    private Label uiReplaceLabel;
    private TextBox uiReplaceTextBox;
    private Label uiErrorLabel;
    private Label uiSourcesLabel;
    private FlowLayoutPanel uiSourcesPanel;
    private LinkLabel uiSourcesAllLink;
    private LinkLabel uiSourcesNoneLink;
    private FlowLayoutPanel uiRowSelectPanel;
    private Label uiRowSelectLabel;
    private LinkLabel uiRowAllLink;
    private LinkLabel uiRowNoneLink;
    private CheckBox uiStopOnErrorCheck;
    private DataGridView uiPreviewGrid;
    private DataGridViewCheckBoxColumn uiApplyColumn;
    private DataGridViewTextBoxColumn uiOldTitleColumn;
    private DataGridViewTextBoxColumn uiNewTitleColumn;
    private DataGridViewTextBoxColumn uiSourcesColumn;
    private RichTextBox uiLogBox;
    private TableLayoutPanel uiStatusPanel;
    private ProgressBar uiProgressBar;
    private Label uiStatusLabel;
    private LinkLabel uiResetEditsLink;
    private FlowLayoutPanel uiButtonPanel;
    private Button uiCancelButton;
    private Button uiApplyButton;
}
