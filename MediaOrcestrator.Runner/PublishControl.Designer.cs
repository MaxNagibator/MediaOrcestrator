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
            uiVideoLabel = new Label();
            uiVideoPathTextBox = new TextBox();
            uiBrowseVideoButton = new Button();
            uiClearVideoButton = new Button();
            uiCoverLabel = new Label();
            uiCoverPathTextBox = new TextBox();
            uiBrowseCoverButton = new Button();
            uiClearCoverButton = new Button();
            uiCoverPreviewPictureBox = new PictureBox();
            uiCoverTemplateGroup = new GroupBox();
            uiCoverProfileLabel = new Label();
            uiCoverProfileCombo = new ComboBox();
            uiSetupTemplateButton = new Button();
            uiCoverNumberLabel = new Label();
            uiCoverNumberInput = new NumericUpDown();
            uiGenerateCoverButton = new Button();
            uiRunChainCheckBox = new CheckBox();
            uiPublishButton = new Button();
            uiPublishProgressBar = new ProgressBar();
            uiStatusLabel = new Label();
            uiToolTip = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)uiCoverPreviewPictureBox).BeginInit();
            uiCoverTemplateGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)uiCoverNumberInput).BeginInit();
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
            uiDescriptionTemplateComboBox.Enabled = false;
            uiDescriptionTemplateComboBox.FormattingEnabled = true;
            uiDescriptionTemplateComboBox.Location = new Point(110, 76);
            uiDescriptionTemplateComboBox.Name = "uiDescriptionTemplateComboBox";
            uiDescriptionTemplateComboBox.Size = new Size(700, 23);
            uiDescriptionTemplateComboBox.TabIndex = 6;
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
            uiDescriptionTextBox.Size = new Size(766, 130);
            uiDescriptionTextBox.TabIndex = 7;
            uiDescriptionTextBox.TextChanged += uiDescriptionTextBox_TextChanged;
            //
            // uiVideoLabel
            //
            uiVideoLabel.AutoSize = true;
            uiVideoLabel.Location = new Point(12, 250);
            uiVideoLabel.Name = "uiVideoLabel";
            uiVideoLabel.Size = new Size(44, 15);
            uiVideoLabel.TabIndex = 8;
            uiVideoLabel.Text = "Видео:";
            //
            // uiVideoPathTextBox
            //
            uiVideoPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiVideoPathTextBox.Cursor = Cursors.Hand;
            uiVideoPathTextBox.Location = new Point(110, 247);
            uiVideoPathTextBox.Name = "uiVideoPathTextBox";
            uiVideoPathTextBox.PlaceholderText = "Нажмите, чтобы выбрать видеофайл";
            uiVideoPathTextBox.ReadOnly = true;
            uiVideoPathTextBox.Size = new Size(695, 23);
            uiVideoPathTextBox.TabIndex = 9;
            uiToolTip.SetToolTip(uiVideoPathTextBox, "Нажмите, чтобы выбрать видеофайл");
            uiVideoPathTextBox.Click += uiVideoPathTextBox_Click;
            //
            // uiBrowseVideoButton
            //
            uiBrowseVideoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiBrowseVideoButton.Location = new Point(811, 246);
            uiBrowseVideoButton.Name = "uiBrowseVideoButton";
            uiBrowseVideoButton.Size = new Size(34, 25);
            uiBrowseVideoButton.TabIndex = 10;
            uiBrowseVideoButton.Text = "⋯";
            uiBrowseVideoButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiBrowseVideoButton, "Выбрать видеофайл");
            uiBrowseVideoButton.Click += uiBrowseVideoButton_Click;
            //
            // uiClearVideoButton
            //
            uiClearVideoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiClearVideoButton.Location = new Point(849, 246);
            uiClearVideoButton.Name = "uiClearVideoButton";
            uiClearVideoButton.Size = new Size(28, 25);
            uiClearVideoButton.TabIndex = 11;
            uiClearVideoButton.Text = "×";
            uiClearVideoButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiClearVideoButton, "Убрать выбранный видеофайл");
            uiClearVideoButton.Click += uiClearVideoButton_Click;
            //
            // uiCoverLabel
            //
            uiCoverLabel.AutoSize = true;
            uiCoverLabel.Location = new Point(12, 282);
            uiCoverLabel.Name = "uiCoverLabel";
            uiCoverLabel.Size = new Size(60, 15);
            uiCoverLabel.TabIndex = 12;
            uiCoverLabel.Text = "Обложка:";
            //
            // uiCoverPathTextBox
            //
            uiCoverPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiCoverPathTextBox.Cursor = Cursors.Hand;
            uiCoverPathTextBox.Location = new Point(110, 279);
            uiCoverPathTextBox.Name = "uiCoverPathTextBox";
            uiCoverPathTextBox.PlaceholderText = "Нажмите, чтобы выбрать обложку";
            uiCoverPathTextBox.ReadOnly = true;
            uiCoverPathTextBox.Size = new Size(695, 23);
            uiCoverPathTextBox.TabIndex = 13;
            uiToolTip.SetToolTip(uiCoverPathTextBox, "Нажмите, чтобы выбрать обложку");
            uiCoverPathTextBox.Click += uiCoverPathTextBox_Click;
            //
            // uiBrowseCoverButton
            //
            uiBrowseCoverButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiBrowseCoverButton.Location = new Point(811, 278);
            uiBrowseCoverButton.Name = "uiBrowseCoverButton";
            uiBrowseCoverButton.Size = new Size(34, 25);
            uiBrowseCoverButton.TabIndex = 14;
            uiBrowseCoverButton.Text = "⋯";
            uiBrowseCoverButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiBrowseCoverButton, "Выбрать обложку");
            uiBrowseCoverButton.Click += uiBrowseCoverButton_Click;
            //
            // uiClearCoverButton
            //
            uiClearCoverButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiClearCoverButton.Location = new Point(849, 278);
            uiClearCoverButton.Name = "uiClearCoverButton";
            uiClearCoverButton.Size = new Size(28, 25);
            uiClearCoverButton.TabIndex = 15;
            uiClearCoverButton.Text = "×";
            uiClearCoverButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiClearCoverButton, "Убрать выбранную обложку");
            uiClearCoverButton.Click += uiClearCoverButton_Click;
            //
            // uiCoverPreviewPictureBox
            //
            uiCoverPreviewPictureBox.BorderStyle = BorderStyle.FixedSingle;
            uiCoverPreviewPictureBox.Cursor = Cursors.Hand;
            uiCoverPreviewPictureBox.Location = new Point(110, 310);
            uiCoverPreviewPictureBox.Name = "uiCoverPreviewPictureBox";
            uiCoverPreviewPictureBox.Size = new Size(160, 90);
            uiCoverPreviewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            uiCoverPreviewPictureBox.TabIndex = 16;
            uiCoverPreviewPictureBox.TabStop = false;
            uiToolTip.SetToolTip(uiCoverPreviewPictureBox, "Нажмите, чтобы выбрать обложку");
            uiCoverPreviewPictureBox.Click += uiCoverPathTextBox_Click;
            //
            // uiCoverTemplateGroup
            //
            uiCoverTemplateGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiCoverTemplateGroup.Controls.Add(uiCoverProfileLabel);
            uiCoverTemplateGroup.Controls.Add(uiCoverProfileCombo);
            uiCoverTemplateGroup.Controls.Add(uiSetupTemplateButton);
            uiCoverTemplateGroup.Controls.Add(uiCoverNumberLabel);
            uiCoverTemplateGroup.Controls.Add(uiCoverNumberInput);
            uiCoverTemplateGroup.Controls.Add(uiGenerateCoverButton);
            uiCoverTemplateGroup.Location = new Point(280, 305);
            uiCoverTemplateGroup.Name = "uiCoverTemplateGroup";
            uiCoverTemplateGroup.Size = new Size(596, 95);
            uiCoverTemplateGroup.TabIndex = 17;
            uiCoverTemplateGroup.TabStop = false;
            uiCoverTemplateGroup.Text = "Из шаблона";
            //
            // uiCoverProfileLabel
            //
            uiCoverProfileLabel.AutoSize = true;
            uiCoverProfileLabel.Location = new Point(10, 25);
            uiCoverProfileLabel.Name = "uiCoverProfileLabel";
            uiCoverProfileLabel.Size = new Size(54, 15);
            uiCoverProfileLabel.TabIndex = 0;
            uiCoverProfileLabel.Text = "Профиль:";
            //
            // uiCoverProfileCombo
            //
            uiCoverProfileCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiCoverProfileCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            uiCoverProfileCombo.Location = new Point(75, 22);
            uiCoverProfileCombo.Name = "uiCoverProfileCombo";
            uiCoverProfileCombo.Size = new Size(395, 23);
            uiCoverProfileCombo.TabIndex = 1;
            uiToolTip.SetToolTip(uiCoverProfileCombo, "Сохранённый профиль шаблона обложки");
            uiCoverProfileCombo.SelectedIndexChanged += uiCoverProfileCombo_SelectedIndexChanged;
            //
            // uiSetupTemplateButton
            //
            uiSetupTemplateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiSetupTemplateButton.Location = new Point(478, 21);
            uiSetupTemplateButton.Name = "uiSetupTemplateButton";
            uiSetupTemplateButton.Size = new Size(110, 25);
            uiSetupTemplateButton.TabIndex = 2;
            uiSetupTemplateButton.Text = "Настроить…";
            uiSetupTemplateButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiSetupTemplateButton, "Открыть редактор шаблонов обложек");
            uiSetupTemplateButton.Click += uiSetupTemplateButton_Click;
            //
            // uiCoverNumberLabel
            //
            uiCoverNumberLabel.AutoSize = true;
            uiCoverNumberLabel.Location = new Point(10, 59);
            uiCoverNumberLabel.Name = "uiCoverNumberLabel";
            uiCoverNumberLabel.Size = new Size(22, 15);
            uiCoverNumberLabel.TabIndex = 3;
            uiCoverNumberLabel.Text = "№:";
            //
            // uiCoverNumberInput
            //
            uiCoverNumberInput.Location = new Point(40, 56);
            uiCoverNumberInput.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            uiCoverNumberInput.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            uiCoverNumberInput.Name = "uiCoverNumberInput";
            uiCoverNumberInput.Size = new Size(80, 23);
            uiCoverNumberInput.TabIndex = 4;
            uiCoverNumberInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
            uiToolTip.SetToolTip(uiCoverNumberInput, "Номер для подстановки в {number}. По названию резолвится автоматически.");
            //
            // uiGenerateCoverButton
            //
            uiGenerateCoverButton.Location = new Point(135, 55);
            uiGenerateCoverButton.Name = "uiGenerateCoverButton";
            uiGenerateCoverButton.Size = new Size(200, 25);
            uiGenerateCoverButton.TabIndex = 5;
            uiGenerateCoverButton.Text = "Сгенерировать обложку";
            uiGenerateCoverButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiGenerateCoverButton, "Сгенерировать обложку из выбранного шаблона и поставить её на публикацию");
            uiGenerateCoverButton.Click += uiGenerateCoverButton_Click;
            //
            // uiRunChainCheckBox
            //
            uiRunChainCheckBox.AutoSize = true;
            uiRunChainCheckBox.Checked = true;
            uiRunChainCheckBox.CheckState = CheckState.Checked;
            uiRunChainCheckBox.Location = new Point(12, 413);
            uiRunChainCheckBox.Name = "uiRunChainCheckBox";
            uiRunChainCheckBox.Size = new Size(290, 19);
            uiRunChainCheckBox.TabIndex = 17;
            uiRunChainCheckBox.Text = "Запустить цепочку синхронизации после публикации";
            uiRunChainCheckBox.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiRunChainCheckBox, "После публикации обойти исходящие связи источника и синхронизировать новое медиа по цепочке");
            //
            // uiPublishButton
            //
            uiPublishButton.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            uiPublishButton.Location = new Point(12, 440);
            uiPublishButton.Name = "uiPublishButton";
            uiPublishButton.Size = new Size(180, 34);
            uiPublishButton.TabIndex = 18;
            uiPublishButton.Text = "Опубликовать";
            uiPublishButton.UseVisualStyleBackColor = true;
            uiToolTip.SetToolTip(uiPublishButton, "Опубликовать (Ctrl+Enter)");
            uiPublishButton.Click += uiPublishButton_Click;
            //
            // uiPublishProgressBar
            //
            uiPublishProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiPublishProgressBar.Location = new Point(198, 443);
            uiPublishProgressBar.MarqueeAnimationSpeed = 30;
            uiPublishProgressBar.Name = "uiPublishProgressBar";
            uiPublishProgressBar.Size = new Size(678, 6);
            uiPublishProgressBar.Style = ProgressBarStyle.Marquee;
            uiPublishProgressBar.TabIndex = 19;
            uiPublishProgressBar.Visible = false;
            //
            // uiStatusLabel
            //
            uiStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiStatusLabel.AutoEllipsis = true;
            uiStatusLabel.ForeColor = Color.DimGray;
            uiStatusLabel.Location = new Point(198, 453);
            uiStatusLabel.Name = "uiStatusLabel";
            uiStatusLabel.Size = new Size(678, 21);
            uiStatusLabel.TabIndex = 20;
            uiStatusLabel.Text = "—";
            uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // PublishControl
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiStatusLabel);
            Controls.Add(uiPublishProgressBar);
            Controls.Add(uiPublishButton);
            Controls.Add(uiRunChainCheckBox);
            Controls.Add(uiCoverTemplateGroup);
            Controls.Add(uiCoverPreviewPictureBox);
            Controls.Add(uiClearCoverButton);
            Controls.Add(uiBrowseCoverButton);
            Controls.Add(uiCoverPathTextBox);
            Controls.Add(uiCoverLabel);
            Controls.Add(uiClearVideoButton);
            Controls.Add(uiBrowseVideoButton);
            Controls.Add(uiVideoPathTextBox);
            Controls.Add(uiVideoLabel);
            Controls.Add(uiDescriptionTextBox);
            Controls.Add(uiDescriptionTemplateComboBox);
            Controls.Add(uiDescriptionCounterLabel);
            Controls.Add(uiDescriptionLabel);
            Controls.Add(uiTitleComboBox);
            Controls.Add(uiTitleLabel);
            Controls.Add(uiSourceComboBox);
            Controls.Add(uiSourceLabel);
            Name = "PublishControl";
            Size = new Size(891, 486);
            ((System.ComponentModel.ISupportInitialize)uiCoverPreviewPictureBox).EndInit();
            uiCoverTemplateGroup.ResumeLayout(false);
            uiCoverTemplateGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)uiCoverNumberInput).EndInit();
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
        private Label uiVideoLabel;
        private TextBox uiVideoPathTextBox;
        private Button uiBrowseVideoButton;
        private Button uiClearVideoButton;
        private Label uiCoverLabel;
        private TextBox uiCoverPathTextBox;
        private Button uiBrowseCoverButton;
        private Button uiClearCoverButton;
        private PictureBox uiCoverPreviewPictureBox;
        private GroupBox uiCoverTemplateGroup;
        private Label uiCoverProfileLabel;
        private ComboBox uiCoverProfileCombo;
        private Button uiSetupTemplateButton;
        private Label uiCoverNumberLabel;
        private NumericUpDown uiCoverNumberInput;
        private Button uiGenerateCoverButton;
        private CheckBox uiRunChainCheckBox;
        private Button uiPublishButton;
        private ProgressBar uiPublishProgressBar;
        private Label uiStatusLabel;
        private ToolTip uiToolTip;
    }
}
