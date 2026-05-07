using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Modules;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaOrcestrator.Runner;

public enum MediaSort
{
    ByTitle = 0,
    BySortNumber = 1,
}

public enum CommentSort
{
    OldestFirst = 0,
    NewestFirst = 1,
    MostLiked = 2,
    MostReplied = 3,
    NoAuthorReply = 4,
}

public sealed record CommentsRenderOptions
{
    public string Search { get; init; } = "";
    public CommentSort CommentSort { get; init; } = CommentSort.OldestFirst;
    public bool InvertCommentSort { get; init; }
    public MediaSort MediaSort { get; init; } = MediaSort.ByTitle;
    public bool InvertMediaSort { get; init; }
    public string MediaSearch { get; init; } = "";
}

public sealed record CommentsBrowserFetchRequest(string SourceId, string ExternalId);

public sealed record CommentsBrowserCommentRequest(string SourceId, string ExternalMediaId, string ExternalCommentId);

public enum CommentMutationKind
{
    None = 0,
    Create = 1,
    Edit = 2,
    Delete = 3,
    Restore = 4,
    Like = 5,
    Unlike = 6,
}

public sealed record CommentMutationRequest(
    CommentMutationKind Kind,
    string SourceId,
    string ExternalMediaId,
    string? ExternalCommentId,
    string? ParentExternalCommentId,
    string Text);

public sealed partial class CommentsBrowserView : UserControl
{
    private const string TemplateResourceName = "MediaOrcestrator.Runner.Resources.comments.template.html";

    private static readonly string HtmlTemplate = LoadTemplate();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public CommentsBrowserView()
    {
        InitializeComponent();
        ScriptingBridge bridge = new(this);
        uiWebBrowser.ObjectForScripting = bridge;
    }

    public event EventHandler<CommentsBrowserFetchRequest>? FetchRequested;

    public event EventHandler<string>? MediaRequested;

    public event EventHandler<CommentMutationRequest>? MutationRequested;

    public event EventHandler<CommentsBrowserCommentRequest>? OpenCommentExternalRequested;

    public event EventHandler<CommentsBrowserFetchRequest>? OpenExternalRequested;

    public static string BuildGroupKey(Media? media, string sourceId, string externalMediaId)
    {
        return media?.Id ?? $"__orphan__|{sourceId}|{externalMediaId}";
    }

    public bool TryApplyLikeUpdate(string commentRecordId, bool likedByMe, int likeCount)
    {
        return InvokeApply("__applyLike", commentRecordId, likedByMe, likeCount);
    }

    public bool TryApplyEdit(string commentRecordId, string newText)
    {
        return InvokeApply("__applyEdit", commentRecordId, newText);
    }

    public bool TryApplyDeleted(string commentRecordId, bool isDeleted)
    {
        return InvokeApply("__applyDeleted", commentRecordId, isDeleted);
    }

    public bool TryApplyCreate(
        Orcestrator orcestrator,
        CommentRecord newRecord,
        string groupKey,
        string? parentCompositeId)
    {
        var doc = uiWebBrowser.Document;
        if (doc == null)
        {
            return false;
        }

        try
        {
            var sourcesById = orcestrator.GetSources().ToDictionary(x => x.Id);
            var sourceTitles = sourcesById.ToDictionary(x => x.Key, x => x.Value.TitleFull);
            var dto = MapRecordToDto(newRecord, sourcesById, sourceTitles);
            var json = JsonSerializer.Serialize(dto, JsonOptions);

            var result = doc.InvokeScript("__applyCreate",
                [json, groupKey, parentCompositeId ?? string.Empty]);

            return result is true;
        }
        catch
        {
            return false;
        }
    }

    public void Render(
        Orcestrator orcestrator,
        IReadOnlyList<CommentRecord> records,
        CommentsRenderOptions? options = null)
    {
        options ??= new();

        var sourcesById = orcestrator.GetSources().ToDictionary(x => x.Id);
        var sourceTitles = sourcesById.ToDictionary(x => x.Key, x => x.Value.TitleFull);
        var linkByKey = new Dictionary<(string SourceId, string ExternalId), (Media Media, int SortNumber)>();

        foreach (var media in orcestrator.GetMedias())
        {
            foreach (var link in media.Sources)
            {
                if (link is { SourceId: not null, ExternalId: not null })
                {
                    linkByKey[(link.SourceId, link.ExternalId)] = (media, link.SortNumber);
                }
            }
        }

        var perSource = records
            .GroupBy(r => (r.SourceId, r.ExternalMediaId))
            .Select(g =>
            {
                linkByKey.TryGetValue(g.Key, out var info);
                return new
                {
                    g.Key.SourceId,
                    g.Key.ExternalMediaId,
                    info.Media,
                    SortNumber = info.Media != null ? info.SortNumber : int.MaxValue,
                    SourceTitle = sourceTitles.GetValueOrDefault(g.Key.SourceId) ?? g.Key.SourceId,
                    Records = g.ToList(),
                };
            })
            .ToList();

        var groupsRaw = perSource
            .GroupBy(s => s.Media?.Id ?? $"__orphan__|{s.SourceId}|{s.ExternalMediaId}")
            .Select(grp =>
            {
                var first = grp.First();
                var media = first.Media;
                var title = media?.Title ?? $"<{first.ExternalMediaId}>";

                var allRecords = grp.SelectMany(s => s.Records).ToList();
                var ordered = OrderForRender(allRecords, options.CommentSort, options.InvertCommentSort);

                var comments = ordered
                    .Select(r => new CommentDto(r.Id,
                        ComposedParentId(r),
                        string.IsNullOrEmpty(r.AuthorName) ? null : r.AuthorName,
                        string.IsNullOrEmpty(r.AuthorAvatarUrl) ? null : r.AuthorAvatarUrl,
                        r.PublishedAt.ToLocalTime().ToString("g"),
                        r.LikeCount.GetValueOrDefault(),
                        r.IsDeleted,
                        r.Text,
                        r.IsAuthor,
                        r.LikedByAuthor,
                        sourceTitles.GetValueOrDefault(r.SourceId) ?? r.SourceId,
                        r.SourceId,
                        r.ExternalMediaId,
                        r.ExternalCommentId,
                        HasCommentPermalink(sourcesById, r),
                        HasMutations(sourcesById, r.SourceId),
                        r.CanEdit,
                        r.CanDelete,
                        HasLikes(sourcesById, r.SourceId),
                        r.LikedByMe))
                    .ToList();

                var sources = grp
                    .OrderBy(s => s.SourceTitle, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new MediaSourceDto(s.SourceId,
                        s.ExternalMediaId,
                        s.SourceTitle,
                        s.Records.Count,
                        HasExternalLink(sourcesById, s.SourceId, s.ExternalMediaId),
                        HasMutations(sourcesById, s.SourceId)))
                    .ToList();

                var dto = new GroupDto(grp.Key,
                    media?.Id,
                    title,
                    media == null,
                    allRecords.Count,
                    sources,
                    comments);

                var sortNumber = media != null ? grp.Min(s => s.SortNumber) : int.MaxValue;
                return (Group: dto, SortNumber: sortNumber);
            })
            .ToList();

        if (!string.IsNullOrEmpty(options.MediaSearch))
        {
            groupsRaw = groupsRaw
                .Where(g => g.Group.MediaTitle.Contains(options.MediaSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        IEnumerable<(GroupDto Group, int SortNumber)> sortedGroups = options.MediaSort switch
        {
            MediaSort.BySortNumber => groupsRaw
                .OrderBy(g => g.SortNumber)
                .ThenBy(g => g.Group.MediaTitle, StringComparer.OrdinalIgnoreCase),
            _ => groupsRaw.OrderBy(g => g.Group.MediaTitle, StringComparer.OrdinalIgnoreCase),
        };

        if (options.InvertMediaSort)
        {
            sortedGroups = sortedGroups.Reverse();
        }

        var groups = sortedGroups.Select(g => g.Group).ToList();

        var json = JsonSerializer.Serialize(new RenderData(options.Search, groups), JsonOptions);
        uiWebBrowser.DocumentText = HtmlTemplate.Replace("{{data}}", EscapeForInlineScript(json));
    }

    private static string EscapeForInlineScript(string json)
    {
        return json.Replace("</", @"<\/", StringComparison.Ordinal);
    }

    private void uiWebBrowser_Navigating(object? sender, WebBrowserNavigatingEventArgs e)
    {
        var url = e.Url;
        if (url == null || !string.Equals(url.Scheme, "app", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        var host = url.Host;
        var segments = url.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        switch (host)
        {
            case "media" when segments.Length >= 1:
                MediaRequested?.Invoke(this, segments[0]);
                break;

            case "fetch" when segments.Length >= 2:
                FetchRequested?.Invoke(this, new(segments[0], segments[1]));
                break;

            case "open" when segments.Length >= 2:
                OpenExternalRequested?.Invoke(this, new(segments[0], segments[1]));
                break;

            case "comment" when segments.Length >= 3:
                OpenCommentExternalRequested?.Invoke(this, new(segments[0], segments[1], segments[2]));
                break;
        }
    }

    private static bool HasExternalLink(Dictionary<string, Source> sourcesById, string sourceId, string externalId)
    {
        if (string.IsNullOrEmpty(externalId))
        {
            return false;
        }

        if (!sourcesById.TryGetValue(sourceId, out var source) || source.Type == null)
        {
            return false;
        }

        try
        {
            return source.Type.GetExternalUri(externalId, source.Settings) != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasMutations(Dictionary<string, Source> sourcesById, string sourceId)
    {
        return sourcesById.TryGetValue(sourceId, out var source) && source.Type is ISupportsCommentMutations;
    }

    private static bool HasLikes(Dictionary<string, Source> sourcesById, string sourceId)
    {
        return sourcesById.TryGetValue(sourceId, out var source) && source.Type is ISupportsCommentLikes;
    }

    private static bool HasCommentPermalink(Dictionary<string, Source> sourcesById, CommentRecord record)
    {
        if (string.IsNullOrEmpty(record.ExternalMediaId) || string.IsNullOrEmpty(record.ExternalCommentId))
        {
            return false;
        }

        return sourcesById.TryGetValue(record.SourceId, out var source)
               && source.Type is ISupportsCommentPermalinks;
    }

    private static string? ComposedParentId(CommentRecord r)
    {
        if (string.IsNullOrEmpty(r.ParentExternalCommentId))
        {
            return null;
        }

        return $"{r.SourceId}|{r.ExternalMediaId}|{r.ParentExternalCommentId}";
    }

    private static List<CommentRecord> OrderForRender(IEnumerable<CommentRecord> source, CommentSort sort, bool invert)
    {
        var list = source.ToList();
        var byId = new Dictionary<string, CommentRecord>(list.Count);
        foreach (var r in list)
        {
            byId[r.Id] = r;
        }

        var childrenByParent = new Dictionary<string, List<CommentRecord>>();
        foreach (var r in list)
        {
            var parentId = ComposedParentId(r);
            if (parentId == null || !byId.ContainsKey(parentId))
            {
                continue;
            }

            if (!childrenByParent.TryGetValue(parentId, out var bucket))
            {
                bucket = [];
                childrenByParent[parentId] = bucket;
            }

            bucket.Add(r);
        }

        foreach (var bucket in childrenByParent.Values)
        {
            bucket.Sort((a, b) => a.PublishedAt.CompareTo(b.PublishedAt));
        }

        var roots = list
            .Where(r =>
            {
                var parentId = ComposedParentId(r);
                return parentId == null || !byId.ContainsKey(parentId);
            })
            .ToList();

        var stats = new Dictionary<string, (int DescendantCount, bool HasAuthorReply)>(roots.Count);
        foreach (var root in roots)
        {
            stats[root.Id] = CollectStats(root, childrenByParent);
        }

        IEnumerable<CommentRecord> sortedRoots = sort switch
        {
            CommentSort.NewestFirst => roots.OrderByDescending(r => r.PublishedAt),
            CommentSort.MostLiked => roots
                .OrderByDescending(r => r.LikeCount.GetValueOrDefault())
                .ThenByDescending(r => r.PublishedAt),
            CommentSort.MostReplied => roots
                .OrderByDescending(r => stats[r.Id].DescendantCount)
                .ThenByDescending(r => r.PublishedAt),
            CommentSort.NoAuthorReply => roots
                .OrderBy(r => stats[r.Id].HasAuthorReply ? 1 : 0)
                .ThenBy(r => r.PublishedAt),
            _ => roots.OrderBy(r => r.PublishedAt),
        };

        if (invert)
        {
            sortedRoots = sortedRoots.Reverse();
        }

        var result = new List<CommentRecord>(list.Count);
        foreach (var root in sortedRoots)
        {
            AppendRecursive(result, root, childrenByParent);
        }

        return result;
    }

    private static (int DescendantCount, bool HasAuthorReply) CollectStats(
        CommentRecord root,
        Dictionary<string, List<CommentRecord>> childrenByParent)
    {
        var count = 0;
        var hasAuthor = false;
        var stack = new Stack<CommentRecord>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!childrenByParent.TryGetValue(current.Id, out var kids))
            {
                continue;
            }

            foreach (var kid in kids)
            {
                count++;
                if (kid.IsAuthor)
                {
                    hasAuthor = true;
                }

                stack.Push(kid);
            }
        }

        return (count, hasAuthor);
    }

    private static void AppendRecursive(
        List<CommentRecord> output,
        CommentRecord node,
        Dictionary<string, List<CommentRecord>> childrenByParent)
    {
        output.Add(node);
        if (!childrenByParent.TryGetValue(node.Id, out var kids))
        {
            return;
        }

        foreach (var kid in kids)
        {
            AppendRecursive(output, kid, childrenByParent);
        }
    }

    private static string LoadTemplate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
                           ?? throw new InvalidOperationException($"Не найден embedded ресурс {TemplateResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private bool InvokeApply(string functionName, params object[] args)
    {
        var doc = uiWebBrowser.Document;
        if (doc == null)
        {
            return false;
        }

        try
        {
            var result = doc.InvokeScript(functionName, args);
            return result is true;
        }
        catch
        {
            return false;
        }
    }

    private CommentDto MapRecordToDto(
        CommentRecord r,
        Dictionary<string, Source> sourcesById,
        Dictionary<string, string> sourceTitles)
    {
        return new(r.Id,
            ComposedParentId(r),
            string.IsNullOrEmpty(r.AuthorName) ? null : r.AuthorName,
            string.IsNullOrEmpty(r.AuthorAvatarUrl) ? null : r.AuthorAvatarUrl,
            r.PublishedAt.ToLocalTime().ToString("g"),
            r.LikeCount.GetValueOrDefault(),
            r.IsDeleted,
            r.Text,
            r.IsAuthor,
            r.LikedByAuthor,
            sourceTitles.GetValueOrDefault(r.SourceId) ?? r.SourceId,
            r.SourceId,
            r.ExternalMediaId,
            r.ExternalCommentId,
            HasCommentPermalink(sourcesById, r),
            HasMutations(sourcesById, r.SourceId),
            r.CanEdit,
            r.CanDelete,
            HasLikes(sourcesById, r.SourceId),
            r.LikedByMe);
    }

    private void RaiseMutation(CommentMutationRequest request)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => MutationRequested?.Invoke(this, request));
            return;
        }

        MutationRequested?.Invoke(this, request);
    }

    [ComVisible(true)]
    public sealed class ScriptingBridge
    {
        private readonly CommentsBrowserView _owner;

        internal ScriptingBridge(CommentsBrowserView owner)
        {
            _owner = owner;
        }

        public void OnAction(string kind, string sourceId, string externalMediaId, string externalCommentId, string parentExternalCommentId, string text)
        {
            if (!Enum.TryParse<CommentMutationKind>(kind, true, out var parsed))
            {
                return;
            }

            var request = new CommentMutationRequest(parsed,
                sourceId ?? string.Empty,
                externalMediaId ?? string.Empty,
                string.IsNullOrEmpty(externalCommentId) ? null : externalCommentId,
                string.IsNullOrEmpty(parentExternalCommentId) ? null : parentExternalCommentId,
                text ?? string.Empty);

            _owner.RaiseMutation(request);
        }
    }

    private sealed record RenderData(string Search, List<GroupDto> Groups);

    private sealed record GroupDto(
        string Key,
        string? MediaId,
        string MediaTitle,
        bool MediaMissing,
        int Total,
        List<MediaSourceDto> Sources,
        List<CommentDto> Comments);

    private sealed record MediaSourceDto(
        string SourceId,
        string ExternalId,
        string SourceTitle,
        int Count,
        bool HasExternalLink,
        bool HasMutations);

    private sealed record CommentDto(
        string Id,
        string? Parent,
        string? Author,
        string? Avatar,
        string Date,
        int Likes,
        bool Deleted,
        string? Text,
        bool IsAuthor,
        bool LikedByAuthor,
        string? Source,
        string SourceId,
        string ExternalMediaId,
        string ExternalCommentId,
        bool HasPermalink,
        bool HasMutations,
        bool CanEdit,
        bool CanDelete,
        bool HasLikes,
        bool LikedByMe);
}
