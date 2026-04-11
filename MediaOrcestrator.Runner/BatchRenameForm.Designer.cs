namespace MediaOrcestrator.Runner;

partial class BatchRenameForm
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
        uiInputPanel = new TableLayoutPanel();
        uiFindLabel = new Label();
        uiFindTextBox = new TextBox();
        uiReplaceLabel = new Label();
        uiReplaceTextBox = new TextBox();
        uiPreviewButton = new Button();
        uiPreviewGrid = new DataGridView();
        uiOldTitleColumn = new DataGridViewTextBoxColumn();
        uiNewTitleColumn = new DataGridViewTextBoxColumn();
        uiStatusColumn = new DataGridViewTextBoxColumn();
        uiStatusLabel = new Label();
        uiButtonPanel = new FlowLayoutPanel();
        uiCancelButton = new Button();
        uiApplyButton = new Button();
        uiMainLayout.SuspendLayout();
        uiInputPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreviewGrid).BeginInit();
        uiButtonPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiMainLayout
        //
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiInputPanel, 0, 0);
        uiMainLayout.Controls.Add(uiPreviewGrid, 0, 1);
        uiMainLayout.Controls.Add(uiStatusLabel, 0, 2);
        uiMainLayout.Controls.Add(uiButtonPanel, 0, 3);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Location = new Point(0, 0);
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(10);
        uiMainLayout.RowCount = 4;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.Size = new Size(684, 461);
        uiMainLayout.TabIndex = 0;
        //
        // uiInputPanel
        //
        uiInputPanel.AutoSize = true;
        uiInputPanel.ColumnCount = 3;
        uiInputPanel.ColumnStyles.Add(new ColumnStyle());
        uiInputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiInputPanel.ColumnStyles.Add(new ColumnStyle());
        uiInputPanel.Controls.Add(uiFindLabel, 0, 0);
        uiInputPanel.Controls.Add(uiFindTextBox, 1, 0);
        uiInputPanel.Controls.Add(uiReplaceLabel, 0, 1);
        uiInputPanel.Controls.Add(uiReplaceTextBox, 1, 1);
        uiInputPanel.Controls.Add(uiPreviewButton, 2, 0);
        uiInputPanel.Dock = DockStyle.Fill;
        uiInputPanel.Location = new Point(13, 13);
        uiInputPanel.Name = "uiInputPanel";
        uiInputPanel.RowCount = 2;
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.RowStyles.Add(new RowStyle());
        uiInputPanel.Size = new Size(658, 60);
        uiInputPanel.TabIndex = 0;
        //
        // uiFindLabel
        //
        uiFindLabel.Anchor = AnchorStyles.Left;
        uiFindLabel.AutoSize = true;
        uiFindLabel.Name = "uiFindLabel";
        uiFindLabel.Text = "Найти:";
        //
        // uiFindTextBox
        //
        uiFindTextBox.Dock = DockStyle.Fill;
        uiFindTextBox.Name = "uiFindTextBox";
        uiFindTextBox.TabIndex = 0;
        //
        // uiReplaceLabel
        //
        uiReplaceLabel.Anchor = AnchorStyles.Left;
        uiReplaceLabel.AutoSize = true;
        uiReplaceLabel.Name = "uiReplaceLabel";
        uiReplaceLabel.Text = "Заменить:";
        //
        // uiReplaceTextBox
        //
        uiReplaceTextBox.Dock = DockStyle.Fill;
        uiReplaceTextBox.Name = "uiReplaceTextBox";
        uiReplaceTextBox.TabIndex = 1;
        //
        // uiPreviewButton
        //
        uiPreviewButton.AutoSize = true;
        uiPreviewButton.Name = "uiPreviewButton";
        uiPreviewButton.TabIndex = 2;
        uiPreviewButton.Text = "Обновить превью";
        uiPreviewButton.UseVisualStyleBackColor = true;
        uiPreviewButton.Click += uiPreviewButton_Click;
        //
        // uiPreviewGrid
        //
        uiPreviewGrid.AllowUserToAddRows = false;
        uiPreviewGrid.AllowUserToDeleteRows = false;
        uiPreviewGrid.AllowUserToResizeRows = false;
        uiPreviewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        uiPreviewGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        uiPreviewGrid.Columns.AddRange(new DataGridViewColumn[] { uiOldTitleColumn, uiNewTitleColumn, uiStatusColumn });
        uiPreviewGrid.Dock = DockStyle.Fill;
        uiPreviewGrid.Name = "uiPreviewGrid";
        uiPreviewGrid.ReadOnly = true;
        uiPreviewGrid.RowHeadersVisible = false;
        uiPreviewGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        uiPreviewGrid.TabIndex = 1;
        //
        // uiOldTitleColumn
        //
        uiOldTitleColumn.HeaderText = "Было";
        uiOldTitleColumn.Name = "uiOldTitleColumn";
        uiOldTitleColumn.ReadOnly = true;
        //
        // uiNewTitleColumn
        //
        uiNewTitleColumn.HeaderText = "Стало";
        uiNewTitleColumn.Name = "uiNewTitleColumn";
        uiNewTitleColumn.ReadOnly = true;
        //
        // uiStatusColumn
        //
        uiStatusColumn.FillWeight = 50F;
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
        uiApplyButton.Click += uiApplyButton_Click;
        //
        // BatchRenameForm
        //
        AcceptButton = uiPreviewButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(684, 461);
        Controls.Add(uiMainLayout);
        MinimumSize = new Size(500, 400);
        Name = "BatchRenameForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Пакетное переименование";
        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        uiInputPanel.ResumeLayout(false);
        uiInputPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreviewGrid).EndInit();
        uiButtonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiMainLayout;
    private TableLayoutPanel uiInputPanel;
    private Label uiFindLabel;
    private TextBox uiFindTextBox;
    private Label uiReplaceLabel;
    private TextBox uiReplaceTextBox;
    private Button uiPreviewButton;
    private DataGridView uiPreviewGrid;
    private DataGridViewTextBoxColumn uiOldTitleColumn;
    private DataGridViewTextBoxColumn uiNewTitleColumn;
    private DataGridViewTextBoxColumn uiStatusColumn;
    private Label uiStatusLabel;
    private FlowLayoutPanel uiButtonPanel;
    private Button uiCancelButton;
    private Button uiApplyButton;
}
