namespace MediaOrcestrator.Runner
{
    partial class PublishControl
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
            uiSourceLabel = new Label();
            uiSourceComboBox = new ComboBox();
            uiTitleLabel = new Label();
            uiTitleTextBox = new TextBox();
            uiDescriptionLabel = new Label();
            uiDescriptionTextBox = new TextBox();
            uiVideoSectionLabel = new Label();
            uiVideoDropPanel = new Panel();
            uiVideoDropLabel = new Label();
            uiBrowseVideoButton = new Button();
            uiCoverSectionLabel = new Label();
            uiCoverDropPanel = new Panel();
            uiCoverPreviewPictureBox = new PictureBox();
            uiCoverDropLabel = new Label();
            uiBrowseCoverButton = new Button();
            uiClearCoverButton = new Button();
            uiPublishButton = new Button();
            uiStatusLabel = new Label();
            uiVideoDropPanel.SuspendLayout();
            uiCoverDropPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)uiCoverPreviewPictureBox).BeginInit();
            SuspendLayout();
            // 
            // uiSourceLabel
            // 
            uiSourceLabel.AutoSize = true;
            uiSourceLabel.Location = new Point(12, 15);
            uiSourceLabel.Name = "uiSourceLabel";
            uiSourceLabel.Size = new Size(64, 15);
            uiSourceLabel.TabIndex = 0;
            uiSourceLabel.Text = "Источник:";
            // 
            // uiSourceComboBox
            // 
            uiSourceComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiSourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiSourceComboBox.FormattingEnabled = true;
            uiSourceComboBox.Location = new Point(110, 12);
            uiSourceComboBox.Name = "uiSourceComboBox";
            uiSourceComboBox.Size = new Size(766, 23);
            uiSourceComboBox.TabIndex = 1;
            uiSourceComboBox.SelectedIndexChanged += uiSourceComboBox_SelectedIndexChanged;
            // 
            // uiTitleLabel
            // 
            uiTitleLabel.AutoSize = true;
            uiTitleLabel.Location = new Point(12, 47);
            uiTitleLabel.Name = "uiTitleLabel";
            uiTitleLabel.Size = new Size(62, 15);
            uiTitleLabel.TabIndex = 2;
            uiTitleLabel.Text = "Название:";
            // 
            // uiTitleTextBox
            // 
            uiTitleTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiTitleTextBox.Location = new Point(110, 44);
            uiTitleTextBox.Name = "uiTitleTextBox";
            uiTitleTextBox.Size = new Size(766, 23);
            uiTitleTextBox.TabIndex = 3;
            uiTitleTextBox.TextChanged += uiTitleTextBox_TextChanged;
            // 
            // uiDescriptionLabel
            // 
            uiDescriptionLabel.AutoSize = true;
            uiDescriptionLabel.Location = new Point(12, 79);
            uiDescriptionLabel.Name = "uiDescriptionLabel";
            uiDescriptionLabel.Size = new Size(65, 15);
            uiDescriptionLabel.TabIndex = 4;
            uiDescriptionLabel.Text = "Описание:";
            // 
            // uiDescriptionTextBox
            // 
            uiDescriptionTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiDescriptionTextBox.Location = new Point(110, 76);
            uiDescriptionTextBox.Multiline = true;
            uiDescriptionTextBox.Name = "uiDescriptionTextBox";
            uiDescriptionTextBox.ScrollBars = ScrollBars.Vertical;
            uiDescriptionTextBox.Size = new Size(766, 100);
            uiDescriptionTextBox.TabIndex = 5;
            // 
            // uiVideoSectionLabel
            // 
            uiVideoSectionLabel.AutoSize = true;
            uiVideoSectionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiVideoSectionLabel.Location = new Point(12, 191);
            uiVideoSectionLabel.Name = "uiVideoSectionLabel";
            uiVideoSectionLabel.Size = new Size(44, 15);
            uiVideoSectionLabel.TabIndex = 6;
            uiVideoSectionLabel.Text = "Видео";
            // 
            // uiVideoDropPanel
            // 
            uiVideoDropPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiVideoDropPanel.BackColor = Color.WhiteSmoke;
            uiVideoDropPanel.BorderStyle = BorderStyle.FixedSingle;
            uiVideoDropPanel.Controls.Add(uiVideoDropLabel);
            uiVideoDropPanel.Location = new Point(12, 210);
            uiVideoDropPanel.Name = "uiVideoDropPanel";
            uiVideoDropPanel.Size = new Size(756, 70);
            uiVideoDropPanel.TabIndex = 7;
            // 
            // uiVideoDropLabel
            // 
            uiVideoDropLabel.Dock = DockStyle.Fill;
            uiVideoDropLabel.ForeColor = Color.DimGray;
            uiVideoDropLabel.Location = new Point(0, 0);
            uiVideoDropLabel.Name = "uiVideoDropLabel";
            uiVideoDropLabel.Size = new Size(754, 68);
            uiVideoDropLabel.TabIndex = 0;
            uiVideoDropLabel.Text = "Нажмите «Обзор...» чтобы выбрать видеофайл";
            uiVideoDropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // uiBrowseVideoButton
            // 
            uiBrowseVideoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiBrowseVideoButton.Location = new Point(774, 233);
            uiBrowseVideoButton.Name = "uiBrowseVideoButton";
            uiBrowseVideoButton.Size = new Size(102, 26);
            uiBrowseVideoButton.TabIndex = 8;
            uiBrowseVideoButton.Text = "Обзор...";
            uiBrowseVideoButton.UseVisualStyleBackColor = true;
            uiBrowseVideoButton.Click += uiBrowseVideoButton_Click;
            // 
            // uiCoverSectionLabel
            // 
            uiCoverSectionLabel.AutoSize = true;
            uiCoverSectionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiCoverSectionLabel.Location = new Point(12, 295);
            uiCoverSectionLabel.Name = "uiCoverSectionLabel";
            uiCoverSectionLabel.Size = new Size(60, 15);
            uiCoverSectionLabel.TabIndex = 9;
            uiCoverSectionLabel.Text = "Обложка";
            // 
            // uiCoverDropPanel
            // 
            uiCoverDropPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiCoverDropPanel.BackColor = Color.WhiteSmoke;
            uiCoverDropPanel.BorderStyle = BorderStyle.FixedSingle;
            uiCoverDropPanel.Controls.Add(uiCoverPreviewPictureBox);
            uiCoverDropPanel.Controls.Add(uiCoverDropLabel);
            uiCoverDropPanel.Location = new Point(12, 314);
            uiCoverDropPanel.Name = "uiCoverDropPanel";
            uiCoverDropPanel.Size = new Size(756, 140);
            uiCoverDropPanel.TabIndex = 10;
            // 
            // uiCoverPreviewPictureBox
            // 
            uiCoverPreviewPictureBox.Dock = DockStyle.Left;
            uiCoverPreviewPictureBox.Location = new Point(0, 0);
            uiCoverPreviewPictureBox.Name = "uiCoverPreviewPictureBox";
            uiCoverPreviewPictureBox.Size = new Size(220, 138);
            uiCoverPreviewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            uiCoverPreviewPictureBox.TabIndex = 0;
            uiCoverPreviewPictureBox.TabStop = false;
            // 
            // uiCoverDropLabel
            // 
            uiCoverDropLabel.Dock = DockStyle.Fill;
            uiCoverDropLabel.ForeColor = Color.DimGray;
            uiCoverDropLabel.Location = new Point(0, 0);
            uiCoverDropLabel.Name = "uiCoverDropLabel";
            uiCoverDropLabel.Size = new Size(754, 138);
            uiCoverDropLabel.TabIndex = 1;
            uiCoverDropLabel.Text = "Нажмите «Обзор...» чтобы выбрать обложку";
            uiCoverDropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // uiBrowseCoverButton
            // 
            uiBrowseCoverButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiBrowseCoverButton.Location = new Point(774, 314);
            uiBrowseCoverButton.Name = "uiBrowseCoverButton";
            uiBrowseCoverButton.Size = new Size(102, 26);
            uiBrowseCoverButton.TabIndex = 11;
            uiBrowseCoverButton.Text = "Обзор...";
            uiBrowseCoverButton.UseVisualStyleBackColor = true;
            uiBrowseCoverButton.Click += uiBrowseCoverButton_Click;
            // 
            // uiClearCoverButton
            // 
            uiClearCoverButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiClearCoverButton.Location = new Point(774, 346);
            uiClearCoverButton.Name = "uiClearCoverButton";
            uiClearCoverButton.Size = new Size(102, 26);
            uiClearCoverButton.TabIndex = 12;
            uiClearCoverButton.Text = "Очистить";
            uiClearCoverButton.UseVisualStyleBackColor = true;
            uiClearCoverButton.Click += uiClearCoverButton_Click;
            // 
            // uiPublishButton
            // 
            uiPublishButton.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            uiPublishButton.Location = new Point(12, 475);
            uiPublishButton.Name = "uiPublishButton";
            uiPublishButton.Size = new Size(180, 34);
            uiPublishButton.TabIndex = 13;
            uiPublishButton.Text = "Опубликовать";
            uiPublishButton.UseVisualStyleBackColor = true;
            uiPublishButton.Click += uiPublishButton_Click;
            // 
            // uiStatusLabel
            // 
            uiStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiStatusLabel.AutoEllipsis = true;
            uiStatusLabel.ForeColor = Color.DimGray;
            uiStatusLabel.Location = new Point(198, 484);
            uiStatusLabel.Name = "uiStatusLabel";
            uiStatusLabel.Size = new Size(678, 25);
            uiStatusLabel.TabIndex = 14;
            uiStatusLabel.Text = "—";
            uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // PublishControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiStatusLabel);
            Controls.Add(uiPublishButton);
            Controls.Add(uiClearCoverButton);
            Controls.Add(uiBrowseCoverButton);
            Controls.Add(uiCoverDropPanel);
            Controls.Add(uiCoverSectionLabel);
            Controls.Add(uiBrowseVideoButton);
            Controls.Add(uiVideoDropPanel);
            Controls.Add(uiVideoSectionLabel);
            Controls.Add(uiDescriptionTextBox);
            Controls.Add(uiDescriptionLabel);
            Controls.Add(uiTitleTextBox);
            Controls.Add(uiTitleLabel);
            Controls.Add(uiSourceComboBox);
            Controls.Add(uiSourceLabel);
            Name = "PublishControl";
            Size = new Size(891, 528);
            uiVideoDropPanel.ResumeLayout(false);
            uiCoverDropPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)uiCoverPreviewPictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label uiSourceLabel;
        private ComboBox uiSourceComboBox;
        private Label uiTitleLabel;
        private TextBox uiTitleTextBox;
        private Label uiDescriptionLabel;
        private TextBox uiDescriptionTextBox;
        private Label uiVideoSectionLabel;
        private Panel uiVideoDropPanel;
        private Label uiVideoDropLabel;
        private Button uiBrowseVideoButton;
        private Label uiCoverSectionLabel;
        private Panel uiCoverDropPanel;
        private PictureBox uiCoverPreviewPictureBox;
        private Label uiCoverDropLabel;
        private Button uiBrowseCoverButton;
        private Button uiClearCoverButton;
        private Button uiPublishButton;
        private Label uiStatusLabel;
    }
}
