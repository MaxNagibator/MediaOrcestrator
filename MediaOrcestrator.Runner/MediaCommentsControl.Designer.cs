namespace MediaOrcestrator.Runner;

partial class MediaCommentsControl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _groupFont?.Dispose();
            _regularFont?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private FlowLayoutPanel uiContentFlow;

    private void InitializeComponent()
    {
        uiContentFlow = new FlowLayoutPanel();
        SuspendLayout();
        //
        // uiContentFlow
        //
        uiContentFlow.AutoScroll = true;
        uiContentFlow.Dock = DockStyle.Fill;
        uiContentFlow.FlowDirection = FlowDirection.TopDown;
        uiContentFlow.Location = new Point(0, 0);
        uiContentFlow.Name = "uiContentFlow";
        uiContentFlow.Size = new Size(800, 600);
        uiContentFlow.TabIndex = 0;
        uiContentFlow.WrapContents = false;
        uiContentFlow.Resize += uiContentFlow_Resize;
        //
        // MediaCommentsControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiContentFlow);
        Name = "MediaCommentsControl";
        Size = new Size(800, 600);
        ResumeLayout(false);
    }

    #endregion
}
