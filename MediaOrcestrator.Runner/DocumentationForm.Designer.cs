namespace MediaOrcestrator.Runner;

partial class DocumentationForm
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
        uiWebBrowser = new WebBrowser();
        SuspendLayout();
        //
        // uiWebBrowser
        //
        uiWebBrowser.Dock = DockStyle.Fill;
        uiWebBrowser.IsWebBrowserContextMenuEnabled = false;
        uiWebBrowser.Location = new Point(0, 0);
        uiWebBrowser.MinimumSize = new Size(20, 20);
        uiWebBrowser.Name = "uiWebBrowser";
        uiWebBrowser.ScriptErrorsSuppressed = true;
        uiWebBrowser.Size = new Size(834, 611);
        uiWebBrowser.TabIndex = 0;
        //
        // DocumentationForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(834, 611);
        Controls.Add(uiWebBrowser);
        MinimumSize = new Size(400, 300);
        Name = "DocumentationForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Документация";
        Load += DocumentationForm_Load;
        ResumeLayout(false);
    }

    #endregion

    private WebBrowser uiWebBrowser;
}
