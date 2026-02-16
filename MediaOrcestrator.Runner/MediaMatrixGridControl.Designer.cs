using System.Windows.Forms;
using System.Drawing;

namespace MediaOrcestrator.Runner
{
    partial class MediaMatrixGridControl : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Threading.Timer _searchDebounceTimer;
        private ContextMenuStrip _contextMenu;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                
                if (_searchDebounceTimer != null)
                {
                    _searchDebounceTimer.Dispose();
                    _searchDebounceTimer = null;
                }
                
                if (_contextMenu != null)
                {
                    _contextMenu.Dispose();
                    _contextMenu = null;
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            uiMediaGrid = new OptimizedMediaGridView();
            uiRefreshButton = new Button();
            uiSearchTextBox = new TextBox();
            uiMergerSelectedMediaButton = new Button();
            uiLoadingLabel = new Label();
            uiStatusStrip = new StatusStrip();
            uiTotalCountLabel = new ToolStripStatusLabel();
            uiFilteredCountLabel = new ToolStripStatusLabel();
            uiToolStrip = new ToolStrip();
            uiSearchLabel = new ToolStripLabel();
            uiSearchToolStripTextBox = new ToolStripTextBox();
            uiClearSearchButton = new ToolStripButton();
            uiStatusLabel = new ToolStripLabel();
            uiStatusFilterComboBox = new ToolStripComboBox();
            uiRelationsDropDownButton = new ToolStripDropDownButton();
            uiSelectAllButton = new ToolStripButton();
            uiDeselectAllButton = new ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).BeginInit();
            uiStatusStrip.SuspendLayout();
            uiToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // uiToolStrip
            // 
            uiToolStrip.Items.AddRange(new ToolStripItem[] { uiSearchLabel, uiSearchToolStripTextBox, uiClearSearchButton, uiStatusLabel, uiStatusFilterComboBox, uiRelationsDropDownButton, uiSelectAllButton, uiDeselectAllButton });
            uiToolStrip.Location = new Point(0, 0);
            uiToolStrip.Name = "uiToolStrip";
            uiToolStrip.Size = new Size(595, 25);
            uiToolStrip.TabIndex = 7;
            // 
            // uiSearchLabel
            // 
            uiSearchLabel.Name = "uiSearchLabel";
            uiSearchLabel.Size = new Size(45, 22);
            uiSearchLabel.Text = "Поиск:";
            // 
            // uiSearchToolStripTextBox
            // 
            uiSearchToolStripTextBox.Name = "uiSearchToolStripTextBox";
            uiSearchToolStripTextBox.Size = new Size(200, 25);
            uiSearchToolStripTextBox.TextChanged += uiSearchToolStripTextBox_TextChanged;
            // 
            // uiClearSearchButton
            // 
            uiClearSearchButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            uiClearSearchButton.Name = "uiClearSearchButton";
            uiClearSearchButton.Size = new Size(65, 22);
            uiClearSearchButton.Text = "Очистить";
            uiClearSearchButton.Click += uiClearSearchButton_Click;
            // 
            // uiStatusLabel
            // 
            uiStatusLabel.Name = "uiStatusLabel";
            uiStatusLabel.Size = new Size(48, 22);
            uiStatusLabel.Text = "Статус:";
            uiStatusLabel.Margin = new Padding(10, 1, 0, 2);
            // 
            // uiStatusFilterComboBox
            // 
            uiStatusFilterComboBox.Name = "uiStatusFilterComboBox";
            uiStatusFilterComboBox.Size = new Size(100, 25);
            uiStatusFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiStatusFilterComboBox.Items.AddRange(new object[] { "Все", "OK", "Ошибка", "Нет" });
            uiStatusFilterComboBox.SelectedIndex = 0;
            uiStatusFilterComboBox.SelectedIndexChanged += uiStatusFilterComboBox_SelectedIndexChanged;
            // 
            // uiRelationsDropDownButton
            // 
            uiRelationsDropDownButton.Name = "uiRelationsDropDownButton";
            uiRelationsDropDownButton.Size = new Size(130, 22);
            uiRelationsDropDownButton.Text = "Фильтр по связям";
            uiRelationsDropDownButton.Margin = new Padding(10, 1, 0, 2);
            // 
            // uiSelectAllButton
            // 
            uiSelectAllButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            uiSelectAllButton.Name = "uiSelectAllButton";
            uiSelectAllButton.Size = new Size(90, 22);
            uiSelectAllButton.Text = "Выбрать все";
            uiSelectAllButton.Margin = new Padding(10, 1, 0, 2);
            uiSelectAllButton.Click += uiSelectAllButton_Click;
            // 
            // uiDeselectAllButton
            // 
            uiDeselectAllButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            uiDeselectAllButton.Name = "uiDeselectAllButton";
            uiDeselectAllButton.Size = new Size(110, 22);
            uiDeselectAllButton.Text = "Снять выделение";
            uiDeselectAllButton.Click += uiDeselectAllButton_Click;
            // 
            // uMediaGrid
            // 
            uiMediaGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiMediaGrid.Location = new Point(0, 65);
            uiMediaGrid.Name = "uMediaGrid";
            uiMediaGrid.Size = new Size(595, 313);
            uiMediaGrid.TabIndex = 0;
            uiMediaGrid.MouseClick += uiMediaGrid_MouseClick;
            // 
            // button1
            // 
            uiRefreshButton.Location = new Point(3, 28);
            uiRefreshButton.Name = "button1";
            uiRefreshButton.Size = new Size(75, 23);
            uiRefreshButton.TabIndex = 2;
            uiRefreshButton.Text = "button1";
            uiRefreshButton.UseVisualStyleBackColor = true;
            uiRefreshButton.Click += uiRefreshButton_Click;
            // 
            // textBox1
            // 
            uiSearchTextBox.Location = new Point(84, 28);
            uiSearchTextBox.Name = "textBox1";
            uiSearchTextBox.Size = new Size(100, 23);
            uiSearchTextBox.TabIndex = 3;
            uiSearchTextBox.Visible = false;
            // 
            // uiMergerSelectedMediaButton
            // 
            uiMergerSelectedMediaButton.Location = new Point(453, 28);
            uiMergerSelectedMediaButton.Name = "uiMergerSelectedMediaButton";
            uiMergerSelectedMediaButton.Size = new Size(139, 23);
            uiMergerSelectedMediaButton.TabIndex = 4;
            uiMergerSelectedMediaButton.Text = "MERGE SELECTED";
            uiMergerSelectedMediaButton.UseVisualStyleBackColor = true;
            uiMergerSelectedMediaButton.Click += uiMergerSelectedMediaButton_Click;
            // 
            // uiLoadingLabel
            // 
            uiLoadingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiLoadingLabel.AutoSize = true;
            uiLoadingLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiLoadingLabel.ForeColor = Color.Blue;
            uiLoadingLabel.Location = new Point(190, 32);
            uiLoadingLabel.Name = "uiLoadingLabel";
            uiLoadingLabel.Size = new Size(85, 15);
            uiLoadingLabel.TabIndex = 5;
            uiLoadingLabel.Text = "Загрузка...";
            uiLoadingLabel.Visible = false;
            // 
            // uiStatusStrip
            // 
            uiStatusStrip.Items.AddRange(new ToolStripItem[] { uiTotalCountLabel, uiFilteredCountLabel });
            uiStatusStrip.Location = new Point(0, 378);
            uiStatusStrip.Name = "uiStatusStrip";
            uiStatusStrip.Size = new Size(595, 22);
            uiStatusStrip.TabIndex = 6;
            // 
            // uiTotalCountLabel
            // 
            uiTotalCountLabel.Name = "uiTotalCountLabel";
            uiTotalCountLabel.Size = new Size(90, 17);
            uiTotalCountLabel.Text = "Всего: 0";
            // 
            // uiFilteredCountLabel
            // 
            uiFilteredCountLabel.Name = "uiFilteredCountLabel";
            uiFilteredCountLabel.Size = new Size(90, 17);
            uiFilteredCountLabel.Text = "Отфильтровано: 0";
            // 
            // MediaMatrixGridControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiLoadingLabel);
            Controls.Add(uiMergerSelectedMediaButton);
            Controls.Add(uiSearchTextBox);
            Controls.Add(uiRefreshButton);
            Controls.Add(uiMediaGrid);
            Controls.Add(uiToolStrip);
            Controls.Add(uiStatusStrip);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).EndInit();
            uiStatusStrip.ResumeLayout(false);
            uiStatusStrip.PerformLayout();
            uiToolStrip.ResumeLayout(false);
            uiToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private OptimizedMediaGridView uiMediaGrid;
        private Button uiRefreshButton;
        private TextBox uiSearchTextBox;
        private Button uiMergerSelectedMediaButton;
        private Label uiLoadingLabel;
        private StatusStrip uiStatusStrip;
        private ToolStripStatusLabel uiTotalCountLabel;
        private ToolStripStatusLabel uiFilteredCountLabel;
        private ToolStrip uiToolStrip;
        private ToolStripLabel uiSearchLabel;
        private ToolStripTextBox uiSearchToolStripTextBox;
        private ToolStripButton uiClearSearchButton;
        private ToolStripLabel uiStatusLabel;
        private ToolStripComboBox uiStatusFilterComboBox;
        private ToolStripDropDownButton uiRelationsDropDownButton;
        private ToolStripButton uiSelectAllButton;
        private ToolStripButton uiDeselectAllButton;
    }
}
