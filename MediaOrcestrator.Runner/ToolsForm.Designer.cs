namespace MediaOrcestrator.Runner;

partial class ToolsForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        toolsDataGridView = new DataGridView();
        colName = new DataGridViewTextBoxColumn();
        colInstalled = new DataGridViewTextBoxColumn();
        colLatest = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        colAction = new DataGridViewButtonColumn();
        checkUpdatesButton = new Button();
        statusLabel = new Label();
        progressBar = new ProgressBar();

        ((System.ComponentModel.ISupportInitialize)toolsDataGridView).BeginInit();
        SuspendLayout();

        // toolsDataGridView
        toolsDataGridView.AllowUserToAddRows = false;
        toolsDataGridView.AllowUserToDeleteRows = false;
        toolsDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        toolsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        toolsDataGridView.Columns.AddRange(colName, colInstalled, colLatest, colStatus, colAction);
        toolsDataGridView.Location = new Point(12, 12);
        toolsDataGridView.Name = "toolsDataGridView";
        toolsDataGridView.ReadOnly = true;
        toolsDataGridView.RowHeadersVisible = false;
        toolsDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        toolsDataGridView.Size = new Size(560, 200);
        toolsDataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        colName.HeaderText = "Инструмент";
        colName.Name = "colName";
        colName.FillWeight = 25;

        colInstalled.HeaderText = "Установлена";
        colInstalled.Name = "colInstalled";
        colInstalled.FillWeight = 25;

        colLatest.HeaderText = "Доступна";
        colLatest.Name = "colLatest";
        colLatest.FillWeight = 25;

        colStatus.HeaderText = "Статус";
        colStatus.Name = "colStatus";
        colStatus.FillWeight = 25;

        colAction.HeaderText = "";
        colAction.Name = "colAction";
        colAction.Text = "";
        colAction.UseColumnTextForButtonValue = false;
        colAction.FillWeight = 20;
        colAction.ReadOnly = false;

        checkUpdatesButton.Location = new Point(12, 220);
        checkUpdatesButton.Name = "checkUpdatesButton";
        checkUpdatesButton.Size = new Size(180, 30);
        checkUpdatesButton.Text = "Проверить обновления";
        checkUpdatesButton.UseVisualStyleBackColor = true;
        checkUpdatesButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        progressBar.Location = new Point(200, 225);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(270, 20);
        progressBar.Visible = false;
        progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        statusLabel.Location = new Point(12, 258);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(560, 20);
        statusLabel.Text = "";
        statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(584, 281);
        Controls.Add(toolsDataGridView);
        Controls.Add(checkUpdatesButton);
        Controls.Add(progressBar);
        Controls.Add(statusLabel);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(500, 300);
        Name = "ToolsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Управление инструментами";
        ((System.ComponentModel.ISupportInitialize)toolsDataGridView).EndInit();
        ResumeLayout(false);
    }

    private DataGridView toolsDataGridView;
    private DataGridViewTextBoxColumn colName;
    private DataGridViewTextBoxColumn colInstalled;
    private DataGridViewTextBoxColumn colLatest;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewButtonColumn colAction;
    private Button checkUpdatesButton;
    private Label statusLabel;
    private ProgressBar progressBar;
}
