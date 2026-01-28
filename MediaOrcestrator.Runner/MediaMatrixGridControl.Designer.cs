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
            uMediaHeaderPanel = new TableLayoutPanel();
            uMediaGridPanel = new TableLayoutPanel();
            SuspendLayout();
            // 
            // uMediaHeaderPanel
            // 
            uMediaHeaderPanel.ColumnCount = 1;
            uMediaHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uMediaHeaderPanel.Dock = DockStyle.Top;
            uMediaHeaderPanel.Location = new Point(0, 0);
            uMediaHeaderPanel.Name = "uMediaHeaderPanel";
            uMediaHeaderPanel.RowCount = 1;
            uMediaHeaderPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            uMediaHeaderPanel.Size = new Size(595, 30);
            uMediaHeaderPanel.TabIndex = 0;
            // 
            // uMediaGridPanel
            // 
            uMediaGridPanel.AutoScroll = true;
            uMediaGridPanel.ColumnCount = 1;
            uMediaGridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uMediaGridPanel.Dock = DockStyle.Fill;
            uMediaGridPanel.Location = new Point(0, 30);
            uMediaGridPanel.Name = "uMediaGridPanel";
            uMediaGridPanel.Size = new Size(595, 370);
            uMediaGridPanel.TabIndex = 1;
            // 
            // MediaMatrixGridControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uMediaGridPanel);
            Controls.Add(uMediaHeaderPanel);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ResumeLayout(false);
        }

        private TableLayoutPanel uMediaHeaderPanel;
        private TableLayoutPanel uMediaGridPanel;
    }
}
