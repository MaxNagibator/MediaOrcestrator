using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Modules;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaOrcestrator.Runner;

public sealed record CommentsRenderOptions
{
    public string Search { get; init; } = "";
    public CommentsLayoutMode Layout { get; init; } = CommentsLayoutMode.Grouped;
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
    string Text,
    string? AuthorId = null);

public sealed record CommentAuthorView(
    string Id,
    string Name,
    string? Avatar,
    bool IsDefault);

public delegate IReadOnlyList<CommentAuthorView> CommentAuthorsResolver(string sourceId, string externalMediaId);

public sealed partial class CommentsBrowserView : UserControl
{
    private const string TemplateResourceName = "MediaOrcestrator.Runner.Resources.comments.template.html";
    private const string StylesResourceName = "MediaOrcestrator.Runner.Resources.comments.template.css";
    private const string ScriptResourceName = "MediaOrcestrator.Runner.Resources.comments.template.js";

    private static readonly string HtmlTemplate = LoadTemplate();

    private static readonly string EmptyDataJson = """{"search":"","groups":[]}""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private bool _prewarmed;

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

    public CommentAuthorsResolver? AuthorsResolver { get; set; }

    public static string BuildGroupKey(Media? media, string sourceId, string externalMediaId)
    {
        return media?.Id ?? $"__orphan__|{sourceId}|{externalMediaId}";
    }

    public static string BuildRenderJson(
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
                var ordered = OrderForRender(allRecords);
                var replyCounts = CountDescendants(allRecords);
                var thumbnailBySourceId = BuildThumbnailMap(media);

                var comments = ordered
                    .Select(r => new CommentDto(r.Id,
                        ComposedParentId(r),
                        string.IsNullOrEmpty(r.AuthorName) ? null : r.AuthorName,
                        string.IsNullOrEmpty(r.AuthorAvatarUrl) ? null : r.AuthorAvatarUrl,
                        r.PublishedAt.ToLocalTime().ToString("g"),
                        r.PublishedAt.ToUniversalTime().ToString("o"),
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
                        r.LikedByMe,
                        HasAuthors(sourcesById, r.SourceId),
                        title,
                        thumbnailBySourceId.GetValueOrDefault(r.SourceId),
                        replyCounts.GetValueOrDefault(r.Id),
                        HasExternalLink(sourcesById, r.SourceId, r.ExternalMediaId)))
                    .ToList();

                var sources = grp
                    .OrderBy(s => s.SourceTitle, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new MediaSourceDto(s.SourceId,
                        s.ExternalMediaId,
                        s.SourceTitle,
                        s.Records.Count,
                        HasExternalLink(sourcesById, s.SourceId, s.ExternalMediaId),
                        HasMutations(sourcesById, s.SourceId),
                        HasAuthors(sourcesById, s.SourceId)))
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

        var groups = groupsRaw
            .OrderByDescending(g => g.SortNumber)
            .ThenByDescending(g => g.Group.MediaTitle, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Group)
            .ToList();

        return JsonSerializer.Serialize(new RenderData(options.Search,
                options.Layout.ToString().ToLowerInvariant(),
                groups),
            JsonOptions);
    }

    public void Prewarm()
    {
        if (_prewarmed)
        {
            return;
        }

        _prewarmed = true;
        uiWebBrowser.DocumentText = HtmlTemplate.Replace("{{data}}", EscapeForInlineScript(EmptyDataJson));
    }

    public bool TryApplyLikeUpdate(string commentRecordId, bool likedByMe, int likeCount)
    {
        return InvokeApply("__applyLike", commentRecordId, likedByMe, likeCount);
    }

    public void NotifyAuthors(string sourceId, string externalMediaId, IReadOnlyList<CommentAuthorView> authors)
    {
        var json = JsonSerializer.Serialize(authors, JsonOptions);
        InvokeApply("__setAuthors", sourceId, externalMediaId, json);
    }

    public bool TryApplyFetched(
        Orcestrator orcestrator,
        IReadOnlyList<CommentRecord> records,
        string groupKey,
        CommentsRenderOptions options)
    {
        try
        {
            var json = BuildRenderJson(orcestrator, records, options);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("groups", out var groupsArray))
            {
                return false;
            }

            foreach (var group in groupsArray.EnumerateArray())
            {
                if (group.TryGetProperty("key", out var keyElement)
                    && keyElement.GetString() == groupKey)
                {
                    var groupJson = group.GetRawText();
                    return InvokeApply("__applyGroup", groupKey, groupJson);
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
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
        Media? media,
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
            var mediaTitle = media?.Title ?? $"<{newRecord.ExternalMediaId}>";
            var thumbnailMap = BuildThumbnailMap(media);
            var dto = MapRecordToDto(newRecord,
                sourcesById,
                sourceTitles,
                mediaTitle,
                thumbnailMap.GetValueOrDefault(newRecord.SourceId));

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
        var json = BuildRenderJson(orcestrator, records, options);
        ApplyJson(json);
    }

    public void ApplyJson(string json)
    {
        if (InvokeApply("__applyAll", json))
        {
            return;
        }

        _prewarmed = true;
        uiWebBrowser.DocumentText = HtmlTemplate.Replace("{{data}}", EscapeForInlineScript(json));
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

    private static string EscapeForInlineScript(string json)
    {
        return json.Replace("</", @"<\/", StringComparison.Ordinal);
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

    private static bool HasAuthors(Dictionary<string, Source> sourcesById, string sourceId)
    {
        return sourcesById.TryGetValue(sourceId, out var source) && source.Type is ISupportsCommentAuthors;
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

    private static Dictionary<string, string?> BuildThumbnailMap(Media? media)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (media == null)
        {
            return result;
        }

        string? fallback = null;
        foreach (var item in media.Metadata)
        {
            if (item.Key != "PreviewUrl" || string.IsNullOrEmpty(item.Value))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(item.SourceId))
            {
                result[item.SourceId] = item.Value;
            }

            fallback ??= item.Value;
        }

        if (fallback != null)
        {
            foreach (var link in media.Sources)
            {
                if (!string.IsNullOrEmpty(link.SourceId) && !result.ContainsKey(link.SourceId))
                {
                    result[link.SourceId] = fallback;
                }
            }
        }

        return result;
    }

    private static Dictionary<string, int> CountDescendants(IReadOnlyList<CommentRecord> records)
    {
        var byId = new Dictionary<string, CommentRecord>(records.Count, StringComparer.Ordinal);
        foreach (var r in records)
        {
            byId.TryAdd(r.Id, r);
        }

        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var r in records)
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

            bucket.Add(r.Id);
        }

        var counts = new Dictionary<string, int>(records.Count, StringComparer.Ordinal);
        foreach (var r in records)
        {
            counts[r.Id] = CountRecursive(r.Id, childrenByParent);
        }

        return counts;
    }

    private static int CountRecursive(string id, Dictionary<string, List<string>> childrenByParent)
    {
        if (!childrenByParent.TryGetValue(id, out var children))
        {
            return 0;
        }

        var total = children.Count;
        foreach (var child in children)
        {
            total += CountRecursive(child, childrenByParent);
        }

        return total;
    }

    private static string? ComposedParentId(CommentRecord r)
    {
        if (string.IsNullOrEmpty(r.ParentExternalCommentId))
        {
            return null;
        }

        return $"{r.SourceId}|{r.ExternalMediaId}|{r.ParentExternalCommentId}";
    }

    private static List<CommentRecord> OrderForRender(IEnumerable<CommentRecord> source)
    {
        var list = source.ToList();
        var byId = new Dictionary<string, CommentRecord>(list.Count);
        foreach (var r in list)
        {
            byId.TryAdd(r.Id, r);
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

        var sortedRoots = list
            .Where(r =>
            {
                var parentId = ComposedParentId(r);
                return parentId == null || !byId.ContainsKey(parentId);
            })
            .OrderByDescending(r => r.PublishedAt);

        var result = new List<CommentRecord>(list.Count);
        foreach (var root in sortedRoots)
        {
            AppendRecursive(result, root, childrenByParent);
        }

        return result;
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
        var skeleton = ReadEmbeddedResource(TemplateResourceName);
        var styles = ReadEmbeddedResource(StylesResourceName);
        var script = ReadEmbeddedResource(ScriptResourceName);

        return skeleton
            .Replace("{{styles}}", styles)
            .Replace("{{script}}", script);
    }

    private static string ReadEmbeddedResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"Не найден embedded ресурс {name}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string LoadAuthorsJson(string? sourceId, string? externalMediaId)
    {
        if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(externalMediaId))
        {
            return "[]";
        }

        var resolver = AuthorsResolver;
        if (resolver == null)
        {
            return "[]";
        }

        try
        {
            var authors = resolver(sourceId, externalMediaId);
            return JsonSerializer.Serialize(authors, JsonOptions);
        }
        catch
        {
            return "[]";
        }
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
        Dictionary<string, string> sourceTitles,
        string mediaTitle,
        string? mediaThumbnailUrl)
    {
        return new(r.Id,
            ComposedParentId(r),
            string.IsNullOrEmpty(r.AuthorName) ? null : r.AuthorName,
            string.IsNullOrEmpty(r.AuthorAvatarUrl) ? null : r.AuthorAvatarUrl,
            r.PublishedAt.ToLocalTime().ToString("g"),
            r.PublishedAt.ToUniversalTime().ToString("o"),
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
            r.LikedByMe,
            HasAuthors(sourcesById, r.SourceId),
            mediaTitle,
            mediaThumbnailUrl,
            0,
            HasExternalLink(sourcesById, r.SourceId, r.ExternalMediaId));
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
            OnActionAs(kind, sourceId, externalMediaId, externalCommentId, parentExternalCommentId, text, string.Empty);
        }

        public void OnActionAs(string kind, string sourceId, string externalMediaId, string externalCommentId, string parentExternalCommentId, string text, string authorId)
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
                text ?? string.Empty,
                string.IsNullOrEmpty(authorId) ? null : authorId);

            _owner.RaiseMutation(request);
        }

        public string GetAuthors(string sourceId, string externalMediaId)
        {
            return _owner.LoadAuthorsJson(sourceId, externalMediaId);
        }
    }

    private sealed record RenderData(string Search, string Layout, List<GroupDto> Groups);

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
        bool HasMutations,
        bool HasAuthors);

    private sealed record CommentDto(
        string Id,
        string? Parent,
        string? Author,
        string? Avatar,
        string Date,
        string DateIso,
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
        bool LikedByMe,
        bool HasAuthors,
        string MediaTitle,
        string? MediaThumbnailUrl,
        int ReplyCount,
        bool HasMediaExternalLink);
}
