namespace MediaOrcestrator.Runner
{
    partial class MediaSourceControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            uiMainLayout = new TableLayoutPanel();
            uiSourceContainer = new Panel();
            uiTitleLabel = new Label();
            uiTypeLabel = new Label();
            uiDeleteButton = new Button();
            uiMainLayout.SuspendLayout();
            uiSourceContainer.SuspendLayout();
            SuspendLayout();
            // 
            // uiMainLayout
            // 
            uiMainLayout.ColumnCount = 2;
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 107F));
            uiMainLayout.Controls.Add(uiSourceContainer, 0, 0);
            uiMainLayout.Controls.Add(uiDeleteButton, 1, 0);
            uiMainLayout.Dock = DockStyle.Fill;
            uiMainLayout.Location = new Point(0, 0);
            uiMainLayout.Name = "uiMainLayout";
            uiMainLayout.RowCount = 1;
            uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiMainLayout.Size = new Size(390, 62);
            uiMainLayout.TabIndex = 0;
            // 
            // uiSourceContainer
            // 
            uiSourceContainer.Controls.Add(uiTitleLabel);
            uiSourceContainer.Controls.Add(uiTypeLabel);
            uiSourceContainer.Dock = DockStyle.Fill;
            uiSourceContainer.Location = new Point(3, 3);
            uiSourceContainer.Name = "uiSourceContainer";
            uiSourceContainer.Size = new Size(277, 56);
            uiSourceContainer.TabIndex = 0;
            // 
            // uiTitleLabel
            // 
            uiTitleLabel.AutoEllipsis = true;
            uiTitleLabel.Dock = DockStyle.Top;
            uiTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            uiTitleLabel.Location = new Point(0, 0);
            uiTitleLabel.Name = "uiTitleLabel";
            uiTitleLabel.Size = new Size(277, 23);
            uiTitleLabel.TabIndex = 0;
            uiTitleLabel.Text = "Title";
            // 
            // uiTypeLabel
            // 
            uiTypeLabel.AutoEllipsis = true;
            uiTypeLabel.Dock = DockStyle.Bottom;
            uiTypeLabel.ForeColor = Color.Gray;
            uiTypeLabel.Location = new Point(0, 41);
            uiTypeLabel.Name = "uiTypeLabel";
            uiTypeLabel.Size = new Size(277, 15);
            uiTypeLabel.TabIndex = 4;
            uiTypeLabel.Text = "Type";
            // 
            // uiDeleteButton
            // 
            uiDeleteButton.Anchor = AnchorStyles.None;
            uiDeleteButton.Location = new Point(293, 19);
            uiDeleteButton.Name = "uiDeleteButton";
            uiDeleteButton.Size = new Size(87, 23);
            uiDeleteButton.TabIndex = 3;
            uiDeleteButton.Text = "delete";
            uiDeleteButton.UseVisualStyleBackColor = true;
            uiDeleteButton.Click += uiDeleteButton_Click;
            // 
            // MediaSourceControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightSteelBlue;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(uiMainLayout);
            Dock = DockStyle.Top;
            Margin = new Padding(5);
            Name = "MediaSourceControl";
            Size = new Size(390, 62);
            uiMainLayout.ResumeLayout(false);
            uiSourceContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel uiMainLayout;
        private System.Windows.Forms.Panel uiSourceContainer;
        private System.Windows.Forms.Label uiTitleLabel;
        private System.Windows.Forms.Label uiTypeLabel;
        private System.Windows.Forms.Button uiDeleteButton;
    }
}
