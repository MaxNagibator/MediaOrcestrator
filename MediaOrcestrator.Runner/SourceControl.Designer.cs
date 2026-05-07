namespace MediaOrcestrator.Runner
{
    partial class SourceControl
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
            components = new System.ComponentModel.Container();
            uiMainLayout = new TableLayoutPanel();
            uiSourceContainer = new Panel();
            uiTitleLabel = new Label();
            uiTypeLabel = new Label();
            uiEditButton = new Button();
            uiDuplicateButton = new Button();
            uiDeleteButton = new Button();
            uiToolTip = new ToolTip(components);
            uiMainLayout.SuspendLayout();
            uiSourceContainer.SuspendLayout();
            SuspendLayout();
            //
            // uiMainLayout
            //
            uiMainLayout.ColumnCount = 4;
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            uiMainLayout.Controls.Add(uiSourceContainer, 0, 0);
            uiMainLayout.Controls.Add(uiEditButton, 1, 0);
            uiMainLayout.Controls.Add(uiDuplicateButton, 2, 0);
            uiMainLayout.Controls.Add(uiDeleteButton, 3, 0);
            uiMainLayout.Dock = DockStyle.Fill;
            uiMainLayout.Location = new Point(0, 0);
            uiMainLayout.Name = "uiMainLayout";
            uiMainLayout.RowCount = 1;
            uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiMainLayout.Size = new Size(1193, 62);
            uiMainLayout.TabIndex = 0;
            // 
            // uiSourceContainer
            // 
            uiSourceContainer.Controls.Add(uiTitleLabel);
            uiSourceContainer.Controls.Add(uiTypeLabel);
            uiSourceContainer.Dock = DockStyle.Fill;
            uiSourceContainer.Location = new Point(3, 3);
            uiSourceContainer.Name = "uiSourceContainer";
            uiSourceContainer.Size = new Size(1080, 56);
            uiSourceContainer.TabIndex = 0;
            // 
            // uiTitleLabel
            // 
            uiTitleLabel.AutoEllipsis = true;
            uiTitleLabel.Dock = DockStyle.Top;
            uiTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            uiTitleLabel.Location = new Point(0, 0);
            uiTitleLabel.Name = "uiTitleLabel";
            uiTitleLabel.Size = new Size(1080, 23);
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
            uiTypeLabel.Size = new Size(1080, 15);
            uiTypeLabel.TabIndex = 4;
            uiTypeLabel.Text = "Type";
            //
            // uiEditButton
            //
            uiEditButton.Anchor = AnchorStyles.None;
            uiEditButton.Font = new Font("Segoe MDL2 Assets", 11F);
            uiEditButton.Location = new Point(1016, 17);
            uiEditButton.Name = "uiEditButton";
            uiEditButton.Size = new Size(32, 28);
            uiEditButton.TabIndex = 2;
            uiEditButton.Text = "";
            uiEditButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiEditButton, "Изменить");
            uiEditButton.Click += uiEditButton_Click;
            //
            // uiDuplicateButton
            //
            uiDuplicateButton.Anchor = AnchorStyles.None;
            uiDuplicateButton.Font = new Font("Segoe MDL2 Assets", 11F);
            uiDuplicateButton.Location = new Point(1056, 17);
            uiDuplicateButton.Name = "uiDuplicateButton";
            uiDuplicateButton.Size = new Size(32, 28);
            uiDuplicateButton.TabIndex = 3;
            uiDuplicateButton.Text = "";
            uiDuplicateButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiDuplicateButton, "Дублировать");
            uiDuplicateButton.Click += uiDuplicateButton_Click;
            //
            // uiDeleteButton
            //
            uiDeleteButton.Anchor = AnchorStyles.None;
            uiDeleteButton.Font = new Font("Segoe MDL2 Assets", 11F);
            uiDeleteButton.Location = new Point(1096, 17);
            uiDeleteButton.Name = "uiDeleteButton";
            uiDeleteButton.Size = new Size(32, 28);
            uiDeleteButton.TabIndex = 4;
            uiDeleteButton.Text = "";
            uiDeleteButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiDeleteButton, "Удалить");
            uiDeleteButton.Click += uiDeleteButton_Click;
            // 
            // MediaSourceControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightSteelBlue;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(uiMainLayout);
            Margin = new Padding(5);
            Name = "MediaSourceControl";
            Size = new Size(1193, 62);
            uiMainLayout.ResumeLayout(false);
            uiSourceContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel uiMainLayout;
        private System.Windows.Forms.Panel uiSourceContainer;
        private System.Windows.Forms.Label uiTitleLabel;
        private System.Windows.Forms.Label uiTypeLabel;
        private System.Windows.Forms.Button uiEditButton;
        private System.Windows.Forms.Button uiDuplicateButton;
        private System.Windows.Forms.Button uiDeleteButton;
        private System.Windows.Forms.ToolTip uiToolTip;
    }
}
