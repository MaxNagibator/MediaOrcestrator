namespace MediaOrcestrator.Runner;

partial class TasksControl
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
            if (_actionHolder != null)
            {
                _actionHolder.Changed -= OnActionsChanged;
                _actionHolder = null;
            }

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
        uiRootLayout = new TableLayoutPanel();
        uiHeaderPanel = new TableLayoutPanel();
        uiHeaderLabel = new Label();
        uiCancelAllButton = new Button();
        uiBodyPanel = new Panel();
        uiTasksFlowLayoutPanel = new DoubleBufferedFlowLayoutPanel();
        uiEmptyStateLabel = new Label();
        uiTasksToolTip = new ToolTip(components);
        uiRootLayout.SuspendLayout();
        uiHeaderPanel.SuspendLayout();
        uiBodyPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiRootLayout
        //
        uiRootLayout.ColumnCount = 1;
        uiRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiRootLayout.Controls.Add(uiHeaderPanel, 0, 0);
        uiRootLayout.Controls.Add(uiBodyPanel, 0, 1);
        uiRootLayout.Dock = DockStyle.Fill;
        uiRootLayout.Location = new Point(0, 0);
        uiRootLayout.Name = "uiRootLayout";
        uiRootLayout.RowCount = 2;
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiRootLayout.Size = new Size(800, 600);
        uiRootLayout.TabIndex = 0;
        //
        // uiHeaderPanel
        //
        uiHeaderPanel.AutoSize = true;
        uiHeaderPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiHeaderPanel.ColumnCount = 2;
        uiHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiHeaderPanel.Controls.Add(uiHeaderLabel, 0, 0);
        uiHeaderPanel.Controls.Add(uiCancelAllButton, 1, 0);
        uiHeaderPanel.Dock = DockStyle.Fill;
        uiHeaderPanel.Location = new Point(0, 0);
        uiHeaderPanel.Margin = new Padding(0);
        uiHeaderPanel.Name = "uiHeaderPanel";
        uiHeaderPanel.Padding = new Padding(8);
        uiHeaderPanel.RowCount = 1;
        uiHeaderPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiHeaderPanel.Size = new Size(800, 48);
        uiHeaderPanel.TabIndex = 0;
        //
        // uiHeaderLabel
        //
        uiHeaderLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        uiHeaderLabel.AutoEllipsis = true;
        uiHeaderLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        uiHeaderLabel.Margin = new Padding(0, 0, 8, 0);
        uiHeaderLabel.Name = "uiHeaderLabel";
        uiHeaderLabel.Size = new Size(692, 32);
        uiHeaderLabel.TabIndex = 0;
        uiHeaderLabel.Text = "Активных задач нет";
        uiHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // uiCancelAllButton
        //
        uiCancelAllButton.Anchor = AnchorStyles.Right;
        uiCancelAllButton.AutoSize = true;
        uiCancelAllButton.Enabled = false;
        uiCancelAllButton.Margin = new Padding(0);
        uiCancelAllButton.Name = "uiCancelAllButton";
        uiCancelAllButton.Padding = new Padding(10, 4, 10, 4);
        uiCancelAllButton.Size = new Size(100, 30);
        uiCancelAllButton.TabIndex = 1;
        uiCancelAllButton.Text = "Отменить все";
        uiTasksToolTip.SetToolTip(uiCancelAllButton, "Отменить все запущенные задачи");
        uiCancelAllButton.UseVisualStyleBackColor = true;
        uiCancelAllButton.Click += uiCancelAllButton_Click;
        //
        // uiBodyPanel
        //
        uiBodyPanel.Controls.Add(uiTasksFlowLayoutPanel);
        uiBodyPanel.Controls.Add(uiEmptyStateLabel);
        uiBodyPanel.Dock = DockStyle.Fill;
        uiBodyPanel.Location = new Point(0, 48);
        uiBodyPanel.Margin = new Padding(0);
        uiBodyPanel.Name = "uiBodyPanel";
        uiBodyPanel.Padding = new Padding(8, 0, 8, 8);
        uiBodyPanel.Size = new Size(800, 552);
        uiBodyPanel.TabIndex = 1;
        //
        // uiTasksFlowLayoutPanel
        //
        uiTasksFlowLayoutPanel.AutoScroll = true;
        uiTasksFlowLayoutPanel.BackColor = SystemColors.ControlLightLight;
        uiTasksFlowLayoutPanel.BorderStyle = BorderStyle.FixedSingle;
        uiTasksFlowLayoutPanel.Dock = DockStyle.Fill;
        uiTasksFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        uiTasksFlowLayoutPanel.Location = new Point(8, 0);
        uiTasksFlowLayoutPanel.Name = "uiTasksFlowLayoutPanel";
        uiTasksFlowLayoutPanel.Padding = new Padding(8);
        uiTasksFlowLayoutPanel.Size = new Size(784, 544);
        uiTasksFlowLayoutPanel.TabIndex = 0;
        uiTasksFlowLayoutPanel.Visible = false;
        uiTasksFlowLayoutPanel.WrapContents = false;
        uiTasksFlowLayoutPanel.SizeChanged += uiTasksFlowLayoutPanel_SizeChanged;
        //
        // uiEmptyStateLabel
        //
        uiEmptyStateLabel.Dock = DockStyle.Fill;
        uiEmptyStateLabel.Font = new Font("Segoe UI", 10F);
        uiEmptyStateLabel.ForeColor = SystemColors.GrayText;
        uiEmptyStateLabel.Location = new Point(8, 0);
        uiEmptyStateLabel.Name = "uiEmptyStateLabel";
        uiEmptyStateLabel.Size = new Size(784, 544);
        uiEmptyStateLabel.TabIndex = 1;
        uiEmptyStateLabel.Text = "Нет запущенных задач";
        uiEmptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // TasksControl
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiRootLayout);
        Name = "TasksControl";
        Size = new Size(800, 600);
        uiRootLayout.ResumeLayout(false);
        uiRootLayout.PerformLayout();
        uiHeaderPanel.ResumeLayout(false);
        uiHeaderPanel.PerformLayout();
        uiBodyPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiRootLayout;
    private TableLayoutPanel uiHeaderPanel;
    private Label uiHeaderLabel;
    private Button uiCancelAllButton;
    private Panel uiBodyPanel;
    private DoubleBufferedFlowLayoutPanel uiTasksFlowLayoutPanel;
    private Label uiEmptyStateLabel;
    private ToolTip uiTasksToolTip;
}
