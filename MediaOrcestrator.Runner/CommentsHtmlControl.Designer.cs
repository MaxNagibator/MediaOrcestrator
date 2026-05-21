namespace MediaOrcestrator.Runner;

partial class CommentsHtmlControl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _applyFiltersCts?.Cancel();
            _applyFiltersCts?.Dispose();
            _searchDebounce?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private TableLayoutPanel uiFiltersPanel;
    private Label uiSourceLabel;
    private Button uiSourceFilterButton;
    private ContextMenuStrip uiSourceMenu;
    private Label uiSearchLabel;
    private TextBox uiSearchTextBox;
    private Button uiForceFetchAllButton;
    private Label uiLimitLabel;
    private NumericUpDown uiLimitNumeric;
    private Label uiReplyStatusLabel;
    private ComboBox uiReplyStatusComboBox;
    private Button uiAppearanceButton;
    private TabControl uiTabs;
    private TabPage uiGroupedTab;
    private TabPage uiFlatTab;
    private CommentsBrowserView uiBrowserView;
    private StatusStrip uiStatusStrip;
    private ToolStripStatusLabel uiStatusLabel;
    private ToolStripProgressBar uiFetchProgressBar;
    private ToolStripStatusLabel uiFetchCounterLabel;
    private ToolTip uiToolTip;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        uiFiltersPanel = new TableLayoutPanel();
        uiSourceLabel = new Label();
        uiSourceFilterButton = new Button();
        uiSourceMenu = new ContextMenuStrip(components);
        uiSearchLabel = new Label();
        uiSearchTextBox = new TextBox();
        uiForceFetchAllButton = new Button();
        uiReplyStatusLabel = new Label();
        uiReplyStatusComboBox = new ComboBox();
        uiLimitLabel = new Label();
        uiLimitNumeric = new NumericUpDown();
        uiAppearanceButton = new Button();
        uiTabs = new TabControl();
        uiGroupedTab = new TabPage();
        uiFlatTab = new TabPage();
        uiBrowserView = new CommentsBrowserView();
        uiStatusStrip = new StatusStrip();
        uiStatusLabel = new ToolStripStatusLabel();
        uiFetchProgressBar = new ToolStripProgressBar();
        uiFetchCounterLabel = new ToolStripStatusLabel();
        uiToolTip = new ToolTip(components);
        uiFiltersPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiLimitNumeric).BeginInit();
        uiStatusStrip.SuspendLayout();
        SuspendLayout();
        //
        // uiFiltersPanel
        //
        uiFiltersPanel.ColumnCount = 10;
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiFiltersPanel.Controls.Add(uiSourceLabel, 0, 0);
        uiFiltersPanel.Controls.Add(uiSourceFilterButton, 1, 0);
        uiFiltersPanel.Controls.Add(uiSearchLabel, 2, 0);
        uiFiltersPanel.Controls.Add(uiSearchTextBox, 3, 0);
        uiFiltersPanel.Controls.Add(uiForceFetchAllButton, 4, 0);
        uiFiltersPanel.Controls.Add(uiReplyStatusLabel, 5, 0);
        uiFiltersPanel.Controls.Add(uiReplyStatusComboBox, 6, 0);
        uiFiltersPanel.Controls.Add(uiLimitLabel, 7, 0);
        uiFiltersPanel.Controls.Add(uiLimitNumeric, 8, 0);
        uiFiltersPanel.Controls.Add(uiAppearanceButton, 9, 0);
        uiFiltersPanel.Dock = DockStyle.Top;
        uiFiltersPanel.Location = new Point(0, 0);
        uiFiltersPanel.Name = "uiFiltersPanel";
        uiFiltersPanel.Padding = new Padding(8, 4, 8, 4);
        uiFiltersPanel.RowCount = 1;
        uiFiltersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        uiFiltersPanel.Size = new Size(1100, 36);
        uiFiltersPanel.TabIndex = 0;
        //
        // uiSourceLabel
        //
        uiSourceLabel.AutoSize = true;
        uiSourceLabel.Margin = new Padding(0, 7, 6, 0);
        uiSourceLabel.Name = "uiSourceLabel";
        uiSourceLabel.TabIndex = 0;
        uiSourceLabel.Text = "Источник:";
        //
        // uiSourceFilterButton
        //
        uiSourceFilterButton.AutoEllipsis = true;
        uiSourceFilterButton.Margin = new Padding(0, 2, 12, 2);
        uiSourceFilterButton.MinimumSize = new Size(200, 25);
        uiSourceFilterButton.Name = "uiSourceFilterButton";
        uiSourceFilterButton.Size = new Size(200, 25);
        uiSourceFilterButton.TabIndex = 1;
        uiSourceFilterButton.Text = "(любой)";
        uiSourceFilterButton.TextAlign = ContentAlignment.MiddleLeft;
        uiSourceFilterButton.UseVisualStyleBackColor = true;
        uiSourceFilterButton.Click += uiSourceFilterButton_Click;
        //
        // uiSourceMenu
        //
        uiSourceMenu.Name = "uiSourceMenu";
        uiSourceMenu.ShowCheckMargin = true;
        uiSourceMenu.ShowImageMargin = false;
        uiSourceMenu.Closing += uiSourceMenu_Closing;
        uiSourceMenu.Closed += uiSourceMenu_Closed;
        //
        // uiSearchLabel
        //
        uiSearchLabel.AutoSize = true;
        uiSearchLabel.Margin = new Padding(0, 7, 6, 0);
        uiSearchLabel.Name = "uiSearchLabel";
        uiSearchLabel.TabIndex = 2;
        uiSearchLabel.Text = "Поиск:";
        //
        // uiSearchTextBox
        //
        uiSearchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiSearchTextBox.Margin = new Padding(0, 3, 12, 3);
        uiSearchTextBox.Name = "uiSearchTextBox";
        uiSearchTextBox.PlaceholderText = "текст, автор или название медиа";
        uiSearchTextBox.Size = new Size(200, 23);
        uiSearchTextBox.TabIndex = 3;
        uiSearchTextBox.TextChanged += uiSearchTextBox_TextChanged;
        uiSearchTextBox.KeyDown += uiSearchTextBox_KeyDown;
        //
        // uiForceFetchAllButton
        //
        uiForceFetchAllButton.AutoSize = true;
        uiForceFetchAllButton.Enabled = false;
        uiForceFetchAllButton.Margin = new Padding(0, 2, 12, 2);
        uiForceFetchAllButton.MinimumSize = new Size(180, 25);
        uiForceFetchAllButton.Name = "uiForceFetchAllButton";
        uiForceFetchAllButton.TabIndex = 4;
        uiForceFetchAllButton.Text = "Загрузить из источника...";
        uiForceFetchAllButton.UseVisualStyleBackColor = true;
        uiForceFetchAllButton.Click += uiForceFetchAllButton_Click;
        uiToolTip.SetToolTip(uiForceFetchAllButton,
            "Открыть параметры и загрузить комментарии для медиа выбранного источника."
            + Environment.NewLine
            + "Для каждого попавшего медиа всегда загружаются все его комментарии.");
        //
        // uiReplyStatusLabel
        //
        uiReplyStatusLabel.AutoSize = true;
        uiReplyStatusLabel.Margin = new Padding(0, 7, 6, 0);
        uiReplyStatusLabel.Name = "uiReplyStatusLabel";
        uiReplyStatusLabel.Text = "Статус ответа:";
        //
        // uiReplyStatusComboBox
        //
        uiReplyStatusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        uiReplyStatusComboBox.Margin = new Padding(0, 3, 12, 3);
        uiReplyStatusComboBox.Name = "uiReplyStatusComboBox";
        uiReplyStatusComboBox.Size = new Size(190, 23);
        uiReplyStatusComboBox.SelectedIndexChanged += uiReplyStatusComboBox_SelectedIndexChanged;
        //
        // uiLimitLabel
        //
        uiLimitLabel.AutoSize = true;
        uiLimitLabel.Margin = new Padding(0, 7, 6, 0);
        uiLimitLabel.Name = "uiLimitLabel";
        uiLimitLabel.TabIndex = 5;
        uiLimitLabel.Text = "Показывать:";
        //
        // uiLimitNumeric
        //
        uiLimitNumeric.Increment = 100;
        uiLimitNumeric.Margin = new Padding(0, 3, 0, 3);
        uiLimitNumeric.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        uiLimitNumeric.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
        uiLimitNumeric.Name = "uiLimitNumeric";
        uiLimitNumeric.Size = new Size(80, 23);
        uiLimitNumeric.TabIndex = 6;
        uiLimitNumeric.Value = new decimal(new int[] { 1000, 0, 0, 0 });
        uiLimitNumeric.ValueChanged += uiFetchSettingsValueChanged;
        uiToolTip.SetToolTip(uiLimitNumeric,
            "Сколько комментариев показывать в списке."
            + Environment.NewLine
            + "Загрузка из источника всегда тянет все комментарии медиа целиком.");
        //
        // uiAppearanceButton
        //
        uiAppearanceButton.AutoSize = true;
        uiAppearanceButton.Margin = new Padding(12, 2, 0, 2);
        uiAppearanceButton.MinimumSize = new Size(80, 25);
        uiAppearanceButton.Name = "uiAppearanceButton";
        uiAppearanceButton.TabIndex = 7;
        uiAppearanceButton.Text = "Вид...";
        uiAppearanceButton.UseVisualStyleBackColor = true;
        uiAppearanceButton.Click += uiAppearanceButton_Click;
        uiToolTip.SetToolTip(uiAppearanceButton,
            "Настроить размеры шрифта элементов ленты комментариев.");
        //
        // uiTabs
        //
        uiTabs.Dock = DockStyle.Fill;
        uiTabs.Name = "uiTabs";
        uiTabs.Location = new Point(0, 36);
        uiTabs.Size = new Size(1100, 592);
        uiTabs.TabIndex = 1;
        uiTabs.Controls.Add(uiFlatTab);
        uiTabs.Controls.Add(uiGroupedTab);
        uiTabs.SelectedIndexChanged += uiTabs_SelectedIndexChanged;
        //
        // uiGroupedTab
        //
        uiGroupedTab.Name = "uiGroupedTab";
        uiGroupedTab.Text = "Сгруппировано";
        uiGroupedTab.Padding = new Padding(0);
        uiGroupedTab.UseVisualStyleBackColor = true;
        //
        // uiFlatTab
        //
        uiFlatTab.Name = "uiFlatTab";
        uiFlatTab.Text = "Плоский список";
        uiFlatTab.Padding = new Padding(0);
        uiFlatTab.UseVisualStyleBackColor = true;
        //
        // uiBrowserView
        //
        uiBrowserView.Dock = DockStyle.Fill;
        uiBrowserView.Name = "uiBrowserView";
        uiBrowserView.TabIndex = 0;
        uiFlatTab.Controls.Add(uiBrowserView);
        //
        // uiStatusStrip
        //
        uiStatusStrip.Items.AddRange(new ToolStripItem[] { uiStatusLabel, uiFetchCounterLabel, uiFetchProgressBar });
        uiStatusStrip.Location = new Point(0, 628);
        uiStatusStrip.Name = "uiStatusStrip";
        uiStatusStrip.Size = new Size(1100, 22);
        uiStatusStrip.TabIndex = 2;
        //
        // uiStatusLabel
        //
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Size = new Size(1085, 17);
        uiStatusLabel.Spring = true;
        uiStatusLabel.Text = "Готов";
        uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiFetchProgressBar
        //
        uiFetchProgressBar.Alignment = ToolStripItemAlignment.Right;
        uiFetchProgressBar.Name = "uiFetchProgressBar";
        uiFetchProgressBar.Size = new Size(200, 16);
        uiFetchProgressBar.Visible = false;
        //
        // uiFetchCounterLabel
        //
        uiFetchCounterLabel.Alignment = ToolStripItemAlignment.Right;
        uiFetchCounterLabel.Name = "uiFetchCounterLabel";
        uiFetchCounterLabel.Padding = new Padding(6, 0, 6, 0);
        uiFetchCounterLabel.TextAlign = ContentAlignment.MiddleRight;
        uiFetchCounterLabel.Visible = false;
        //
        // CommentsHtmlControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiTabs);
        Controls.Add(uiStatusStrip);
        Controls.Add(uiFiltersPanel);
        Name = "CommentsHtmlControl";
        Size = new Size(1100, 650);
        uiFiltersPanel.ResumeLayout(false);
        uiFiltersPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiLimitNumeric).EndInit();
        uiStatusStrip.ResumeLayout(false);
        uiStatusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
