namespace MediaOrcestrator.Runner;

partial class UpdateProgressForm
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
        uiStatusLabel = new Label();
        uiProgressBar = new ProgressBar();
        uiCancelButton = new Button();
        SuspendLayout();
        //
        // uiStatusLabel
        //
        uiStatusLabel.Location = new Point(12, 15);
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Size = new Size(380, 20);
        uiStatusLabel.TabIndex = 0;
        uiStatusLabel.Text = "Скачивание обновления...";
        //
        // uiProgressBar
        //
        uiProgressBar.Location = new Point(12, 40);
        uiProgressBar.Maximum = 100;
        uiProgressBar.Name = "uiProgressBar";
        uiProgressBar.Size = new Size(380, 25);
        uiProgressBar.TabIndex = 1;
        //
        // uiCancelButton
        //
        uiCancelButton.Location = new Point(317, 75);
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Size = new Size(75, 25);
        uiCancelButton.TabIndex = 2;
        uiCancelButton.Text = "Отмена";
        uiCancelButton.UseVisualStyleBackColor = true;
        uiCancelButton.Click += uiCancelButton_Click;
        //
        // UpdateProgressForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(404, 111);
        Controls.Add(uiCancelButton);
        Controls.Add(uiProgressBar);
        Controls.Add(uiStatusLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "UpdateProgressForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Обновление приложения";
        ResumeLayout(false);
    }

    #endregion

    private Label uiStatusLabel;
    private ProgressBar uiProgressBar;
    private Button uiCancelButton;
}
