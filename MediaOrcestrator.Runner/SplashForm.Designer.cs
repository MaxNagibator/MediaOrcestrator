namespace MediaOrcestrator.Runner;

partial class SplashForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code
    private TableLayoutPanel uiRootLayout;
    private Label uiTitleLabel;
    private Label uiVersionLabel;
    private Label uiStatusLabel;
    private ProgressBar uiProgressBar;

    private void InitializeComponent()
    {
        uiRootLayout = new TableLayoutPanel();
        uiTitleLabel = new Label();
        uiVersionLabel = new Label();
        uiStatusLabel = new Label();
        uiProgressBar = new ProgressBar();
        uiRootLayout.SuspendLayout();
        SuspendLayout();
        //
        // uiRootLayout
        //
        uiRootLayout.ColumnCount = 1;
        uiRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiRootLayout.Controls.Add(uiTitleLabel, 0, 0);
        uiRootLayout.Controls.Add(uiVersionLabel, 0, 1);
        uiRootLayout.Controls.Add(uiStatusLabel, 0, 2);
        uiRootLayout.Controls.Add(uiProgressBar, 0, 3);
        uiRootLayout.Dock = DockStyle.Fill;
        uiRootLayout.Location = new Point(0, 0);
        uiRootLayout.Name = "uiRootLayout";
        uiRootLayout.Padding = new Padding(24);
        uiRootLayout.RowCount = 4;
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiRootLayout.Size = new Size(480, 190);
        uiRootLayout.TabIndex = 0;
        //
        // uiTitleLabel
        //
        uiTitleLabel.AutoSize = true;
        uiTitleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        uiTitleLabel.Location = new Point(27, 24);
        uiTitleLabel.Margin = new Padding(3, 0, 3, 4);
        uiTitleLabel.Name = "uiTitleLabel";
        uiTitleLabel.Size = new Size(220, 30);
        uiTitleLabel.TabIndex = 0;
        uiTitleLabel.Text = "Медиа оркестратор";
        //
        // uiVersionLabel
        //
        uiVersionLabel.AutoSize = true;
        uiVersionLabel.ForeColor = SystemColors.GrayText;
        uiVersionLabel.Location = new Point(27, 58);
        uiVersionLabel.Margin = new Padding(3, 0, 3, 12);
        uiVersionLabel.Name = "uiVersionLabel";
        uiVersionLabel.Size = new Size(60, 15);
        uiVersionLabel.TabIndex = 1;
        uiVersionLabel.Text = "Версия";
        //
        // uiStatusLabel
        //
        uiStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiStatusLabel.AutoEllipsis = true;
        uiStatusLabel.Location = new Point(27, 85);
        uiStatusLabel.Margin = new Padding(3, 0, 3, 8);
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Size = new Size(426, 40);
        uiStatusLabel.TabIndex = 2;
        uiStatusLabel.Text = "Запуск...";
        //
        // uiProgressBar
        //
        uiProgressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        uiProgressBar.Location = new Point(27, 144);
        uiProgressBar.Maximum = 100;
        uiProgressBar.Name = "uiProgressBar";
        uiProgressBar.Size = new Size(426, 18);
        uiProgressBar.Style = ProgressBarStyle.Continuous;
        uiProgressBar.TabIndex = 3;
        //
        // SplashForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Window;
        ClientSize = new Size(480, 190);
        ControlBox = false;
        Controls.Add(uiRootLayout);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SplashForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Медиа оркестратор";
        TopMost = true;
        uiRootLayout.ResumeLayout(false);
        uiRootLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
