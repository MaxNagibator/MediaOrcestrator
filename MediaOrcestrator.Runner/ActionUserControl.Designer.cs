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
        if (disposing && (components != null))
        {
            components.Dispose();
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
        label1 = new Label();
        button1 = new Button();
        progressBar1 = new ProgressBar();
        label2 = new Label();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(6, 7);
        label1.Name = "label1";
        label1.Size = new Size(38, 15);
        label1.TabIndex = 0;
        label1.Text = "label1";
        // 
        // button1
        // 
        button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        button1.Location = new Point(585, 3);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 1;
        button1.Text = "отмена";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // progressBar1
        // 
        progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        progressBar1.Location = new Point(448, 3);
        progressBar1.Name = "progressBar1";
        progressBar1.Size = new Size(131, 23);
        progressBar1.TabIndex = 2;
        // 
        // label2
        // 
        label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        label2.AutoSize = true;
        label2.Location = new Point(483, 7);
        label2.Name = "label2";
        label2.Size = new Size(66, 15);
        label2.TabIndex = 3;
        label2.Text = "100999/999";
        // 
        // ActionUserControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(label2);
        Controls.Add(progressBar1);
        Controls.Add(button1);
        Controls.Add(label1);
        Name = "ActionUserControl";
        Size = new Size(663, 30);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Button button1;
    private ProgressBar progressBar1;
    private Label label2;
}
