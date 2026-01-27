namespace MediaOrcestrator.Runner;

partial class SourceSettingsForm
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
        panel1 = new Panel();
        uiNameTextBox = new TextBox();
        label1 = new Label();
        button1 = new Button();
        SuspendLayout();
        // 
        // panel1
        // 
        panel1.Location = new Point(94, 114);
        panel1.Name = "panel1";
        panel1.Size = new Size(649, 274);
        panel1.TabIndex = 0;
        // 
        // textBox1
        // 
        uiNameTextBox.Location = new Point(151, 65);
        uiNameTextBox.Name = "textBox1";
        uiNameTextBox.Size = new Size(100, 23);
        uiNameTextBox.TabIndex = 1;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(89, 49);
        label1.Name = "label1";
        label1.Size = new Size(38, 15);
        label1.TabIndex = 2;
        label1.Text = "label1";
        // 
        // button1
        // 
        button1.Location = new Point(570, 417);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 3;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // SourceSettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(button1);
        Controls.Add(label1);
        Controls.Add(uiNameTextBox);
        Controls.Add(panel1);
        Name = "SourceSettingsForm";
        Text = "SourceSettingsForm";
        Load += SourceSettingsForm_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Panel panel1;
    private TextBox uiNameTextBox;
    private Label label1;
    private Button button1;
}
