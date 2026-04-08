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
            uiStorageTabPage = new TabPage();
            uiRelationsTabPage = new TabPage();
            uiAuditTabPage = new TabPage();
            uiSyncSourcePanel = new FlowLayoutPanel();
            uiQuickSyncButton = new Button();
            uiClearSpecificTypeButton = new Button();
            uiClearTypeComboBox = new ComboBox();
            uiClearDatabaseButton = new Button();
            uiForceScanButton = new Button();
            uiSyncButton = new Button();
            uiSyncTreeTabPage = new TabPage();
            uiSyncTreeControl = new SyncTreeControl();
            uiLogsTabPage = new TabPage();
            uiToolsTabPage = new TabPage();
            groupBox3 = new GroupBox();
            uiVkVideoAuthStatePathTextBox = new TextBox();
            uiVkVideoAuthStateOpenBrowserButton = new Button();
            groupBox2 = new GroupBox();
            uiYoutubeAuthStatePathTextBox = new TextBox();
            uiYoutubeAuthStateOpenBrowserButton = new Button();
            groupBox1 = new GroupBox();
            uiRubuteAuthStatePathTextBox = new TextBox();
            uiRubuteAuthStateOpenBrowserButton = new Button();
            uiManageToolsButton = new Button();
            uiCheckUpdatesButton = new Button();
            uiSyncNewButton = new Button();
            uiAuditToolTip = new ToolTip();
            uiMainTabControl.SuspendLayout();
            uiFilesTabPage.SuspendLayout();
            uiStorageTabPage.SuspendLayout();
            uiRelationsTabPage.SuspendLayout();
            uiAuditTabPage.SuspendLayout();
            uiSyncTreeTabPage.SuspendLayout();
            uiToolsTabPage.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
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
            // uiMediaMatrixGridControl
            // 
            uiMediaMatrixGridControl.Dock = DockStyle.Fill;
            uiMediaMatrixGridControl.Location = new Point(3, 3);
            uiMediaMatrixGridControl.Name = "uiMediaMatrixGridControl";
            uiMediaMatrixGridControl.Size = new Size(1202, 747);
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
            uiMainTabControl.Controls.Add(uiSyncTreeTabPage);
            uiMainTabControl.Controls.Add(uiLogsTabPage);
            uiMainTabControl.Controls.Add(uiToolsTabPage);
            uiMainTabControl.Location = new Point(12, 12);
            uiMainTabControl.Name = "uiMainTabControl";
            uiMainTabControl.SelectedIndex = 0;
            uiMainTabControl.Size = new Size(1216, 781);
            uiMainTabControl.TabIndex = 6;
            // 
            // uiFilesTabPage
            // 
            uiFilesTabPage.Controls.Add(uiMediaMatrixGridControl);
            uiFilesTabPage.Location = new Point(4, 24);
            uiFilesTabPage.Name = "uiFilesTabPage";
            uiFilesTabPage.Padding = new Padding(3);
            uiFilesTabPage.Size = new Size(1208, 753);
            uiFilesTabPage.TabIndex = 0;
            uiFilesTabPage.Text = "Фаилы";
            uiFilesTabPage.UseVisualStyleBackColor = true;
            // 
            // uiStorageTabPage
            // 
            uiStorageTabPage.Controls.Add(uiSourcesComboBox);
            uiStorageTabPage.Controls.Add(uiAddSourceButton);
            uiStorageTabPage.Controls.Add(uiMediaSourcePanel);
            uiStorageTabPage.Location = new Point(4, 24);
            uiStorageTabPage.Name = "uiStorageTabPage";
            uiStorageTabPage.Padding = new Padding(3);
            uiStorageTabPage.Size = new Size(1208, 753);
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
            uiRelationsTabPage.Size = new Size(1208, 753);
            uiRelationsTabPage.TabIndex = 2;
            uiRelationsTabPage.Text = "Связи";
            uiRelationsTabPage.UseVisualStyleBackColor = true;
            // 
            // uiAuditTabPage
            // 
            uiAuditTabPage.Controls.Add(uiSyncNewButton);
            uiAuditTabPage.Controls.Add(uiSyncSourcePanel);
            uiAuditTabPage.Controls.Add(uiQuickSyncButton);
            uiAuditTabPage.Controls.Add(uiClearSpecificTypeButton);
            uiAuditTabPage.Controls.Add(uiClearTypeComboBox);
            uiAuditTabPage.Controls.Add(uiClearDatabaseButton);
            uiAuditTabPage.Controls.Add(uiForceScanButton);
            uiAuditTabPage.Controls.Add(uiSyncButton);
            uiAuditTabPage.Location = new Point(4, 24);
            uiAuditTabPage.Name = "uiAuditTabPage";
            uiAuditTabPage.Size = new Size(1208, 753);
            uiAuditTabPage.TabIndex = 3;
            uiAuditTabPage.Text = "Аудит";
            uiAuditTabPage.UseVisualStyleBackColor = true;
            //
            // uiSyncSourcePanel
            //
            uiSyncSourcePanel.AutoSize = true;
            uiSyncSourcePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            uiSyncSourcePanel.FlowDirection = FlowDirection.TopDown;
            uiSyncSourcePanel.Location = new Point(6, 157);
            uiSyncSourcePanel.Name = "uiSyncSourcePanel";
            uiSyncSourcePanel.Size = new Size(269, 110);
            uiSyncSourcePanel.TabIndex = 7;
            // 
            // uiQuickSyncButton
            // 
            uiQuickSyncButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiQuickSyncButton.Location = new Point(281, 195);
            uiQuickSyncButton.Name = "uiQuickSyncButton";
            uiQuickSyncButton.Size = new Size(348, 32);
            uiQuickSyncButton.TabIndex = 6;
            uiQuickSyncButton.Text = "Быстрая синхронизация";
            uiQuickSyncButton.UseVisualStyleBackColor = true;
            uiQuickSyncButton.Click += uiQuickSyncButton_Click;
            // 
            // uiClearSpecificTypeButton
            // 
            uiClearSpecificTypeButton.Location = new Point(511, 354);
            uiClearSpecificTypeButton.Name = "uiClearSpecificTypeButton";
            uiClearSpecificTypeButton.Size = new Size(118, 23);
            uiClearSpecificTypeButton.TabIndex = 5;
            uiClearSpecificTypeButton.Text = "Очистить тип";
            uiClearSpecificTypeButton.UseVisualStyleBackColor = true;
            uiClearSpecificTypeButton.Click += uiClearSpecificTypeButton_Click;
            // 
            // uiClearTypeComboBox
            // 
            uiClearTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiClearTypeComboBox.FormattingEnabled = true;
            uiClearTypeComboBox.Items.AddRange(new object[] { "medias", "sources", "source_relations" });
            uiClearTypeComboBox.Location = new Point(281, 354);
            uiClearTypeComboBox.Name = "uiClearTypeComboBox";
            uiClearTypeComboBox.Size = new Size(224, 23);
            uiClearTypeComboBox.TabIndex = 4;
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
            // uiSyncButton
            // 
            uiSyncButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiSyncButton.Location = new Point(281, 157);
            uiSyncButton.Name = "uiSyncButton";
            uiSyncButton.Size = new Size(348, 32);
            uiSyncButton.TabIndex = 1;
            uiSyncButton.Text = "Полная синхронизация";
            uiSyncButton.UseVisualStyleBackColor = true;
            uiSyncButton.Click += uiSyncButton_Click;
            //
            // uiAuditToolTip
            //
            uiAuditToolTip.SetToolTip(uiSyncButton, "Полное перечитывание всех медиа из всех источников");
            uiAuditToolTip.SetToolTip(uiQuickSyncButton, "Загружает список медиа без метаданных.\nМожно выбрать конкретный источник кнопкой слева.");
            uiAuditToolTip.SetToolTip(uiSyncNewButton, "Синхронизирует до первого уже известного медиа.\nМожно выбрать конкретный источник кнопкой слева.");
            uiAuditToolTip.SetToolTip(uiSyncSourcePanel, "Выберите источник для синхронизации.\nЕсли ничего не выбрано — синхронизируются все.");
            uiAuditToolTip.SetToolTip(uiClearDatabaseButton, "Удаляет все данные из базы. Операция необратима!");
            uiAuditToolTip.SetToolTip(uiClearSpecificTypeButton, "Удаляет все записи выбранного типа коллекции из базы данных");
            uiAuditToolTip.SetToolTip(uiForceScanButton, "Отладочная функция принудительного сканирования");
            //
            // uiSyncTreeTabPage
            // 
            uiSyncTreeTabPage.Controls.Add(uiSyncTreeControl);
            uiSyncTreeTabPage.Location = new Point(4, 24);
            uiSyncTreeTabPage.Name = "uiSyncTreeTabPage";
            uiSyncTreeTabPage.Size = new Size(1208, 753);
            uiSyncTreeTabPage.TabIndex = 6;
            uiSyncTreeTabPage.Text = "Дерево синхронизации";
            uiSyncTreeTabPage.UseVisualStyleBackColor = true;
            // 
            // uiSyncTreeControl
            // 
            uiSyncTreeControl.Dock = DockStyle.Fill;
            uiSyncTreeControl.Location = new Point(0, 0);
            uiSyncTreeControl.Name = "uiSyncTreeControl";
            uiSyncTreeControl.Size = new Size(1208, 753);
            uiSyncTreeControl.TabIndex = 0;
            // 
            // uiLogsTabPage
            // 
            uiLogsTabPage.Location = new Point(4, 24);
            uiLogsTabPage.Name = "uiLogsTabPage";
            uiLogsTabPage.Padding = new Padding(3);
            uiLogsTabPage.Size = new Size(1208, 753);
            uiLogsTabPage.TabIndex = 4;
            uiLogsTabPage.Text = "Логи";
            uiLogsTabPage.UseVisualStyleBackColor = true;
            // 
            // uiToolsTabPage
            // 
            uiToolsTabPage.Controls.Add(groupBox3);
            uiToolsTabPage.Controls.Add(groupBox2);
            uiToolsTabPage.Controls.Add(groupBox1);
            uiToolsTabPage.Controls.Add(uiManageToolsButton);
            uiToolsTabPage.Controls.Add(uiCheckUpdatesButton);
            uiToolsTabPage.Location = new Point(4, 24);
            uiToolsTabPage.Name = "uiToolsTabPage";
            uiToolsTabPage.Size = new Size(1208, 753);
            uiToolsTabPage.TabIndex = 5;
            uiToolsTabPage.Text = "Вспомогательное";
            uiToolsTabPage.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(uiVkVideoAuthStatePathTextBox);
            groupBox3.Controls.Add(uiVkVideoAuthStateOpenBrowserButton);
            groupBox3.Location = new Point(219, 415);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(467, 100);
            groupBox3.TabIndex = 3;
            groupBox3.TabStop = false;
            groupBox3.Text = "VkVideo auth_state";
            // 
            // uiVkVideoAuthStatePathTextBox
            // 
            uiVkVideoAuthStatePathTextBox.Location = new Point(7, 51);
            uiVkVideoAuthStatePathTextBox.Name = "uiVkVideoAuthStatePathTextBox";
            uiVkVideoAuthStatePathTextBox.Size = new Size(454, 23);
            uiVkVideoAuthStatePathTextBox.TabIndex = 1;
            uiVkVideoAuthStatePathTextBox.Text = "E:\\bobgroup\\projects\\mediaOrcestrator\\vkvideoAuthState\\auth_state";
            // 
            // uiVkVideoAuthStateOpenBrowserButton
            // 
            uiVkVideoAuthStateOpenBrowserButton.Location = new Point(6, 22);
            uiVkVideoAuthStateOpenBrowserButton.Name = "uiVkVideoAuthStateOpenBrowserButton";
            uiVkVideoAuthStateOpenBrowserButton.Size = new Size(101, 23);
            uiVkVideoAuthStateOpenBrowserButton.TabIndex = 0;
            uiVkVideoAuthStateOpenBrowserButton.Text = "открыть брузер";
            uiVkVideoAuthStateOpenBrowserButton.UseVisualStyleBackColor = true;
            uiVkVideoAuthStateOpenBrowserButton.Click += uiVkVideoAuthStateOpenBrowserButton_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(uiYoutubeAuthStatePathTextBox);
            groupBox2.Controls.Add(uiYoutubeAuthStateOpenBrowserButton);
            groupBox2.Location = new Point(219, 309);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(467, 100);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "Youtube auth_state";
            // 
            // uiYoutubeAuthStatePathTextBox
            // 
            uiYoutubeAuthStatePathTextBox.Location = new Point(7, 51);
            uiYoutubeAuthStatePathTextBox.Name = "uiYoutubeAuthStatePathTextBox";
            uiYoutubeAuthStatePathTextBox.Size = new Size(454, 23);
            uiYoutubeAuthStatePathTextBox.TabIndex = 1;
            uiYoutubeAuthStatePathTextBox.Text = "E:\\bobgroup\\projects\\mediaOrcestrator\\youtubeAuthState\\auth_state";
            // 
            // uiYoutubeAuthStateOpenBrowserButton
            // 
            uiYoutubeAuthStateOpenBrowserButton.Location = new Point(6, 22);
            uiYoutubeAuthStateOpenBrowserButton.Name = "uiYoutubeAuthStateOpenBrowserButton";
            uiYoutubeAuthStateOpenBrowserButton.Size = new Size(101, 23);
            uiYoutubeAuthStateOpenBrowserButton.TabIndex = 0;
            uiYoutubeAuthStateOpenBrowserButton.Text = "открыть брузер";
            uiYoutubeAuthStateOpenBrowserButton.UseVisualStyleBackColor = true;
            uiYoutubeAuthStateOpenBrowserButton.Click += uiYoutubeAuthStateOpenBrowserButton_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(uiRubuteAuthStatePathTextBox);
            groupBox1.Controls.Add(uiRubuteAuthStateOpenBrowserButton);
            groupBox1.Location = new Point(219, 203);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(467, 100);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Rutube auth_state";
            // 
            // uiRubuteAuthStatePathTextBox
            // 
            uiRubuteAuthStatePathTextBox.Location = new Point(7, 51);
            uiRubuteAuthStatePathTextBox.Name = "uiRubuteAuthStatePathTextBox";
            uiRubuteAuthStatePathTextBox.Size = new Size(454, 23);
            uiRubuteAuthStatePathTextBox.TabIndex = 1;
            uiRubuteAuthStatePathTextBox.Text = "E:\\bobgroup\\projects\\mediaOrcestrator\\rutubeAuthState\\auth_state";
            // 
            // uiRubuteAuthStateOpenBrowserButton
            // 
            uiRubuteAuthStateOpenBrowserButton.Location = new Point(6, 22);
            uiRubuteAuthStateOpenBrowserButton.Name = "uiRubuteAuthStateOpenBrowserButton";
            uiRubuteAuthStateOpenBrowserButton.Size = new Size(101, 23);
            uiRubuteAuthStateOpenBrowserButton.TabIndex = 0;
            uiRubuteAuthStateOpenBrowserButton.Text = "открыть брузер";
            uiRubuteAuthStateOpenBrowserButton.UseVisualStyleBackColor = true;
            uiRubuteAuthStateOpenBrowserButton.Click += uiRubuteAuthStateOpenBrowserButton_Click;
            // 
            // uiManageToolsButton
            // 
            uiManageToolsButton.Location = new Point(6, 300);
            uiManageToolsButton.Name = "uiManageToolsButton";
            uiManageToolsButton.Size = new Size(200, 35);
            uiManageToolsButton.TabIndex = 4;
            uiManageToolsButton.Text = "Управление инструментами";
            uiManageToolsButton.UseVisualStyleBackColor = true;
            uiManageToolsButton.Click += uiManageToolsButton_Click;
            // 
            // uiCheckUpdatesButton
            // 
            uiCheckUpdatesButton.Location = new Point(6, 341);
            uiCheckUpdatesButton.Name = "uiCheckUpdatesButton";
            uiCheckUpdatesButton.Size = new Size(200, 35);
            uiCheckUpdatesButton.TabIndex = 5;
            uiCheckUpdatesButton.Text = "Проверить обновления";
            uiCheckUpdatesButton.UseVisualStyleBackColor = true;
            uiCheckUpdatesButton.Click += uiCheckUpdatesButton_Click;
            // 
            // uiSyncNewButton
            // 
            uiSyncNewButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiSyncNewButton.Location = new Point(281, 233);
            uiSyncNewButton.Name = "uiSyncNewButton";
            uiSyncNewButton.Size = new Size(348, 32);
            uiSyncNewButton.TabIndex = 8;
            uiSyncNewButton.Text = "Синхронизация новых";
            uiSyncNewButton.UseVisualStyleBackColor = true;
            uiSyncNewButton.Click += uiSyncNewButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1240, 805);
            Controls.Add(uiMainTabControl);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Медиа оркестратор";
            Load += MainForm_Load;
            uiMainTabControl.ResumeLayout(false);
            uiFilesTabPage.ResumeLayout(false);
            uiStorageTabPage.ResumeLayout(false);
            uiRelationsTabPage.ResumeLayout(false);
            uiRelationsTabPage.PerformLayout();
            uiAuditTabPage.ResumeLayout(false);
            uiSyncTreeTabPage.ResumeLayout(false);
            uiToolsTabPage.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel uiMediaSourcePanel;
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
        private TabPage uiSyncTreeTabPage;
        private SyncTreeControl uiSyncTreeControl;
        private Button uiForceScanButton;
        private Button uiClearDatabaseButton;
        private ComboBox uiClearTypeComboBox;
        private Button uiClearSpecificTypeButton;
        private TabPage uiLogsTabPage;
        private TabPage uiToolsTabPage;
        private GroupBox groupBox1;
        private Button uiRubuteAuthStateOpenBrowserButton;
        private TextBox uiRubuteAuthStatePathTextBox;
        private Button uiSyncButton;
        private GroupBox groupBox2;
        private TextBox uiYoutubeAuthStatePathTextBox;
        private Button uiYoutubeAuthStateOpenBrowserButton;
        private GroupBox groupBox3;
        private TextBox uiVkVideoAuthStatePathTextBox;
        private Button uiVkVideoAuthStateOpenBrowserButton;
        private Button uiQuickSyncButton;
        private FlowLayoutPanel uiSyncSourcePanel;
        private Button uiManageToolsButton;
        private Button uiCheckUpdatesButton;
        private Button uiSyncNewButton;
        private ToolTip uiAuditToolTip;
    }
}
