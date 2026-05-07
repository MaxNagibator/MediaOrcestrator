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
            components = new System.ComponentModel.Container();
            uiSourceLabel = new Label();
            uiSourceComboBox = new ComboBox();
            uiTitleLabel = new Label();
            uiTitleComboBox = new ComboBox();
            uiDescriptionLabel = new Label();
            uiDescriptionCounterLabel = new Label();
            uiDescriptionTemplateComboBox = new ComboBox();
            uiDescriptionTextBox = new TextBox();
            uiVideoSectionLabel = new Label();
            uiVideoDropPanel = new Panel();
            uiVideoDropLabel = new Label();
            uiBrowseVideoButton = new Button();
            uiClearVideoButton = new Button();
            uiCoverSectionLabel = new Label();
            uiCoverDropPanel = new Panel();
            uiCoverPreviewPictureBox = new PictureBox();
            uiCoverDropLabel = new Label();
            uiBrowseCoverButton = new Button();
            uiClearCoverButton = new Button();
            uiPublishButton = new Button();
            uiPublishProgressBar = new ProgressBar();
            uiStatusLabel = new Label();
            uiToolTip = new ToolTip(components);
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
            uiToolTip.SetToolTip(uiSourceComboBox, "Источник, в который будет опубликовано видео");
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
            // uiTitleComboBox
            //
            uiTitleComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiTitleComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            uiTitleComboBox.FormattingEnabled = true;
            uiTitleComboBox.Location = new Point(110, 44);
            uiTitleComboBox.Name = "uiTitleComboBox";
            uiTitleComboBox.Size = new Size(766, 23);
            uiTitleComboBox.TabIndex = 3;
            uiToolTip.SetToolTip(uiTitleComboBox, "Название публикации — введите вручную или выберите вариант из истории источника");
            uiTitleComboBox.DropDown += uiTitleComboBox_DropDown;
            uiTitleComboBox.Enter += uiTitleComboBox_Enter;
            uiTitleComboBox.SelectionChangeCommitted += uiTitleComboBox_SelectionChangeCommitted;
            uiTitleComboBox.TextChanged += uiTitleComboBox_TextChanged;
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
            // uiDescriptionCounterLabel
            //
            uiDescriptionCounterLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiDescriptionCounterLabel.ForeColor = Color.DimGray;
            uiDescriptionCounterLabel.Location = new Point(816, 79);
            uiDescriptionCounterLabel.Name = "uiDescriptionCounterLabel";
            uiDescriptionCounterLabel.Size = new Size(60, 15);
            uiDescriptionCounterLabel.TabIndex = 5;
            uiDescriptionCounterLabel.Text = "0";
            uiDescriptionCounterLabel.TextAlign = ContentAlignment.TopRight;
            uiToolTip.SetToolTip(uiDescriptionCounterLabel, "Длина описания в символах");
            //
            // uiDescriptionTemplateComboBox
            //
            uiDescriptionTemplateComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiDescriptionTemplateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiDescriptionTemplateComboBox.FormattingEnabled = true;
            uiDescriptionTemplateComboBox.Location = new Point(110, 76);
            uiDescriptionTemplateComboBox.Name = "uiDescriptionTemplateComboBox";
            uiDescriptionTemplateComboBox.Size = new Size(700, 23);
            uiDescriptionTemplateComboBox.TabIndex = 6;
            uiDescriptionTemplateComboBox.Enabled = false;
            uiToolTip.SetToolTip(uiDescriptionTemplateComboBox, "Подставить описание из последних публикаций в этот источник");
            uiDescriptionTemplateComboBox.SelectionChangeCommitted += uiDescriptionTemplateComboBox_SelectionChangeCommitted;
            //
            // uiDescriptionTextBox
            //
            uiDescriptionTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiDescriptionTextBox.Location = new Point(110, 105);
            uiDescriptionTextBox.Multiline = true;
            uiDescriptionTextBox.Name = "uiDescriptionTextBox";
            uiDescriptionTextBox.ScrollBars = ScrollBars.Vertical;
            uiDescriptionTextBox.Size = new Size(766, 100);
            uiDescriptionTextBox.TabIndex = 7;
            uiDescriptionTextBox.TextChanged += uiDescriptionTextBox_TextChanged;
            //
            // uiVideoSectionLabel
            //
            uiVideoSectionLabel.AutoSize = true;
            uiVideoSectionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiVideoSectionLabel.Location = new Point(12, 220);
            uiVideoSectionLabel.Name = "uiVideoSectionLabel";
            uiVideoSectionLabel.Size = new Size(44, 15);
            uiVideoSectionLabel.TabIndex = 8;
            uiVideoSectionLabel.Text = "Видео";
            //
            // uiVideoDropPanel
            //
            uiVideoDropPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiVideoDropPanel.BackColor = Color.WhiteSmoke;
            uiVideoDropPanel.BorderStyle = BorderStyle.FixedSingle;
            uiVideoDropPanel.Controls.Add(uiVideoDropLabel);
            uiVideoDropPanel.Cursor = Cursors.Hand;
            uiVideoDropPanel.Location = new Point(12, 239);
            uiVideoDropPanel.Name = "uiVideoDropPanel";
            uiVideoDropPanel.Size = new Size(756, 70);
            uiVideoDropPanel.TabIndex = 9;
            uiToolTip.SetToolTip(uiVideoDropPanel, "Нажмите для выбора видеофайла");
            uiVideoDropPanel.Click += uiVideoDropPanel_Click;
            //
            // uiVideoDropLabel
            //
            uiVideoDropLabel.Cursor = Cursors.Hand;
            uiVideoDropLabel.Dock = DockStyle.Fill;
            uiVideoDropLabel.ForeColor = Color.DimGray;
            uiVideoDropLabel.Location = new Point(0, 0);
            uiVideoDropLabel.Name = "uiVideoDropLabel";
            uiVideoDropLabel.Size = new Size(754, 68);
            uiVideoDropLabel.TabIndex = 0;
            uiVideoDropLabel.Text = "Нажмите для выбора видеофайла";
            uiVideoDropLabel.TextAlign = ContentAlignment.MiddleCenter;
            uiVideoDropLabel.Click += uiVideoDropPanel_Click;
            //
            // uiBrowseVideoButton
            //
            uiBrowseVideoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiBrowseVideoButton.Location = new Point(774, 248);
            uiBrowseVideoButton.Name = "uiBrowseVideoButton";
            uiBrowseVideoButton.Size = new Size(102, 26);
            uiBrowseVideoButton.TabIndex = 10;
            uiBrowseVideoButton.Text = "Обзор...";
            uiBrowseVideoButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiBrowseVideoButton, "Выбрать видеофайл");
            uiBrowseVideoButton.Click += uiBrowseVideoButton_Click;
            //
            // uiClearVideoButton
            //
            uiClearVideoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiClearVideoButton.Location = new Point(774, 280);
            uiClearVideoButton.Name = "uiClearVideoButton";
            uiClearVideoButton.Size = new Size(102, 26);
            uiClearVideoButton.TabIndex = 11;
            uiClearVideoButton.Text = "Очистить";
            uiClearVideoButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiClearVideoButton, "Убрать выбранный видеофайл");
            uiClearVideoButton.Click += uiClearVideoButton_Click;
            //
            // uiCoverSectionLabel
            //
            uiCoverSectionLabel.AutoSize = true;
            uiCoverSectionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            uiCoverSectionLabel.Location = new Point(12, 324);
            uiCoverSectionLabel.Name = "uiCoverSectionLabel";
            uiCoverSectionLabel.Size = new Size(60, 15);
            uiCoverSectionLabel.TabIndex = 12;
            uiCoverSectionLabel.Text = "Обложка";
            //
            // uiCoverDropPanel
            //
            uiCoverDropPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiCoverDropPanel.BackColor = Color.WhiteSmoke;
            uiCoverDropPanel.BorderStyle = BorderStyle.FixedSingle;
            uiCoverDropPanel.Controls.Add(uiCoverPreviewPictureBox);
            uiCoverDropPanel.Controls.Add(uiCoverDropLabel);
            uiCoverDropPanel.Cursor = Cursors.Hand;
            uiCoverDropPanel.Location = new Point(12, 343);
            uiCoverDropPanel.Name = "uiCoverDropPanel";
            uiCoverDropPanel.Size = new Size(756, 140);
            uiCoverDropPanel.TabIndex = 13;
            uiToolTip.SetToolTip(uiCoverDropPanel, "Нажмите для выбора изображения");
            uiCoverDropPanel.Click += uiCoverDropPanel_Click;
            //
            // uiCoverPreviewPictureBox
            //
            uiCoverPreviewPictureBox.Cursor = Cursors.Hand;
            uiCoverPreviewPictureBox.Dock = DockStyle.Left;
            uiCoverPreviewPictureBox.Location = new Point(0, 0);
            uiCoverPreviewPictureBox.Name = "uiCoverPreviewPictureBox";
            uiCoverPreviewPictureBox.Size = new Size(220, 138);
            uiCoverPreviewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            uiCoverPreviewPictureBox.TabIndex = 0;
            uiCoverPreviewPictureBox.TabStop = false;
            uiCoverPreviewPictureBox.Click += uiCoverDropPanel_Click;
            //
            // uiCoverDropLabel
            //
            uiCoverDropLabel.Cursor = Cursors.Hand;
            uiCoverDropLabel.Dock = DockStyle.Fill;
            uiCoverDropLabel.ForeColor = Color.DimGray;
            uiCoverDropLabel.Location = new Point(220, 0);
            uiCoverDropLabel.Name = "uiCoverDropLabel";
            uiCoverDropLabel.Size = new Size(534, 138);
            uiCoverDropLabel.TabIndex = 1;
            uiCoverDropLabel.Text = "Нажмите для выбора изображения";
            uiCoverDropLabel.TextAlign = ContentAlignment.MiddleCenter;
            uiCoverDropLabel.Click += uiCoverDropPanel_Click;
            //
            // uiBrowseCoverButton
            //
            uiBrowseCoverButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiBrowseCoverButton.Location = new Point(774, 343);
            uiBrowseCoverButton.Name = "uiBrowseCoverButton";
            uiBrowseCoverButton.Size = new Size(102, 26);
            uiBrowseCoverButton.TabIndex = 14;
            uiBrowseCoverButton.Text = "Обзор...";
            uiBrowseCoverButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiBrowseCoverButton, "Выбрать обложку");
            uiBrowseCoverButton.Click += uiBrowseCoverButton_Click;
            //
            // uiClearCoverButton
            //
            uiClearCoverButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiClearCoverButton.Location = new Point(774, 375);
            uiClearCoverButton.Name = "uiClearCoverButton";
            uiClearCoverButton.Size = new Size(102, 26);
            uiClearCoverButton.TabIndex = 15;
            uiClearCoverButton.Text = "Очистить";
            uiClearCoverButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiClearCoverButton, "Убрать выбранную обложку");
            uiClearCoverButton.Click += uiClearCoverButton_Click;
            //
            // uiPublishButton
            //
            uiPublishButton.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            uiPublishButton.Location = new Point(12, 504);
            uiPublishButton.Name = "uiPublishButton";
            uiPublishButton.Size = new Size(180, 34);
            uiPublishButton.TabIndex = 16;
            uiPublishButton.Text = "Опубликовать";
            uiPublishButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiPublishButton, "Опубликовать (Ctrl+Enter)");
            uiPublishButton.Click += uiPublishButton_Click;
            //
            // uiPublishProgressBar
            //
            uiPublishProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiPublishProgressBar.Location = new Point(198, 507);
            uiPublishProgressBar.MarqueeAnimationSpeed = 30;
            uiPublishProgressBar.Name = "uiPublishProgressBar";
            uiPublishProgressBar.Size = new Size(678, 6);
            uiPublishProgressBar.Style = ProgressBarStyle.Marquee;
            uiPublishProgressBar.TabIndex = 17;
            uiPublishProgressBar.Visible = false;
            //
            // uiStatusLabel
            //
            uiStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiStatusLabel.AutoEllipsis = true;
            uiStatusLabel.ForeColor = Color.DimGray;
            uiStatusLabel.Location = new Point(198, 517);
            uiStatusLabel.Name = "uiStatusLabel";
            uiStatusLabel.Size = new Size(678, 21);
            uiStatusLabel.TabIndex = 18;
            uiStatusLabel.Text = "—";
            uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // PublishControl
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiPublishProgressBar);
            Controls.Add(uiStatusLabel);
            Controls.Add(uiPublishButton);
            Controls.Add(uiClearCoverButton);
            Controls.Add(uiBrowseCoverButton);
            Controls.Add(uiCoverDropPanel);
            Controls.Add(uiCoverSectionLabel);
            Controls.Add(uiClearVideoButton);
            Controls.Add(uiBrowseVideoButton);
            Controls.Add(uiVideoDropPanel);
            Controls.Add(uiVideoSectionLabel);
            Controls.Add(uiDescriptionTextBox);
            Controls.Add(uiDescriptionTemplateComboBox);
            Controls.Add(uiDescriptionCounterLabel);
            Controls.Add(uiDescriptionLabel);
            Controls.Add(uiTitleComboBox);
            Controls.Add(uiTitleLabel);
            Controls.Add(uiSourceComboBox);
            Controls.Add(uiSourceLabel);
            Name = "PublishControl";
            Size = new Size(891, 557);
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
        private ComboBox uiTitleComboBox;
        private Label uiDescriptionLabel;
        private Label uiDescriptionCounterLabel;
        private ComboBox uiDescriptionTemplateComboBox;
        private TextBox uiDescriptionTextBox;
        private Label uiVideoSectionLabel;
        private Panel uiVideoDropPanel;
        private Label uiVideoDropLabel;
        private Button uiBrowseVideoButton;
        private Button uiClearVideoButton;
        private Label uiCoverSectionLabel;
        private Panel uiCoverDropPanel;
        private PictureBox uiCoverPreviewPictureBox;
        private Label uiCoverDropLabel;
        private Button uiBrowseCoverButton;
        private Button uiClearCoverButton;
        private Button uiPublishButton;
        private ProgressBar uiPublishProgressBar;
        private Label uiStatusLabel;
        private ToolTip uiToolTip;
    }
}
