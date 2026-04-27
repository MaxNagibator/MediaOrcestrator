namespace MediaOrcestrator.Runner;

partial class CommentsViewControl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private Panel uiFiltersPanel;
    private Label uiSourceLabel;
    private ComboBox uiSourceComboBox;
    private Label uiSearchLabel;
    private TextBox uiSearchTextBox;
    private Label uiLimitLabel;
    private NumericUpDown uiLimitNumeric;
    private Button uiRefreshButton;
    private Button uiForceFetchAllButton;
    private Label uiCountLabel;
    private DataGridView uiCommentsGrid;

    private void InitializeComponent()
    {
        uiFiltersPanel = new Panel();
        uiSourceLabel = new Label();
        uiSourceComboBox = new ComboBox();
        uiSearchLabel = new Label();
        uiSearchTextBox = new TextBox();
        uiLimitLabel = new Label();
        uiLimitNumeric = new NumericUpDown();
        uiRefreshButton = new Button();
        uiForceFetchAllButton = new Button();
        uiCountLabel = new Label();
        uiCommentsGrid = new DataGridView();
        uiFiltersPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiLimitNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiCommentsGrid).BeginInit();
        SuspendLayout();
        //
        // uiFiltersPanel
        //
        uiFiltersPanel.Controls.Add(uiCountLabel);
        uiFiltersPanel.Controls.Add(uiForceFetchAllButton);
        uiFiltersPanel.Controls.Add(uiRefreshButton);
        uiFiltersPanel.Controls.Add(uiLimitNumeric);
        uiFiltersPanel.Controls.Add(uiLimitLabel);
        uiFiltersPanel.Controls.Add(uiSearchTextBox);
        uiFiltersPanel.Controls.Add(uiSearchLabel);
        uiFiltersPanel.Controls.Add(uiSourceComboBox);
        uiFiltersPanel.Controls.Add(uiSourceLabel);
        uiFiltersPanel.Dock = DockStyle.Top;
        uiFiltersPanel.Location = new Point(0, 0);
        uiFiltersPanel.Name = "uiFiltersPanel";
        uiFiltersPanel.Padding = new Padding(8, 8, 8, 8);
        uiFiltersPanel.Size = new Size(1100, 64);
        uiFiltersPanel.TabIndex = 0;
        //
        // uiSourceLabel
        //
        uiSourceLabel.AutoSize = true;
        uiSourceLabel.Location = new Point(8, 14);
        uiSourceLabel.Name = "uiSourceLabel";
        uiSourceLabel.Size = new Size(60, 15);
        uiSourceLabel.TabIndex = 0;
        uiSourceLabel.Text = "Источник:";
        //
        // uiSourceComboBox
        //
        uiSourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        uiSourceComboBox.Location = new Point(80, 11);
        uiSourceComboBox.Name = "uiSourceComboBox";
        uiSourceComboBox.Size = new Size(220, 23);
        uiSourceComboBox.TabIndex = 1;
        uiSourceComboBox.SelectedIndexChanged += uiSourceComboBox_SelectedIndexChanged;
        //
        // uiSearchLabel
        //
        uiSearchLabel.AutoSize = true;
        uiSearchLabel.Location = new Point(316, 14);
        uiSearchLabel.Name = "uiSearchLabel";
        uiSearchLabel.Size = new Size(45, 15);
        uiSearchLabel.TabIndex = 2;
        uiSearchLabel.Text = "Поиск:";
        //
        // uiSearchTextBox
        //
        uiSearchTextBox.Location = new Point(370, 11);
        uiSearchTextBox.Name = "uiSearchTextBox";
        uiSearchTextBox.PlaceholderText = "текст или автор";
        uiSearchTextBox.Size = new Size(260, 23);
        uiSearchTextBox.TabIndex = 3;
        uiSearchTextBox.KeyDown += uiSearchTextBox_KeyDown;
        //
        // uiLimitLabel
        //
        uiLimitLabel.AutoSize = true;
        uiLimitLabel.Location = new Point(646, 14);
        uiLimitLabel.Name = "uiLimitLabel";
        uiLimitLabel.Size = new Size(50, 15);
        uiLimitLabel.TabIndex = 4;
        uiLimitLabel.Text = "Лимит:";
        //
        // uiLimitNumeric
        //
        uiLimitNumeric.Increment = 100;
        uiLimitNumeric.Location = new Point(700, 11);
        uiLimitNumeric.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        uiLimitNumeric.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
        uiLimitNumeric.Name = "uiLimitNumeric";
        uiLimitNumeric.Size = new Size(80, 23);
        uiLimitNumeric.TabIndex = 5;
        uiLimitNumeric.Value = new decimal(new int[] { 1000, 0, 0, 0 });
        //
        // uiRefreshButton
        //
        uiRefreshButton.Location = new Point(792, 10);
        uiRefreshButton.Name = "uiRefreshButton";
        uiRefreshButton.Size = new Size(110, 25);
        uiRefreshButton.TabIndex = 6;
        uiRefreshButton.Text = "Обновить";
        uiRefreshButton.UseVisualStyleBackColor = true;
        uiRefreshButton.Click += uiRefreshButton_Click;
        //
        // uiForceFetchAllButton
        //
        uiForceFetchAllButton.Enabled = false;
        uiForceFetchAllButton.Location = new Point(908, 10);
        uiForceFetchAllButton.Name = "uiForceFetchAllButton";
        uiForceFetchAllButton.Size = new Size(190, 25);
        uiForceFetchAllButton.TabIndex = 7;
        uiForceFetchAllButton.Text = "Загрузить все из источника";
        uiForceFetchAllButton.UseVisualStyleBackColor = true;
        uiForceFetchAllButton.Click += uiForceFetchAllButton_Click;
        //
        // uiCountLabel
        //
        uiCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        uiCountLabel.AutoSize = true;
        uiCountLabel.ForeColor = Color.FromArgb(100, 100, 100);
        uiCountLabel.Location = new Point(8, 38);
        uiCountLabel.Name = "uiCountLabel";
        uiCountLabel.Size = new Size(60, 15);
        uiCountLabel.TabIndex = 8;
        uiCountLabel.Text = "Найдено: 0";
        //
        // uiCommentsGrid
        //
        uiCommentsGrid.AllowUserToAddRows = false;
        uiCommentsGrid.AllowUserToDeleteRows = false;
        uiCommentsGrid.AllowUserToResizeRows = false;
        uiCommentsGrid.AutoGenerateColumns = false;
        uiCommentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        uiCommentsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        uiCommentsGrid.Dock = DockStyle.Fill;
        uiCommentsGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
        uiCommentsGrid.Location = new Point(0, 64);
        uiCommentsGrid.Name = "uiCommentsGrid";
        uiCommentsGrid.ReadOnly = true;
        uiCommentsGrid.RowHeadersVisible = false;
        uiCommentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        uiCommentsGrid.Size = new Size(1100, 600);
        uiCommentsGrid.TabIndex = 1;
        uiCommentsGrid.CellDoubleClick += uiCommentsGrid_CellDoubleClick;
        //
        // CommentsViewControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiCommentsGrid);
        Controls.Add(uiFiltersPanel);
        Name = "CommentsViewControl";
        Size = new Size(1100, 650);
        uiFiltersPanel.ResumeLayout(false);
        uiFiltersPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiLimitNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiCommentsGrid).EndInit();
        ResumeLayout(false);
    }

    #endregion
}
