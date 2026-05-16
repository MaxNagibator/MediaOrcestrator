namespace MediaOrcestrator.Runner;

partial class TaskErrorForm
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
    private TextBox uiErrorTextBox;
    private Button uiCopyButton;
    private Button uiOpenLogButton;
    private Button uiCloseButton;

    private void InitializeComponent()
    {
        uiTitleLabel = new Label();
        uiErrorTextBox = new TextBox();
        uiCopyButton = new Button();
        uiOpenLogButton = new Button();
        uiCloseButton = new Button();
        SuspendLayout();
        //
        // uiTitleLabel
        //
        uiTitleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiTitleLabel.AutoEllipsis = true;
        uiTitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        uiTitleLabel.Location = new Point(14, 14);
        uiTitleLabel.Name = "uiTitleLabel";
        uiTitleLabel.Size = new Size(684, 24);
        uiTitleLabel.TabIndex = 0;
        uiTitleLabel.Text = "Задача завершилась с ошибкой";
        //
        // uiErrorTextBox
        //
        uiErrorTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        uiErrorTextBox.Font = new Font("Consolas", 9F);
        uiErrorTextBox.Location = new Point(14, 46);
        uiErrorTextBox.Multiline = true;
        uiErrorTextBox.Name = "uiErrorTextBox";
        uiErrorTextBox.ReadOnly = true;
        uiErrorTextBox.ScrollBars = ScrollBars.Both;
        uiErrorTextBox.Size = new Size(684, 427);
        uiErrorTextBox.TabIndex = 1;
        uiErrorTextBox.WordWrap = false;
        //
        // uiCopyButton
        //
        uiCopyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        uiCopyButton.Location = new Point(14, 527);
        uiCopyButton.Name = "uiCopyButton";
        uiCopyButton.Size = new Size(160, 28);
        uiCopyButton.TabIndex = 2;
        uiCopyButton.Text = "Копировать";
        uiCopyButton.UseVisualStyleBackColor = true;
        uiCopyButton.Click += uiCopyButton_Click;
        //
        // uiOpenLogButton
        //
        uiOpenLogButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        uiOpenLogButton.Location = new Point(184, 527);
        uiOpenLogButton.Name = "uiOpenLogButton";
        uiOpenLogButton.Size = new Size(160, 28);
        uiOpenLogButton.TabIndex = 3;
        uiOpenLogButton.Text = "Открыть лог";
        uiOpenLogButton.UseVisualStyleBackColor = true;
        uiOpenLogButton.Click += uiOpenLogButton_Click;
        //
        // uiCloseButton
        //
        uiCloseButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        uiCloseButton.DialogResult = DialogResult.Cancel;
        uiCloseButton.Location = new Point(610, 527);
        uiCloseButton.Name = "uiCloseButton";
        uiCloseButton.Size = new Size(88, 28);
        uiCloseButton.TabIndex = 4;
        uiCloseButton.Text = "Закрыть";
        uiCloseButton.UseVisualStyleBackColor = true;
        uiCloseButton.Click += uiCloseButton_Click;
        //
        // TaskErrorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = uiCloseButton;
        ClientSize = new Size(712, 568);
        Controls.Add(uiTitleLabel);
        Controls.Add(uiErrorTextBox);
        Controls.Add(uiCopyButton);
        Controls.Add(uiOpenLogButton);
        Controls.Add(uiCloseButton);
        MinimumSize = new Size(520, 400);
        Name = "TaskErrorForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Ошибка задачи";
        Load += TaskErrorForm_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
