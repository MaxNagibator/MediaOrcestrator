namespace MediaOrcestrator.Runner;

partial class MediaDetailForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            uiPreviewBox.Image?.Dispose();
            _titleFont?.Dispose();
            _headerFont?.Dispose();
            _groupFont?.Dispose();
            _boldFont?.Dispose();
            _regularFont?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private PictureBox uiPreviewBox;
    private Label uiTitleLabel;
    private Label uiDescriptionLabel;
    private Panel uiHeaderPanel;
    private Label uiSeparator;
    private TabControl uiTabControl;
    private TabPage uiSourcesTab;
    private TabPage uiCommentsTab;
    private Panel uiContentPanel;
    private CommentsBrowserView uiCommentsBrowser;

    private void InitializeComponent()
    {
        uiHeaderPanel = new Panel();
        uiPreviewBox = new PictureBox();
        uiTitleLabel = new Label();
        uiDescriptionLabel = new Label();
        uiSeparator = new Label();
        uiTabControl = new TabControl();
        uiSourcesTab = new TabPage();
        uiCommentsTab = new TabPage();
        uiContentPanel = new Panel();
        uiCommentsBrowser = new CommentsBrowserView();
        uiHeaderPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)uiPreviewBox).BeginInit();
        uiTabControl.SuspendLayout();
        uiSourcesTab.SuspendLayout();
        uiCommentsTab.SuspendLayout();
        SuspendLayout();
        //
        // uiHeaderPanel
        //
        uiHeaderPanel.Controls.Add(uiPreviewBox);
        uiHeaderPanel.Controls.Add(uiTitleLabel);
        uiHeaderPanel.Controls.Add(uiDescriptionLabel);
        uiHeaderPanel.Dock = DockStyle.Top;
        uiHeaderPanel.Location = new Point(0, 0);
        uiHeaderPanel.Margin = new Padding(6, 7, 6, 7);
        uiHeaderPanel.Name = "uiHeaderPanel";
        uiHeaderPanel.Padding = new Padding(26, 30, 26, 30);
        uiHeaderPanel.Size = new Size(1108, 345);
        uiHeaderPanel.TabIndex = 3;
        uiHeaderPanel.Resize += uiHeaderPanel_Resize;
        //
        // uiPreviewBox
        //
        uiPreviewBox.BackColor = Color.FromArgb(240, 240, 240);
        uiPreviewBox.BorderStyle = BorderStyle.FixedSingle;
        uiPreviewBox.Location = new Point(26, 30);
        uiPreviewBox.Margin = new Padding(6, 7, 6, 7);
        uiPreviewBox.Name = "uiPreviewBox";
        uiPreviewBox.Size = new Size(341, 293);
        uiPreviewBox.SizeMode = PictureBoxSizeMode.Zoom;
        uiPreviewBox.TabIndex = 0;
        uiPreviewBox.TabStop = false;
        //
        // uiTitleLabel
        //
        uiTitleLabel.AutoEllipsis = true;
        uiTitleLabel.Location = new Point(394, 30);
        uiTitleLabel.Margin = new Padding(6, 0, 6, 0);
        uiTitleLabel.Name = "uiTitleLabel";
        uiTitleLabel.Size = new Size(643, 99);
        uiTitleLabel.TabIndex = 1;
        //
        // uiDescriptionLabel
        //
        uiDescriptionLabel.AutoEllipsis = true;
        uiDescriptionLabel.ForeColor = Color.FromArgb(80, 80, 80);
        uiDescriptionLabel.Location = new Point(394, 138);
        uiDescriptionLabel.Margin = new Padding(6, 0, 6, 0);
        uiDescriptionLabel.Name = "uiDescriptionLabel";
        uiDescriptionLabel.Size = new Size(643, 178);
        uiDescriptionLabel.TabIndex = 2;
        //
        // uiSeparator
        //
        uiSeparator.BackColor = Color.FromArgb(200, 200, 200);
        uiSeparator.Dock = DockStyle.Top;
        uiSeparator.Location = new Point(0, 345);
        uiSeparator.Margin = new Padding(6, 0, 6, 0);
        uiSeparator.Name = "uiSeparator";
        uiSeparator.Size = new Size(1108, 5);
        uiSeparator.TabIndex = 2;
        //
        // uiTabControl
        //
        uiTabControl.Controls.Add(uiSourcesTab);
        uiTabControl.Controls.Add(uiCommentsTab);
        uiTabControl.Dock = DockStyle.Fill;
        uiTabControl.Location = new Point(0, 350);
        uiTabControl.Margin = new Padding(6, 7, 6, 7);
        uiTabControl.Name = "uiTabControl";
        uiTabControl.SelectedIndex = 0;
        uiTabControl.Size = new Size(1108, 1083);
        uiTabControl.TabIndex = 0;
        //
        // uiSourcesTab
        //
        uiSourcesTab.Controls.Add(uiContentPanel);
        uiSourcesTab.Location = new Point(4, 46);
        uiSourcesTab.Margin = new Padding(6, 7, 6, 7);
        uiSourcesTab.Name = "uiSourcesTab";
        uiSourcesTab.Padding = new Padding(6, 7, 6, 7);
        uiSourcesTab.Size = new Size(1100, 1033);
        uiSourcesTab.TabIndex = 0;
        uiSourcesTab.Text = "Источники";
        uiSourcesTab.UseVisualStyleBackColor = true;
        //
        // uiCommentsTab
        //
        uiCommentsTab.Controls.Add(uiCommentsBrowser);
        uiCommentsTab.Location = new Point(4, 46);
        uiCommentsTab.Margin = new Padding(6, 7, 6, 7);
        uiCommentsTab.Name = "uiCommentsTab";
        uiCommentsTab.Padding = new Padding(6, 7, 6, 7);
        uiCommentsTab.Size = new Size(1100, 1033);
        uiCommentsTab.TabIndex = 1;
        uiCommentsTab.Text = "Комментарии";
        uiCommentsTab.UseVisualStyleBackColor = true;
        //
        // uiContentPanel
        //
        uiContentPanel.AutoScroll = true;
        uiContentPanel.Dock = DockStyle.Fill;
        uiContentPanel.Location = new Point(6, 7);
        uiContentPanel.Margin = new Padding(6, 7, 6, 7);
        uiContentPanel.Name = "uiContentPanel";
        uiContentPanel.Padding = new Padding(26, 10, 26, 30);
        uiContentPanel.Size = new Size(1088, 1019);
        uiContentPanel.TabIndex = 0;
        //
        // uiCommentsBrowser
        //
        uiCommentsBrowser.Dock = DockStyle.Fill;
        uiCommentsBrowser.Location = new Point(6, 7);
        uiCommentsBrowser.Margin = new Padding(6, 7, 6, 7);
        uiCommentsBrowser.Name = "uiCommentsBrowser";
        uiCommentsBrowser.Size = new Size(1088, 1019);
        uiCommentsBrowser.TabIndex = 0;
        //
        // MediaDetailForm
        //
        AutoScaleDimensions = new SizeF(15F, 37F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1108, 1433);
        Controls.Add(uiTabControl);
        Controls.Add(uiSeparator);
        Controls.Add(uiHeaderPanel);
        Margin = new Padding(6, 7, 6, 7);
        MinimumSize = new Size(825, 624);
        Name = "MediaDetailForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        uiHeaderPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)uiPreviewBox).EndInit();
        uiTabControl.ResumeLayout(false);
        uiSourcesTab.ResumeLayout(false);
        uiCommentsTab.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
