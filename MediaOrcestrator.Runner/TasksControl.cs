using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public sealed partial class TasksControl : UserControl
{
    private const int IndentStep = 18;

    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(8);

    private readonly Dictionary<Guid, ActionUserControl> _rows = [];
    private readonly Dictionary<Guid, CompletedActionRow> _completedRows = [];

    private ActionHolder? _actionHolder;
    private bool _completedCollapsed = true;

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
        ResizeRows(uiTasksFlowLayoutPanel);
    }

    private void uiCompletedFlowLayoutPanel_SizeChanged(object sender, EventArgs e)
    {
        ResizeRows(uiCompletedFlowLayoutPanel);
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

    private void uiCompletedHeaderButton_Click(object sender, EventArgs e)
    {
        _completedCollapsed = !_completedCollapsed;
        Rebuild();
    }

    private void uiClearCompletedButton_Click(object sender, EventArgs e)
    {
        _actionHolder?.ClearCompleted();
    }

    private void uiAutoHideTimer_Tick(object sender, EventArgs e)
    {
        if (_actionHolder == null)
        {
            return;
        }

        var now = DateTime.Now;
        var stale = _actionHolder.CompletedSnapshot()
            .Where(a => a is { State: ActionState.Succeeded, FinishedAt: not null }
                        && now - a.FinishedAt.Value >= AutoHideDelay)
            .ToArray();

        foreach (var action in stale)
        {
            _actionHolder.Dismiss(action);
        }
    }

    private static int CalculateRowWidth(DoubleBufferedFlowLayoutPanel panel)
    {
        var width = panel.ClientSize.Width - panel.Padding.Horizontal;
        if (panel.VerticalScroll.Visible)
        {
            width -= SystemInformation.VerticalScrollBarWidth;
        }

        return Math.Max(width, 0);
    }

    private static void SyncCompletedPanel(
        DoubleBufferedFlowLayoutPanel panel,
        Dictionary<Guid, CompletedActionRow> rows,
        IReadOnlyList<ActionHolder.RunningAction> actions)
    {
        panel.SuspendLayout();
        try
        {
            var present = actions.Select(a => a.Id).ToHashSet();

            foreach (var (id, row) in rows.Where(kv => !present.Contains(kv.Key)).ToArray())
            {
                rows.Remove(id);
                panel.Controls.Remove(row);
                row.Dispose();
            }

            var rowWidth = CalculateRowWidth(panel);
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];

                if (!rows.TryGetValue(action.Id, out var row))
                {
                    row = new();
                    row.SetAction(action);
                    rows[action.Id] = row;
                    panel.Controls.Add(row);
                }

                row.Width = rowWidth;
                panel.Controls.SetChildIndex(row, i);
            }
        }
        finally
        {
            panel.ResumeLayout();
        }
    }

    private void Rebuild()
    {
        if (_actionHolder == null)
        {
            return;
        }

        var snapshot = _actionHolder.Snapshot();
        var completed = _actionHolder.CompletedSnapshot();

        SyncPanel(uiTasksFlowLayoutPanel, _rows, snapshot, true);
        SyncCompletedPanel(uiCompletedFlowLayoutPanel, _completedRows, completed);

        uiHeaderLabel.Text = snapshot.Count == 0
            ? "Активных задач нет"
            : $"Активных задач: {snapshot.Count}";

        uiCancelAllButton.Enabled = snapshot.Count > 0;
        uiEmptyStateLabel.Visible = snapshot.Count == 0;
        uiTasksFlowLayoutPanel.Visible = snapshot.Count > 0;

        var hasCompleted = completed.Count > 0;
        uiCompletedPanel.Visible = hasCompleted;
        var arrow = _completedCollapsed ? "▸" : "▾";
        uiCompletedHeaderButton.Text = $"Завершённые ({completed.Count}) {arrow}";
        uiCompletedFlowLayoutPanel.Visible = hasCompleted && !_completedCollapsed;

        var hasSucceeded = completed.Any(a => a.State == ActionState.Succeeded);
        if (hasSucceeded && !uiAutoHideTimer.Enabled)
        {
            uiAutoHideTimer.Start();
        }
        else if (!hasSucceeded && uiAutoHideTimer.Enabled)
        {
            uiAutoHideTimer.Stop();
        }

        RunningCountChanged?.Invoke(this, snapshot.Count);
    }

    private void SyncPanel(
        DoubleBufferedFlowLayoutPanel panel,
        Dictionary<Guid, ActionUserControl> rows,
        IReadOnlyList<ActionHolder.RunningAction> actions,
        bool applyIndent)
    {
        panel.SuspendLayout();
        try
        {
            var present = actions.Select(a => a.Id).ToHashSet();

            foreach (var (id, row) in rows.Where(kv => !present.Contains(kv.Key)).ToArray())
            {
                rows.Remove(id);
                panel.Controls.Remove(row);
                row.Dispose();
            }

            var rowWidth = CalculateRowWidth(panel);
            var indentStep = LogicalToDeviceUnits(IndentStep);
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var indent = applyIndent ? indentStep * Math.Max(action.Depth, 0) : 0;

                if (!rows.TryGetValue(action.Id, out var row))
                {
                    row = new();
                    row.SetAction(action);
                    rows[action.Id] = row;
                    panel.Controls.Add(row);
                }

                var margin = new Padding(indent, 0, 0, 6);
                if (row.Margin != margin)
                {
                    row.Margin = margin;
                }

                row.Width = Math.Max(rowWidth - indent, 0);
                panel.Controls.SetChildIndex(row, i);
            }
        }
        finally
        {
            panel.ResumeLayout();
        }
    }

    private void ResizeRows(DoubleBufferedFlowLayoutPanel panel)
    {
        var rowWidth = CalculateRowWidth(panel);
        if (rowWidth <= 0)
        {
            return;
        }

        foreach (Control control in panel.Controls)
        {
            control.Width = Math.Max(rowWidth - control.Margin.Left, 0);
        }
    }
}
