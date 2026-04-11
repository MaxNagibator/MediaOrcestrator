namespace MediaOrcestrator.Runner;

partial class BatchPreviewForm
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
        uiMainLayout = new TableLayoutPanel();
        uiTopLayout = new TableLayoutPanel();
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
        uiTargetsListBox = new CheckedListBox();
        uiResultGrid = new DataGridView();
        uiTitleColumn = new DataGridViewTextBoxColumn();
        uiTargetColumn = new DataGridViewTextBoxColumn();
        uiStatusColumn = new DataGridViewTextBoxColumn();
        uiStatusLabel = new Label();
        uiButtonPanel = new FlowLayoutPanel();
        uiCancelButton = new Button();
        uiApplyButton = new Button();
        uiMainLayout.SuspendLayout();
        uiTopLayout.SuspendLayout();
        uiSourceGroup.SuspendLayout();
        uiSourcePanelLayout.SuspendLayout();
        uiDonorLayout.SuspendLayout();
        uiFileLayout.SuspendLayout();
        uiTemplateLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiCoverThumbnail).BeginInit();
        uiTargetsGroup.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiResultGrid).BeginInit();
        uiButtonPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiMainLayout
        //
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiTopLayout, 0, 0);
        uiMainLayout.Controls.Add(uiResultGrid, 0, 1);
        uiMainLayout.Controls.Add(uiStatusLabel, 0, 2);
        uiMainLayout.Controls.Add(uiButtonPanel, 0, 3);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(10);
        uiMainLayout.RowCount = 4;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.TabIndex = 0;
        //
        // uiTopLayout
        //
        uiTopLayout.AutoSize = true;
        uiTopLayout.ColumnCount = 2;
        uiTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        uiTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        uiTopLayout.Controls.Add(uiSourceGroup, 0, 0);
        uiTopLayout.Controls.Add(uiTargetsGroup, 1, 0);
        uiTopLayout.Dock = DockStyle.Fill;
        uiTopLayout.Name = "uiTopLayout";
        uiTopLayout.RowCount = 1;
        uiTopLayout.RowStyles.Add(new RowStyle());
        uiTopLayout.TabIndex = 0;
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
        uiDonorComboBox.Name = "uiDonorComboBox";
        uiDonorComboBox.Size = new Size(250, 23);
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
        uiFromFileRadio.Name = "uiFromFileRadio";
        uiFromFileRadio.TabIndex = 0;
        uiFromFileRadio.Text = "Из файла:";
        uiFromFileRadio.UseVisualStyleBackColor = true;
        uiFromFileRadio.CheckedChanged += OnFromFileCheckedChanged;
        //
        // uiFilePathTextBox
        //
        uiFilePathTextBox.Name = "uiFilePathTextBox";
        uiFilePathTextBox.ReadOnly = true;
        uiFilePathTextBox.Size = new Size(200, 23);
        uiFilePathTextBox.TabIndex = 1;
        //
        // uiBrowseButton
        //
        uiBrowseButton.AutoSize = true;
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
        uiFromTemplateRadio.Name = "uiFromTemplateRadio";
        uiFromTemplateRadio.TabIndex = 0;
        uiFromTemplateRadio.Text = "Из шаблона:";
        uiFromTemplateRadio.UseVisualStyleBackColor = true;
        uiFromTemplateRadio.CheckedChanged += OnFromTemplateCheckedChanged;
        //
        // uiTemplateButton
        //
        uiTemplateButton.AutoSize = true;
        uiTemplateButton.Name = "uiTemplateButton";
        uiTemplateButton.TabIndex = 1;
        uiTemplateButton.Text = "Настроить...";
        uiTemplateButton.UseVisualStyleBackColor = true;
        uiTemplateButton.Click += OnTemplateButtonClick;
        //
        // uiProfileCombo
        //
        uiProfileCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        uiProfileCombo.Margin = new Padding(6, 3, 0, 0);
        uiProfileCombo.Name = "uiProfileCombo";
        uiProfileCombo.Size = new Size(160, 23);
        uiProfileCombo.TabIndex = 2;
        uiProfileCombo.SelectedIndexChanged += OnProfileComboSelectedIndexChanged;
        //
        // uiCoverThumbnail
        //
        uiCoverThumbnail.BackColor = Color.Black;
        uiCoverThumbnail.BorderStyle = BorderStyle.FixedSingle;
        uiCoverThumbnail.Margin = new Padding(6, 0, 0, 0);
        uiCoverThumbnail.Name = "uiCoverThumbnail";
        uiCoverThumbnail.Size = new Size(160, 90);
        uiCoverThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        uiCoverThumbnail.TabIndex = 3;
        uiCoverThumbnail.TabStop = false;
        //
        // uiTargetsGroup
        //
        uiTargetsGroup.Controls.Add(uiTargetsListBox);
        uiTargetsGroup.Dock = DockStyle.Fill;
        uiTargetsGroup.Name = "uiTargetsGroup";
        uiTargetsGroup.TabIndex = 1;
        uiTargetsGroup.TabStop = false;
        uiTargetsGroup.Text = "Куда загрузить";
        //
        // uiTargetsListBox
        //
        uiTargetsListBox.CheckOnClick = true;
        uiTargetsListBox.Dock = DockStyle.Fill;
        uiTargetsListBox.Name = "uiTargetsListBox";
        uiTargetsListBox.TabIndex = 0;
        uiTargetsListBox.ItemCheck += OnTargetsItemCheck;
        //
        // uiResultGrid
        //
        uiResultGrid.AllowUserToAddRows = false;
        uiResultGrid.AllowUserToDeleteRows = false;
        uiResultGrid.AllowUserToResizeRows = false;
        uiResultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        uiResultGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        uiResultGrid.Columns.AddRange(new DataGridViewColumn[] { uiTitleColumn, uiTargetColumn, uiStatusColumn });
        uiResultGrid.Dock = DockStyle.Fill;
        uiResultGrid.Name = "uiResultGrid";
        uiResultGrid.ReadOnly = true;
        uiResultGrid.RowHeadersVisible = false;
        uiResultGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        uiResultGrid.TabIndex = 1;
        //
        // uiTitleColumn
        //
        uiTitleColumn.HeaderText = "Название";
        uiTitleColumn.Name = "uiTitleColumn";
        uiTitleColumn.ReadOnly = true;
        //
        // uiTargetColumn
        //
        uiTargetColumn.HeaderText = "Источник";
        uiTargetColumn.Name = "uiTargetColumn";
        uiTargetColumn.ReadOnly = true;
        //
        // uiStatusColumn
        //
        uiStatusColumn.FillWeight = 60F;
        uiStatusColumn.HeaderText = "Статус";
        uiStatusColumn.Name = "uiStatusColumn";
        uiStatusColumn.ReadOnly = true;
        //
        // uiStatusLabel
        //
        uiStatusLabel.AutoSize = true;
        uiStatusLabel.Dock = DockStyle.Fill;
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.TabIndex = 2;
        uiStatusLabel.Text = "";
        //
        // uiButtonPanel
        //
        uiButtonPanel.AutoSize = true;
        uiButtonPanel.Controls.Add(uiCancelButton);
        uiButtonPanel.Controls.Add(uiApplyButton);
        uiButtonPanel.Dock = DockStyle.Fill;
        uiButtonPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonPanel.Name = "uiButtonPanel";
        uiButtonPanel.TabIndex = 3;
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
        // uiApplyButton
        //
        uiApplyButton.Enabled = false;
        uiApplyButton.Name = "uiApplyButton";
        uiApplyButton.Size = new Size(100, 27);
        uiApplyButton.TabIndex = 1;
        uiApplyButton.Text = "Применить";
        uiApplyButton.UseVisualStyleBackColor = true;
        uiApplyButton.Click += OnApplyButtonClick;
        //
        // BatchPreviewForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(734, 511);
        Controls.Add(uiMainLayout);
        MinimumSize = new Size(600, 450);
        Name = "BatchPreviewForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Обновление превью";
        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        uiTopLayout.ResumeLayout(false);
        uiTopLayout.PerformLayout();
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
        ((System.ComponentModel.ISupportInitialize)uiResultGrid).EndInit();
        uiButtonPanel.ResumeLayout(false);
        uiButtonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiMainLayout;
    private TableLayoutPanel uiTopLayout;
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
    private CheckedListBox uiTargetsListBox;
    private DataGridView uiResultGrid;
    private DataGridViewTextBoxColumn uiTitleColumn;
    private DataGridViewTextBoxColumn uiTargetColumn;
    private DataGridViewTextBoxColumn uiStatusColumn;
    private Label uiStatusLabel;
    private FlowLayoutPanel uiButtonPanel;
    private Button uiCancelButton;
    private Button uiApplyButton;
}
