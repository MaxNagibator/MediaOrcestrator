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
            uMediaGrid = new DataGridView();
            button1 = new Button();
            textBox1 = new TextBox();
            uiMergerSelectedMediaButton = new Button();
            ((System.ComponentModel.ISupportInitialize)uMediaGrid).BeginInit();
            SuspendLayout();
            // 
            // uMediaGrid
            // 
            uMediaGrid.AllowUserToAddRows = false;
            uMediaGrid.AllowUserToDeleteRows = false;
            uMediaGrid.AllowUserToResizeRows = false;
            uMediaGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uMediaGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            uMediaGrid.Location = new Point(0, 40);
            uMediaGrid.Name = "uMediaGrid";
            uMediaGrid.RowHeadersVisible = false;
            uMediaGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            uMediaGrid.Size = new Size(595, 360);
            uMediaGrid.TabIndex = 0;
            uMediaGrid.MouseClick += uMediaGrid_MouseClick;
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
            // MediaMatrixGridControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiMergerSelectedMediaButton);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Controls.Add(uMediaGrid);
            Name = "MediaMatrixGridControl";
            Size = new Size(595, 400);
            ((System.ComponentModel.ISupportInitialize)uMediaGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private DataGridView uMediaGrid;
        private Button button1;
        private TextBox textBox1;
        private Button uiMergerSelectedMediaButton;
    }
}
