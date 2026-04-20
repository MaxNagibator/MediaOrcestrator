using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class SyncTreeControl : UserControl
{
    private const int IconPending = 0;
    private const int IconWorking = 1;
    private const int IconOk = 2;
    private const int IconError = 3;
    private readonly Dictionary<SyncIntent, TreeNode> _intentNodeMap = new();

    private Orcestrator? _orcestrator;
    private SyncRetryRunner? _retryRunner;
    private SyncPlanner _planner;
    private ActionHolder _actionHolder;
    private ILogger<SyncTreeControl>? _logger;
    private List<SyncIntent>? _rootIntents;
    private CancellationTokenSource? _cts;
    private bool _suppressCheckEvents;
    private Font? _boldFont;

    public SyncTreeControl()
    {
        InitializeComponent();
        InitializeImageList();
        uiTreeView.AllowDrop = true;
        uiTreeView.ItemDrag += uiTreeView_ItemDrag;
        uiTreeView.DragOver += uiTreeView_DragOver;
        uiTreeView.DragDrop += uiTreeView_DragDrop;
        uiTreeView.AfterCheck += UiTreeView_AfterCheck;
        uiFilterControl.FilterChanged += (_, _) => ApplyTreeFilter();
    }

    public void Initialize(SyncPlanner planner,
        Orcestrator orcestrator,
        SyncRetryRunner retryRunner,
        ActionHolder actionHolder,
        ILogger<SyncTreeControl> logger)
    {
        // TODO: DI
        _orcestrator = orcestrator;
        _retryRunner = retryRunner;
        _planner = planner;
        _actionHolder = actionHolder;
        _logger = logger;
    }

    private void uiSelectAllButton_Click(object sender, EventArgs e)
    {
        SetAllNodesChecked(true);
    }

    private void uiDeselectAllButton_Click(object sender, EventArgs e)
    {
        SetAllNodesChecked(false);
    }

    private void UiTreeView_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_suppressCheckEvents || e.Node == null)
        {
            return;
        }

        _suppressCheckEvents = true;
        try
        {
            if (e.Node.Checked)
            {
                SetParentsChecked(e.Node);
            }
            else
            {
                SetChildrenRecursive(e.Node, false);
            }
        }
        finally
        {
            _suppressCheckEvents = false;
        }
    }

    private void uiTreeView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e is { Button: MouseButtons.Left, Item: TreeNode { Parent: null } node })
        {
            DoDragDrop(node, DragDropEffects.Move);
        }
    }

    private void uiTreeView_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data == null || !e.Data.GetDataPresent(typeof(TreeNode)))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        var point = uiTreeView.PointToClient(new(e.X, e.Y));
        var targetNode = uiTreeView.GetNodeAt(point);

        e.Effect = targetNode?.Parent == null ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void uiTreeView_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode draggedNode)
        {
            return;
        }

        var point = uiTreeView.PointToClient(new(e.X, e.Y));
        var targetNode = uiTreeView.GetNodeAt(point);

        if (draggedNode == targetNode)
        {
            return;
        }

        uiTreeView.BeginUpdate();
        try
        {
            draggedNode.Remove();

            if (targetNode == null)
            {
                uiTreeView.Nodes.Add(draggedNode);
            }
            else
            {
                var targetIndex = targetNode.Index;

                if (point.Y > targetNode.Bounds.Top + targetNode.Bounds.Height / 2)
                {
                    targetIndex++;
                }

                uiTreeView.Nodes.Insert(targetIndex, draggedNode);
            }

            uiTreeView.SelectedNode = draggedNode;
        }
        finally
        {
            uiTreeView.EndUpdate();
        }
    }

    private async void uiExecuteButton_Click(object? sender, EventArgs e)
    {
        if (_rootIntents == null || _orcestrator == null || _logger == null)
        {
            return;
        }

        ClearSelection(_rootIntents);
        var allIntents = new List<SyncIntent>();
        UpdateIntentsFromTree(uiTreeView.Nodes, allIntents);

        var filteredRootIntents = new List<SyncIntent>();
        foreach (TreeNode mediaNode in uiTreeView.Nodes)
        {
            foreach (TreeNode intentNode in mediaNode.Nodes)
            {
                if (intentNode.Tag is SyncIntent { IsSelected: true } intent)
                {
                    filteredRootIntents.Add(intent);
                }
            }
        }

        if (filteredRootIntents.Count == 0)
        {
            MessageBox.Show("Ничего не выбрано.");
            return;
        }

        uiExecuteButton.Enabled = false;
        uiStopButton.Enabled = true;
        uiTreeView.Enabled = false;

        _cts = new();

        LogToUi($"Запуск синхронизации для {filteredRootIntents.Count} цепочек...", Color.Yellow);

        var action = _actionHolder.Register("Синхронизация цепочки", "В процессе", filteredRootIntents.Count, _cts);
        var processed = 0;
        try
        {
            var relations = _orcestrator.GetRelations();
            var executor = new ParallelSyncExecutor(filteredRootIntents, relations);

            await executor.ExecuteAsync(async intent =>
            {
                var node = _intentNodeMap.GetValueOrDefault(intent);
                UpdateStatusLabel($"Обработка: {intent.Media.Title}");

                var progress = new Progress<SyncAttemptStatus>(status => ApplyAttemptStatusToNode(intent, node, status));

                await _retryRunner!.RunAsync(intent.Media, intent.Relation, progress, cancellationToken: _cts!.Token);
                processed++;
                action.ProgressPlus();
            }, _cts.Token, (intent, ex) =>
            {
                var node = _intentNodeMap.GetValueOrDefault(intent);
                UpdateNodeState(node, IconError, Color.Red,
                    $"[Ошибка] {intent.From.TitleFull} -> {intent.To.TitleFull}");

                _logger!.LogError(ex, "Ошибка при выполнении синхронизации для {Intent}", intent);
                LogToUi($"Ошибка для {intent.Media.Title}: {ex.Message}", Color.Red);

                if (!uiStopIfErrorCheckBox.Checked)
                {
                    return;
                }

                LogToUi("Синхронизация прервана в результате ошибки.", Color.Orange);
                _cts?.Cancel();
            });

            UpdateStatusLabel("Завершено");
            LogToUi("Процесс синхронизации полностью завершен.", Color.LightGreen);
        }
        catch (OperationCanceledException)
        {
            UpdateStatusLabel("Остановлено");
            LogToUi("Процесс был остановлен.", Color.Orange);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Критическая ошибка при выполнении синхронизации.");
            LogToUi($"Критическая ошибка: {ex.Message}", Color.Red);
        }
        finally
        {
            uiExecuteButton.Enabled = true;
            uiStopButton.Enabled = false;
            uiTreeView.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void uiStopButton_Click(object sender, EventArgs e)
    {
        _cts?.Cancel();
        uiStopButton.Enabled = false;
        LogToUi("Запрошена остановка...", Color.Orange);
    }

    private void uiConstructButton_Click(object sender, EventArgs e)
    {
        uiConstructButton.Enabled = false;
        try
        {
            var medias = _orcestrator.GetMedias();
            var relations = _orcestrator.GetRelations();
            var intents = _planner.Plan(medias, relations);
            _rootIntents = intents;
            uiFilterControl.ShowStatusFilter = false;
            uiFilterControl.PopulateRelationsFilter(_orcestrator);
            PopulateTree();
            LogToUi("Планировщик инициализирован. Готов к работе.");
        }
        finally
        {
            uiConstructButton.Enabled = true;
        }
    }

    private static TreeNode CreateIntentNode(SyncIntent intent, Dictionary<SyncIntent, TreeNode> intentNodeMap, HashSet<(string FromId, string ToId)>? relationFilter = null)
    {
        var label = $"{intent.From.TitleFull} -> {intent.To.TitleFull}";
        var node = new TreeNode(label)
        {
            Tag = intent,
            Checked = intent.IsSelected,
            ForeColor = Color.DodgerBlue,
            ImageIndex = IconPending,
            SelectedImageIndex = IconPending,
        };

        intentNodeMap[intent] = node;

        foreach (var nextIntent in intent.NextIntents)
        {
            if (relationFilter is { Count: > 0 } && !relationFilter.Contains((nextIntent.Relation.FromId, nextIntent.Relation.ToId)))
            {
                continue;
            }

            node.Nodes.Add(CreateIntentNode(nextIntent, intentNodeMap, relationFilter));
        }

        return node;
    }

    private static void UpdateIntentsFromTree(TreeNodeCollection nodes, List<SyncIntent> syncIntents)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is SyncIntent intent)
            {
                intent.IsSelected = node.Checked;
                syncIntents.Add(intent);
            }

            UpdateIntentsFromTree(node.Nodes, syncIntents);
        }
    }

    private static void ClearSelection(IEnumerable<SyncIntent> intents)
    {
        foreach (var intent in intents)
        {
            intent.IsSelected = false;
            ClearSelection(intent.NextIntents);
        }
    }

    private static Bitmap CreateColorIcon(Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(color);
        g.Clear(Color.Transparent);
        g.FillEllipse(brush, 2, 2, 12, 12);
        return bmp;
    }

    private void LogToUi(string message, Color? color = null)
    {
        if (uiLogRichTextBox.InvokeRequired)
        {
            uiLogRichTextBox.Invoke(() => LogToUi(message, color));
            return;
        }

        uiLogRichTextBox.SelectionStart = uiLogRichTextBox.TextLength;
        uiLogRichTextBox.SelectionLength = 0;
        uiLogRichTextBox.SelectionColor = color ?? Color.LightGray;
        uiLogRichTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        uiLogRichTextBox.ScrollToCaret();
    }

    private void UpdateStatusLabel(string text)
    {
        if (uiStatusStrip.InvokeRequired)
        {
            uiStatusStrip.Invoke(() => uiStatusLabel.Text = text);
        }
        else
        {
            uiStatusLabel.Text = text;
        }
    }

    private void ApplyAttemptStatusToNode(SyncIntent intent, TreeNode? node, SyncAttemptStatus status)
    {
        var route = $"{intent.From.TitleFull} -> {intent.To.TitleFull}";

        switch (status.Kind)
        {
            case SyncAttemptKind.Started:
                var startedLabel = status.AttemptNumber > 1
                    ? $"[В работе, попытка {status.AttemptNumber}/{status.MaxAttempts}] {route}"
                    : $"[В работе] {route}";

                UpdateNodeState(node, IconWorking, Color.Orange, startedLabel);

                if (status.AttemptNumber == 1)
                {
                    LogToUi($"Выполнение: {intent.Media.Title} ({intent.From.TypeId} -> {intent.To.TypeId})");
                }

                break;

            case SyncAttemptKind.Succeeded:
                UpdateNodeState(node, IconOk, Color.Green, $"[OK] {route}");

                if (node != null)
                {
                    node.Checked = false;
                }

                LogToUi($"[Успех] {intent.Media.Title} передан в {intent.To.Title}", Color.LightGreen);
                break;

            case SyncAttemptKind.FailedRetrying:
                var nextAttempt = status.AttemptNumber + 1;
                var delayMinutes = status.NextDelay?.TotalMinutes ?? 0;
                var nextAt = status.NextAttemptAt?.LocalDateTime;

                _logger?.LogError(status.Error, "Ошибка при выполнении синхронизации для {Intent}", intent);
                LogToUi($"Ошибка для {intent.Media.Title}: {status.Error?.Message}", Color.Red);
                LogToUi($"Попытка №{nextAttempt} через {delayMinutes:F0} мин (запланирована на {nextAt:dd.MM HH:mm:ss})");

                UpdateNodeState(node,
                    IconError,
                    Color.Red,
                    $"[В работе. Ожидание {delayMinutes:F0} мин перед попыткой {nextAttempt}/{status.MaxAttempts}] {route}");

                break;

            case SyncAttemptKind.FailedFinal:
                UpdateNodeState(node, IconError, Color.Red, $"[Ошибка] {route}");
                break;

            case SyncAttemptKind.NonRetriable:
                LogToUi($"[Не ретраим] {intent.Media.Title}: {status.Error?.Message}", Color.Red);
                break;

            case SyncAttemptKind.Joined:
                UpdateNodeState(node, IconWorking, Color.Orange, $"[Ожидание: операция уже запущена] {route}");
                LogToUi($"[Ожидание] {intent.Media.Title} — операция уже выполняется", Color.Orange);
                break;
        }
    }

    private void UpdateNodeState(TreeNode? node, int imageIndex, Color color, string text)
    {
        if (node == null)
        {
            return;
        }

        if (uiTreeView.InvokeRequired)
        {
            uiTreeView.Invoke(() => UpdateNodeState(node, imageIndex, color, text));
            return;
        }

        node.ImageIndex = imageIndex;
        node.SelectedImageIndex = imageIndex;
        node.ForeColor = color;
        node.Text = text;
    }

    private void PopulateTree()
    {
        ApplyTreeFilter();
    }

    private void ApplyTreeFilter()
    {
        if (_rootIntents == null)
        {
            return;
        }

        var filterState = uiFilterControl.BuildFilterState();
        var hasActiveFilter = !string.IsNullOrEmpty(filterState.SearchText)
                              || filterState.StatusFilter != null
                              || filterState.SourceFilter is { Count: > 0 };

        uiTreeView.BeginUpdate();
        _suppressCheckEvents = true;

        var totalIntents = _rootIntents.Count;
        var visibleIntentsCount = 0;

        try
        {
            uiTreeView.Nodes.Clear();
            _intentNodeMap.Clear();

            _boldFont?.Dispose();
            _boldFont = new(uiTreeView.Font, FontStyle.Bold);

            var intentsByMedia = _rootIntents
                .GroupBy(i => i.Media.Id)
                .OrderBy(g => g.Min(i => i.Sort))
                .ThenBy(g => g.First().Media.Title, NaturalStringComparer.Instance)
                .Select(g => g.OrderBy(x => x.From.TitleFull, NaturalStringComparer.Instance).ThenBy(x => x.Sort));

            foreach (var group in intentsByMedia)
            {
                var media = group.First().Media;

                if (!string.IsNullOrEmpty(filterState.SearchText) && !media.Title.Contains(filterState.SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (filterState.StatusFilter != null && media.Sources.All(s => s.Status != filterState.StatusFilter))
                {
                    continue;
                }

                if (filterState.SourceFilter is { Count: > 0 } && !media.Sources.Any(l => filterState.SourceFilter.Contains(l.SourceId)))
                {
                    continue;
                }

                var mediaNode = new TreeNode(media.Title)
                {
                    Tag = media,
                    Checked = group.All(i => i.IsSelected),
                    NodeFont = _boldFont,
                    StateImageKey = "blank",
                };

                var hasVisibleIntents = false;
                foreach (var intent in group)
                {
                    if (filterState.RelationFilter is { Count: > 0 } && !filterState.RelationFilter.Contains((intent.Relation.FromId, intent.Relation.ToId)))
                    {
                        continue;
                    }

                    mediaNode.Nodes.Add(CreateIntentNode(intent, _intentNodeMap, filterState.RelationFilter));
                    visibleIntentsCount++;
                    hasVisibleIntents = true;
                }

                if (hasVisibleIntents || !hasActiveFilter)
                {
                    uiTreeView.Nodes.Add(mediaNode);
                }
            }

            uiTreeView.ExpandAll();

            if (uiTreeView.Nodes.Count > 0)
            {
                uiTreeView.TopNode = uiTreeView.Nodes[0];
            }
        }
        finally
        {
            _suppressCheckEvents = false;
            uiTreeView.EndUpdate();
        }

        UpdateStatusLabel(hasActiveFilter
            ? $"Показано: {visibleIntentsCount} из {totalIntents} действий"
            : $"Всего действий: {totalIntents}");
    }

    private void SetAllNodesChecked(bool isChecked)
    {
        foreach (TreeNode node in uiTreeView.Nodes)
        {
            node.Checked = isChecked;
            SetChildrenRecursive(node, isChecked);
        }
    }

    private void SetChildrenRecursive(TreeNode node, bool isChecked)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = isChecked;
            SetChildrenRecursive(child, isChecked);
        }
    }

    private void SetParentsChecked(TreeNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            parent.Checked = true;
            parent = parent.Parent;
        }
    }

    private void InitializeImageList()
    {
        uiIconsImageList.Images.Add(CreateColorIcon(Color.DodgerBlue));
        uiIconsImageList.Images.Add(CreateColorIcon(Color.Orange));
        uiIconsImageList.Images.Add(CreateColorIcon(Color.Green));
        uiIconsImageList.Images.Add(CreateColorIcon(Color.Red));
    }
}
