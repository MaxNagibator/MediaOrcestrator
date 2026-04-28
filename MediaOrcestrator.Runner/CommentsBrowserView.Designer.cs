namespace MediaOrcestrator.Runner;

partial class CommentsBrowserView
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private WebBrowser uiWebBrowser;

    private void InitializeComponent()
    {
        uiWebBrowser = new WebBrowser();
        SuspendLayout();
        //
        // uiWebBrowser
        //
        uiWebBrowser.AllowWebBrowserDrop = false;
        uiWebBrowser.Dock = DockStyle.Fill;
        uiWebBrowser.IsWebBrowserContextMenuEnabled = false;
        uiWebBrowser.Location = new Point(0, 0);
        uiWebBrowser.MinimumSize = new Size(20, 20);
        uiWebBrowser.Name = "uiWebBrowser";
        uiWebBrowser.ScriptErrorsSuppressed = true;
        uiWebBrowser.Size = new Size(1100, 600);
        uiWebBrowser.TabIndex = 0;
        uiWebBrowser.WebBrowserShortcutsEnabled = true;
        uiWebBrowser.Navigating += uiWebBrowser_Navigating;
        //
        // CommentsBrowserView
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiWebBrowser);
        Name = "CommentsBrowserView";
        Size = new Size(1100, 600);
        ResumeLayout(false);
    }

    #endregion
}
