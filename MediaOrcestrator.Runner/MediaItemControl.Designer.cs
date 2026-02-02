using System.Windows.Forms;
using System.Drawing;

namespace MediaOrcestrator.Runner
{
    partial class MediaItemControl
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
            uiMainLayout = new TableLayoutPanel();
            SuspendLayout();
            // 
            // uiMainLayout
            // 
            uiMainLayout.ColumnCount = 1;
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiMainLayout.Dock = DockStyle.Fill;
            uiMainLayout.Location = new Point(0, 0);
            uiMainLayout.Name = "uiMainLayout";
            uiMainLayout.RowCount = 1;
            uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiMainLayout.Size = new Size(675, 40);
            uiMainLayout.TabIndex = 0;
            // 
            // MediaItemControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(uiMainLayout);
            Name = "MediaItemControl";
            Size = new Size(675, 40);
            ResumeLayout(false);
        }

        private TableLayoutPanel uiMainLayout;
    }
}
