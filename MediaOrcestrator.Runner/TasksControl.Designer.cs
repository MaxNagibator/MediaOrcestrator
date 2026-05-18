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
        uiCompletedPanel = new TableLayoutPanel();
        uiCompletedHeaderPanel = new TableLayoutPanel();
        uiCompletedHeaderButton = new Button();
        uiClearCompletedButton = new Button();
        uiCompletedFlowLayoutPanel = new DoubleBufferedFlowLayoutPanel();
        uiTasksToolTip = new ToolTip(components);
        uiAutoHideTimer = new System.Windows.Forms.Timer(components);
        uiRootLayout.SuspendLayout();
        uiHeaderPanel.SuspendLayout();
        uiBodyPanel.SuspendLayout();
        uiCompletedPanel.SuspendLayout();
        uiCompletedHeaderPanel.SuspendLayout();
        SuspendLayout();
        //
        // uiRootLayout
        //
        uiRootLayout.ColumnCount = 1;
        uiRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiRootLayout.Controls.Add(uiHeaderPanel, 0, 0);
        uiRootLayout.Controls.Add(uiBodyPanel, 0, 1);
        uiRootLayout.Controls.Add(uiCompletedPanel, 0, 2);
        uiRootLayout.Dock = DockStyle.Fill;
        uiRootLayout.Location = new Point(0, 0);
        uiRootLayout.Name = "uiRootLayout";
        uiRootLayout.RowCount = 3;
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiRootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        uiBodyPanel.Size = new Size(800, 404);
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
        uiTasksFlowLayoutPanel.Size = new Size(784, 396);
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
        uiEmptyStateLabel.Size = new Size(784, 396);
        uiEmptyStateLabel.TabIndex = 1;
        uiEmptyStateLabel.Text = "Нет запущенных задач";
        uiEmptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // uiCompletedPanel
        //
        uiCompletedPanel.AutoSize = true;
        uiCompletedPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiCompletedPanel.ColumnCount = 1;
        uiCompletedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiCompletedPanel.Controls.Add(uiCompletedHeaderPanel, 0, 0);
        uiCompletedPanel.Controls.Add(uiCompletedFlowLayoutPanel, 0, 1);
        uiCompletedPanel.Dock = DockStyle.Fill;
        uiCompletedPanel.Location = new Point(0, 452);
        uiCompletedPanel.Margin = new Padding(0);
        uiCompletedPanel.Name = "uiCompletedPanel";
        uiCompletedPanel.Padding = new Padding(8, 0, 8, 8);
        uiCompletedPanel.RowCount = 2;
        uiCompletedPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiCompletedPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiCompletedPanel.Size = new Size(800, 148);
        uiCompletedPanel.TabIndex = 2;
        uiCompletedPanel.Visible = false;
        //
        // uiCompletedHeaderPanel
        //
        uiCompletedHeaderPanel.AutoSize = true;
        uiCompletedHeaderPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        uiCompletedHeaderPanel.ColumnCount = 2;
        uiCompletedHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiCompletedHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        uiCompletedHeaderPanel.Controls.Add(uiCompletedHeaderButton, 0, 0);
        uiCompletedHeaderPanel.Controls.Add(uiClearCompletedButton, 1, 0);
        uiCompletedHeaderPanel.Dock = DockStyle.Fill;
        uiCompletedHeaderPanel.Location = new Point(0, 0);
        uiCompletedHeaderPanel.Margin = new Padding(0, 6, 0, 4);
        uiCompletedHeaderPanel.Name = "uiCompletedHeaderPanel";
        uiCompletedHeaderPanel.RowCount = 1;
        uiCompletedHeaderPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        uiCompletedHeaderPanel.Size = new Size(784, 30);
        uiCompletedHeaderPanel.TabIndex = 0;
        //
        // uiCompletedHeaderButton
        //
        uiCompletedHeaderButton.Dock = DockStyle.Fill;
        uiCompletedHeaderButton.FlatStyle = FlatStyle.Flat;
        uiCompletedHeaderButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        uiCompletedHeaderButton.Margin = new Padding(0, 0, 8, 0);
        uiCompletedHeaderButton.Name = "uiCompletedHeaderButton";
        uiCompletedHeaderButton.Padding = new Padding(6, 4, 6, 4);
        uiCompletedHeaderButton.Size = new Size(676, 28);
        uiCompletedHeaderButton.TabIndex = 0;
        uiCompletedHeaderButton.Text = "Завершённые (0) ▾";
        uiCompletedHeaderButton.TextAlign = ContentAlignment.MiddleLeft;
        uiTasksToolTip.SetToolTip(uiCompletedHeaderButton, "Свернуть или развернуть список завершённых задач");
        uiCompletedHeaderButton.UseVisualStyleBackColor = true;
        uiCompletedHeaderButton.Click += uiCompletedHeaderButton_Click;
        //
        // uiClearCompletedButton
        //
        uiClearCompletedButton.Anchor = AnchorStyles.Right;
        uiClearCompletedButton.AutoSize = true;
        uiClearCompletedButton.Margin = new Padding(0);
        uiClearCompletedButton.Name = "uiClearCompletedButton";
        uiClearCompletedButton.Padding = new Padding(10, 6, 10, 6);
        uiClearCompletedButton.Size = new Size(140, 28);
        uiClearCompletedButton.TabIndex = 1;
        uiClearCompletedButton.Text = "Очистить завершённые";
        uiTasksToolTip.SetToolTip(uiClearCompletedButton, "Убрать все завершённые задачи из истории");
        uiClearCompletedButton.UseVisualStyleBackColor = true;
        uiClearCompletedButton.Click += uiClearCompletedButton_Click;
        //
        // uiCompletedFlowLayoutPanel
        //
        uiCompletedFlowLayoutPanel.AutoScroll = true;
        uiCompletedFlowLayoutPanel.BackColor = SystemColors.ControlLightLight;
        uiCompletedFlowLayoutPanel.BorderStyle = BorderStyle.FixedSingle;
        uiCompletedFlowLayoutPanel.Dock = DockStyle.Fill;
        uiCompletedFlowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        uiCompletedFlowLayoutPanel.Location = new Point(0, 40);
        uiCompletedFlowLayoutPanel.Margin = new Padding(0);
        uiCompletedFlowLayoutPanel.MaximumSize = new Size(0, 220);
        uiCompletedFlowLayoutPanel.MinimumSize = new Size(0, 220);
        uiCompletedFlowLayoutPanel.Name = "uiCompletedFlowLayoutPanel";
        uiCompletedFlowLayoutPanel.Padding = new Padding(8);
        uiCompletedFlowLayoutPanel.Size = new Size(784, 220);
        uiCompletedFlowLayoutPanel.TabIndex = 1;
        uiCompletedFlowLayoutPanel.WrapContents = false;
        uiCompletedFlowLayoutPanel.SizeChanged += uiCompletedFlowLayoutPanel_SizeChanged;
        //
        // uiAutoHideTimer
        //
        uiAutoHideTimer.Interval = 2000;
        uiAutoHideTimer.Tick += uiAutoHideTimer_Tick;
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
        uiCompletedPanel.ResumeLayout(false);
        uiCompletedPanel.PerformLayout();
        uiCompletedHeaderPanel.ResumeLayout(false);
        uiCompletedHeaderPanel.PerformLayout();
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
    private TableLayoutPanel uiCompletedPanel;
    private TableLayoutPanel uiCompletedHeaderPanel;
    private Button uiCompletedHeaderButton;
    private Button uiClearCompletedButton;
    private DoubleBufferedFlowLayoutPanel uiCompletedFlowLayoutPanel;
    private ToolTip uiTasksToolTip;
    private System.Windows.Forms.Timer uiAutoHideTimer;
}
