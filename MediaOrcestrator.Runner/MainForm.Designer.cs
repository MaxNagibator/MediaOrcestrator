namespace MediaOrcestrator.Runner
{
    partial class MainForm
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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            uiMediaSourcePanel = new Panel();
            uiSyncButton = new Button();
            uiMediaMatrixGridControl = new MediaMatrixGridControl();
            uiAddSourceButton = new Button();
            uiSourcesComboBox = new ComboBox();
            uiRelationsPanel = new Panel();
            uiAddRelationButton = new Button();
            uiRelationToLabel = new Label();
            uiRelationFromLabel = new Label();
            uiRelationToComboBox = new ComboBox();
            uiRelationFromComboBox = new ComboBox();
            uiMainTabControl = new TabControl();
            uiFilesTabPage = new TabPage();
            uiRelationViewModeCheckBox = new CheckBox();
            uiStorageTabPage = new TabPage();
            uiRelationsTabPage = new TabPage();
            uiAuditTabPage = new TabPage();
            uiLogsTabPage = new TabPage();
            uiMainTabControl.SuspendLayout();
            uiForceScanButton = new Button();
            uiClearDatabaseButton = new Button();
            uiMainTabControl.SuspendLayout();
            uiFilesTabPage.SuspendLayout();
            uiStorageTabPage.SuspendLayout();
            uiRelationsTabPage.SuspendLayout();
            uiAuditTabPage.SuspendLayout();
            uiLogsTabPage.SuspendLayout();
            SuspendLayout();
            // 
            // uiMediaSourcePanel
            // 
            uiMediaSourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiMediaSourcePanel.AutoScroll = true;
            uiMediaSourcePanel.BackColor = SystemColors.ControlDark;
            uiMediaSourcePanel.Location = new Point(6, 35);
            uiMediaSourcePanel.Name = "uiMediaSourcePanel";
            uiMediaSourcePanel.Padding = new Padding(5);
            uiMediaSourcePanel.Size = new Size(1113, 663);
            uiMediaSourcePanel.TabIndex = 0;
            uiMediaSourcePanel.SizeChanged += uiMediaSourcePanel_SizeChanged;
            // 
            // uiSyncButton
            // 
            uiSyncButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiSyncButton.Location = new Point(281, 238);
            uiSyncButton.Name = "uiSyncButton";
            uiSyncButton.Size = new Size(348, 32);
            uiSyncButton.TabIndex = 1;
            uiSyncButton.Text = "Синхронизировать";
            uiSyncButton.UseVisualStyleBackColor = true;
            uiSyncButton.Click += uiSyncButton_Click;
            // 
            // uiMediaMatrixGridControl
            // 
            uiMediaMatrixGridControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiMediaMatrixGridControl.Location = new Point(3, 31);
            uiMediaMatrixGridControl.Name = "uiMediaMatrixGridControl";
            uiMediaMatrixGridControl.Size = new Size(1116, 667);
            uiMediaMatrixGridControl.TabIndex = 2;
            // 
            // uiAddSourceButton
            // 
            uiAddSourceButton.Location = new Point(360, 5);
            uiAddSourceButton.Name = "uiAddSourceButton";
            uiAddSourceButton.Size = new Size(348, 25);
            uiAddSourceButton.TabIndex = 3;
            uiAddSourceButton.Text = "Добавить источник";
            uiAddSourceButton.UseVisualStyleBackColor = true;
            uiAddSourceButton.Click += uiAddSourceButton_Click;
            // 
            // uiSourcesComboBox
            // 
            uiSourcesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiSourcesComboBox.FormattingEnabled = true;
            uiSourcesComboBox.Location = new Point(6, 6);
            uiSourcesComboBox.Name = "uiSourcesComboBox";
            uiSourcesComboBox.Size = new Size(348, 23);
            uiSourcesComboBox.TabIndex = 4;
            // 
            // uiRelationsPanel
            // 
            uiRelationsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiRelationsPanel.AutoScroll = true;
            uiRelationsPanel.BackColor = SystemColors.ControlDark;
            uiRelationsPanel.Location = new Point(3, 51);
            uiRelationsPanel.Name = "uiRelationsPanel";
            uiRelationsPanel.Padding = new Padding(5);
            uiRelationsPanel.Size = new Size(1119, 650);
            uiRelationsPanel.TabIndex = 1;
            // 
            // uiAddRelationButton
            // 
            uiAddRelationButton.Location = new Point(643, 21);
            uiAddRelationButton.Name = "uiAddRelationButton";
            uiAddRelationButton.Size = new Size(314, 25);
            uiAddRelationButton.TabIndex = 9;
            uiAddRelationButton.Text = "add";
            uiAddRelationButton.UseVisualStyleBackColor = true;
            uiAddRelationButton.Click += UiAddRelationButton_Click;
            // 
            // uiRelationToLabel
            // 
            uiRelationToLabel.AutoSize = true;
            uiRelationToLabel.Location = new Point(323, 4);
            uiRelationToLabel.Name = "uiRelationToLabel";
            uiRelationToLabel.Size = new Size(32, 15);
            uiRelationToLabel.TabIndex = 8;
            uiRelationToLabel.Text = "Куда";
            // 
            // uiRelationFromLabel
            // 
            uiRelationFromLabel.AutoSize = true;
            uiRelationFromLabel.Location = new Point(3, 4);
            uiRelationFromLabel.Name = "uiRelationFromLabel";
            uiRelationFromLabel.Size = new Size(45, 15);
            uiRelationFromLabel.TabIndex = 7;
            uiRelationFromLabel.Text = "Откуда";
            // 
            // uiRelationToComboBox
            // 
            uiRelationToComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiRelationToComboBox.FormattingEnabled = true;
            uiRelationToComboBox.Location = new Point(323, 22);
            uiRelationToComboBox.Name = "uiRelationToComboBox";
            uiRelationToComboBox.Size = new Size(314, 23);
            uiRelationToComboBox.TabIndex = 6;
            // 
            // uiRelationFromComboBox
            // 
            uiRelationFromComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiRelationFromComboBox.FormattingEnabled = true;
            uiRelationFromComboBox.Location = new Point(3, 22);
            uiRelationFromComboBox.Name = "uiRelationFromComboBox";
            uiRelationFromComboBox.Size = new Size(314, 23);
            uiRelationFromComboBox.TabIndex = 5;
            // 
            // uiMainTabControl
            // 
            uiMainTabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiMainTabControl.Controls.Add(uiFilesTabPage);
            uiMainTabControl.Controls.Add(uiStorageTabPage);
            uiMainTabControl.Controls.Add(uiRelationsTabPage);
            uiMainTabControl.Controls.Add(uiAuditTabPage);
            uiMainTabControl.Controls.Add(uiLogsTabPage);
            uiMainTabControl.Location = new Point(12, 12);
            uiMainTabControl.Name = "uiMainTabControl";
            uiMainTabControl.SelectedIndex = 0;
            uiMainTabControl.Size = new Size(1133, 770);
            uiMainTabControl.TabIndex = 6;
            // 
            // uiFilesTabPage
            // 
            uiFilesTabPage.Controls.Add(uiRelationViewModeCheckBox);
            uiFilesTabPage.Controls.Add(uiMediaMatrixGridControl);
            uiFilesTabPage.Location = new Point(4, 24);
            uiFilesTabPage.Name = "uiFilesTabPage";
            uiFilesTabPage.Padding = new Padding(3);
            uiFilesTabPage.Size = new Size(1125, 742);
            uiFilesTabPage.TabIndex = 0;
            uiFilesTabPage.Text = "Фаилы";
            uiFilesTabPage.UseVisualStyleBackColor = true;
            // 
            // uiRelationViewModeCheckBox
            // 
            uiRelationViewModeCheckBox.AutoSize = true;
            uiRelationViewModeCheckBox.Location = new Point(6, 6);
            uiRelationViewModeCheckBox.Name = "uiRelationViewModeCheckBox";
            uiRelationViewModeCheckBox.Size = new Size(122, 19);
            uiRelationViewModeCheckBox.TabIndex = 3;
            uiRelationViewModeCheckBox.Text = "Режим по связям";
            uiRelationViewModeCheckBox.UseVisualStyleBackColor = true;
            uiRelationViewModeCheckBox.CheckedChanged += uiRelationViewModeCheckBox_CheckedChanged;
            // 
            // uiStorageTabPage
            // 
            uiStorageTabPage.Controls.Add(uiSourcesComboBox);
            uiStorageTabPage.Controls.Add(uiAddSourceButton);
            uiStorageTabPage.Controls.Add(uiMediaSourcePanel);
            uiStorageTabPage.Location = new Point(4, 24);
            uiStorageTabPage.Name = "uiStorageTabPage";
            uiStorageTabPage.Padding = new Padding(3);
            uiStorageTabPage.Size = new Size(1125, 742);
            uiStorageTabPage.TabIndex = 1;
            uiStorageTabPage.Text = "Хранилища";
            uiStorageTabPage.UseVisualStyleBackColor = true;
            // 
            // uiRelationsTabPage
            // 
            uiRelationsTabPage.Controls.Add(uiRelationsPanel);
            uiRelationsTabPage.Controls.Add(uiAddRelationButton);
            uiRelationsTabPage.Controls.Add(uiRelationFromComboBox);
            uiRelationsTabPage.Controls.Add(uiRelationToLabel);
            uiRelationsTabPage.Controls.Add(uiRelationToComboBox);
            uiRelationsTabPage.Controls.Add(uiRelationFromLabel);
            uiRelationsTabPage.Location = new Point(4, 24);
            uiRelationsTabPage.Name = "uiRelationsTabPage";
            uiRelationsTabPage.Size = new Size(1125, 742);
            uiRelationsTabPage.TabIndex = 2;
            uiRelationsTabPage.Text = "Связи";
            uiRelationsTabPage.UseVisualStyleBackColor = true;
            // 
            // uiAuditTabPage
            // 
            uiAuditTabPage.Controls.Add(uiClearDatabaseButton);
            uiAuditTabPage.Controls.Add(uiForceScanButton);
            uiAuditTabPage.Controls.Add(uiSyncButton);
            uiAuditTabPage.Location = new Point(4, 24);
            uiAuditTabPage.Name = "uiAuditTabPage";
            uiAuditTabPage.Size = new Size(1125, 742);
            uiAuditTabPage.TabIndex = 3;
            uiAuditTabPage.Text = "Аудит";
            uiAuditTabPage.UseVisualStyleBackColor = true;
            // 
            // uiLogsTabPage
            // 
            uiLogsTabPage.Location = new Point(4, 24);
            uiLogsTabPage.Name = "uiLogsTabPage";
            uiLogsTabPage.Padding = new Padding(3);
            uiLogsTabPage.Size = new Size(1125, 742);
            uiLogsTabPage.TabIndex = 4;
            uiLogsTabPage.Text = "Логи";
            uiLogsTabPage.UseVisualStyleBackColor = true;
            // 
            // uiForceScanButton
            // 
            uiForceScanButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiForceScanButton.Location = new Point(265, 473);
            uiForceScanButton.Name = "uiForceScanButton";
            uiForceScanButton.Size = new Size(348, 32);
            uiForceScanButton.TabIndex = 2;
            uiForceScanButton.Text = "Принудительное сканирование";
            uiForceScanButton.UseVisualStyleBackColor = true;
            uiForceScanButton.Click += uiForceScanButton_Click;
            // 
            // uiClearDatabaseButton
            // 
            uiClearDatabaseButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiClearDatabaseButton.Location = new Point(281, 305);
            uiClearDatabaseButton.Name = "uiClearDatabaseButton";
            uiClearDatabaseButton.Size = new Size(348, 32);
            uiClearDatabaseButton.TabIndex = 3;
            uiClearDatabaseButton.Text = "Очистить базу данных";
            uiClearDatabaseButton.UseVisualStyleBackColor = true;
            uiClearDatabaseButton.Click += uiClearDatabaseButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1157, 794);
            Controls.Add(uiMainTabControl);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Медиа оркестратор";
            Load += MainForm_Load;
            uiMainTabControl.ResumeLayout(false);
            uiFilesTabPage.ResumeLayout(false);
            uiFilesTabPage.PerformLayout();
            uiStorageTabPage.ResumeLayout(false);
            uiRelationsTabPage.ResumeLayout(false);
            uiRelationsTabPage.PerformLayout();
            uiAuditTabPage.ResumeLayout(false);
            uiLogsTabPage.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel uiMediaSourcePanel;
        private Button uiSyncButton;
        private MediaMatrixGridControl uiMediaMatrixGridControl;
        private Button uiAddSourceButton;
        private ComboBox uiSourcesComboBox;
        private Panel uiRelationsPanel;
        private Button uiAddRelationButton;
        private Label uiRelationToLabel;
        private Label uiRelationFromLabel;
        private ComboBox uiRelationToComboBox;
        private ComboBox uiRelationFromComboBox;
        private TabControl uiMainTabControl;
        private TabPage uiFilesTabPage;
        private TabPage uiStorageTabPage;
        private TabPage uiRelationsTabPage;
        private TabPage uiAuditTabPage;
        private CheckBox uiRelationViewModeCheckBox;
        private Button uiForceScanButton;
        private Button uiClearDatabaseButton;
        private TabPage uiLogsTabPage;
    }
}
