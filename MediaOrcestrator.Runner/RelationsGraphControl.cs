using MediaOrcestrator.Domain;
using DColor = Microsoft.Msagl.Drawing.Color;
using DEdge = Microsoft.Msagl.Drawing.Edge;
using DGraph = Microsoft.Msagl.Drawing.Graph;
using DShape = Microsoft.Msagl.Drawing.Shape;
using DStyle = Microsoft.Msagl.Drawing.Style;
using IViewerEdge = Microsoft.Msagl.Drawing.IViewerEdge;
using IViewerNode = Microsoft.Msagl.Drawing.IViewerNode;

namespace MediaOrcestrator.Runner;

public partial class RelationsGraphControl : UserControl
{
    private static readonly Dictionary<NodeRole, DColor> RoleGraphColors = new()
    {
        [NodeRole.Reader] = new(152, 251, 152),
        [NodeRole.Writer] = new(173, 216, 230),
        [NodeRole.Transit] = new(255, 228, 181),
    };

    private readonly ContextMenuStrip _edgeMenu;
    private readonly ContextMenuStrip _nodeMenu;
    private readonly HashSet<string> _disabledSourceIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _disabledEdgeKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NodeRole> _nodeRoles = new(StringComparer.Ordinal);

    private DEdge? _selectedEdge;
    private string? _menuTargetNodeId;
    private SelectionMode _mode = SelectionMode.Idle;
    private string? _pendingFromId;
    private string? _focusedNodeId;
    private bool _forceFitOnNextSet;

    public RelationsGraphControl()
    {
        InitializeComponent();

        uiReaderLegend.Image = CreateLegendSwatch(Color.PaleGreen);
        uiWriterLegend.Image = CreateLegendSwatch(Color.LightBlue);
        uiTransitLegend.Image = CreateLegendSwatch(Color.Moccasin);

        _edgeMenu = new();
        _edgeMenu.Items.Add("Инвертировать", null, (_, _) => RaiseEdgeEvent(InvertRequested));
        _edgeMenu.Items.Add("Удалить", null, (_, _) => RaiseEdgeEvent(DeleteRequested));

        _nodeMenu = new();
        _nodeMenu.Items.Add("Создать связь отсюда", null, OnNodeMenuCreateClick);

        uiViewer.MouseDown += OnViewerMouseDown;
    }

    public event EventHandler<RelationGraphEdgeEventArgs>? CreateRequested;
    public event EventHandler<RelationGraphEdgeEventArgs>? DeleteRequested;
    public event EventHandler<RelationGraphEdgeEventArgs>? InvertRequested;
    public event EventHandler? RefreshRequested;

    private enum NodeRole
    {
        Reader = 0,
        Writer = 1,
        Transit = 2,
    }

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
        _nodeRoles.Clear();

        var graph = new DGraph("relations");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var hasOut = new HashSet<string>(StringComparer.Ordinal);
        var hasIn = new HashSet<string>(StringComparer.Ordinal);

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
            hasOut.Add(fromId);
            hasIn.Add(toId);

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

        foreach (var nodeId in seen)
        {
            var isOut = hasOut.Contains(nodeId);
            var isIn = hasIn.Contains(nodeId);
            _nodeRoles[nodeId] = (isOut, isIn) switch
            {
                (true, false) => NodeRole.Reader,
                (false, true) => NodeRole.Writer,
                _ => NodeRole.Transit,
            };
        }

        var isEmpty = graph.NodeCount == 0;
        uiEmptyStateLabel.Visible = isEmpty;
        uiViewer.Visible = !isEmpty;

        _focusedNodeId = null;
        _pendingFromId = null;
        SetMode(SelectionMode.Idle);

        ApplyVisualState(graph);

        var shouldPreserveZoom = !_forceFitOnNextSet && uiViewer.Graph != null;
        _forceFitOnNextSet = false;

        var savedZoom = shouldPreserveZoom ? uiViewer.ZoomF : (double?)null;
        uiViewer.Graph = graph;

        if (savedZoom.HasValue)
        {
            uiViewer.ZoomF = savedZoom.Value;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && _mode != SelectionMode.Idle)
        {
            _pendingFromId = null;
            SetMode(SelectionMode.Idle);
            RefreshVisualState();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void OnViewerMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            var rightObj = uiViewer.ObjectUnderMouseCursor;

            if (rightObj is IViewerEdge viewerEdge)
            {
                _selectedEdge = viewerEdge.Edge;
                _edgeMenu.Show(uiViewer, e.Location);
            }
            else if (rightObj is IViewerNode viewerNodeRight)
            {
                _menuTargetNodeId = viewerNodeRight.Node.Id;
                _nodeMenu.Show(uiViewer, e.Location);
            }

            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var obj = uiViewer.ObjectUnderMouseCursor;

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

    private void OnHelpButtonClick(object? sender, EventArgs e)
    {
        DocumentationForm.ShowAppDoc(FindForm(), "Граф связей — справка", "relations-graph.md");
    }

    private void OnRebuildButtonClick(object? sender, EventArgs e)
    {
        _forceFitOnNextSet = true;
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private static Image CreateLegendSwatch(Color color)
    {
        var bitmap = new Bitmap(12, 12);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(color);
            graphics.DrawRectangle(Pens.Gray, 0, 0, 11, 11);
        }

        return bitmap;
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
            var isDimmed = neighbors != null && !neighbors.Contains(node.Id);

            if (isDimmed)
            {
                node.Attr.FillColor = DColor.WhiteSmoke;
                node.Attr.Color = DColor.LightGray;
                node.Attr.LineWidth = 1;
                node.Label.FontColor = DColor.LightGray;
                continue;
            }

            node.Attr.FillColor = ResolveNodeFillColor(node.Id);
            node.Label.FontColor = DColor.Black;

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

    private DColor ResolveNodeFillColor(string nodeId)
    {
        if (_disabledSourceIds.Contains(nodeId))
        {
            return DColor.LightGray;
        }

        return _nodeRoles.TryGetValue(nodeId, out var role)
            ? RoleGraphColors[role]
            : DColor.White;
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
                CreateRequested?.Invoke(this, new(fromId, nodeId));

                _pendingFromId = fromId;
                _focusedNodeId = null;
                SetMode(SelectionMode.SelectingTo);
                RefreshVisualState();
                break;

            default:
                _focusedNodeId = _focusedNodeId == nodeId ? null : nodeId;
                RefreshVisualState();
                break;
        }
    }

    private void RefreshVisualState()
    {
        var graph = uiViewer.Graph;

        if (graph == null)
        {
            return;
        }

        ApplyVisualState(graph);
        uiViewer.Invalidate();
    }

    private void SetMode(SelectionMode mode)
    {
        _mode = mode;
        uiStatusLabel.Text = mode switch
        {
            SelectionMode.SelectingFrom => "Выберите начальный узел",
            SelectionMode.SelectingTo => "Выберите целевой узел (Esc — выйти)",
            _ => string.Empty,
        };

        uiCreateRelationButton.Checked = mode != SelectionMode.Idle;
        uiViewer.Cursor = mode != SelectionMode.Idle ? Cursors.Cross : Cursors.Default;
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
}

public sealed class RelationGraphEdgeEventArgs(string fromSourceId, string toSourceId) : EventArgs
{
    public string FromSourceId { get; } = fromSourceId;

    public string ToSourceId { get; } = toSourceId;
}
