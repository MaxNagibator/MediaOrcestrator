using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class SyncTreeControl : UserControl
{
    private const int IconPending = 0;
    private const int IconWorking = 1;
    private const int IconOk = 2;
    private const int IconError = 3;

    private Orcestrator? _orcestrator;
    private ILogger<SyncTreeControl>? _logger;
    private List<SyncIntent>? _rootIntents;
    private CancellationTokenSource? _cts;

    public SyncTreeControl()
    {
        InitializeComponent();
        InitializeImageList();
        uiTreeView.CheckBoxes = true;
    }

    public void Initialize(List<SyncIntent> rootIntents, Orcestrator orcestrator, ILogger<SyncTreeControl> logger)
    {
        _orcestrator = orcestrator;
        _logger = logger;
        _rootIntents = rootIntents;
        PopulateTree();
        LogToUi("Планировщик инициализирован. Готов к работе.");
    }

    private void uiSelectAllButton_Click(object sender, EventArgs e)
    {
        SetAllNodesChecked(true);
    }

    private void uiDeselectAllButton_Click(object sender, EventArgs e)
    {
        SetAllNodesChecked(false);
    }

    private async void uiExecuteButton_Click(object? sender, EventArgs e)
    {
        if (_rootIntents == null || _orcestrator == null || _logger == null)
        {
            return;
        }

        UpdateIntentsFromTree(uiTreeView.Nodes);

        var selectedRootIntents = _rootIntents.Where(i => i.IsSelected).ToList();
        if (selectedRootIntents.Count == 0)
        {
            MessageBox.Show("Ничего не выбрано.");
            return;
        }

        uiExecuteButton.Enabled = false;
        uiStopButton.Enabled = true;
        uiTreeView.Enabled = false;

        _cts = new();

        LogToUi($"Запуск синхронизации для {selectedRootIntents.Count} цепочек...", Color.Yellow);

        try
        {
            foreach (var intent in selectedRootIntents.TakeWhile(_ => !_cts.IsCancellationRequested))
            {
                UpdateStatusLabel($"Обработка: {intent.Media.Title}");

                try
                {
                    await ExecuteIntent(intent, GetNodeForIntent(uiTreeView.Nodes, intent), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    LogToUi("Синхронизация прервана пользователем.", Color.Orange);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при выполнении синхронизации для {Intent}", intent);
                    LogToUi($"Ошибка для {intent.Media.Title}: {ex.Message}", Color.Red);
                }
            }

            if (_cts.IsCancellationRequested)
            {
                UpdateStatusLabel("Остановлено");
                LogToUi("Процесс был остановлен.", Color.Orange);
            }
            else
            {
                UpdateStatusLabel("Завершено");
                LogToUi("Процесс синхронизации полностью завершен.", Color.LightGreen);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при выполнении синхронизации.");
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

    private static TreeNode CreateIntentNode(SyncIntent intent)
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

        foreach (var nextIntent in intent.NextIntents)
        {
            node.Nodes.Add(CreateIntentNode(nextIntent));
        }

        return node;
    }

    private static void UpdateIntentsFromTree(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is SyncIntent intent)
            {
                intent.IsSelected = node.Checked;
            }

            UpdateIntentsFromTree(node.Nodes);
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

    private TreeNode? GetNodeForIntent(TreeNodeCollection nodes, SyncIntent targetIntent)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag == targetIntent)
            {
                return node;
            }

            var childNode = GetNodeForIntent(node.Nodes, targetIntent);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }

    private async Task ExecuteIntent(SyncIntent intent, TreeNode? node, CancellationToken ct)
    {
        if (_orcestrator == null || _logger == null || !intent.IsSelected)
        {
            return;
        }

        ct.ThrowIfCancellationRequested();

        UpdateNodeState(node, IconWorking, Color.Orange, $"[В работе] {intent.From.TitleFull} -> {intent.To.TitleFull}");
        LogToUi($"Выполнение: {intent.Media.Title} ({intent.From.TypeId} -> {intent.To.TypeId})");

        try
        {
            var fromMediaSource = intent.Media.Sources.FirstOrDefault(x => x.SourceId == intent.From.Id);
            if (fromMediaSource == null)
            {
                throw new($"MediaSourceLink не найден для {intent.From.Id}");
            }

            await _orcestrator.TransferByRelation(intent.Media, intent.Relation, fromMediaSource.ExternalId);
            ct.ThrowIfCancellationRequested();

            UpdateNodeState(node, IconOk, Color.Green, $"[OK] {intent.From.TitleFull} -> {intent.To.TitleFull}");
            LogToUi($"[Успех] {intent.Media.Title} передан в {intent.To.Title}", Color.LightGreen);

            foreach (var nextIntent in intent.NextIntents)
            {
                await ExecuteIntent(nextIntent, GetNodeForIntent(node?.Nodes ?? uiTreeView.Nodes, nextIntent), ct);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            UpdateNodeState(node, IconError, Color.Red, $"[Ошибка] {intent.From.TitleFull} -> {intent.To.TitleFull}");
            throw;
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
        uiTreeView.Nodes.Clear();
        if (_rootIntents == null)
        {
            return;
        }

        var intentsByMedia = _rootIntents.GroupBy(i => i.Media.Id);

        foreach (var group in intentsByMedia)
        {
            var media = group.First().Media;
            var mediaNode = new TreeNode(media.Title)
            {
                Tag = media,
                Checked = group.All(i => i.IsSelected),
                NodeFont = new(uiTreeView.Font, FontStyle.Bold),
            };

            foreach (var intent in group)
            {
                mediaNode.Nodes.Add(CreateIntentNode(intent));
            }

            uiTreeView.Nodes.Add(mediaNode);
        }

        uiTreeView.ExpandAll();
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

    private void InitializeImageList()
    {
        uiIconsImageList.Images.Add(CreateColorIcon(Color.DodgerBlue));
        uiIconsImageList.Images.Add(CreateColorIcon(Color.Orange));
        uiIconsImageList.Images.Add(CreateColorIcon(Color.Green));
        uiIconsImageList.Images.Add(CreateColorIcon(Color.Red));
    }
}
