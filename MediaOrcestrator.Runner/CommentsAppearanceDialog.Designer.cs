namespace MediaOrcestrator.Runner;

partial class CommentsAppearanceDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private TableLayoutPanel uiLayout;
    private Label uiHintLabel;
    private Label uiBaseLabel;
    private NumericUpDown uiBaseNumeric;
    private Label uiCommentLabel;
    private NumericUpDown uiCommentNumeric;
    private Label uiAuthorLabel;
    private NumericUpDown uiAuthorNumeric;
    private Label uiMetaLabel;
    private NumericUpDown uiMetaNumeric;
    private Label uiMediaTitleLabel;
    private NumericUpDown uiMediaTitleNumeric;
    private Label uiBadgeLabel;
    private NumericUpDown uiBadgeNumeric;
    private Label uiLineHeightLabel;
    private NumericUpDown uiLineHeightNumeric;
    private Label uiReplySectionLabel;
    private Label uiReplyPresetLabel;
    private ComboBox uiReplyPresetCombo;
    private Label uiReplyCustomLabel;
    private TextBox uiReplyCustomTextBox;
    private TableLayoutPanel uiButtonsRow;
    private Button uiResetButton;
    private FlowLayoutPanel uiButtonsPanel;
    private Button uiSaveButton;
    private Button uiCancelButton;
    private ToolTip uiToolTip;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        uiLayout = new TableLayoutPanel();
        uiHintLabel = new Label();
        uiBaseLabel = new Label();
        uiBaseNumeric = new NumericUpDown();
        uiCommentLabel = new Label();
        uiCommentNumeric = new NumericUpDown();
        uiAuthorLabel = new Label();
        uiAuthorNumeric = new NumericUpDown();
        uiMetaLabel = new Label();
        uiMetaNumeric = new NumericUpDown();
        uiMediaTitleLabel = new Label();
        uiMediaTitleNumeric = new NumericUpDown();
        uiBadgeLabel = new Label();
        uiBadgeNumeric = new NumericUpDown();
        uiLineHeightLabel = new Label();
        uiLineHeightNumeric = new NumericUpDown();
        uiReplySectionLabel = new Label();
        uiReplyPresetLabel = new Label();
        uiReplyPresetCombo = new ComboBox();
        uiReplyCustomLabel = new Label();
        uiReplyCustomTextBox = new TextBox();
        uiButtonsRow = new TableLayoutPanel();
        uiResetButton = new Button();
        uiButtonsPanel = new FlowLayoutPanel();
        uiSaveButton = new Button();
        uiCancelButton = new Button();
        uiToolTip = new ToolTip(components);
        uiLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiBaseNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiCommentNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiAuthorNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiMetaNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiMediaTitleNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiBadgeNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiLineHeightNumeric).BeginInit();
        uiButtonsRow.SuspendLayout();
        uiButtonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiLayout
        //
        uiLayout.ColumnCount = 2;
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiLayout.Controls.Add(uiHintLabel, 0, 0);
        uiLayout.SetColumnSpan(uiHintLabel, 2);
        uiLayout.Controls.Add(uiBaseLabel, 0, 1);
        uiLayout.Controls.Add(uiBaseNumeric, 1, 1);
        uiLayout.Controls.Add(uiCommentLabel, 0, 2);
        uiLayout.Controls.Add(uiCommentNumeric, 1, 2);
        uiLayout.Controls.Add(uiAuthorLabel, 0, 3);
        uiLayout.Controls.Add(uiAuthorNumeric, 1, 3);
        uiLayout.Controls.Add(uiMetaLabel, 0, 4);
        uiLayout.Controls.Add(uiMetaNumeric, 1, 4);
        uiLayout.Controls.Add(uiMediaTitleLabel, 0, 5);
        uiLayout.Controls.Add(uiMediaTitleNumeric, 1, 5);
        uiLayout.Controls.Add(uiBadgeLabel, 0, 6);
        uiLayout.Controls.Add(uiBadgeNumeric, 1, 6);
        uiLayout.Controls.Add(uiLineHeightLabel, 0, 7);
        uiLayout.Controls.Add(uiLineHeightNumeric, 1, 7);
        uiLayout.Controls.Add(uiReplySectionLabel, 0, 8);
        uiLayout.SetColumnSpan(uiReplySectionLabel, 2);
        uiLayout.Controls.Add(uiReplyPresetLabel, 0, 9);
        uiLayout.Controls.Add(uiReplyPresetCombo, 1, 9);
        uiLayout.Controls.Add(uiReplyCustomLabel, 0, 10);
        uiLayout.Controls.Add(uiReplyCustomTextBox, 1, 10);
        uiLayout.Controls.Add(uiButtonsRow, 0, 11);
        uiLayout.SetColumnSpan(uiButtonsRow, 2);
        uiLayout.Dock = DockStyle.Fill;
        uiLayout.Name = "uiLayout";
        uiLayout.Padding = new Padding(12);
        uiLayout.RowCount = 12;
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        //
        // uiHintLabel
        //
        uiHintLabel.AutoSize = true;
        uiHintLabel.Margin = new Padding(0, 0, 0, 10);
        uiHintLabel.MaximumSize = new Size(300, 0);
        uiHintLabel.Name = "uiHintLabel";
        uiHintLabel.Text = "Размеры — в пикселях, применяются ко всем площадкам сразу.";
        //
        // uiBaseLabel
        //
        uiBaseLabel.AutoSize = true;
        uiBaseLabel.Margin = new Padding(0, 7, 8, 0);
        uiBaseLabel.Name = "uiBaseLabel";
        uiBaseLabel.Text = "Базовый шрифт";
        //
        // uiBaseNumeric
        //
        uiBaseNumeric.Margin = new Padding(0, 3, 0, 3);
        uiBaseNumeric.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
        uiBaseNumeric.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        uiBaseNumeric.Name = "uiBaseNumeric";
        uiBaseNumeric.Size = new Size(70, 23);
        uiBaseNumeric.Value = new decimal(new int[] { 13, 0, 0, 0 });
        uiToolTip.SetToolTip(uiBaseNumeric,
            "Размер текста по умолчанию (html, body). От него наследуется всё без явного размера.");
        //
        // uiCommentLabel
        //
        uiCommentLabel.AutoSize = true;
        uiCommentLabel.Margin = new Padding(0, 7, 8, 0);
        uiCommentLabel.Name = "uiCommentLabel";
        uiCommentLabel.Text = "Текст комментария";
        //
        // uiCommentNumeric
        //
        uiCommentNumeric.Margin = new Padding(0, 3, 0, 3);
        uiCommentNumeric.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
        uiCommentNumeric.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        uiCommentNumeric.Name = "uiCommentNumeric";
        uiCommentNumeric.Size = new Size(70, 23);
        uiCommentNumeric.Value = new decimal(new int[] { 13, 0, 0, 0 });
        uiToolTip.SetToolTip(uiCommentNumeric, "Тело комментария.");
        //
        // uiAuthorLabel
        //
        uiAuthorLabel.AutoSize = true;
        uiAuthorLabel.Margin = new Padding(0, 7, 8, 0);
        uiAuthorLabel.Name = "uiAuthorLabel";
        uiAuthorLabel.Text = "Имя автора";
        //
        // uiAuthorNumeric
        //
        uiAuthorNumeric.Margin = new Padding(0, 3, 0, 3);
        uiAuthorNumeric.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
        uiAuthorNumeric.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        uiAuthorNumeric.Name = "uiAuthorNumeric";
        uiAuthorNumeric.Size = new Size(70, 23);
        uiAuthorNumeric.Value = new decimal(new int[] { 12, 0, 0, 0 });
        uiToolTip.SetToolTip(uiAuthorNumeric, "Имя автора и чип автора в плоском списке.");
        //
        // uiMetaLabel
        //
        uiMetaLabel.AutoSize = true;
        uiMetaLabel.Margin = new Padding(0, 7, 8, 0);
        uiMetaLabel.Name = "uiMetaLabel";
        uiMetaLabel.Text = "Дата и мета";
        //
        // uiMetaNumeric
        //
        uiMetaNumeric.Margin = new Padding(0, 3, 0, 3);
        uiMetaNumeric.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
        uiMetaNumeric.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        uiMetaNumeric.Name = "uiMetaNumeric";
        uiMetaNumeric.Size = new Size(70, 23);
        uiMetaNumeric.Value = new decimal(new int[] { 12, 0, 0, 0 });
        uiToolTip.SetToolTip(uiMetaNumeric, "Дата, шапка строки и служебная мета над текстом.");
        //
        // uiMediaTitleLabel
        //
        uiMediaTitleLabel.AutoSize = true;
        uiMediaTitleLabel.Margin = new Padding(0, 7, 8, 0);
        uiMediaTitleLabel.Name = "uiMediaTitleLabel";
        uiMediaTitleLabel.Text = "Заголовок медиа";
        //
        // uiMediaTitleNumeric
        //
        uiMediaTitleNumeric.Margin = new Padding(0, 3, 0, 3);
        uiMediaTitleNumeric.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
        uiMediaTitleNumeric.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        uiMediaTitleNumeric.Name = "uiMediaTitleNumeric";
        uiMediaTitleNumeric.Size = new Size(70, 23);
        uiMediaTitleNumeric.Value = new decimal(new int[] { 14, 0, 0, 0 });
        uiToolTip.SetToolTip(uiMediaTitleNumeric, "Заголовок медиа в группе и подпись миниатюры.");
        //
        // uiBadgeLabel
        //
        uiBadgeLabel.AutoSize = true;
        uiBadgeLabel.Margin = new Padding(0, 7, 8, 0);
        uiBadgeLabel.Name = "uiBadgeLabel";
        uiBadgeLabel.Text = "Бейджи";
        //
        // uiBadgeNumeric
        //
        uiBadgeNumeric.Margin = new Padding(0, 3, 0, 3);
        uiBadgeNumeric.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
        uiBadgeNumeric.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        uiBadgeNumeric.Name = "uiBadgeNumeric";
        uiBadgeNumeric.Size = new Size(70, 23);
        uiBadgeNumeric.Value = new decimal(new int[] { 10, 0, 0, 0 });
        uiToolTip.SetToolTip(uiBadgeNumeric, "Бейджи «автор» и источник.");
        //
        // uiLineHeightLabel
        //
        uiLineHeightLabel.AutoSize = true;
        uiLineHeightLabel.Margin = new Padding(0, 7, 8, 0);
        uiLineHeightLabel.Name = "uiLineHeightLabel";
        uiLineHeightLabel.Text = "Межстрочный интервал";
        //
        // uiLineHeightNumeric
        //
        uiLineHeightNumeric.DecimalPlaces = 2;
        uiLineHeightNumeric.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        uiLineHeightNumeric.Margin = new Padding(0, 3, 0, 3);
        uiLineHeightNumeric.Maximum = new decimal(new int[] { 250, 0, 0, 131072 });
        uiLineHeightNumeric.Minimum = new decimal(new int[] { 100, 0, 0, 131072 });
        uiLineHeightNumeric.Name = "uiLineHeightNumeric";
        uiLineHeightNumeric.Size = new Size(70, 23);
        uiLineHeightNumeric.Value = new decimal(new int[] { 145, 0, 0, 131072 });
        uiToolTip.SetToolTip(uiLineHeightNumeric, "Межстрочный интервал тела (line-height).");
        //
        // uiReplySectionLabel
        //
        uiReplySectionLabel.AutoSize = true;
        uiReplySectionLabel.Margin = new Padding(0, 14, 0, 6);
        uiReplySectionLabel.MaximumSize = new Size(320, 0);
        uiReplySectionLabel.Name = "uiReplySectionLabel";
        uiReplySectionLabel.Text = "Начало текста ответа:";
        //
        // uiReplyPresetLabel
        //
        uiReplyPresetLabel.AutoSize = true;
        uiReplyPresetLabel.Margin = new Padding(0, 7, 8, 0);
        uiReplyPresetLabel.Name = "uiReplyPresetLabel";
        uiReplyPresetLabel.Text = "Вариант";
        //
        // uiReplyPresetCombo
        //
        uiReplyPresetCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiReplyPresetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        uiReplyPresetCombo.Margin = new Padding(0, 3, 0, 3);
        uiReplyPresetCombo.Name = "uiReplyPresetCombo";
        uiReplyPresetCombo.Size = new Size(210, 23);
        uiReplyPresetCombo.SelectedIndexChanged += uiReplyPresetCombo_SelectedIndexChanged;
        uiToolTip.SetToolTip(uiReplyPresetCombo,
            "Чем подставлять начало текста при ответе на комментарий.");
        //
        // uiReplyCustomLabel
        //
        uiReplyCustomLabel.AutoSize = true;
        uiReplyCustomLabel.Margin = new Padding(0, 7, 8, 0);
        uiReplyCustomLabel.Name = "uiReplyCustomLabel";
        uiReplyCustomLabel.Text = "Свой шаблон";
        //
        // uiReplyCustomTextBox
        //
        uiReplyCustomTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiReplyCustomTextBox.Margin = new Padding(0, 3, 0, 3);
        uiReplyCustomTextBox.Name = "uiReplyCustomTextBox";
        uiReplyCustomTextBox.PlaceholderText = "{name} — подставится имя автора";
        uiReplyCustomTextBox.Size = new Size(210, 23);
        uiToolTip.SetToolTip(uiReplyCustomTextBox,
            "Доступен при варианте «Свой вариант». {name} заменяется именем автора;"
            + Environment.NewLine
            + "у автора без имени шаблон с {name} не вставляется.");
        //
        // uiButtonsRow
        //
        uiButtonsRow.ColumnCount = 2;
        uiButtonsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiButtonsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiButtonsRow.Controls.Add(uiResetButton, 0, 0);
        uiButtonsRow.Controls.Add(uiButtonsPanel, 1, 0);
        uiButtonsRow.Dock = DockStyle.Fill;
        uiButtonsRow.Margin = new Padding(0, 12, 0, 0);
        uiButtonsRow.Name = "uiButtonsRow";
        uiButtonsRow.RowCount = 1;
        uiButtonsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        //
        // uiResetButton
        //
        uiResetButton.Anchor = AnchorStyles.Left;
        uiResetButton.AutoSize = true;
        uiResetButton.Margin = new Padding(0);
        uiResetButton.MinimumSize = new Size(90, 27);
        uiResetButton.Name = "uiResetButton";
        uiResetButton.Text = "Сбросить";
        uiResetButton.UseVisualStyleBackColor = true;
        uiResetButton.Click += uiResetButton_Click;
        uiToolTip.SetToolTip(uiResetButton, "Вернуть все значения по умолчанию.");
        //
        // uiButtonsPanel
        //
        uiButtonsPanel.AutoSize = true;
        uiButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiButtonsPanel.Controls.Add(uiCancelButton);
        uiButtonsPanel.Controls.Add(uiSaveButton);
        uiButtonsPanel.Dock = DockStyle.Right;
        uiButtonsPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonsPanel.Margin = new Padding(0);
        uiButtonsPanel.Name = "uiButtonsPanel";
        uiButtonsPanel.WrapContents = false;
        //
        // uiSaveButton
        //
        uiSaveButton.AutoSize = true;
        uiSaveButton.DialogResult = DialogResult.OK;
        uiSaveButton.Margin = new Padding(0, 0, 8, 0);
        uiSaveButton.MinimumSize = new Size(90, 27);
        uiSaveButton.Name = "uiSaveButton";
        uiSaveButton.Text = "Сохранить";
        uiSaveButton.UseVisualStyleBackColor = true;
        //
        // uiCancelButton
        //
        uiCancelButton.AutoSize = true;
        uiCancelButton.DialogResult = DialogResult.Cancel;
        uiCancelButton.Margin = new Padding(0);
        uiCancelButton.MinimumSize = new Size(90, 27);
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Text = "Отмена";
        uiCancelButton.UseVisualStyleBackColor = true;
        //
        // CommentsAppearanceDialog
        //
        AcceptButton = uiSaveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(360, 440);
        Controls.Add(uiLayout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CommentsAppearanceDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки комментариев";
        uiLayout.ResumeLayout(false);
        uiLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiBaseNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiCommentNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiAuthorNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiMetaNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiMediaTitleNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiBadgeNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiLineHeightNumeric).EndInit();
        uiButtonsRow.ResumeLayout(false);
        uiButtonsRow.PerformLayout();
        uiButtonsPanel.ResumeLayout(false);
        uiButtonsPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
