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
            components = new System.ComponentModel.Container();
            uiMediaGrid = new OptimizedMediaGridView();
            uiRefreshButton = new Button();
            uiSearchTextBox = new TextBox();
            uiMergerSelectedMediaButton = new Button();
            uiLoadingLabel = new Label();
            uiStatusStrip = new StatusStrip();
            uiTotalCountLabel = new ToolStripStatusLabel();
            uiFilteredCountLabel = new ToolStripStatusLabel();
            uiConvertProgressBar = new ToolStripProgressBar();
            uiConvertStatusLabel = new ToolStripStatusLabel();
            uiFilterControl = new FilterToolStripControl();
            uiConvertCancelMenu = new ContextMenuStrip(components);
            uiCancelConvertItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).BeginInit();
            uiStatusStrip.SuspendLayout();
            uiConvertCancelMenu.SuspendLayout();
            SuspendLayout();
            // 
            // uiMediaGrid
            // 
            uiMediaGrid.AllowUserToAddRows = false;
            uiMediaGrid.AllowUserToDeleteRows = false;
            uiMediaGrid.AllowUserToOrderColumns = true;
            uiMediaGrid.AllowUserToResizeRows = false;
            uiMediaGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiMediaGrid.ColumnHeadersHeight = 35;
            uiMediaGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            uiMediaGrid.Location = new Point(0, 65);
            uiMediaGrid.Name = "uiMediaGrid";
            uiMediaGrid.RowHeadersVisible = false;
            uiMediaGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            uiMediaGrid.Size = new Size(1131, 444);
            uiMediaGrid.TabIndex = 0;
            uiMediaGrid.MouseClick += uiMediaGrid_MouseClick;
            // 
            // uiRefreshButton
            // 
            uiRefreshButton.Location = new Point(3, 28);
            uiRefreshButton.Name = "uiRefreshButton";
            uiRefreshButton.Size = new Size(75, 23);
            uiRefreshButton.TabIndex = 2;
            uiRefreshButton.Text = "button1";
            uiRefreshButton.UseVisualStyleBackColor = true;
            uiRefreshButton.Click += uiRefreshButton_Click;
            // 
            // uiSearchTextBox
            // 
            uiSearchTextBox.Location = new Point(84, 28);
            uiSearchTextBox.Name = "uiSearchTextBox";
            uiSearchTextBox.Size = new Size(100, 23);
            uiSearchTextBox.TabIndex = 3;
            uiSearchTextBox.Visible = false;
            //
            // uiMergerSelectedMediaButton
            // 
            uiMergerSelectedMediaButton.Location = new Point(453, 28);
            uiMergerSelectedMediaButton.Name = "uiMergerSelectedMediaButton";
            uiMergerSelectedMediaButton.Size = new Size(158, 23);
            uiMergerSelectedMediaButton.TabIndex = 4;
            uiMergerSelectedMediaButton.Text = "Объеденить выбранные";
            uiMergerSelectedMediaButton.UseVisualStyleBackColor = true;
            uiMergerSelectedMediaButton.Click += uiMergerSelectedMediaButton_Click;
            // 
            // uiLoadingLabel
            // 
            uiLoadingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiLoadingLabel.AutoSize = true;
            uiLoadingLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiLoadingLabel.ForeColor = Color.Blue;
            uiLoadingLabel.Location = new Point(936, 32);
            uiLoadingLabel.Name = "uiLoadingLabel";
            uiLoadingLabel.Size = new Size(66, 15);
            uiLoadingLabel.TabIndex = 5;
            uiLoadingLabel.Text = "Загрузка...";
            uiLoadingLabel.Visible = false;
            // 
            // uiStatusStrip
            // 
            uiStatusStrip.Items.AddRange(new ToolStripItem[] { uiTotalCountLabel, uiFilteredCountLabel, uiConvertProgressBar, uiConvertStatusLabel });
            uiStatusStrip.Location = new Point(0, 509);
            uiStatusStrip.Name = "uiStatusStrip";
            uiStatusStrip.Size = new Size(1131, 22);
            uiStatusStrip.TabIndex = 6;
            // 
            // uiTotalCountLabel
            // 
            uiTotalCountLabel.Name = "uiTotalCountLabel";
            uiTotalCountLabel.Size = new Size(50, 17);
            uiTotalCountLabel.Text = "Всего: 0";
            // 
            // uiFilteredCountLabel
            // 
            uiFilteredCountLabel.Name = "uiFilteredCountLabel";
            uiFilteredCountLabel.Size = new Size(107, 17);
            uiFilteredCountLabel.Text = "Отфильтровано: 0";
            //
            // uiConvertProgressBar
            //
            uiConvertProgressBar.Name = "uiConvertProgressBar";
            uiConvertProgressBar.Size = new Size(150, 16);
            uiConvertProgressBar.Visible = false;
            uiConvertProgressBar.MouseDown += uiConvertProgressBar_MouseDown;
            //
            // uiConvertStatusLabel
            //
            uiConvertStatusLabel.Name = "uiConvertStatusLabel";
            uiConvertStatusLabel.Size = new Size(0, 17);
            uiConvertStatusLabel.Visible = false;
            //
            // uiFilterControl
            //
            uiFilterControl.Dock = DockStyle.Top;
            uiFilterControl.Location = new Point(0, 0);
            uiFilterControl.Name = "uiFilterControl";
            uiFilterControl.ShowStatusFilter = true;
            uiFilterControl.Size = new Size(1131, 25);
            uiFilterControl.TabIndex = 7;
            //
            // uiConvertCancelMenu
            //
            uiConvertCancelMenu.Items.AddRange(new ToolStripItem[] { uiCancelConvertItem });
            uiConvertCancelMenu.Name = "uiConvertCancelMenu";
            uiConvertCancelMenu.Size = new Size(206, 26);
            //
            // uiCancelConvertItem
            //
            uiCancelConvertItem.Name = "uiCancelConvertItem";
            uiCancelConvertItem.Size = new Size(205, 22);
            uiCancelConvertItem.Text = "Отменить конвертацию";
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
            Controls.Add(uiFilterControl);
            Controls.Add(uiStatusStrip);
            Name = "MediaMatrixGridControl";
            Size = new Size(1131, 531);
            ((System.ComponentModel.ISupportInitialize)uiMediaGrid).EndInit();
            uiStatusStrip.ResumeLayout(false);
            uiStatusStrip.PerformLayout();
            uiConvertCancelMenu.ResumeLayout(false);
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
        private ToolStripProgressBar uiConvertProgressBar;
        private ToolStripStatusLabel uiConvertStatusLabel;
        private ContextMenuStrip uiConvertCancelMenu;
        private ToolStripMenuItem uiCancelConvertItem;
    }
}
