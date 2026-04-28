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
            components = new System.ComponentModel.Container();
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
            uiPublishTabPage = new TabPage();
            uiStorageTabPage = new TabPage();
            uiRelationsTabPage = new TabPage();
            uiRelationsGraphTabPage = new TabPage();
            uiRelationsGraphControl = new RelationsGraphControl();
            uiAuditTabPage = new TabPage();
            button1 = new Button();
            uiRunningActionsFlowLayoutPanel = new FlowLayoutPanel();
            uiAuditSyncHeaderLabel = new Label();
            uiAuditBulkPanel = new Panel();
            uiBulkSourcesLabel = new Label();
            uiSyncButton = new Button();
            uiQuickSyncButton = new Button();
            uiSyncNewButton = new Button();
            uiBulkProgressLabel = new Label();
            uiAuditSourcesPanel = new FlowLayoutPanel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            uiAuditMaintenanceHeaderLabel = new Label();
            uiClearDatabaseButton = new Button();
            uiClearTypeComboBox = new ComboBox();
            uiClearSpecificTypeButton = new Button();
            uiForceScanButton = new Button();
            uiSyncTreeTabPage = new TabPage();
            uiSyncTreeControl = new SyncTreeControl();
            uiLogsTabPage = new TabPage();
            uiLogsToolbarPanel = new Panel();
            uiReportIssueButton = new Button();
            uiCommentsTabPage = new TabPage();
            uiCommentsViewControl = new CommentsViewControl();
            uiCommentsHtmlTabPage = new TabPage();
            uiCommentsHtmlControl = new CommentsHtmlControl();
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
            uiOpenSettingsButton = new Button();
            uiCheckUpdatesButton = new Button();
            uiAuditToolTip = new ToolTip(components);
            uiMainTabControl.SuspendLayout();
            uiFilesTabPage.SuspendLayout();
            uiStorageTabPage.SuspendLayout();
            uiRelationsTabPage.SuspendLayout();
            uiRelationsGraphTabPage.SuspendLayout();
            uiAuditTabPage.SuspendLayout();
            uiAuditBulkPanel.SuspendLayout();
            uiAuditSourcesPanel.SuspendLayout();
            uiSyncTreeTabPage.SuspendLayout();
            uiLogsTabPage.SuspendLayout();
            uiLogsToolbarPanel.SuspendLayout();
            uiCommentsTabPage.SuspendLayout();
            uiCommentsHtmlTabPage.SuspendLayout();
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
            uiMediaMatrixGridControl.Size = new Size(1206, 751);
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
            uiMainTabControl.Controls.Add(uiPublishTabPage);
            uiMainTabControl.Controls.Add(uiStorageTabPage);
            uiMainTabControl.Controls.Add(uiRelationsTabPage);
            uiMainTabControl.Controls.Add(uiRelationsGraphTabPage);
            uiMainTabControl.Controls.Add(uiAuditTabPage);
            uiMainTabControl.Controls.Add(uiSyncTreeTabPage);
            uiMainTabControl.Controls.Add(uiCommentsTabPage);
            uiMainTabControl.Controls.Add(uiCommentsHtmlTabPage);
            uiMainTabControl.Controls.Add(uiLogsTabPage);
            uiMainTabControl.Controls.Add(uiToolsTabPage);
            uiMainTabControl.Location = new Point(12, 12);
            uiMainTabControl.Name = "uiMainTabControl";
            uiMainTabControl.SelectedIndex = 0;
            uiMainTabControl.Size = new Size(1220, 785);
            uiMainTabControl.TabIndex = 6;
            // 
            // uiFilesTabPage
            // 
            uiFilesTabPage.Controls.Add(uiMediaMatrixGridControl);
            uiFilesTabPage.Location = new Point(4, 24);
            uiFilesTabPage.Name = "uiFilesTabPage";
            uiFilesTabPage.Padding = new Padding(3);
            uiFilesTabPage.Size = new Size(1212, 757);
            uiFilesTabPage.TabIndex = 0;
            uiFilesTabPage.Text = "Фаилы";
            uiFilesTabPage.UseVisualStyleBackColor = true;
            // 
            // uiPublishTabPage
            // 
            uiPublishTabPage.Location = new Point(4, 24);
            uiPublishTabPage.Name = "uiPublishTabPage";
            uiPublishTabPage.Padding = new Padding(3);
            uiPublishTabPage.Size = new Size(1212, 757);
            uiPublishTabPage.TabIndex = 8;
            uiPublishTabPage.Text = "Публикация";
            uiPublishTabPage.UseVisualStyleBackColor = true;
            // 
            // uiStorageTabPage
            // 
            uiStorageTabPage.Controls.Add(uiSourcesComboBox);
            uiStorageTabPage.Controls.Add(uiAddSourceButton);
            uiStorageTabPage.Controls.Add(uiMediaSourcePanel);
            uiStorageTabPage.Location = new Point(4, 24);
            uiStorageTabPage.Name = "uiStorageTabPage";
            uiStorageTabPage.Padding = new Padding(3);
            uiStorageTabPage.Size = new Size(1212, 757);
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
            uiRelationsTabPage.Size = new Size(1212, 757);
            uiRelationsTabPage.TabIndex = 2;
            uiRelationsTabPage.Text = "Связи";
            uiRelationsTabPage.UseVisualStyleBackColor = true;
            // 
            // uiRelationsGraphTabPage
            // 
            uiRelationsGraphTabPage.Controls.Add(uiRelationsGraphControl);
            uiRelationsGraphTabPage.Location = new Point(4, 24);
            uiRelationsGraphTabPage.Name = "uiRelationsGraphTabPage";
            uiRelationsGraphTabPage.Size = new Size(1212, 757);
            uiRelationsGraphTabPage.TabIndex = 7;
            uiRelationsGraphTabPage.Text = "Граф связей";
            uiRelationsGraphTabPage.UseVisualStyleBackColor = true;
            // 
            // uiRelationsGraphControl
            // 
            uiRelationsGraphControl.Dock = DockStyle.Fill;
            uiRelationsGraphControl.Location = new Point(0, 0);
            uiRelationsGraphControl.Name = "uiRelationsGraphControl";
            uiRelationsGraphControl.Size = new Size(1212, 757);
            uiRelationsGraphControl.TabIndex = 0;
            // 
            // uiAuditTabPage
            // 
            uiAuditTabPage.Controls.Add(button1);
            uiAuditTabPage.Controls.Add(uiRunningActionsFlowLayoutPanel);
            uiAuditTabPage.Controls.Add(uiAuditSyncHeaderLabel);
            uiAuditTabPage.Controls.Add(uiAuditBulkPanel);
            uiAuditTabPage.Controls.Add(uiAuditSourcesPanel);
            uiAuditTabPage.Controls.Add(uiAuditMaintenanceHeaderLabel);
            uiAuditTabPage.Controls.Add(uiClearDatabaseButton);
            uiAuditTabPage.Controls.Add(uiClearTypeComboBox);
            uiAuditTabPage.Controls.Add(uiClearSpecificTypeButton);
            uiAuditTabPage.Controls.Add(uiForceScanButton);
            uiAuditTabPage.Location = new Point(4, 24);
            uiAuditTabPage.Name = "uiAuditTabPage";
            uiAuditTabPage.Size = new Size(1212, 757);
            uiAuditTabPage.TabIndex = 3;
            uiAuditTabPage.Text = "Аудит";
            uiAuditTabPage.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button1.Location = new Point(6, 552);
            button1.Name = "button1";
            button1.Size = new Size(315, 23);
            button1.TabIndex = 0;
            button1.Text = "обновить список запущенных процессов";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // uiRunningActionsFlowLayoutPanel
            // 
            uiRunningActionsFlowLayoutPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uiRunningActionsFlowLayoutPanel.AutoScroll = true;
            uiRunningActionsFlowLayoutPanel.Location = new Point(3, 582);
            uiRunningActionsFlowLayoutPanel.Name = "uiRunningActionsFlowLayoutPanel";
            uiRunningActionsFlowLayoutPanel.Size = new Size(1199, 115);
            uiRunningActionsFlowLayoutPanel.TabIndex = 8;
            // 
            // uiAuditSyncHeaderLabel
            // 
            uiAuditSyncHeaderLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiAuditSyncHeaderLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            uiAuditSyncHeaderLabel.Location = new Point(6, 6);
            uiAuditSyncHeaderLabel.Name = "uiAuditSyncHeaderLabel";
            uiAuditSyncHeaderLabel.Size = new Size(1196, 20);
            uiAuditSyncHeaderLabel.TabIndex = 0;
            uiAuditSyncHeaderLabel.Text = "Синхронизация по источникам";
            // 
            // uiAuditBulkPanel
            // 
            uiAuditBulkPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiAuditBulkPanel.Controls.Add(uiBulkSourcesLabel);
            uiAuditBulkPanel.Controls.Add(uiSyncButton);
            uiAuditBulkPanel.Controls.Add(uiQuickSyncButton);
            uiAuditBulkPanel.Controls.Add(uiSyncNewButton);
            uiAuditBulkPanel.Controls.Add(uiBulkProgressLabel);
            uiAuditBulkPanel.Location = new Point(6, 30);
            uiAuditBulkPanel.Name = "uiAuditBulkPanel";
            uiAuditBulkPanel.Size = new Size(1196, 40);
            uiAuditBulkPanel.TabIndex = 1;
            // 
            // uiBulkSourcesLabel
            // 
            uiBulkSourcesLabel.AutoSize = true;
            uiBulkSourcesLabel.Location = new Point(6, 11);
            uiBulkSourcesLabel.Name = "uiBulkSourcesLabel";
            uiBulkSourcesLabel.Size = new Size(91, 15);
            uiBulkSourcesLabel.TabIndex = 0;
            uiBulkSourcesLabel.Text = "Все источники:";
            // 
            // uiSyncButton
            // 
            uiSyncButton.Location = new Point(110, 6);
            uiSyncButton.Name = "uiSyncButton";
            uiSyncButton.Size = new Size(130, 28);
            uiSyncButton.TabIndex = 1;
            uiSyncButton.Text = "Полная";
            uiAuditToolTip.SetToolTip(uiSyncButton, "Полная синхронизация по всем источникам");
            uiSyncButton.UseVisualStyleBackColor = true;
            uiSyncButton.Click += uiSyncButton_Click;
            // 
            // uiQuickSyncButton
            // 
            uiQuickSyncButton.Location = new Point(244, 6);
            uiQuickSyncButton.Name = "uiQuickSyncButton";
            uiQuickSyncButton.Size = new Size(130, 28);
            uiQuickSyncButton.TabIndex = 2;
            uiQuickSyncButton.Text = "Быстрая";
            uiAuditToolTip.SetToolTip(uiQuickSyncButton, "Загружает список медиа без метаданных по всем источникам");
            uiQuickSyncButton.UseVisualStyleBackColor = true;
            uiQuickSyncButton.Click += uiQuickSyncButton_Click;
            // 
            // uiSyncNewButton
            // 
            uiSyncNewButton.Location = new Point(378, 6);
            uiSyncNewButton.Name = "uiSyncNewButton";
            uiSyncNewButton.Size = new Size(130, 28);
            uiSyncNewButton.TabIndex = 3;
            uiSyncNewButton.Text = "Новые";
            uiAuditToolTip.SetToolTip(uiSyncNewButton, "Синхронизирует до первого уже известного медиа по всем источникам");
            uiSyncNewButton.UseVisualStyleBackColor = true;
            uiSyncNewButton.Click += uiSyncNewButton_Click;
            // 
            // uiBulkProgressLabel
            // 
            uiBulkProgressLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiBulkProgressLabel.AutoEllipsis = true;
            uiBulkProgressLabel.ForeColor = Color.DimGray;
            uiBulkProgressLabel.Location = new Point(514, 11);
            uiBulkProgressLabel.Name = "uiBulkProgressLabel";
            uiBulkProgressLabel.Size = new Size(676, 18);
            uiBulkProgressLabel.TabIndex = 4;
            uiBulkProgressLabel.Text = "—";
            // 
            // uiAuditSourcesPanel
            // 
            uiAuditSourcesPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiAuditSourcesPanel.AutoScroll = true;
            uiAuditSourcesPanel.BorderStyle = BorderStyle.FixedSingle;
            uiAuditSourcesPanel.Controls.Add(flowLayoutPanel1);
            uiAuditSourcesPanel.FlowDirection = FlowDirection.TopDown;
            uiAuditSourcesPanel.Location = new Point(6, 76);
            uiAuditSourcesPanel.Name = "uiAuditSourcesPanel";
            uiAuditSourcesPanel.Size = new Size(1196, 466);
            uiAuditSourcesPanel.TabIndex = 2;
            uiAuditSourcesPanel.WrapContents = false;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.BorderStyle = BorderStyle.FixedSingle;
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.Location = new Point(3, 3);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(0, 466);
            flowLayoutPanel1.TabIndex = 3;
            flowLayoutPanel1.WrapContents = false;
            // 
            // uiAuditMaintenanceHeaderLabel
            // 
            uiAuditMaintenanceHeaderLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            uiAuditMaintenanceHeaderLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            uiAuditMaintenanceHeaderLabel.Location = new Point(6, 700);
            uiAuditMaintenanceHeaderLabel.Name = "uiAuditMaintenanceHeaderLabel";
            uiAuditMaintenanceHeaderLabel.Size = new Size(1196, 20);
            uiAuditMaintenanceHeaderLabel.TabIndex = 3;
            uiAuditMaintenanceHeaderLabel.Text = "Обслуживание";
            // 
            // uiClearDatabaseButton
            // 
            uiClearDatabaseButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uiClearDatabaseButton.Location = new Point(6, 724);
            uiClearDatabaseButton.Name = "uiClearDatabaseButton";
            uiClearDatabaseButton.Size = new Size(220, 30);
            uiClearDatabaseButton.TabIndex = 4;
            uiClearDatabaseButton.Text = "Очистить базу данных";
            uiAuditToolTip.SetToolTip(uiClearDatabaseButton, "Удаляет все данные из базы. Операция необратима!");
            uiClearDatabaseButton.UseVisualStyleBackColor = true;
            uiClearDatabaseButton.Click += uiClearDatabaseButton_Click;
            // 
            // uiClearTypeComboBox
            // 
            uiClearTypeComboBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uiClearTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiClearTypeComboBox.FormattingEnabled = true;
            uiClearTypeComboBox.Items.AddRange(new object[] { "medias", "sources", "source_relations" });
            uiClearTypeComboBox.Location = new Point(232, 728);
            uiClearTypeComboBox.Name = "uiClearTypeComboBox";
            uiClearTypeComboBox.Size = new Size(180, 23);
            uiClearTypeComboBox.TabIndex = 5;
            // 
            // uiClearSpecificTypeButton
            // 
            uiClearSpecificTypeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uiClearSpecificTypeButton.Location = new Point(418, 724);
            uiClearSpecificTypeButton.Name = "uiClearSpecificTypeButton";
            uiClearSpecificTypeButton.Size = new Size(140, 30);
            uiClearSpecificTypeButton.TabIndex = 6;
            uiClearSpecificTypeButton.Text = "Очистить тип";
            uiAuditToolTip.SetToolTip(uiClearSpecificTypeButton, "Удаляет все записи выбранного типа коллекции из базы данных");
            uiClearSpecificTypeButton.UseVisualStyleBackColor = true;
            uiClearSpecificTypeButton.Click += uiClearSpecificTypeButton_Click;
            // 
            // uiForceScanButton
            // 
            uiForceScanButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uiForceScanButton.Location = new Point(564, 724);
            uiForceScanButton.Name = "uiForceScanButton";
            uiForceScanButton.Size = new Size(220, 30);
            uiForceScanButton.TabIndex = 7;
            uiForceScanButton.Text = "Принудительное сканирование";
            uiAuditToolTip.SetToolTip(uiForceScanButton, "Отладочная функция принудительного сканирования");
            uiForceScanButton.UseVisualStyleBackColor = true;
            uiForceScanButton.Click += uiForceScanButton_Click;
            // 
            // uiSyncTreeTabPage
            // 
            uiSyncTreeTabPage.Controls.Add(uiSyncTreeControl);
            uiSyncTreeTabPage.Location = new Point(4, 24);
            uiSyncTreeTabPage.Name = "uiSyncTreeTabPage";
            uiSyncTreeTabPage.Size = new Size(1212, 757);
            uiSyncTreeTabPage.TabIndex = 6;
            uiSyncTreeTabPage.Text = "Дерево синхронизации";
            uiSyncTreeTabPage.UseVisualStyleBackColor = true;
            // 
            // uiSyncTreeControl
            // 
            uiSyncTreeControl.Dock = DockStyle.Fill;
            uiSyncTreeControl.Location = new Point(0, 0);
            uiSyncTreeControl.Name = "uiSyncTreeControl";
            uiSyncTreeControl.Size = new Size(1212, 757);
            uiSyncTreeControl.TabIndex = 0;
            //
            // uiCommentsTabPage
            //
            uiCommentsTabPage.Controls.Add(uiCommentsViewControl);
            uiCommentsTabPage.Location = new Point(4, 24);
            uiCommentsTabPage.Name = "uiCommentsTabPage";
            uiCommentsTabPage.Padding = new Padding(3);
            uiCommentsTabPage.Size = new Size(1212, 757);
            uiCommentsTabPage.TabIndex = 9;
            uiCommentsTabPage.Text = "Комментарии";
            uiCommentsTabPage.UseVisualStyleBackColor = true;
            //
            // uiCommentsViewControl
            //
            uiCommentsViewControl.Dock = DockStyle.Fill;
            uiCommentsViewControl.Location = new Point(3, 3);
            uiCommentsViewControl.Name = "uiCommentsViewControl";
            uiCommentsViewControl.Size = new Size(1206, 751);
            uiCommentsViewControl.TabIndex = 0;
            //
            // uiCommentsHtmlTabPage
            //
            uiCommentsHtmlTabPage.Controls.Add(uiCommentsHtmlControl);
            uiCommentsHtmlTabPage.Location = new Point(4, 24);
            uiCommentsHtmlTabPage.Name = "uiCommentsHtmlTabPage";
            uiCommentsHtmlTabPage.Padding = new Padding(3);
            uiCommentsHtmlTabPage.Size = new Size(1212, 757);
            uiCommentsHtmlTabPage.TabIndex = 11;
            uiCommentsHtmlTabPage.Text = "Комментарии (HTML)";
            uiCommentsHtmlTabPage.UseVisualStyleBackColor = true;
            //
            // uiCommentsHtmlControl
            //
            uiCommentsHtmlControl.Dock = DockStyle.Fill;
            uiCommentsHtmlControl.Location = new Point(3, 3);
            uiCommentsHtmlControl.Name = "uiCommentsHtmlControl";
            uiCommentsHtmlControl.Size = new Size(1206, 751);
            uiCommentsHtmlControl.TabIndex = 0;
            //
            // uiLogsTabPage
            //
            uiLogsTabPage.Controls.Add(uiLogsToolbarPanel);
            uiLogsTabPage.Location = new Point(4, 24);
            uiLogsTabPage.Name = "uiLogsTabPage";
            uiLogsTabPage.Padding = new Padding(3);
            uiLogsTabPage.Size = new Size(1212, 757);
            uiLogsTabPage.TabIndex = 4;
            uiLogsTabPage.Text = "Логи";
            uiLogsTabPage.UseVisualStyleBackColor = true;
            // 
            // uiLogsToolbarPanel
            // 
            uiLogsToolbarPanel.Controls.Add(uiReportIssueButton);
            uiLogsToolbarPanel.Dock = DockStyle.Top;
            uiLogsToolbarPanel.Location = new Point(3, 3);
            uiLogsToolbarPanel.Name = "uiLogsToolbarPanel";
            uiLogsToolbarPanel.Size = new Size(1206, 34);
            uiLogsToolbarPanel.TabIndex = 0;
            // 
            // uiReportIssueButton
            // 
            uiReportIssueButton.Location = new Point(3, 4);
            uiReportIssueButton.Name = "uiReportIssueButton";
            uiReportIssueButton.Size = new Size(200, 26);
            uiReportIssueButton.TabIndex = 0;
            uiReportIssueButton.Text = "Сообщить о проблеме";
            uiReportIssueButton.UseVisualStyleBackColor = true;
            uiReportIssueButton.Click += uiReportIssueButton_Click;
            // 
            // uiToolsTabPage
            // 
            uiToolsTabPage.Controls.Add(groupBox3);
            uiToolsTabPage.Controls.Add(groupBox2);
            uiToolsTabPage.Controls.Add(groupBox1);
            uiToolsTabPage.Controls.Add(uiManageToolsButton);
            uiToolsTabPage.Controls.Add(uiOpenSettingsButton);
            uiToolsTabPage.Controls.Add(uiCheckUpdatesButton);
            uiToolsTabPage.Location = new Point(4, 24);
            uiToolsTabPage.Name = "uiToolsTabPage";
            uiToolsTabPage.Size = new Size(1212, 757);
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
            // uiOpenSettingsButton
            // 
            uiOpenSettingsButton.Location = new Point(6, 259);
            uiOpenSettingsButton.Name = "uiOpenSettingsButton";
            uiOpenSettingsButton.Size = new Size(200, 35);
            uiOpenSettingsButton.TabIndex = 6;
            uiOpenSettingsButton.Text = "Настройки приложения";
            uiOpenSettingsButton.UseVisualStyleBackColor = true;
            uiOpenSettingsButton.Click += uiOpenSettingsButton_Click;
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1244, 809);
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
            uiRelationsGraphTabPage.ResumeLayout(false);
            uiAuditTabPage.ResumeLayout(false);
            uiAuditBulkPanel.ResumeLayout(false);
            uiAuditBulkPanel.PerformLayout();
            uiAuditSourcesPanel.ResumeLayout(false);
            uiSyncTreeTabPage.ResumeLayout(false);
            uiCommentsTabPage.ResumeLayout(false);
            uiCommentsHtmlTabPage.ResumeLayout(false);
            uiLogsTabPage.ResumeLayout(false);
            uiLogsToolbarPanel.ResumeLayout(false);
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
        private TabPage uiPublishTabPage;
        private TabPage uiStorageTabPage;
        private TabPage uiRelationsTabPage;
        private TabPage uiRelationsGraphTabPage;
        private RelationsGraphControl uiRelationsGraphControl;
        private TabPage uiAuditTabPage;
        private TabPage uiSyncTreeTabPage;
        private SyncTreeControl uiSyncTreeControl;
        private Button uiForceScanButton;
        private Button uiClearDatabaseButton;
        private ComboBox uiClearTypeComboBox;
        private Button uiClearSpecificTypeButton;
        private TabPage uiLogsTabPage;
        private Panel uiLogsToolbarPanel;
        private Button uiReportIssueButton;
        private TabPage uiCommentsTabPage;
        private CommentsViewControl uiCommentsViewControl;
        private TabPage uiCommentsHtmlTabPage;
        private CommentsHtmlControl uiCommentsHtmlControl;
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
        private FlowLayoutPanel uiAuditSourcesPanel;
        private Panel uiAuditBulkPanel;
        private Label uiAuditSyncHeaderLabel;
        private Label uiAuditMaintenanceHeaderLabel;
        private Label uiBulkSourcesLabel;
        private Label uiBulkProgressLabel;
        private Button uiManageToolsButton;
        private Button uiOpenSettingsButton;
        private Button uiCheckUpdatesButton;
        private Button uiSyncNewButton;
        private ToolTip uiAuditToolTip;
        private FlowLayoutPanel uiRunningActionsFlowLayoutPanel;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button button1;
    }
}
