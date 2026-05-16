using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public sealed partial class TasksControl : UserControl
{
    private const int IndentStep = 18;

    private ActionHolder? _actionHolder;

    public TasksControl()
    {
        InitializeComponent();
    }

    public event EventHandler<int>? RunningCountChanged;

    public void Initialize(ActionHolder actionHolder)
    {
        if (_actionHolder != null)
        {
            _actionHolder.Changed -= OnActionsChanged;
        }

        _actionHolder = actionHolder;
        _actionHolder.Changed += OnActionsChanged;
        Rebuild();
    }

    private void OnActionsChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(Rebuild);
            return;
        }

        Rebuild();
    }

    private void uiTasksFlowLayoutPanel_SizeChanged(object sender, EventArgs e)
    {
        var rowWidth = CalculateRowWidth();
        if (rowWidth <= 0)
        {
            return;
        }

        foreach (Control control in uiTasksFlowLayoutPanel.Controls)
        {
            control.Width = Math.Max(rowWidth - control.Margin.Left, 0);
        }
    }

    private void uiCancelAllButton_Click(object sender, EventArgs e)
    {
        if (_actionHolder == null)
        {
            return;
        }

        var snapshot = _actionHolder.Snapshot();
        if (snapshot.Count == 0)
        {
            return;
        }

        var confirm = MessageBox.Show(this,
            $"Отменить все активные задачи ({snapshot.Count})?",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        foreach (var action in snapshot)
        {
            action.Cancel();
        }
    }

    private void Rebuild()
    {
        if (_actionHolder == null)
        {
            return;
        }

        var snapshot = _actionHolder.Snapshot();

        uiTasksFlowLayoutPanel.SuspendLayout();
        try
        {
            foreach (Control control in uiTasksFlowLayoutPanel.Controls)
            {
                control.Dispose();
            }

            uiTasksFlowLayoutPanel.Controls.Clear();

            var rowWidth = CalculateRowWidth();
            var indentStep = LogicalToDeviceUnits(IndentStep);
            foreach (var action in snapshot)
            {
                var indent = indentStep * Math.Max(action.Depth, 0);
                var row = new ActionUserControl
                {
                    Width = Math.Max(rowWidth - indent, 0),
                    Margin = new(indent, 0, 0, 6),
                };

                row.SetAction(action);
                uiTasksFlowLayoutPanel.Controls.Add(row);
            }
        }
        finally
        {
            uiTasksFlowLayoutPanel.ResumeLayout();
        }

        uiHeaderLabel.Text = snapshot.Count == 0
            ? "Активных задач нет"
            : $"Активных задач: {snapshot.Count}";

        uiCancelAllButton.Enabled = snapshot.Count > 0;
        uiEmptyStateLabel.Visible = snapshot.Count == 0;
        uiTasksFlowLayoutPanel.Visible = snapshot.Count > 0;

        RunningCountChanged?.Invoke(this, snapshot.Count);
    }

    private int CalculateRowWidth()
    {
        var width = uiTasksFlowLayoutPanel.ClientSize.Width - uiTasksFlowLayoutPanel.Padding.Horizontal;
        if (uiTasksFlowLayoutPanel.VerticalScroll.Visible)
        {
            width -= SystemInformation.VerticalScrollBarWidth;
        }

        return Math.Max(width, 0);
    }
}
