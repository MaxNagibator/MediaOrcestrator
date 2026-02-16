namespace MediaOrcestrator.Runner;

partial class InputDialog
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
    private Label lblPrompt;
    private TextBox txtInput;
    private Button btnOk;
    private Button btnCancel;

    private void InitializeComponent()
    {
        lblPrompt = new Label();
        txtInput = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();
        SuspendLayout();
        // 
        // lblPrompt
        // 
        lblPrompt.AutoSize = true;
        lblPrompt.Location = new Point(14, 17);
        lblPrompt.Margin = new Padding(4, 0, 4, 0);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(115, 15);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Тут текстовый текст";
        // 
        // txtInput
        // 
        txtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtInput.Location = new Point(14, 46);
        txtInput.Margin = new Padding(4, 3, 4, 3);
        txtInput.Name = "txtInput";
        txtInput.Size = new Size(482, 23);
        txtInput.TabIndex = 1;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnOk.Location = new Point(314, 81);
        btnOk.Margin = new Padding(4, 3, 4, 3);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(88, 27);
        btnOk.TabIndex = 2;
        btnOk.Text = "OK";
        btnOk.Click += btnOk_Click;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.Location = new Point(409, 81);
        btnCancel.Margin = new Padding(4, 3, 4, 3);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(88, 27);
        btnCancel.TabIndex = 3;
        btnCancel.Text = "Отмена";
        btnCancel.Click += btnCancel_Click;
        // 
        // InputDialog
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(510, 121);
        Controls.Add(lblPrompt);
        Controls.Add(txtInput);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Margin = new Padding(4, 3, 4, 3);
        Name = "InputDialog";
        StartPosition = FormStartPosition.CenterParent;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
