namespace MediaOrcestrator.Runner;

partial class CompletedActionRow
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
        uiLabel = new Label();
        uiDetailsButton = new Button();
        uiRowToolTip = new ToolTip(components);
        SuspendLayout();
        //
        // uiLabel
        //
        uiLabel.AutoEllipsis = true;
        uiLabel.Dock = DockStyle.Fill;
        uiLabel.Name = "uiLabel";
        uiLabel.Padding = new Padding(8, 0, 0, 0);
        uiLabel.Size = new Size(575, 26);
        uiLabel.TabIndex = 0;
        uiLabel.Text = "Завершённая задача";
        uiLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiAccentStrip
        //
        uiAccentStrip.BackColor = Color.Silver;
        uiAccentStrip.Dock = DockStyle.Left;
        uiAccentStrip.Margin = new Padding(0);
        uiAccentStrip.Name = "uiAccentStrip";
        uiAccentStrip.Size = new Size(3, 26);
        uiAccentStrip.TabIndex = 1;
        //
        // uiDetailsButton
        //
        uiDetailsButton.AutoSize = true;
        uiDetailsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiDetailsButton.Dock = DockStyle.Right;
        uiDetailsButton.Name = "uiDetailsButton";
        uiDetailsButton.Padding = new Padding(10, 2, 10, 2);
        uiDetailsButton.Size = new Size(85, 26);
        uiDetailsButton.TabIndex = 2;
        uiDetailsButton.Text = "Подробнее";
        uiRowToolTip.SetToolTip(uiDetailsButton, "Показать текст ошибки");
        uiDetailsButton.UseVisualStyleBackColor = true;
        uiDetailsButton.Visible = false;
        uiDetailsButton.Click += uiDetailsButton_Click;
        //
        // CompletedActionRow
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Window;
        Controls.Add(uiLabel);
        Controls.Add(uiDetailsButton);
        Controls.Add(uiAccentStrip);
        Margin = new Padding(0, 0, 0, 2);
        MinimumSize = new Size(0, 26);
        Name = "CompletedActionRow";
        Size = new Size(663, 26);
        ResumeLayout(false);
    }

    #endregion

    private Panel uiAccentStrip;
    private Label uiLabel;
    private Button uiDetailsButton;
    private ToolTip uiRowToolTip;
}
