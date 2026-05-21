namespace MediaOrcestrator.Runner;

partial class CommentsFetchOptionsDialog
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
    private Label uiSinceTitleLabel;
    private NumericUpDown uiSinceNumeric;
    private Label uiSinceUnitLabel;
    private Label uiOnlyRecentTitleLabel;
    private NumericUpDown uiOnlyRecentNumeric;
    private Label uiOnlyRecentUnitLabel;
    private Label uiMaxParallelismTitleLabel;
    private NumericUpDown uiMaxParallelismNumeric;
    private Label uiMaxParallelismUnitLabel;
    private FlowLayoutPanel uiButtonsPanel;
    private Button uiOkButton;
    private Button uiCancelButton;
    private ToolTip uiToolTip;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        uiLayout = new TableLayoutPanel();
        uiSinceTitleLabel = new Label();
        uiSinceNumeric = new NumericUpDown();
        uiSinceUnitLabel = new Label();
        uiOnlyRecentTitleLabel = new Label();
        uiOnlyRecentNumeric = new NumericUpDown();
        uiOnlyRecentUnitLabel = new Label();
        uiMaxParallelismTitleLabel = new Label();
        uiMaxParallelismNumeric = new NumericUpDown();
        uiMaxParallelismUnitLabel = new Label();
        uiButtonsPanel = new FlowLayoutPanel();
        uiOkButton = new Button();
        uiCancelButton = new Button();
        uiToolTip = new ToolTip(components);
        uiLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiSinceNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiOnlyRecentNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)uiMaxParallelismNumeric).BeginInit();
        uiButtonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiLayout
        //
        uiLayout.ColumnCount = 3;
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiLayout.Controls.Add(uiSinceTitleLabel, 0, 0);
        uiLayout.Controls.Add(uiSinceNumeric, 1, 0);
        uiLayout.Controls.Add(uiSinceUnitLabel, 2, 0);
        uiLayout.Controls.Add(uiOnlyRecentTitleLabel, 0, 1);
        uiLayout.Controls.Add(uiOnlyRecentNumeric, 1, 1);
        uiLayout.Controls.Add(uiOnlyRecentUnitLabel, 2, 1);
        uiLayout.Controls.Add(uiMaxParallelismTitleLabel, 0, 2);
        uiLayout.Controls.Add(uiMaxParallelismNumeric, 1, 2);
        uiLayout.Controls.Add(uiMaxParallelismUnitLabel, 2, 2);
        uiLayout.Controls.Add(uiButtonsPanel, 0, 3);
        uiLayout.SetColumnSpan(uiButtonsPanel, 3);
        uiLayout.Dock = DockStyle.Fill;
        uiLayout.Name = "uiLayout";
        uiLayout.Padding = new Padding(12);
        uiLayout.RowCount = 4;
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.RowStyles.Add(new RowStyle());
        //
        // uiSinceTitleLabel
        //
        uiSinceTitleLabel.AutoSize = true;
        uiSinceTitleLabel.Margin = new Padding(0, 7, 8, 0);
        uiSinceTitleLabel.Name = "uiSinceTitleLabel";
        uiSinceTitleLabel.Text = "Медиа не старше";
        //
        // uiSinceNumeric
        //
        uiSinceNumeric.Margin = new Padding(0, 3, 8, 3);
        uiSinceNumeric.Maximum = new decimal(new int[] { 3650, 0, 0, 0 });
        uiSinceNumeric.Name = "uiSinceNumeric";
        uiSinceNumeric.Size = new Size(80, 23);
        uiToolTip.SetToolTip(uiSinceNumeric,
            "Загружать комментарии только для медиа, опубликованных не позже N дней назад. 0 = без фильтра."
            + Environment.NewLine
            + "Использует CreationDate из метаданных. Медиа без этого поля будут пропущены.");
        //
        // uiSinceUnitLabel
        //
        uiSinceUnitLabel.AutoSize = true;
        uiSinceUnitLabel.Margin = new Padding(0, 7, 0, 0);
        uiSinceUnitLabel.Name = "uiSinceUnitLabel";
        uiSinceUnitLabel.Text = "дн.";
        //
        // uiOnlyRecentTitleLabel
        //
        uiOnlyRecentTitleLabel.AutoSize = true;
        uiOnlyRecentTitleLabel.Margin = new Padding(0, 7, 8, 0);
        uiOnlyRecentTitleLabel.Name = "uiOnlyRecentTitleLabel";
        uiOnlyRecentTitleLabel.Text = "Только последние";
        //
        // uiOnlyRecentNumeric
        //
        uiOnlyRecentNumeric.Margin = new Padding(0, 3, 8, 3);
        uiOnlyRecentNumeric.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        uiOnlyRecentNumeric.Name = "uiOnlyRecentNumeric";
        uiOnlyRecentNumeric.Size = new Size(80, 23);
        uiToolTip.SetToolTip(uiOnlyRecentNumeric,
            "Брать только N самых новых медиа по порядку в источнике (SortNumber). 0 = без фильтра."
            + Environment.NewLine
            + "Применяется поверх фильтра «Медиа не старше».");
        //
        // uiOnlyRecentUnitLabel
        //
        uiOnlyRecentUnitLabel.AutoSize = true;
        uiOnlyRecentUnitLabel.Margin = new Padding(0, 7, 0, 0);
        uiOnlyRecentUnitLabel.Name = "uiOnlyRecentUnitLabel";
        uiOnlyRecentUnitLabel.Text = "медиа";
        //
        // uiMaxParallelismTitleLabel
        //
        uiMaxParallelismTitleLabel.AutoSize = true;
        uiMaxParallelismTitleLabel.Margin = new Padding(0, 7, 8, 0);
        uiMaxParallelismTitleLabel.Name = "uiMaxParallelismTitleLabel";
        uiMaxParallelismTitleLabel.Text = "Параллельно";
        //
        // uiMaxParallelismNumeric
        //
        uiMaxParallelismNumeric.Margin = new Padding(0, 3, 8, 3);
        uiMaxParallelismNumeric.Minimum = new decimal(new int[] { CommentsViewSettings.MinFetchMaxParallelism, 0, 0, 0 });
        uiMaxParallelismNumeric.Maximum = new decimal(new int[] { CommentsViewSettings.MaxFetchMaxParallelism, 0, 0, 0 });
        uiMaxParallelismNumeric.Value = new decimal(new int[] { CommentsViewSettings.DefaultFetchMaxParallelism, 0, 0, 0 });
        uiMaxParallelismNumeric.Name = "uiMaxParallelismNumeric";
        uiMaxParallelismNumeric.Size = new Size(80, 23);
        uiToolTip.SetToolTip(uiMaxParallelismNumeric,
            "Сколько медиа загружать одновременно в пределах одного источника."
            + Environment.NewLine
            + "Выше — быстрее, но риск упереться в лимиты API и забить сеть."
            + Environment.NewLine
            + $"Допустимо {CommentsViewSettings.MinFetchMaxParallelism}..{CommentsViewSettings.MaxFetchMaxParallelism}, по умолчанию {CommentsViewSettings.DefaultFetchMaxParallelism}.");
        //
        // uiMaxParallelismUnitLabel
        //
        uiMaxParallelismUnitLabel.AutoSize = true;
        uiMaxParallelismUnitLabel.Margin = new Padding(0, 7, 0, 0);
        uiMaxParallelismUnitLabel.Name = "uiMaxParallelismUnitLabel";
        uiMaxParallelismUnitLabel.Text = "потоков";
        //
        // uiButtonsPanel
        //
        uiButtonsPanel.AutoSize = true;
        uiButtonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiButtonsPanel.Controls.Add(uiCancelButton);
        uiButtonsPanel.Controls.Add(uiOkButton);
        uiButtonsPanel.Dock = DockStyle.Right;
        uiButtonsPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonsPanel.Margin = new Padding(0, 12, 0, 0);
        uiButtonsPanel.Name = "uiButtonsPanel";
        uiButtonsPanel.WrapContents = false;
        //
        // uiOkButton
        //
        uiOkButton.AutoSize = true;
        uiOkButton.Margin = new Padding(0, 0, 8, 0);
        uiOkButton.MinimumSize = new Size(90, 27);
        uiOkButton.Name = "uiOkButton";
        uiOkButton.Text = "Загрузить";
        uiOkButton.UseVisualStyleBackColor = true;
        uiOkButton.Click += uiOkButton_Click;
        //
        // uiCancelButton
        //
        uiCancelButton.AutoSize = true;
        uiCancelButton.Margin = new Padding(0);
        uiCancelButton.MinimumSize = new Size(90, 27);
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Text = "Отмена";
        uiCancelButton.UseVisualStyleBackColor = true;
        uiCancelButton.Click += uiCancelButton_Click;
        //
        // CommentsFetchOptionsDialog
        //
        AcceptButton = uiOkButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCancelButton;
        ClientSize = new Size(360, 185);
        Controls.Add(uiLayout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CommentsFetchOptionsDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Параметры загрузки комментариев";
        uiLayout.ResumeLayout(false);
        uiLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)uiSinceNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiOnlyRecentNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)uiMaxParallelismNumeric).EndInit();
        uiButtonsPanel.ResumeLayout(false);
        uiButtonsPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
