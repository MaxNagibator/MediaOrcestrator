using System.Windows.Forms;
using System.Drawing;

namespace MediaOrcestrator.Runner
{
    partial class MediaMatrixGridControl : UserControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            uiMediaHeaderPanel = new TableLayoutPanel();
            uiMediaGridPanel = new TableLayoutPanel();
            SuspendLayout();
            // 
            // uiMediaHeaderPanel
            // 
            uiMediaHeaderPanel.ColumnCount = 1;
            uiMediaHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiMediaHeaderPanel.Dock = DockStyle.Top;
            uiMediaHeaderPanel.Location = new Point(0, 0);
            uiMediaHeaderPanel.Name = "uiMediaHeaderPanel";
            uiMediaHeaderPanel.RowCount = 1;
            uiMediaHeaderPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            uiMediaHeaderPanel.Size = new Size(595, 30);
            uiMediaHeaderPanel.TabIndex = 0;
            // 
            // uiMediaGridPanel
            // 
            uiMediaGridPanel.AutoScroll = true;
            uiMediaGridPanel.ColumnCount = 1;
            uiMediaGridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiMediaGridPanel.Dock = DockStyle.Fill;
            uiMediaGridPanel.Location = new Point(0, 30);
            uiMediaGridPanel.Name = "uiMediaGridPanel";
            uiMediaGridPanel.Size = new Size(595, 370);
            uiMediaGridPanel.TabIndex = 1;
            // 
            // MediaMatrixGridControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiMediaGridPanel);
            Controls.Add(uiMediaHeaderPanel);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ResumeLayout(false);
        }

        private TableLayoutPanel uiMediaHeaderPanel;
        private TableLayoutPanel uiMediaGridPanel;
    }
}
