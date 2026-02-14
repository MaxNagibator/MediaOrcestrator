using System.Windows.Forms;
using System.Drawing;

namespace MediaOrcestrator.Runner
{
    partial class MediaMatrixGridControl : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private Font _statusFont;
        private Font _headerFont;
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
                
                if (_statusFont != null)
                {
                    _statusFont.Dispose();
                    _statusFont = null;
                }
                
                if (_headerFont != null)
                {
                    _headerFont.Dispose();
                    _headerFont = null;
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
            uiMediaGrid = new DataGridView();
            uiRefreshButton = new Button();
            uiSearchTextBox = new TextBox();
            uiMergerSelectedMediaButton = new Button();
            uiLoadingLabel = new Label();
            uiStatusStrip = new StatusStrip();
            uiTotalCountLabel = new ToolStripStatusLabel();
            uiFilteredCountLabel = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).BeginInit();
            uiStatusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // uMediaGrid
            // 
            uiMediaGrid.AllowUserToAddRows = false;
            uiMediaGrid.AllowUserToDeleteRows = false;
            uiMediaGrid.AllowUserToResizeRows = false;
            uiMediaGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiMediaGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            uiMediaGrid.Location = new Point(0, 40);
            uiMediaGrid.Name = "uMediaGrid";
            uiMediaGrid.RowHeadersVisible = false;
            uiMediaGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            uiMediaGrid.Size = new Size(595, 338);
            uiMediaGrid.TabIndex = 0;
            uiMediaGrid.MouseClick += uiMediaGrid_MouseClick;
            // 
            // button1
            // 
            uiRefreshButton.Location = new Point(109, 3);
            uiRefreshButton.Name = "button1";
            uiRefreshButton.Size = new Size(75, 23);
            uiRefreshButton.TabIndex = 2;
            uiRefreshButton.Text = "button1";
            uiRefreshButton.UseVisualStyleBackColor = true;
            uiRefreshButton.Click += uiRefreshButton_Click;
            // 
            // textBox1
            // 
            uiSearchTextBox.Location = new Point(3, 3);
            uiSearchTextBox.Name = "textBox1";
            uiSearchTextBox.Size = new Size(100, 23);
            uiSearchTextBox.TabIndex = 3;
            uiSearchTextBox.TextChanged += uiSearchTextBox_TextChanged;
            // 
            // uiMergerSelectedMediaButton
            // 
            uiMergerSelectedMediaButton.Location = new Point(453, 3);
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
            uiLoadingLabel.Location = new Point(190, 7);
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
            Controls.Add(uiStatusStrip);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).EndInit();
            uiStatusStrip.ResumeLayout(false);
            uiStatusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private DataGridView uiMediaGrid;
        private Button uiRefreshButton;
        private TextBox uiSearchTextBox;
        private Button uiMergerSelectedMediaButton;
        private Label uiLoadingLabel;
        private StatusStrip uiStatusStrip;
        private ToolStripStatusLabel uiTotalCountLabel;
        private ToolStripStatusLabel uiFilteredCountLabel;
    }
}
