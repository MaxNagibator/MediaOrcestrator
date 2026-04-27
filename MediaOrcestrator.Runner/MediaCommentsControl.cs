using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class MediaCommentsControl : UserControl
{
    private readonly Font _groupFont;
    private readonly Font _regularFont;

    private CommentsService? _commentsService;
    private ILogger? _logger;

    public MediaCommentsControl()
    {
        InitializeComponent();

        _groupFont = new(Font.FontFamily, 9.5f, FontStyle.Bold);
        _regularFont = new(Font.FontFamily, 9, FontStyle.Regular);
    }

    public void Initialize(
        Media media,
        Dictionary<string, Source> sourceDict,
        CommentsService? commentsService,
        ILogger? logger)
    {
        _commentsService = commentsService;
        _logger = logger;

        uiContentFlow.Controls.Clear();

        if (_commentsService == null)
        {
            uiContentFlow.Controls.Add(BuildPlaceholder("Сервис комментариев недоступен"));
            return;
        }

        var commentSources = media.Sources
            .Select(link => new
            {
                Link = link,
                Source = sourceDict.GetValueOrDefault(link.SourceId),
            })
            .Where(x => x.Source?.Type is ISupportsComments)
            .ToList();

        if (commentSources.Count == 0)
        {
            uiContentFlow.Controls.Add(BuildPlaceholder("Источники этого медиа не предоставляют комментарии"));
            return;
        }

        foreach (var item in commentSources)
        {
            uiContentFlow.Controls.Add(BuildSection(media, item.Source!, item.Link));
        }
    }

    private void uiContentFlow_Resize(object? sender, EventArgs e)
    {
        uiContentFlow.SuspendLayout();
        var width = uiContentFlow.ClientSize.Width - uiContentFlow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
        foreach (Control child in uiContentFlow.Controls)
        {
            child.Width = Math.Max(200, width);
        }

        uiContentFlow.ResumeLayout();
    }

    private static TreeNode BuildNode(CommentRecord record, Dictionary<string, List<CommentRecord>> byParent)
    {
        var likes = record.LikeCount > 0 ? $"  ♥ {record.LikeCount}" : "";
        var when = record.PublishedAt.ToLocalTime().ToString("g");
        var text = record.IsDeleted ? "[удалён]" : (record.Text ?? string.Empty).Replace("\r", " ").Replace("\n", " ");

        var node = new TreeNode($"{record.AuthorName} · {when}{likes}\n  {text}");

        if (!byParent.TryGetValue(record.ExternalCommentId, out var children))
        {
            return node;
        }

        foreach (var child in children)
        {
            node.Nodes.Add(BuildNode(child, byParent));
        }

        return node;
    }

    private Label BuildPlaceholder(string text)
    {
        return new()
        {
            Text = text,
            ForeColor = Color.Gray,
            AutoSize = true,
            Padding = new(8),
            Font = _regularFont,
        };
    }

    private GroupBox BuildSection(Media media, Source source, MediaSourceLink link)
    {
        var groupBox = new GroupBox
        {
            Text = source.TitleFull,
            Font = _groupFont,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Padding = new(8, 4, 8, 8),
            MinimumSize = new(0, 180),
        };

        var statusLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = _regularFont,
            Location = new(8, 22),
        };

        var refreshButton = new Button
        {
            Text = "Обновить",
            AutoSize = true,
            Location = new(8, 46),
        };

        var tree = new TreeView
        {
            Font = _regularFont,
            Location = new(8, 78),
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            HideSelection = false,
            Width = 700,
            Height = 350,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        groupBox.Controls.Add(statusLabel);
        groupBox.Controls.Add(refreshButton);
        groupBox.Controls.Add(tree);

        UpdateStatus(statusLabel, link);
        ReloadTree(tree, link);

        refreshButton.Click += async (_, _) => await OnRefreshClick(refreshButton, statusLabel, tree, source, media, link);

        return groupBox;
    }

    private async Task OnRefreshClick(
        Button refreshButton,
        Label statusLabel,
        TreeView tree,
        Source source,
        Media media,
        MediaSourceLink link)
    {
        if (_commentsService == null)
        {
            return;
        }

        refreshButton.Enabled = false;
        statusLabel.Text = "Загрузка...";

        try
        {
            await Task.Run(() => _commentsService.RefreshAsync(source, media, link));
            UpdateStatus(statusLabel, link);
            ReloadTree(tree, link);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка загрузки комментариев из {Source}", source.TitleFull);
            statusLabel.Text = $"Ошибка: {ex.Message}";
            statusLabel.ForeColor = Color.Firebrick;
        }
        finally
        {
            refreshButton.Enabled = true;
        }
    }

    private void UpdateStatus(Label label, MediaSourceLink link)
    {
        if (link.CommentsFetchedAt == null)
        {
            label.Text = "Кэш пуст. Нажмите «Обновить», чтобы загрузить.";
            label.ForeColor = Color.Gray;
            return;
        }

        var fetched = link.CommentsFetchedAt.Value.ToLocalTime();
        var stale = _commentsService?.IsStale(link) == true ? " (устарел)" : "";
        label.Text = $"Комментариев: {link.CommentsCount ?? 0}, обновлён: {fetched:g}{stale}";
        label.ForeColor = Color.FromArgb(100, 100, 100);
    }

    private void ReloadTree(TreeView tree, MediaSourceLink link)
    {
        tree.BeginUpdate();
        try
        {
            tree.Nodes.Clear();

            if (_commentsService == null)
            {
                return;
            }

            var records = _commentsService.GetCached(link);
            if (records.Count == 0)
            {
                return;
            }

            var byParent = records
                .GroupBy(r => r.ParentExternalCommentId ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.PublishedAt).ToList());

            if (!byParent.TryGetValue(string.Empty, out var roots))
            {
                return;
            }

            foreach (var root in roots)
            {
                tree.Nodes.Add(BuildNode(root, byParent));
            }
        }
        finally
        {
            tree.EndUpdate();
        }
    }
}
