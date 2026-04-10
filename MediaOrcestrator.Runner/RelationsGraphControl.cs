using MediaOrcestrator.Domain;
using Microsoft.Msagl.GraphViewerGdi;
using DColor = Microsoft.Msagl.Drawing.Color;
using DEdge = Microsoft.Msagl.Drawing.Edge;
using DGraph = Microsoft.Msagl.Drawing.Graph;
using DShape = Microsoft.Msagl.Drawing.Shape;
using DStyle = Microsoft.Msagl.Drawing.Style;
using IViewerEdge = Microsoft.Msagl.Drawing.IViewerEdge;
using IViewerNode = Microsoft.Msagl.Drawing.IViewerNode;

namespace MediaOrcestrator.Runner;

public sealed class RelationsGraphControl : UserControl
{
    private readonly GViewer _viewer;
    private readonly ContextMenuStrip _edgeMenu;
    private readonly ContextMenuStrip _nodeMenu;
    private readonly ToolStripButton _createRelationButton;
    private readonly ToolStripLabel _statusLabel;
    private readonly HashSet<string> _disabledSourceIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _disabledEdgeKeys = new(StringComparer.Ordinal);

    private DEdge? _selectedEdge;
    private string? _menuTargetNodeId;
    private SelectionMode _mode = SelectionMode.Idle;
    private string? _pendingFromId;
    private string? _focusedNodeId;

    public RelationsGraphControl()
    {
        _viewer = new()
        {
            Dock = DockStyle.Fill,
            ToolBarIsVisible = true,
            OutsideAreaBrush = Brushes.White,
        };

        _edgeMenu = new();
        _edgeMenu.Items.Add("Инвертировать", null, (_, _) => RaiseEdgeEvent(InvertRequested));
        _edgeMenu.Items.Add("Удалить", null, (_, _) => RaiseEdgeEvent(DeleteRequested));

        _nodeMenu = new();
        _nodeMenu.Items.Add("Создать связь отсюда", null, OnNodeMenuCreateClick);

        _createRelationButton = new("Создать связь");
        _createRelationButton.Click += OnCreateRelationButtonClick;

        _statusLabel = new(string.Empty);

        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
        };

        toolStrip.Items.Add(_createRelationButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(_statusLabel);

        _viewer.MouseDown += OnViewerMouseDown;

        Controls.Add(_viewer);
        Controls.Add(toolStrip);
    }

    public event EventHandler<RelationGraphEdgeEventArgs>? CreateRequested;
    public event EventHandler<RelationGraphEdgeEventArgs>? DeleteRequested;
    public event EventHandler<RelationGraphEdgeEventArgs>? InvertRequested;

    private enum SelectionMode
    {
        Idle = 0,
        SelectingFrom = 1,
        SelectingTo = 2,
    }

    public void SetRelations(IReadOnlyCollection<SourceSyncRelation> relations)
    {
        _disabledSourceIds.Clear();
        _disabledEdgeKeys.Clear();

        var graph = new DGraph("relations");
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var relation in relations)
        {
            var fromId = relation.From?.Id ?? relation.FromId;
            var toId = relation.To?.Id ?? relation.ToId;

            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                continue;
            }

            EnsureNode(graph, seen, fromId, relation.From);
            EnsureNode(graph, seen, toId, relation.To);

            var edge = graph.AddEdge(fromId, toId);

            if (relation.From?.IsDisable == true)
            {
                _disabledSourceIds.Add(fromId);
            }

            if (relation.To?.IsDisable == true)
            {
                _disabledSourceIds.Add(toId);
            }

            if (relation.IsDisable)
            {
                _disabledEdgeKeys.Add(EdgeKey(fromId, toId));
                edge.Attr.AddStyle(DStyle.Dashed);
            }
        }

        _focusedNodeId = null;
        _pendingFromId = null;
        SetMode(SelectionMode.Idle);

        ApplyVisualState(graph);
        _viewer.Graph = graph;
    }

    private void OnViewerMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            var rightObj = _viewer.ObjectUnderMouseCursor;

            if (rightObj is IViewerEdge viewerEdge)
            {
                _selectedEdge = viewerEdge.Edge;
                _edgeMenu.Show(_viewer, e.Location);
            }
            else if (rightObj is IViewerNode viewerNodeRight)
            {
                _menuTargetNodeId = viewerNodeRight.Node.Id;
                _nodeMenu.Show(_viewer, e.Location);
            }

            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var obj = _viewer.ObjectUnderMouseCursor;

        if (obj is IViewerNode viewerNode)
        {
            HandleNodeClick(viewerNode.Node.Id);
            return;
        }

        if (obj == null && _mode == SelectionMode.Idle && _focusedNodeId != null)
        {
            _focusedNodeId = null;
            RefreshVisualState();
        }
    }

    private void OnNodeMenuCreateClick(object? sender, EventArgs e)
    {
        if (_menuTargetNodeId == null)
        {
            return;
        }

        _focusedNodeId = null;
        _pendingFromId = _menuTargetNodeId;
        _menuTargetNodeId = null;
        SetMode(SelectionMode.SelectingTo);
        RefreshVisualState();
    }

    private void OnCreateRelationButtonClick(object? sender, EventArgs e)
    {
        if (_mode == SelectionMode.Idle)
        {
            _focusedNodeId = null;
            _pendingFromId = null;
            SetMode(SelectionMode.SelectingFrom);
        }
        else
        {
            _pendingFromId = null;
            SetMode(SelectionMode.Idle);
        }

        RefreshVisualState();
    }

    private static void EnsureNode(DGraph graph, HashSet<string> seen, string id, Source? source)
    {
        if (!seen.Add(id))
        {
            return;
        }

        var node = graph.AddNode(id);
        node.LabelText = source?.TitleFull ?? id;
        node.Attr.Shape = DShape.Box;
    }

    private static HashSet<string> ComputeNeighborhood(DGraph graph, string focusedId)
    {
        var set = new HashSet<string>(StringComparer.Ordinal) { focusedId };

        foreach (var edge in graph.Edges)
        {
            if (edge.Source == focusedId)
            {
                set.Add(edge.Target);
            }

            if (edge.Target == focusedId)
            {
                set.Add(edge.Source);
            }
        }

        return set;
    }

    private static string EdgeKey(string from, string to)
    {
        return $"{from}->{to}";
    }

    private void ApplyVisualState(DGraph graph)
    {
        var neighbors = _focusedNodeId != null
            ? ComputeNeighborhood(graph, _focusedNodeId)
            : null;

        foreach (var node in graph.Nodes)
        {
            node.Attr.FillColor = _disabledSourceIds.Contains(node.Id)
                ? DColor.LightGray
                : DColor.White;

            if (node.Id == _pendingFromId)
            {
                node.Attr.Color = DColor.Orange;
                node.Attr.LineWidth = 3;
                continue;
            }

            if (node.Id == _focusedNodeId)
            {
                node.Attr.Color = DColor.Blue;
                node.Attr.LineWidth = 2;
                continue;
            }

            if (neighbors != null && !neighbors.Contains(node.Id))
            {
                node.Attr.Color = DColor.LightGray;
                node.Attr.LineWidth = 1;
                continue;
            }

            node.Attr.Color = DColor.Black;
            node.Attr.LineWidth = 1;
        }

        foreach (var edge in graph.Edges)
        {
            var key = EdgeKey(edge.Source, edge.Target);
            var isDisabled = _disabledEdgeKeys.Contains(key);

            var touchesFocus = _focusedNodeId != null
                               && (edge.Source == _focusedNodeId || edge.Target == _focusedNodeId);

            if (_focusedNodeId != null && !touchesFocus)
            {
                edge.Attr.Color = DColor.LightGray;
                edge.Attr.LineWidth = 1;
                continue;
            }

            edge.Attr.Color = isDisabled ? DColor.LightGray : DColor.Black;
            edge.Attr.LineWidth = touchesFocus ? 2 : 1;
        }
    }

    private void HandleNodeClick(string nodeId)
    {
        switch (_mode)
        {
            case SelectionMode.SelectingFrom:
                _pendingFromId = nodeId;
                SetMode(SelectionMode.SelectingTo);
                RefreshVisualState();
                break;

            case SelectionMode.SelectingTo:
                if (_pendingFromId == null || _pendingFromId == nodeId)
                {
                    return;
                }

                var fromId = _pendingFromId;
                _pendingFromId = null;
                SetMode(SelectionMode.Idle);
                CreateRequested?.Invoke(this, new(fromId, nodeId));
                break;

            default:
                _focusedNodeId = _focusedNodeId == nodeId ? null : nodeId;
                RefreshVisualState();
                break;
        }
    }

    private void RefreshVisualState()
    {
        var graph = _viewer.Graph;

        if (graph == null)
        {
            return;
        }

        ApplyVisualState(graph);
        _viewer.Invalidate();
    }

    private void SetMode(SelectionMode mode)
    {
        _mode = mode;
        _statusLabel.Text = mode switch
        {
            SelectionMode.SelectingFrom => "Выберите начальный узел",
            SelectionMode.SelectingTo => "Выберите целевой узел",
            _ => string.Empty,
        };

        _createRelationButton.Checked = mode != SelectionMode.Idle;
    }

    private void RaiseEdgeEvent(EventHandler<RelationGraphEdgeEventArgs>? handler)
    {
        if (_selectedEdge == null)
        {
            return;
        }

        handler?.Invoke(this, new(_selectedEdge.Source, _selectedEdge.Target));
        _selectedEdge = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _edgeMenu.Dispose();
            _nodeMenu.Dispose();
        }

        base.Dispose(disposing);
    }
}

public sealed class RelationGraphEdgeEventArgs(string fromSourceId, string toSourceId) : EventArgs
{
    public string FromSourceId { get; } = fromSourceId;

    public string ToSourceId { get; } = toSourceId;
}
