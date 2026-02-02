namespace MediaOrcestrator.Runner
{
    partial class RelationControl
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            uiMainLayout = new TableLayoutPanel();
            uiFromContainer = new Panel();
            uiFromTitleLabel = new Label();
            uiFromTypeLabel = new Label();
            uiArrowLabel = new Label();
            uiToContainer = new Panel();
            uiToTitleLabel = new Label();
            uiToTypeLabel = new Label();
            uiDeleteButton = new Button();
            uiSelectCheckBox = new CheckBox();
            uiMainLayout.SuspendLayout();
            uiFromContainer.SuspendLayout();
            uiToContainer.SuspendLayout();
            SuspendLayout();
            // 
            // uiMainLayout
            // 
            uiMainLayout.ColumnCount = 5;
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            uiMainLayout.Controls.Add(uiSelectCheckBox, 0, 0);
            uiMainLayout.Controls.Add(uiFromContainer, 1, 0);
            uiMainLayout.Controls.Add(uiArrowLabel, 2, 0);
            uiMainLayout.Controls.Add(uiToContainer, 3, 0);
            uiMainLayout.Controls.Add(uiDeleteButton, 4, 0);
            uiMainLayout.Dock = DockStyle.Fill;
            uiMainLayout.Location = new Point(0, 0);
            uiMainLayout.Name = "uiMainLayout";
            uiMainLayout.RowCount = 1;
            uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiMainLayout.Size = new Size(2029, 59);
            uiMainLayout.TabIndex = 0;
            // 
            // uiFromContainer
            // 
            uiFromContainer.Controls.Add(uiFromTitleLabel);
            uiFromContainer.Controls.Add(uiFromTypeLabel);
            uiFromContainer.Dock = DockStyle.Fill;
            uiFromContainer.Location = new Point(3, 3);
            uiFromContainer.Name = "uiFromContainer";
            uiFromContainer.Size = new Size(958, 53);
            uiFromContainer.TabIndex = 0;
            // 
            // uiFromTitleLabel
            // 
            uiFromTitleLabel.AutoEllipsis = true;
            uiFromTitleLabel.Dock = DockStyle.Top;
            uiFromTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            uiFromTitleLabel.Location = new Point(0, 0);
            uiFromTitleLabel.Name = "uiFromTitleLabel";
            uiFromTitleLabel.Size = new Size(958, 23);
            uiFromTitleLabel.TabIndex = 0;
            uiFromTitleLabel.Text = "From";
            // 
            // uiFromTypeLabel
            // 
            uiFromTypeLabel.AutoEllipsis = true;
            uiFromTypeLabel.Dock = DockStyle.Bottom;
            uiFromTypeLabel.ForeColor = Color.Gray;
            uiFromTypeLabel.Location = new Point(0, 38);
            uiFromTypeLabel.Name = "uiFromTypeLabel";
            uiFromTypeLabel.Size = new Size(958, 15);
            uiFromTypeLabel.TabIndex = 4;
            uiFromTypeLabel.Text = "Type";
            // 
            // uiArrowLabel
            // 
            uiArrowLabel.Dock = DockStyle.Fill;
            uiArrowLabel.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            uiArrowLabel.Location = new Point(967, 0);
            uiArrowLabel.Name = "uiArrowLabel";
            uiArrowLabel.Size = new Size(54, 59);
            uiArrowLabel.TabIndex = 2;
            uiArrowLabel.Text = "→";
            uiArrowLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // uiToContainer
            // 
            uiToContainer.Controls.Add(uiToTitleLabel);
            uiToContainer.Controls.Add(uiToTypeLabel);
            uiToContainer.Dock = DockStyle.Fill;
            uiToContainer.Location = new Point(1027, 3);
            uiToContainer.Name = "uiToContainer";
            uiToContainer.Size = new Size(958, 53);
            uiToContainer.TabIndex = 1;
            // 
            // uiToTitleLabel
            // 
            uiToTitleLabel.AutoEllipsis = true;
            uiToTitleLabel.Dock = DockStyle.Top;
            uiToTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            uiToTitleLabel.Location = new Point(0, 0);
            uiToTitleLabel.Name = "uiToTitleLabel";
            uiToTitleLabel.Size = new Size(958, 23);
            uiToTitleLabel.TabIndex = 1;
            uiToTitleLabel.Text = "To";
            // 
            // uiToTypeLabel
            // 
            uiToTypeLabel.AutoEllipsis = true;
            uiToTypeLabel.Dock = DockStyle.Bottom;
            uiToTypeLabel.ForeColor = Color.Gray;
            uiToTypeLabel.Location = new Point(0, 38);
            uiToTypeLabel.Name = "uiToTypeLabel";
            uiToTypeLabel.Size = new Size(958, 15);
            uiToTypeLabel.TabIndex = 5;
            uiToTypeLabel.Text = "Type";
            // 
            // uiDeleteButton
            // 
            uiDeleteButton.Anchor = AnchorStyles.None;
            uiDeleteButton.BackColor = Color.MistyRose;
            uiDeleteButton.FlatStyle = FlatStyle.Flat;
            uiDeleteButton.Location = new Point(1993, 14);
            uiDeleteButton.Name = "uiDeleteButton";
            uiDeleteButton.Size = new Size(30, 30);
            uiDeleteButton.TabIndex = 3;
            uiDeleteButton.Text = "×";
            uiDeleteButton.UseVisualStyleBackColor = false;
            uiDeleteButton.Click += uiDeleteButton_Click;
            // 
            // uiSelectCheckBox
            // 
            uiSelectCheckBox.Anchor = AnchorStyles.None;
            uiSelectCheckBox.AutoSize = true;
            uiSelectCheckBox.Location = new Point(8, 22);
            uiSelectCheckBox.Name = "uiSelectCheckBox";
            uiSelectCheckBox.Size = new Size(15, 14);
            uiSelectCheckBox.TabIndex = 6;
            uiSelectCheckBox.UseVisualStyleBackColor = true;
            uiSelectCheckBox.CheckedChanged += uiSelectCheckBox_CheckedChanged;
            // 
            // RelationControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(uiMainLayout);
            Dock = DockStyle.Top;
            Margin = new Padding(5);
            Name = "RelationControl";
            Size = new Size(390, 59);
            uiMainLayout.ResumeLayout(false);
            uiFromContainer.ResumeLayout(false);
            uiToContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel uiMainLayout;
        private System.Windows.Forms.Panel uiFromContainer;
        private System.Windows.Forms.Label uiFromTitleLabel;
        private System.Windows.Forms.Label uiFromTypeLabel;
        private System.Windows.Forms.Label uiArrowLabel;
        private System.Windows.Forms.Panel uiToContainer;
        private System.Windows.Forms.Label uiToTitleLabel;
        private System.Windows.Forms.Label uiToTypeLabel;
        private System.Windows.Forms.Button uiDeleteButton;
        private System.Windows.Forms.CheckBox uiSelectCheckBox;
    }
}
