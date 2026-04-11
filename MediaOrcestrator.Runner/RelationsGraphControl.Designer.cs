using Microsoft.Msagl.GraphViewerGdi;

namespace MediaOrcestrator.Runner;

partial class RelationsGraphControl
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
            _edgeMenu?.Dispose();
            _nodeMenu?.Dispose();
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
        uiViewer = new GViewer();
        uiEmptyStateLabel = new Label();
        uiToolStrip = new ToolStrip();
        uiCreateRelationButton = new ToolStripButton();
        uiRebuildButton = new ToolStripButton();
        uiToolSeparator1 = new ToolStripSeparator();
        uiStatusLabel = new ToolStripLabel();
        uiToolSeparator2 = new ToolStripSeparator();
        uiReaderLegend = new ToolStripLabel();
        uiWriterLegend = new ToolStripLabel();
        uiTransitLegend = new ToolStripLabel();
        uiHelpButton = new ToolStripButton();
        uiToolStrip.SuspendLayout();
        SuspendLayout();
        //
        // uiViewer
        //
        uiViewer.Dock = DockStyle.Fill;
        uiViewer.LayoutEditingEnabled = false;
        uiViewer.Location = new Point(0, 25);
        uiViewer.Name = "uiViewer";
        uiViewer.OutsideAreaBrush = Brushes.White;
        uiViewer.Size = new Size(600, 375);
        uiViewer.TabIndex = 0;
        uiViewer.ToolBarIsVisible = true;
        //
        // uiEmptyStateLabel
        //
        uiEmptyStateLabel.Dock = DockStyle.Fill;
        uiEmptyStateLabel.Font = new Font("Segoe UI", 10F);
        uiEmptyStateLabel.ForeColor = SystemColors.GrayText;
        uiEmptyStateLabel.Location = new Point(0, 25);
        uiEmptyStateLabel.Name = "uiEmptyStateLabel";
        uiEmptyStateLabel.Size = new Size(600, 375);
        uiEmptyStateLabel.TabIndex = 1;
        uiEmptyStateLabel.Text = "Нет связей. Кликните правой кнопкой по узлу или нажмите «Создать связь».";
        uiEmptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        uiEmptyStateLabel.Visible = false;
        //
        // uiToolStrip
        //
        uiToolStrip.Dock = DockStyle.Top;
        uiToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        uiToolStrip.Items.AddRange(new ToolStripItem[]
        {
            uiCreateRelationButton,
            uiRebuildButton,
            uiToolSeparator1,
            uiStatusLabel,
            uiToolSeparator2,
            uiReaderLegend,
            uiWriterLegend,
            uiTransitLegend,
            uiHelpButton,
        });
        uiToolStrip.Location = new Point(0, 0);
        uiToolStrip.Name = "uiToolStrip";
        uiToolStrip.RenderMode = ToolStripRenderMode.System;
        uiToolStrip.Size = new Size(600, 25);
        uiToolStrip.TabIndex = 2;
        //
        // uiCreateRelationButton
        //
        uiCreateRelationButton.Name = "uiCreateRelationButton";
        uiCreateRelationButton.Text = "Создать связь";
        uiCreateRelationButton.Click += OnCreateRelationButtonClick;
        //
        // uiRebuildButton
        //
        uiRebuildButton.Name = "uiRebuildButton";
        uiRebuildButton.Text = "Перестроить";
        uiRebuildButton.ToolTipText = "Перечитать связи из базы и пересобрать раскладку графа";
        uiRebuildButton.Click += OnRebuildButtonClick;
        //
        // uiToolSeparator1
        //
        uiToolSeparator1.Name = "uiToolSeparator1";
        //
        // uiStatusLabel
        //
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Text = "";
        //
        // uiToolSeparator2
        //
        uiToolSeparator2.Name = "uiToolSeparator2";
        //
        // uiReaderLegend
        //
        uiReaderLegend.ImageAlign = ContentAlignment.MiddleLeft;
        uiReaderLegend.Name = "uiReaderLegend";
        uiReaderLegend.Text = "Источник";
        //
        // uiWriterLegend
        //
        uiWriterLegend.ImageAlign = ContentAlignment.MiddleLeft;
        uiWriterLegend.Name = "uiWriterLegend";
        uiWriterLegend.Text = "Цель";
        //
        // uiTransitLegend
        //
        uiTransitLegend.ImageAlign = ContentAlignment.MiddleLeft;
        uiTransitLegend.Name = "uiTransitLegend";
        uiTransitLegend.Text = "Транзит";
        //
        // uiHelpButton
        //
        uiHelpButton.Alignment = ToolStripItemAlignment.Right;
        uiHelpButton.Name = "uiHelpButton";
        uiHelpButton.Text = "?";
        uiHelpButton.ToolTipText = "Справка по работе с графом";
        uiHelpButton.Click += OnHelpButtonClick;
        //
        // RelationsGraphControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiViewer);
        Controls.Add(uiEmptyStateLabel);
        Controls.Add(uiToolStrip);
        Name = "RelationsGraphControl";
        Size = new Size(600, 400);
        uiToolStrip.ResumeLayout(false);
        uiToolStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GViewer uiViewer;
    private Label uiEmptyStateLabel;
    private ToolStrip uiToolStrip;
    private ToolStripButton uiCreateRelationButton;
    private ToolStripButton uiRebuildButton;
    private ToolStripSeparator uiToolSeparator1;
    private ToolStripLabel uiStatusLabel;
    private ToolStripSeparator uiToolSeparator2;
    private ToolStripLabel uiReaderLegend;
    private ToolStripLabel uiWriterLegend;
    private ToolStripLabel uiTransitLegend;
    private ToolStripButton uiHelpButton;
}
