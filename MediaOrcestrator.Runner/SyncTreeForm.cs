using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

// TODO: Облагородить и мб перенести в Tab
public class SyncTreeForm : Form
{
    private readonly TreeView _treeView;
    private readonly Button _executeButton;
    private readonly Orcestrator _orcestrator;
    private readonly ILogger<SyncTreeForm> _logger;
    private readonly List<SyncIntent> _rootIntents;

    public SyncTreeForm(Orcestrator orcestrator, List<SyncIntent> rootIntents, ILogger<SyncTreeForm> logger)
    {
        _orcestrator = orcestrator;
        _rootIntents = rootIntents;
        _logger = logger;

        Text = "Дерево синхронизации (Аудит)";
        Size = new(800, 600);
        StartPosition = FormStartPosition.CenterParent;

        _treeView = new()
        {
            Dock = DockStyle.Fill,
            CheckBoxes = true,
        };

        _executeButton = new()
        {
            Text = "Выполнить выбранное",
            Dock = DockStyle.Bottom,
            Height = 40,
        };

        _executeButton.Click += ExecuteButton_Click;

        Controls.Add(_treeView);
        Controls.Add(_executeButton);

        PopulateTree();
    }

    private void PopulateTree()
    {
        _treeView.Nodes.Clear();
        foreach (var node in _rootIntents.Select(CreateNode))
        {
            _treeView.Nodes.Add(node);
        }

        _treeView.ExpandAll();
    }

    private static TreeNode CreateNode(SyncIntent intent)
    {
        var node = new TreeNode(intent.ToString())
        {
            Tag = intent,
            Checked = intent.IsSelected,
        };

        foreach (var nextIntent in intent.NextIntents)
        {
            node.Nodes.Add(CreateNode(nextIntent));
        }

        return node;
    }

    private async void ExecuteButton_Click(object? sender, EventArgs e)
    {
        UpdateIntentsFromTree(_treeView.Nodes);

        var selectedRootIntents = _rootIntents.Where(i => i.IsSelected).ToList();
        if (selectedRootIntents.Count == 0)
        {
            MessageBox.Show("Ничего не выбрано.");
            return;
        }

        _executeButton.Enabled = false;
        _treeView.Enabled = false;

        try
        {
            foreach (var intent in selectedRootIntents)
            {
                try
                {
                    await ExecuteIntent(intent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при выполнении синхронизации для {Intent}", intent);
                }
            }

            MessageBox.Show("Процесс синхронизации завершен. Проверьте логи на наличие ошибок.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при выполнении синхронизации.");
            MessageBox.Show($"Критическая ошибка: {ex.Message}");
        }
        finally
        {
            _executeButton.Enabled = true;
            _treeView.Enabled = true;
        }
    }

    private static void UpdateIntentsFromTree(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is not SyncIntent intent)
            {
                continue;
            }

            intent.IsSelected = node.Checked;
            UpdateIntentsFromTree(node.Nodes);
        }
    }

    private async Task ExecuteIntent(SyncIntent intent)
    {
        if (!intent.IsSelected)
        {
            return;
        }

        _logger.LogInformation("Выполнение: {Intent}", intent);

        var fromMediaSource = intent.Media.Sources.FirstOrDefault(x => x.SourceId == intent.From.Id);
        if (fromMediaSource == null)
        {
            _logger.LogWarning("MediaSourceLink не найден для {SourceId} у медиа {MediaId}", intent.From.Id, intent.Media.Id);
            return;
        }

        await _orcestrator.TransferByRelation(intent.Media, intent.Relation, fromMediaSource.ExternalId);

        foreach (var nextIntent in intent.NextIntents)
        {
            await ExecuteIntent(nextIntent);
        }
    }
}
