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
            button1 = new Button();
            textBox1 = new TextBox();
            SuspendLayout();
            // 
            // uMediaHeaderPanel
            // 
            uMediaHeaderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uMediaHeaderPanel.ColumnCount = 1;
            uMediaHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uMediaHeaderPanel.Location = new Point(0, 40);
            uMediaHeaderPanel.Name = "uMediaHeaderPanel";
            uMediaHeaderPanel.RowCount = 1;
            uMediaHeaderPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            uMediaHeaderPanel.Size = new Size(595, 30);
            uMediaHeaderPanel.TabIndex = 0;
            // 
            // uMediaGridPanel
            // 
            uMediaGridPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uMediaGridPanel.ColumnCount = 1;
            uMediaGridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uMediaGridPanel.Location = new Point(0, 71);
            uMediaGridPanel.Name = "uMediaGridPanel";
            uMediaGridPanel.Size = new Size(595, 329);
            uMediaGridPanel.TabIndex = 1;
            // 
            // button1
            // 
            button1.Location = new Point(109, 3);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 2;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click_1;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(3, 3);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(100, 23);
            textBox1.TabIndex = 3;
            // 
            // MediaMatrixGridControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(textBox1);
            Controls.Add(button1);
            Controls.Add(uMediaGridPanel);
            Controls.Add(uMediaHeaderPanel);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ResumeLayout(false);
            PerformLayout();
        }

        private TableLayoutPanel uMediaHeaderPanel;
        private TableLayoutPanel uMediaGridPanel;
        private Button button1;
        private TextBox textBox1;
    }
}
