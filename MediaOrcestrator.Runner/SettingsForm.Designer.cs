namespace MediaOrcestrator.Runner;

sealed partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        uiPathsGroupBox = new GroupBox();
        uiPluginPathLabel = new Label();
        uiPluginPathTextBox = new TextBox();
        uiPluginPathBrowseButton = new Button();
        uiDatabasePathLabel = new Label();
        uiDatabasePathTextBox = new TextBox();
        uiDatabasePathBrowseButton = new Button();
        uiTempPathLabel = new Label();
        uiTempPathTextBox = new TextBox();
        uiTempPathBrowseButton = new Button();
        uiStatePathLabel = new Label();
        uiStatePathTextBox = new TextBox();
        uiStatePathBrowseButton = new Button();
        uiRestartHintLabel = new Label();
        uiUploadsGroupBox = new GroupBox();
        uiGridUploadIntervalLabel = new Label();
        uiGridUploadIntervalNumeric = new NumericUpDown();
        uiGridUploadIntervalHintLabel = new Label();
        uiSaveAndRestartButton = new Button();
        uiSaveButton = new Button();
        uiCancelButton = new Button();
        uiPathsGroupBox.SuspendLayout();
        uiUploadsGroupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiGridUploadIntervalNumeric).BeginInit();
        SuspendLayout();
        // 
        // uiPathsGroupBox
        // 
        uiPathsGroupBox.Controls.Add(uiPluginPathLabel);
        uiPathsGroupBox.Controls.Add(uiPluginPathTextBox);
        uiPathsGroupBox.Controls.Add(uiPluginPathBrowseButton);
        uiPathsGroupBox.Controls.Add(uiDatabasePathLabel);
        uiPathsGroupBox.Controls.Add(uiDatabasePathTextBox);
        uiPathsGroupBox.Controls.Add(uiDatabasePathBrowseButton);
        uiPathsGroupBox.Controls.Add(uiTempPathLabel);
        uiPathsGroupBox.Controls.Add(uiTempPathTextBox);
        uiPathsGroupBox.Controls.Add(uiTempPathBrowseButton);
        uiPathsGroupBox.Controls.Add(uiStatePathLabel);
        uiPathsGroupBox.Controls.Add(uiStatePathTextBox);
        uiPathsGroupBox.Controls.Add(uiStatePathBrowseButton);
        uiPathsGroupBox.Location = new Point(12, 12);
        uiPathsGroupBox.Name = "uiPathsGroupBox";
        uiPathsGroupBox.Size = new Size(596, 165);
        uiPathsGroupBox.TabIndex = 0;
        uiPathsGroupBox.TabStop = false;
        uiPathsGroupBox.Text = "Пути";
        // 
        // uiPluginPathLabel
        // 
        uiPluginPathLabel.AutoSize = true;
        uiPluginPathLabel.Location = new Point(10, 31);
        uiPluginPathLabel.Name = "uiPluginPathLabel";
        uiPluginPathLabel.Size = new Size(99, 15);
        uiPluginPathLabel.TabIndex = 0;
        uiPluginPathLabel.Text = "Папка плагинов:";
        // 
        // uiPluginPathTextBox
        // 
        uiPluginPathTextBox.Location = new Point(180, 28);
        uiPluginPathTextBox.Name = "uiPluginPathTextBox";
        uiPluginPathTextBox.Size = new Size(360, 23);
        uiPluginPathTextBox.TabIndex = 1;
        // 
        // uiPluginPathBrowseButton
        // 
        uiPluginPathBrowseButton.Location = new Point(546, 27);
        uiPluginPathBrowseButton.Name = "uiPluginPathBrowseButton";
        uiPluginPathBrowseButton.Size = new Size(40, 25);
        uiPluginPathBrowseButton.TabIndex = 2;
        uiPluginPathBrowseButton.Text = "...";
        uiPluginPathBrowseButton.UseVisualStyleBackColor = true;
        uiPluginPathBrowseButton.Click += uiPluginPathBrowseButton_Click;
        // 
        // uiDatabasePathLabel
        // 
        uiDatabasePathLabel.AutoSize = true;
        uiDatabasePathLabel.Location = new Point(10, 65);
        uiDatabasePathLabel.Name = "uiDatabasePathLabel";
        uiDatabasePathLabel.Size = new Size(112, 15);
        uiDatabasePathLabel.TabIndex = 3;
        uiDatabasePathLabel.Text = "Файл базы данных:";
        // 
        // uiDatabasePathTextBox
        // 
        uiDatabasePathTextBox.Location = new Point(180, 62);
        uiDatabasePathTextBox.Name = "uiDatabasePathTextBox";
        uiDatabasePathTextBox.Size = new Size(360, 23);
        uiDatabasePathTextBox.TabIndex = 4;
        // 
        // uiDatabasePathBrowseButton
        // 
        uiDatabasePathBrowseButton.Location = new Point(546, 61);
        uiDatabasePathBrowseButton.Name = "uiDatabasePathBrowseButton";
        uiDatabasePathBrowseButton.Size = new Size(40, 25);
        uiDatabasePathBrowseButton.TabIndex = 5;
        uiDatabasePathBrowseButton.Text = "...";
        uiDatabasePathBrowseButton.UseVisualStyleBackColor = true;
        uiDatabasePathBrowseButton.Click += uiDatabasePathBrowseButton_Click;
        // 
        // uiTempPathLabel
        // 
        uiTempPathLabel.AutoSize = true;
        uiTempPathLabel.Location = new Point(10, 99);
        uiTempPathLabel.Name = "uiTempPathLabel";
        uiTempPathLabel.Size = new Size(106, 15);
        uiTempPathLabel.TabIndex = 6;
        uiTempPathLabel.Text = "Временная папка:";
        // 
        // uiTempPathTextBox
        // 
        uiTempPathTextBox.Location = new Point(180, 96);
        uiTempPathTextBox.Name = "uiTempPathTextBox";
        uiTempPathTextBox.Size = new Size(360, 23);
        uiTempPathTextBox.TabIndex = 7;
        // 
        // uiTempPathBrowseButton
        // 
        uiTempPathBrowseButton.Location = new Point(546, 95);
        uiTempPathBrowseButton.Name = "uiTempPathBrowseButton";
        uiTempPathBrowseButton.Size = new Size(40, 25);
        uiTempPathBrowseButton.TabIndex = 8;
        uiTempPathBrowseButton.Text = "...";
        uiTempPathBrowseButton.UseVisualStyleBackColor = true;
        uiTempPathBrowseButton.Click += uiTempPathBrowseButton_Click;
        // 
        // uiStatePathLabel
        // 
        uiStatePathLabel.AutoSize = true;
        uiStatePathLabel.Location = new Point(10, 133);
        uiStatePathLabel.Name = "uiStatePathLabel";
        uiStatePathLabel.Size = new Size(172, 15);
        uiStatePathLabel.TabIndex = 9;
        uiStatePathLabel.Text = "Папка состояния источников:";
        // 
        // uiStatePathTextBox
        // 
        uiStatePathTextBox.Location = new Point(180, 130);
        uiStatePathTextBox.Name = "uiStatePathTextBox";
        uiStatePathTextBox.Size = new Size(360, 23);
        uiStatePathTextBox.TabIndex = 10;
        // 
        // uiStatePathBrowseButton
        // 
        uiStatePathBrowseButton.Location = new Point(546, 129);
        uiStatePathBrowseButton.Name = "uiStatePathBrowseButton";
        uiStatePathBrowseButton.Size = new Size(40, 25);
        uiStatePathBrowseButton.TabIndex = 11;
        uiStatePathBrowseButton.Text = "...";
        uiStatePathBrowseButton.UseVisualStyleBackColor = true;
        uiStatePathBrowseButton.Click += uiStatePathBrowseButton_Click;
        // 
        // uiRestartHintLabel
        // 
        uiRestartHintLabel.ForeColor = SystemColors.GrayText;
        uiRestartHintLabel.Location = new Point(12, 185);
        uiRestartHintLabel.Name = "uiRestartHintLabel";
        uiRestartHintLabel.Size = new Size(596, 20);
        uiRestartHintLabel.TabIndex = 1;
        uiRestartHintLabel.Text = "Изменения путей применятся только после перезапуска приложения.";
        uiRestartHintLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiUploadsGroupBox
        //
        uiUploadsGroupBox.Controls.Add(uiGridUploadIntervalLabel);
        uiUploadsGroupBox.Controls.Add(uiGridUploadIntervalNumeric);
        uiUploadsGroupBox.Controls.Add(uiGridUploadIntervalHintLabel);
        uiUploadsGroupBox.Location = new Point(12, 211);
        uiUploadsGroupBox.Name = "uiUploadsGroupBox";
        uiUploadsGroupBox.Size = new Size(596, 75);
        uiUploadsGroupBox.TabIndex = 2;
        uiUploadsGroupBox.TabStop = false;
        uiUploadsGroupBox.Text = "Пакетные загрузки";
        //
        // uiGridUploadIntervalLabel
        //
        uiGridUploadIntervalLabel.AutoSize = true;
        uiGridUploadIntervalLabel.Location = new Point(10, 24);
        uiGridUploadIntervalLabel.Name = "uiGridUploadIntervalLabel";
        uiGridUploadIntervalLabel.Size = new Size(196, 15);
        uiGridUploadIntervalLabel.TabIndex = 0;
        uiGridUploadIntervalLabel.Text = "Задержка между загрузками (мин):";
        //
        // uiGridUploadIntervalNumeric
        //
        uiGridUploadIntervalNumeric.Location = new Point(212, 21);
        uiGridUploadIntervalNumeric.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
        uiGridUploadIntervalNumeric.Name = "uiGridUploadIntervalNumeric";
        uiGridUploadIntervalNumeric.Size = new Size(70, 23);
        uiGridUploadIntervalNumeric.TabIndex = 1;
        uiGridUploadIntervalNumeric.TextAlign = HorizontalAlignment.Right;
        //
        // uiGridUploadIntervalHintLabel
        //
        uiGridUploadIntervalHintLabel.AutoSize = true;
        uiGridUploadIntervalHintLabel.ForeColor = SystemColors.GrayText;
        uiGridUploadIntervalHintLabel.Location = new Point(10, 49);
        uiGridUploadIntervalHintLabel.Name = "uiGridUploadIntervalHintLabel";
        uiGridUploadIntervalHintLabel.Size = new Size(287, 15);
        uiGridUploadIntervalHintLabel.TabIndex = 2;
        uiGridUploadIntervalHintLabel.Text = "0 — без задержки. Применяется в главном гриде и дереве синхронизации.";
        // 
        // uiSaveAndRestartButton
        // 
        uiSaveAndRestartButton.Location = new Point(12, 301);
        uiSaveAndRestartButton.Name = "uiSaveAndRestartButton";
        uiSaveAndRestartButton.Size = new Size(220, 30);
        uiSaveAndRestartButton.TabIndex = 2;
        uiSaveAndRestartButton.Text = "Сохранить и перезапустить";
        uiSaveAndRestartButton.UseVisualStyleBackColor = true;
        uiSaveAndRestartButton.Click += uiSaveAndRestartButton_Click;
        // 
        // uiSaveButton
        // 
        uiSaveButton.Location = new Point(238, 301);
        uiSaveButton.Name = "uiSaveButton";
        uiSaveButton.Size = new Size(130, 30);
        uiSaveButton.TabIndex = 3;
        uiSaveButton.Text = "Сохранить";
        uiSaveButton.UseVisualStyleBackColor = true;
        uiSaveButton.Click += uiSaveButton_Click;
        // 
        // uiCancelButton
        // 
        uiCancelButton.Location = new Point(500, 301);
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Size = new Size(108, 30);
        uiCancelButton.TabIndex = 4;
        uiCancelButton.Text = "Отмена";
        uiCancelButton.UseVisualStyleBackColor = true;
        uiCancelButton.Click += uiCancelButton_Click;
        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(618, 339);
        Controls.Add(uiPathsGroupBox);
        Controls.Add(uiRestartHintLabel);
        Controls.Add(uiUploadsGroupBox);
        Controls.Add(uiSaveAndRestartButton);
        Controls.Add(uiSaveButton);
        Controls.Add(uiCancelButton);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки приложения";
        uiPathsGroupBox.ResumeLayout(false);
        uiPathsGroupBox.PerformLayout();
        uiUploadsGroupBox.ResumeLayout(false);
        uiUploadsGroupBox.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiGridUploadIntervalNumeric).EndInit();
        ResumeLayout(false);
    }

    private GroupBox uiPathsGroupBox;
    private Label uiPluginPathLabel;
    private TextBox uiPluginPathTextBox;
    private Button uiPluginPathBrowseButton;
    private Label uiDatabasePathLabel;
    private TextBox uiDatabasePathTextBox;
    private Button uiDatabasePathBrowseButton;
    private Label uiTempPathLabel;
    private TextBox uiTempPathTextBox;
    private Button uiTempPathBrowseButton;
    private Label uiStatePathLabel;
    private TextBox uiStatePathTextBox;
    private Button uiStatePathBrowseButton;
    private Label uiRestartHintLabel;
    private GroupBox uiUploadsGroupBox;
    private Label uiGridUploadIntervalLabel;
    private NumericUpDown uiGridUploadIntervalNumeric;
    private Label uiGridUploadIntervalHintLabel;
    private Button uiSaveAndRestartButton;
    private Button uiSaveButton;
    private Button uiCancelButton;
}
