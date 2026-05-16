namespace MediaOrcestrator.Runner;

partial class ActionUserControl
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
        if (disposing)
        {
            if (_action != null)
            {
                _action.Changed -= OnActionChanged;
                _action = null;
            }

            components?.Dispose();
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
        uiAccentStrip = new Panel();
        uiLayout = new TableLayoutPanel();
        uiTitlePanel = new TableLayoutPanel();
        uiNameLabel = new Label();
        uiSubtitleLabel = new Label();
        uiStatusLabel = new Label();
        uiProgressBar = new ProgressBar();
        uiProgressLabel = new Label();
        uiCancelButton = new Button();
        uiActionToolTip = new ToolTip(components);
        uiLayout.SuspendLayout();
        uiTitlePanel.SuspendLayout();
        SuspendLayout();
        //
        // uiAccentStrip
        //
        uiAccentStrip.BackColor = Color.Silver;
        uiAccentStrip.Dock = DockStyle.Left;
        uiAccentStrip.Location = new Point(0, 0);
        uiAccentStrip.Margin = new Padding(0);
        uiAccentStrip.Name = "uiAccentStrip";
        uiAccentStrip.Size = new Size(4, 42);
        uiAccentStrip.TabIndex = 1;
        //
        // uiLayout
        //
        uiLayout.ColumnCount = 5;
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiLayout.Controls.Add(uiTitlePanel, 0, 0);
        uiLayout.Controls.Add(uiStatusLabel, 1, 0);
        uiLayout.Controls.Add(uiProgressBar, 2, 0);
        uiLayout.Controls.Add(uiProgressLabel, 3, 0);
        uiLayout.Controls.Add(uiCancelButton, 4, 0);
        uiLayout.Dock = DockStyle.Fill;
        uiLayout.Location = new Point(0, 0);
        uiLayout.Name = "uiLayout";
        uiLayout.Padding = new Padding(8, 6, 8, 6);
        uiLayout.RowCount = 1;
        uiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiLayout.Size = new Size(663, 34);
        uiLayout.TabIndex = 0;
        //
        // uiTitlePanel
        //
        uiTitlePanel.Anchor = AnchorStyles.Left;
        uiTitlePanel.AutoSize = true;
        uiTitlePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiTitlePanel.ColumnCount = 1;
        uiTitlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiTitlePanel.Controls.Add(uiNameLabel, 0, 0);
        uiTitlePanel.Controls.Add(uiSubtitleLabel, 0, 1);
        uiTitlePanel.Margin = new Padding(0, 0, 8, 0);
        uiTitlePanel.Name = "uiTitlePanel";
        uiTitlePanel.RowCount = 2;
        uiTitlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiTitlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiTitlePanel.Size = new Size(80, 30);
        uiTitlePanel.TabIndex = 0;
        //
        // uiNameLabel
        //
        uiNameLabel.Anchor = AnchorStyles.Left;
        uiNameLabel.AutoSize = true;
        uiNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        uiNameLabel.Margin = new Padding(0);
        uiNameLabel.Name = "uiNameLabel";
        uiNameLabel.Size = new Size(80, 15);
        uiNameLabel.TabIndex = 0;
        uiNameLabel.Text = "Имя задачи";
        //
        // uiSubtitleLabel
        //
        uiSubtitleLabel.Anchor = AnchorStyles.Left;
        uiSubtitleLabel.AutoEllipsis = true;
        uiSubtitleLabel.AutoSize = true;
        uiSubtitleLabel.ForeColor = SystemColors.GrayText;
        uiSubtitleLabel.Margin = new Padding(0, 1, 0, 0);
        uiSubtitleLabel.Name = "uiSubtitleLabel";
        uiSubtitleLabel.Size = new Size(0, 15);
        uiSubtitleLabel.TabIndex = 1;
        uiSubtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiStatusLabel
        //
        uiStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        uiStatusLabel.AutoEllipsis = true;
        uiStatusLabel.ForeColor = SystemColors.GrayText;
        uiStatusLabel.Margin = new Padding(0, 0, 8, 0);
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Size = new Size(100, 26);
        uiStatusLabel.TabIndex = 1;
        uiStatusLabel.Text = "Статус";
        uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiProgressBar
        //
        uiProgressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        uiProgressBar.Margin = new Padding(0, 0, 8, 0);
        uiProgressBar.Name = "uiProgressBar";
        uiProgressBar.Size = new Size(192, 18);
        uiProgressBar.TabIndex = 2;
        //
        // uiProgressLabel
        //
        uiProgressLabel.Anchor = AnchorStyles.Left;
        uiProgressLabel.AutoSize = true;
        uiProgressLabel.ForeColor = SystemColors.GrayText;
        uiProgressLabel.Margin = new Padding(0, 0, 8, 0);
        uiProgressLabel.Name = "uiProgressLabel";
        uiProgressLabel.Size = new Size(66, 15);
        uiProgressLabel.TabIndex = 3;
        uiProgressLabel.Text = "0 / 0";
        uiProgressLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiCancelButton
        //
        uiCancelButton.Anchor = AnchorStyles.Right;
        uiCancelButton.AutoSize = true;
        uiCancelButton.Margin = new Padding(0);
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Padding = new Padding(10, 4, 10, 4);
        uiCancelButton.Size = new Size(82, 30);
        uiCancelButton.TabIndex = 4;
        uiCancelButton.Text = "Отмена";
        uiActionToolTip.SetToolTip(uiCancelButton, "Отменить задачу");
        uiCancelButton.UseVisualStyleBackColor = true;
        uiCancelButton.Click += uiCancelButton_Click;
        //
        // ActionUserControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Window;
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(uiLayout);
        Controls.Add(uiAccentStrip);
        Margin = new Padding(0, 0, 0, 6);
        MinimumSize = new Size(0, 42);
        Name = "ActionUserControl";
        Size = new Size(663, 42);
        uiTitlePanel.ResumeLayout(false);
        uiTitlePanel.PerformLayout();
        uiLayout.ResumeLayout(false);
        uiLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel uiAccentStrip;
    private TableLayoutPanel uiLayout;
    private TableLayoutPanel uiTitlePanel;
    private Label uiNameLabel;
    private Label uiSubtitleLabel;
    private Label uiStatusLabel;
    private ProgressBar uiProgressBar;
    private Label uiProgressLabel;
    private Button uiCancelButton;
    private ToolTip uiActionToolTip;
}
