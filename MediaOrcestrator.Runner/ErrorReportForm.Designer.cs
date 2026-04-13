namespace MediaOrcestrator.Runner;

partial class ErrorReportForm
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

    #region Windows Form Designer generated code
    private Label uiTitleLabel;
    private Label uiSummaryLabel;
    private Label uiHintLabel;
    private TextBox uiDetailsTextBox;
    private Button uiOpenIssueButton;
    private Button uiCopyButton;
    private Button uiCloseButton;

    private void InitializeComponent()
    {
        uiTitleLabel = new Label();
        uiSummaryLabel = new Label();
        uiHintLabel = new Label();
        uiDetailsTextBox = new TextBox();
        uiOpenIssueButton = new Button();
        uiCopyButton = new Button();
        uiCloseButton = new Button();
        SuspendLayout();
        // 
        // uiTitleLabel
        // 
        uiTitleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiTitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        uiTitleLabel.Location = new Point(14, 14);
        uiTitleLabel.Name = "uiTitleLabel";
        uiTitleLabel.Size = new Size(684, 24);
        uiTitleLabel.TabIndex = 0;
        uiTitleLabel.Text = "Сообщить о проблеме";
        // 
        // uiSummaryLabel
        // 
        uiSummaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiSummaryLabel.Location = new Point(14, 42);
        uiSummaryLabel.Name = "uiSummaryLabel";
        uiSummaryLabel.Size = new Size(684, 40);
        uiSummaryLabel.TabIndex = 1;
        // 
        // uiHintLabel
        // 
        uiHintLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        uiHintLabel.Location = new Point(14, 483);
        uiHintLabel.Name = "uiHintLabel";
        uiHintLabel.Size = new Size(684, 36);
        uiHintLabel.TabIndex = 3;
        uiHintLabel.Text = "«Открыть issue»: откроется GitHub с предзаполненными метаданными и стектрейсом, а лог сессии скопируется в буфер обмена — вставьте его в указанное место (Ctrl+V).";
        // 
        // uiDetailsTextBox
        // 
        uiDetailsTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        uiDetailsTextBox.Font = new Font("Consolas", 9F);
        uiDetailsTextBox.Location = new Point(14, 90);
        uiDetailsTextBox.Multiline = true;
        uiDetailsTextBox.Name = "uiDetailsTextBox";
        uiDetailsTextBox.ReadOnly = true;
        uiDetailsTextBox.ScrollBars = ScrollBars.Both;
        uiDetailsTextBox.Size = new Size(684, 383);
        uiDetailsTextBox.TabIndex = 2;
        uiDetailsTextBox.WordWrap = false;
        // 
        // uiOpenIssueButton
        // 
        uiOpenIssueButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        uiOpenIssueButton.Location = new Point(14, 527);
        uiOpenIssueButton.Name = "uiOpenIssueButton";
        uiOpenIssueButton.Size = new Size(200, 28);
        uiOpenIssueButton.TabIndex = 4;
        uiOpenIssueButton.Text = "Открыть issue на GitHub";
        uiOpenIssueButton.UseVisualStyleBackColor = true;
        uiOpenIssueButton.Click += uiOpenIssueButton_Click;
        // 
        // uiCopyButton
        // 
        uiCopyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        uiCopyButton.Location = new Point(224, 527);
        uiCopyButton.Name = "uiCopyButton";
        uiCopyButton.Size = new Size(160, 28);
        uiCopyButton.TabIndex = 5;
        uiCopyButton.Text = "Скопировать детали";
        uiCopyButton.UseVisualStyleBackColor = true;
        uiCopyButton.Click += uiCopyButton_Click;
        // 
        // uiCloseButton
        // 
        uiCloseButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        uiCloseButton.DialogResult = DialogResult.Cancel;
        uiCloseButton.Location = new Point(610, 527);
        uiCloseButton.Name = "uiCloseButton";
        uiCloseButton.Size = new Size(88, 28);
        uiCloseButton.TabIndex = 6;
        uiCloseButton.Text = "Закрыть";
        uiCloseButton.UseVisualStyleBackColor = true;
        uiCloseButton.Click += uiCloseButton_Click;
        // 
        // ErrorReportForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCloseButton;
        ClientSize = new Size(712, 568);
        Controls.Add(uiTitleLabel);
        Controls.Add(uiSummaryLabel);
        Controls.Add(uiDetailsTextBox);
        Controls.Add(uiHintLabel);
        Controls.Add(uiOpenIssueButton);
        Controls.Add(uiCopyButton);
        Controls.Add(uiCloseButton);
        MinimumSize = new Size(520, 400);
        Name = "ErrorReportForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Сообщить о проблеме";
        Load += ErrorReportForm_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
