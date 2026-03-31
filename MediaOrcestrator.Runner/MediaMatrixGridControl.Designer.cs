using System.Windows.Forms;
using System.Drawing;

namespace MediaOrcestrator.Runner
{
    partial class MediaMatrixGridControl : UserControl
    {
        private System.ComponentModel.IContainer components = null;
        private ContextMenuStrip _contextMenu;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();

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
            uiFilterControl = new FilterToolStripControl();
            uiSelectAllButton = new Button();
            uiDeselectAllButton = new Button();
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).BeginInit();
            uiStatusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // uiFilterControl
            // 
            uiFilterControl.Dock = DockStyle.Top;
            uiFilterControl.Location = new Point(0, 0);
            uiFilterControl.Name = "uiFilterControl";
            uiFilterControl.Size = new Size(595, 25);
            uiFilterControl.TabIndex = 7;
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
            // uiSelectAllButton
            // 
            uiSelectAllButton.Location = new Point(190, 28);
            uiSelectAllButton.Name = "uiSelectAllButton";
            uiSelectAllButton.Size = new Size(90, 23);
            uiSelectAllButton.TabIndex = 8;
            uiSelectAllButton.Text = "Выбрать все";
            uiSelectAllButton.UseVisualStyleBackColor = true;
            uiSelectAllButton.Click += uiSelectAllButton_Click;
            // 
            // uiDeselectAllButton
            // 
            uiDeselectAllButton.Location = new Point(286, 28);
            uiDeselectAllButton.Name = "uiDeselectAllButton";
            uiDeselectAllButton.Size = new Size(110, 23);
            uiDeselectAllButton.TabIndex = 9;
            uiDeselectAllButton.Text = "Снять выделение";
            uiDeselectAllButton.UseVisualStyleBackColor = true;
            uiDeselectAllButton.Click += uiDeselectAllButton_Click;
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
            uiLoadingLabel.Location = new Point(400, 32);
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
            uiConvertCancelMenu = new ContextMenuStrip();
            uiCancelConvertItem = new ToolStripMenuItem();
            //
            // uiConvertProgressBar
            //
            uiConvertProgressBar = new ToolStripProgressBar();
            uiConvertProgressBar.Name = "uiConvertProgressBar";
            uiConvertProgressBar.Size = new Size(150, 16);
            uiConvertProgressBar.Minimum = 0;
            uiConvertProgressBar.Maximum = 100;
            uiConvertProgressBar.Visible = false;
            uiConvertProgressBar.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    uiConvertCancelMenu.Show(Cursor.Position);
                }
            };
            //
            // uiConvertStatusLabel format :c
            //
            uiConvertStatusLabel = new ToolStripStatusLabel();
            uiConvertStatusLabel.Name = "uiConvertStatusLabel";
            uiConvertStatusLabel.Size = new Size(200, 17);
            uiConvertStatusLabel.Text = "";
            uiConvertStatusLabel.Visible = false;
            //
            // uiCancelConvertItem
            //
            uiCancelConvertItem.Name = "uiCancelConvertItem";
            uiCancelConvertItem.Text = "Отменить конвертацию";
            uiConvertCancelMenu.Items.Add(uiCancelConvertItem);
            uiStatusStrip.Items.AddRange(new ToolStripItem[] { uiConvertProgressBar, uiConvertStatusLabel });
            //
            // MediaMatrixGridControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiLoadingLabel);
            Controls.Add(uiMergerSelectedMediaButton);
            Controls.Add(uiDeselectAllButton);
            Controls.Add(uiSelectAllButton);
            Controls.Add(uiSearchTextBox);
            Controls.Add(uiRefreshButton);
            Controls.Add(uiMediaGrid);
            Controls.Add(uiFilterControl);
            Controls.Add(uiStatusStrip);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).EndInit();
            uiStatusStrip.ResumeLayout(false);
            uiStatusStrip.PerformLayout();
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
        private FilterToolStripControl uiFilterControl;
        private Button uiSelectAllButton;
        private Button uiDeselectAllButton;
        private ToolStripProgressBar uiConvertProgressBar;
        private ToolStripStatusLabel uiConvertStatusLabel;
        private ContextMenuStrip uiConvertCancelMenu;
        private ToolStripMenuItem uiCancelConvertItem;
    }
}
