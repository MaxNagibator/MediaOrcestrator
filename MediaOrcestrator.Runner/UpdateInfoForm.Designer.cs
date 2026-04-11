namespace MediaOrcestrator.Runner;

partial class UpdateInfoForm
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

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        uiMainLayout = new TableLayoutPanel();
        uiVersionLabel = new Label();
        uiSizeLabel = new Label();
        uiReleaseNotesLabel = new Label();
        uiReleaseNotesTextBox = new RichTextBox();
        uiButtonsPanel = new FlowLayoutPanel();
        uiLaterButton = new Button();
        uiUpdateButton = new Button();
        uiMainLayout.SuspendLayout();
        uiButtonsPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiMainLayout
        //
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiVersionLabel, 0, 0);
        uiMainLayout.Controls.Add(uiSizeLabel, 0, 1);
        uiMainLayout.Controls.Add(uiReleaseNotesLabel, 0, 2);
        uiMainLayout.Controls.Add(uiReleaseNotesTextBox, 0, 3);
        uiMainLayout.Controls.Add(uiButtonsPanel, 0, 4);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Location = new Point(0, 0);
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(12);
        uiMainLayout.RowCount = 5;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.Size = new Size(784, 561);
        uiMainLayout.TabIndex = 0;
        //
        // uiVersionLabel
        //
        uiVersionLabel.AutoSize = true;
        uiVersionLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        uiVersionLabel.Margin = new Padding(0, 0, 0, 4);
        uiVersionLabel.Name = "uiVersionLabel";
        uiVersionLabel.TabIndex = 0;
        uiVersionLabel.Text = "Доступна новая версия";
        //
        // uiSizeLabel
        //
        uiSizeLabel.AutoSize = true;
        uiSizeLabel.Margin = new Padding(0, 0, 0, 8);
        uiSizeLabel.Name = "uiSizeLabel";
        uiSizeLabel.TabIndex = 1;
        uiSizeLabel.Text = "Размер:";
        //
        // uiReleaseNotesLabel
        //
        uiReleaseNotesLabel.AutoSize = true;
        uiReleaseNotesLabel.Margin = new Padding(0, 0, 0, 2);
        uiReleaseNotesLabel.Name = "uiReleaseNotesLabel";
        uiReleaseNotesLabel.TabIndex = 2;
        uiReleaseNotesLabel.Text = "Что нового:";
        //
        // uiReleaseNotesTextBox
        //
        uiReleaseNotesTextBox.BackColor = SystemColors.Window;
        uiReleaseNotesTextBox.BorderStyle = BorderStyle.FixedSingle;
        uiReleaseNotesTextBox.Dock = DockStyle.Fill;
        uiReleaseNotesTextBox.Font = new Font("Segoe UI", 10F);
        uiReleaseNotesTextBox.Name = "uiReleaseNotesTextBox";
        uiReleaseNotesTextBox.ReadOnly = true;
        uiReleaseNotesTextBox.TabIndex = 3;
        uiReleaseNotesTextBox.Text = "";
        //
        // uiButtonsPanel
        //
        uiButtonsPanel.AutoSize = true;
        uiButtonsPanel.Controls.Add(uiLaterButton);
        uiButtonsPanel.Controls.Add(uiUpdateButton);
        uiButtonsPanel.Dock = DockStyle.Fill;
        uiButtonsPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonsPanel.Name = "uiButtonsPanel";
        uiButtonsPanel.Padding = new Padding(0, 4, 0, 0);
        uiButtonsPanel.TabIndex = 4;
        //
        // uiLaterButton
        //
        uiLaterButton.AutoSize = true;
        uiLaterButton.DialogResult = DialogResult.No;
        uiLaterButton.Name = "uiLaterButton";
        uiLaterButton.TabIndex = 0;
        uiLaterButton.Text = "Позже";
        uiLaterButton.UseVisualStyleBackColor = true;
        //
        // uiUpdateButton
        //
        uiUpdateButton.AutoSize = true;
        uiUpdateButton.DialogResult = DialogResult.Yes;
        uiUpdateButton.Name = "uiUpdateButton";
        uiUpdateButton.TabIndex = 1;
        uiUpdateButton.Text = "Обновить";
        uiUpdateButton.UseVisualStyleBackColor = true;
        //
        // UpdateInfoForm
        //
        AcceptButton = uiUpdateButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = uiLaterButton;
        ClientSize = new Size(784, 561);
        Controls.Add(uiMainLayout);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(400, 350);
        Name = "UpdateInfoForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Обновление приложения";
        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        uiButtonsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiMainLayout;
    private Label uiVersionLabel;
    private Label uiSizeLabel;
    private Label uiReleaseNotesLabel;
    private RichTextBox uiReleaseNotesTextBox;
    private FlowLayoutPanel uiButtonsPanel;
    private Button uiLaterButton;
    private Button uiUpdateButton;
}
