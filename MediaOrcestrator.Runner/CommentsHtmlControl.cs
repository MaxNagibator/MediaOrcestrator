using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Timer = System.Windows.Forms.Timer;

namespace MediaOrcestrator.Runner;

public partial class CommentsHtmlControl : UserControl
{
    private const string AnySourceLabel = "(любой)";
    private const int SearchDebounceMs = 300;

    private readonly Timer _searchDebounce = new() { Interval = SearchDebounceMs };

    private readonly ConcurrentDictionary<(string SourceId, string ExternalMediaId), byte> _authorsInFlight = new();

    private Orcestrator? _orcestrator;
    private CommentsService? _commentsService;
    private ActionHolder? _actionHolder;
    private ILogger? _logger;
    private CommentsViewSettings _settings = new();
    private bool _loaded;
    private bool _suppressSettingsSave;
    private int _applyFiltersVersion;
    private CancellationTokenSource? _applyFiltersCts;

    public CommentsHtmlControl()
    {
        InitializeComponent();
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            ApplyFilters();
        };

        uiBrowserView.MediaRequested += (_, mediaId) => OpenMediaById(mediaId);
        uiBrowserView.FetchRequested += async (_, req) =>
        {
            try
            {
                await FetchForExternalAsync(req.SourceId, req.ExternalId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Не удалось обработать app-ссылку fetch ({Source}/{External})",
                    req.SourceId, req.ExternalId);
            }
        };

        uiBrowserView.OpenExternalRequested += (_, req) => OpenExternal(req.SourceId, req.ExternalId);
        uiBrowserView.OpenCommentExternalRequested += (_, req) => OpenCommentExternal(req.SourceId, req.ExternalMediaId, req.ExternalCommentId);

        uiBrowserView.MutationRequested += async (_, req) =>
        {
            try
            {
                await HandleMutationAsync(req);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Не удалось обработать операцию {Kind} ({Source}/{Media}/{Comment})",
                    req.Kind, req.SourceId, req.ExternalMediaId, req.ExternalCommentId);

                MessageBox.Show($"Не удалось выполнить операцию: {ex.Message}",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                ApplyFilters();
            }
        };
    }

    public void Initialize(
        Orcestrator orcestrator,
        CommentsService commentsService,
        ActionHolder actionHolder,
        ILogger logger)
    {
        _orcestrator = orcestrator;
        _commentsService = commentsService;
        _actionHolder = actionHolder;
        _logger = logger;

        _settings = CommentsViewSettings.Load();

        uiBrowserView.AuthorsResolver = ResolveCommentAuthors;

        using (Splash.Current.StartSpan("Список источников"))
        {
            ReloadSourcesCombo();
        }

        ApplySettingsToUi();

        using (Splash.Current.StartSpan("Прогрев браузера"))
        {
            uiBrowserView.Prewarm();
        }

        UpdateForceFetchButtonState();
    }

    public void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        ApplyFilters(warmAuthors: true);
    }

    private void uiSourceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        SaveSettings();
        UpdateForceFetchButtonState();
        ApplyFilters(warmAuthors: true);
    }

    private void uiReplyStatusComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        SaveSettings();
        ApplyFilters();
    }

    private void uiTabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressSettingsSave)
        {
            return;
        }

        ReparentBrowserToActiveTab();
        SaveSettings();
        ApplyFilters();
    }

    private void uiSearchTextBox_TextChanged(object? sender, EventArgs e)
    {
        SaveSettings();
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private void uiSearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        _searchDebounce.Stop();
        ApplyFilters();
    }

    private void uiFetchSettingsValueChanged(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    private async void uiForceFetchAllButton_Click(object? sender, EventArgs e)
    {
        if (_orcestrator == null || _commentsService == null || _actionHolder == null)
        {
            return;
        }

        if (uiSourceComboBox.SelectedItem is not SourceComboItem { SourceId: { } sourceId })
        {
            return;
        }

        var source = _orcestrator.GetSources().FirstOrDefault(s => s.Id == sourceId);
        if (source == null)
        {
            return;
        }

        using var dialog = new CommentsFetchOptionsDialog(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings.FetchSinceDays = dialog.SinceDays;
        _settings.FetchOnlyRecent = dialog.OnlyRecent;
        _settings.Save();

        await FetchAllForSourceAsync(source);
    }

    private static List<CommentRecord> LoadComments(
        Orcestrator orcestrator,
        CommentsService commentsService,
        string? sourceId,
        string search,
        int limit,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(search))
        {
            return commentsService.Query(sourceId, limit: limit);
        }

        var byText = commentsService.Query(sourceId, textContains: search, limit: limit);

        ct.ThrowIfCancellationRequested();

        var titleMatches = orcestrator.GetMedias()
            .Where(m => !string.IsNullOrEmpty(m.Title) && m.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            .SelectMany(m => m.Sources)
            .Where(l => !string.IsNullOrEmpty(l.SourceId) && !string.IsNullOrEmpty(l.ExternalId))
            .Where(l => sourceId == null || l.SourceId == sourceId)
            .Select(l => (l.SourceId, l.ExternalId))
            .Distinct()
            .ToList();

        if (titleMatches.Count == 0)
        {
            return byText;
        }

        var seen = new HashSet<string>(byText.Select(r => r.Id), StringComparer.Ordinal);
        var combined = new List<CommentRecord>(byText);

        foreach (var (sId, externalId) in titleMatches)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var record in commentsService.GetByMedia(sId, externalId))
            {
                if (seen.Add(record.Id))
                {
                    combined.Add(record);
                }
            }
        }

        return combined;
    }

    private static void BackfillOrphanParents(List<CommentRecord> fetched, CommentsService commentsService, CancellationToken ct)
    {
        if (fetched.Count == 0)
        {
            return;
        }

        var known = new HashSet<string>(fetched.Select(r => r.Id), StringComparer.Ordinal);
        var pending = new Queue<string>();

        foreach (var r in fetched)
        {
            if (string.IsNullOrEmpty(r.ParentExternalCommentId))
            {
                continue;
            }

            var parentId = $"{r.SourceId}|{r.ExternalMediaId}|{r.ParentExternalCommentId}";

            if (known.Add(parentId))
            {
                pending.Enqueue(parentId);
            }
        }

        var safetyLimit = fetched.Count * 2;

        while (pending.Count > 0 && safetyLimit-- > 0)
        {
            ct.ThrowIfCancellationRequested();

            var parentId = pending.Dequeue();
            var parent = commentsService.GetById(parentId);

            if (parent == null)
            {
                continue;
            }

            fetched.Add(parent);

            if (string.IsNullOrEmpty(parent.ParentExternalCommentId))
            {
                continue;
            }

            var grandparentId = $"{parent.SourceId}|{parent.ExternalMediaId}|{parent.ParentExternalCommentId}";

            if (known.Add(grandparentId))
            {
                pending.Enqueue(grandparentId);
            }
        }
    }

    private static List<CommentRecord> FilterByReplyStatus(
        List<CommentRecord> records,
        CommentsReplyStatusFilter filter)
    {
        if (filter == CommentsReplyStatusFilter.All || records.Count == 0)
        {
            return records;
        }

        var byId = new Dictionary<string, CommentRecord>(records.Count, StringComparer.Ordinal);
        foreach (var r in records)
        {
            byId.TryAdd(r.Id, r);
        }

        var childrenByParent = new Dictionary<string, List<CommentRecord>>(StringComparer.Ordinal);
        foreach (var r in records)
        {
            if (string.IsNullOrEmpty(r.ParentExternalCommentId))
            {
                continue;
            }

            var parentId = $"{r.SourceId}|{r.ExternalMediaId}|{r.ParentExternalCommentId}";
            if (!byId.ContainsKey(parentId))
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

        var result = new List<CommentRecord>();
        foreach (var root in records)
        {
            if (!IsRoot(root, byId))
            {
                continue;
            }

            if (root.IsAuthor)
            {
                continue;
            }

            var descendants = new List<CommentRecord>();
            CollectDescendants(root, childrenByParent, descendants);

            if (!MatchesReplyStatus(root, descendants, filter))
            {
                continue;
            }

            result.Add(root);
            result.AddRange(descendants);
        }

        return result;
    }

    private static bool IsRoot(CommentRecord r, Dictionary<string, CommentRecord> byId)
    {
        if (string.IsNullOrEmpty(r.ParentExternalCommentId))
        {
            return true;
        }

        var parentId = $"{r.SourceId}|{r.ExternalMediaId}|{r.ParentExternalCommentId}";
        return !byId.ContainsKey(parentId);
    }

    private static void CollectDescendants(
        CommentRecord node,
        Dictionary<string, List<CommentRecord>> childrenByParent,
        List<CommentRecord> output)
    {
        if (!childrenByParent.TryGetValue(node.Id, out var kids))
        {
            return;
        }

        foreach (var kid in kids)
        {
            output.Add(kid);
            CollectDescendants(kid, childrenByParent, output);
        }
    }

    private static bool MatchesReplyStatus(CommentRecord root, List<CommentRecord> descendants, CommentsReplyStatusFilter filter)
    {
        DateTime? latestAuthorReply = null;
        foreach (var d in descendants)
        {
            if (d.IsAuthor && (latestAuthorReply == null || d.PublishedAt > latestAuthorReply))
            {
                latestAuthorReply = d.PublishedAt;
            }
        }

        var hasAuthorReply = latestAuthorReply != null;

        return filter switch
        {
            CommentsReplyStatusFilter.WithoutReply => !hasAuthorReply,
            CommentsReplyStatusFilter.WithReply => hasAuthorReply,
            CommentsReplyStatusFilter.NewReplies => hasAuthorReply
                                                    && descendants.Any(d => !d.IsAuthor && d.PublishedAt > latestAuthorReply!.Value),
            CommentsReplyStatusFilter.WithoutReplyAndLike => !hasAuthorReply && !root.LikedByAuthor,
            _ => true,
        };
    }

    private static string BuildFilterSummary(int? sinceDays, int? takeRecent)
    {
        var parts = new List<string>(2);

        if (sinceDays != null)
        {
            parts.Add($"медиа не старше {sinceDays.Value} дн.");
        }

        if (takeRecent != null)
        {
            parts.Add($"только последние {takeRecent.Value}");
        }

        return string.Join(", ", parts);
    }

    private static DateTime? TryGetCreationDate(Media media)
    {
        var raw = media.Metadata.FirstOrDefault(m => m.Key == "CreationDate")?.Value;
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return DateTime.TryParse(raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static string? FindRootCommentId(IReadOnlyList<CommentRecord> comments, string externalCommentId)
    {
        var byId = comments.ToDictionary(c => c.ExternalCommentId);
        if (!byId.TryGetValue(externalCommentId, out var current))
        {
            return null;
        }

        if (string.IsNullOrEmpty(current.ParentExternalCommentId))
        {
            return null;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var rootId = current.ParentExternalCommentId;

        while (byId.TryGetValue(rootId!, out var parent)
               && !string.IsNullOrEmpty(parent.ParentExternalCommentId)
               && visited.Add(rootId!))
        {
            rootId = parent.ParentExternalCommentId;
        }

        return rootId;
    }

    private CommentsLayoutMode GetActiveLayoutMode()
    {
        return uiTabs.SelectedTab == uiFlatTab
            ? CommentsLayoutMode.Flat
            : CommentsLayoutMode.Grouped;
    }

    private void ReparentBrowserToActiveTab()
    {
        var target = uiTabs.SelectedTab == uiFlatTab ? uiFlatTab : uiGroupedTab;
        if (uiBrowserView.Parent == target)
        {
            return;
        }

        target.Controls.Add(uiBrowserView);
    }

    private void ApplySettingsToUi()
    {
        _suppressSettingsSave = true;
        try
        {
            uiSearchTextBox.Text = _settings.Search;
            uiLimitNumeric.Value = Math.Clamp(_settings.Limit, (int)uiLimitNumeric.Minimum, (int)uiLimitNumeric.Maximum);

            PopulateReplyStatusCombo();

            uiTabs.SelectedTab = _settings.LayoutMode == CommentsLayoutMode.Flat
                ? uiFlatTab
                : uiGroupedTab;

            ReparentBrowserToActiveTab();

            if (!string.IsNullOrEmpty(_settings.SelectedSourceId)
                && uiSourceComboBox.DataSource is List<SourceComboItem> items
                && items.FirstOrDefault(x => x.SourceId == _settings.SelectedSourceId) is { } match)
            {
                uiSourceComboBox.SelectedItem = match;
            }
        }
        finally
        {
            _suppressSettingsSave = false;
        }
    }

    private void PopulateReplyStatusCombo()
    {
        var items = new List<ReplyStatusComboItem>
        {
            new(CommentsReplyStatusFilter.WithoutReplyAndLike, "Без ответа и лайка"),
            new(CommentsReplyStatusFilter.All, "Все"),
            new(CommentsReplyStatusFilter.WithoutReply, "Без ответа"),
            new(CommentsReplyStatusFilter.WithReply, "С ответом"),
            new(CommentsReplyStatusFilter.NewReplies, "Новые ответы"),
        };

        uiReplyStatusComboBox.BeginUpdate();
        try
        {
            uiReplyStatusComboBox.DataSource = items;
            uiReplyStatusComboBox.DisplayMember = nameof(ReplyStatusComboItem.Label);
            uiReplyStatusComboBox.ValueMember = nameof(ReplyStatusComboItem.Key);

            var current = items.FirstOrDefault(x => x.Key == _settings.ReplyStatus) ?? items[0];
            uiReplyStatusComboBox.SelectedItem = current;
        }
        finally
        {
            uiReplyStatusComboBox.EndUpdate();
        }
    }

    private void SaveSettings()
    {
        if (_suppressSettingsSave)
        {
            return;
        }

        _settings.SelectedSourceId = (uiSourceComboBox.SelectedItem as SourceComboItem)?.SourceId;
        _settings.Search = uiSearchTextBox.Text;
        _settings.Limit = (int)uiLimitNumeric.Value;

        _settings.LayoutMode = GetActiveLayoutMode();

        if (uiReplyStatusComboBox.SelectedItem is ReplyStatusComboItem replyStatus)
        {
            _settings.ReplyStatus = replyStatus.Key;
        }

        _settings.Save();
    }

    private void ReloadSourcesCombo()
    {
        if (_orcestrator == null)
        {
            return;
        }

        var previousValue = uiSourceComboBox.SelectedItem as SourceComboItem;
        var sources = _orcestrator.GetSources();
        var items = new List<SourceComboItem>
        {
            new(null, AnySourceLabel),
        };

        foreach (var source in sources.OrderBy(s => s.TitleFull))
        {
            items.Add(new(source.Id, source.TitleFull));
        }

        uiSourceComboBox.BeginUpdate();
        try
        {
            uiSourceComboBox.DataSource = items;
            uiSourceComboBox.DisplayMember = nameof(SourceComboItem.Label);
            uiSourceComboBox.ValueMember = nameof(SourceComboItem.SourceId);

            if (previousValue?.SourceId != null
                && items.FirstOrDefault(x => x.SourceId == previousValue.SourceId) is { } match)
            {
                uiSourceComboBox.SelectedItem = match;
            }
            else
            {
                uiSourceComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            uiSourceComboBox.EndUpdate();
        }
    }

    private async void ApplyFilters(bool warmAuthors = false)
    {
        if (!_loaded || _orcestrator == null || _commentsService == null)
        {
            return;
        }

        var version = ++_applyFiltersVersion;

        _applyFiltersCts?.Cancel();
        _applyFiltersCts?.Dispose();
        _applyFiltersCts = new();
        var ct = _applyFiltersCts.Token;

        var orcestrator = _orcestrator;
        var commentsService = _commentsService;
        var sourceId = (uiSourceComboBox.SelectedItem as SourceComboItem)?.SourceId;
        var search = uiSearchTextBox.Text.Trim();
        var limit = (int)uiLimitNumeric.Value;
        var layout = GetActiveLayoutMode();
        var replyStatus = (uiReplyStatusComboBox.SelectedItem as ReplyStatusComboItem)?.Key ?? _settings.ReplyStatus;

        uiStatusLabel.Text = "Загрузка...";

        try
        {
            var (records, json, truncated) = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                var fetched = LoadComments(orcestrator, commentsService, sourceId, search, limit + 1, ct);

                ct.ThrowIfCancellationRequested();

                var isTruncated = fetched.Count > limit;
                if (isTruncated)
                {
                    fetched = fetched.Take(limit).ToList();
                }

                BackfillOrphanParents(fetched, commentsService, ct);

                fetched = FilterByReplyStatus(fetched, replyStatus);

                var renderJson = CommentsBrowserView.BuildRenderJson(orcestrator, fetched, new()
                {
                    Search = search,
                    Layout = layout,
                });

                return (Records: fetched, Json: renderJson, Truncated: isTruncated);
            }, ct);

            if (version != _applyFiltersVersion || IsDisposed)
            {
                return;
            }

            try
            {
                uiBrowserView.ApplyJson(json);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            uiStatusLabel.Text = records.Count == 0
                ? "Ничего не найдено"
                : truncated
                    ? $"Комментариев: ≥{records.Count} (увеличьте лимит)"
                    : $"Комментариев: {records.Count}";

            if (warmAuthors)
            {
                WarmAuthors(records);
            }
        }
        catch (OperationCanceledException)
        {
            if (version == _applyFiltersVersion && !IsDisposed)
            {
                uiStatusLabel.Text = "";
            }
        }
        catch (Exception ex)
        {
            if (version != _applyFiltersVersion || IsDisposed)
            {
                return;
            }

            _logger?.LogError(ex, "Не удалось загрузить комментарии");
            uiStatusLabel.Text = "Ошибка загрузки";
            MessageBox.Show($"Не удалось загрузить комментарии: {ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UpdateForceFetchButtonState()
    {
        var sourceItem = uiSourceComboBox.SelectedItem as SourceComboItem;
        uiForceFetchAllButton.Enabled = sourceItem?.SourceId != null;
    }

    private void OpenMediaById(string mediaId)
    {
        if (_orcestrator == null || _actionHolder == null)
        {
            return;
        }

        var media = _orcestrator.GetMedias().FirstOrDefault(m => m.Id == mediaId);
        if (media == null)
        {
            return;
        }

        var detail = new MediaDetailForm(media, _orcestrator, _actionHolder, _commentsService, _logger);
        detail.Show(this);
    }

    private void OpenExternal(string sourceId, string externalId)
    {
        if (_orcestrator == null)
        {
            return;
        }

        var source = _orcestrator.GetSources().FirstOrDefault(s => s.Id == sourceId);
        if (source?.Type == null)
        {
            return;
        }

        try
        {
            var metadata = ResolveMediaMetadata(sourceId, externalId);
            var uri = source.Type.GetExternalUri(externalId, source.Settings, metadata);
            if (uri == null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo(uri.ToString())
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось открыть медиа в источнике ({Source}/{External})",
                sourceId, externalId);
        }
    }

    private async void OpenCommentExternal(string sourceId, string externalMediaId, string externalCommentId)
    {
        if (_orcestrator == null || _commentsService == null)
        {
            return;
        }

        var source = _orcestrator.GetSources().FirstOrDefault(s => s.Id == sourceId);
        if (source?.Type is not ISupportsCommentPermalinks permalinks)
        {
            return;
        }

        var commentsService = _commentsService;
        var rootCommentId = await Task.Run(() =>
        {
            var allComments = commentsService.GetByMedia(sourceId, externalMediaId);
            return FindRootCommentId(allComments, externalCommentId);
        });

        if (IsDisposed)
        {
            return;
        }

        try
        {
            var metadata = ResolveMediaMetadata(sourceId, externalMediaId);
            var uri = permalinks.GetCommentExternalUri(externalMediaId,
                externalCommentId,
                rootCommentId,
                source.Settings,
                metadata);

            if (uri == null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo(uri.ToString())
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось открыть комментарий в источнике ({Source}/{Media}/{Comment})",
                sourceId, externalMediaId, externalCommentId);
        }
    }

    private IReadOnlyList<MetadataItem>? ResolveMediaMetadata(string sourceId, string externalMediaId)
    {
        if (_orcestrator == null)
        {
            return null;
        }

        foreach (var media in _orcestrator.GetMedias())
        {
            foreach (var link in media.Sources)
            {
                if (link.SourceId == sourceId && link.ExternalId == externalMediaId)
                {
                    return media.Metadata.ForSource(sourceId);
                }
            }
        }

        return null;
    }

    private async Task HandleMutationAsync(CommentMutationRequest request)
    {
        if (_orcestrator == null || _commentsService == null || _actionHolder == null)
        {
            return;
        }

        var source = _orcestrator.GetSources().FirstOrDefault(s => s.Id == request.SourceId);
        if (source == null)
        {
            return;
        }

        var isLikeOp = request.Kind is CommentMutationKind.Like or CommentMutationKind.Unlike;
        var supportsRequiredFeature = isLikeOp
            ? source.Type is ISupportsCommentLikes
            : source.Type is ISupportsCommentMutations;

        if (!supportsRequiredFeature)
        {
            MessageBox.Show(isLikeOp
                    ? "Источник не поддерживает лайки на комментариях."
                    : "Источник не поддерживает изменение комментариев.",
                "Операция недоступна",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        var media = _orcestrator.GetMedias()
            .FirstOrDefault(m => m.Sources.Any(l => l.SourceId == request.SourceId && l.ExternalId == request.ExternalMediaId));

        var link = media?.Sources.FirstOrDefault(l => l.SourceId == request.SourceId && l.ExternalId == request.ExternalMediaId);
        if (media == null || link == null)
        {
            MessageBox.Show("Медиа не найдено локально.",
                "Операция недоступна",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        var label = request.Kind switch
        {
            CommentMutationKind.Create => "Отправка комментария",
            CommentMutationKind.Edit => "Изменение комментария",
            CommentMutationKind.Delete => "Удаление комментария",
            CommentMutationKind.Restore => "Восстановление комментария",
            CommentMutationKind.Like => "Лайк комментария",
            CommentMutationKind.Unlike => "Снятие лайка",
            _ => "Комментарий",
        };

        var cts = new CancellationTokenSource();
        var action = _actionHolder.Register($"{label}: «{media.Title}»", "Запущена", 1, cts, kind: ActionKind.Comments);

        var patchedInPlace = false;

        try
        {
            switch (request.Kind)
            {
                case CommentMutationKind.Create:
                    {
                        var record = await Task.Run(() =>
                                _commentsService.CreateCommentAsync(source, link, request.ParentExternalCommentId, request.Text, request.AuthorId, cts.Token),
                            cts.Token);

                        var groupKey = CommentsBrowserView.BuildGroupKey(media, source.Id, link.ExternalId);
                        var parentCompositeId = string.IsNullOrEmpty(request.ParentExternalCommentId)
                            ? null
                            : $"{source.Id}|{link.ExternalId}|{request.ParentExternalCommentId}";

                        patchedInPlace = uiBrowserView.TryApplyCreate(_orcestrator, media, record, groupKey, parentCompositeId);
                        break;
                    }

                case CommentMutationKind.Edit:
                    {
                        var commentId = request.ExternalCommentId
                                        ?? throw new InvalidOperationException("Edit без commentId");

                        var record = await Task.Run(() =>
                                _commentsService.EditCommentAsync(source, link, commentId, request.Text, cts.Token),
                            cts.Token);

                        patchedInPlace = uiBrowserView.TryApplyEdit(record.Id, record.Text);
                        break;
                    }

                case CommentMutationKind.Delete:
                    {
                        var commentId = request.ExternalCommentId
                                        ?? throw new InvalidOperationException("Delete без commentId");

                        await Task.Run(() =>
                                _commentsService.DeleteCommentAsync(source, link, commentId, cts.Token),
                            cts.Token);

                        var recordId = $"{source.Id}|{link.ExternalId}|{commentId}";
                        patchedInPlace = uiBrowserView.TryApplyDeleted(recordId, true);
                        break;
                    }

                case CommentMutationKind.Restore:
                    {
                        var commentId = request.ExternalCommentId
                                        ?? throw new InvalidOperationException("Restore без commentId");

                        await Task.Run(() =>
                                _commentsService.RestoreCommentAsync(source, link, commentId, cts.Token),
                            cts.Token);

                        var recordId = $"{source.Id}|{link.ExternalId}|{commentId}";
                        patchedInPlace = uiBrowserView.TryApplyDeleted(recordId, false);
                        break;
                    }

                case CommentMutationKind.Like:
                case CommentMutationKind.Unlike:
                    {
                        var liked = request.Kind == CommentMutationKind.Like;
                        var commentId = request.ExternalCommentId
                                        ?? throw new InvalidOperationException($"{request.Kind} без commentId");

                        var newCount = await Task.Run(() =>
                                _commentsService.LikeCommentAsync(source, link, commentId, liked, cts.Token),
                            cts.Token);

                        var recordId = $"{source.Id}|{link.ExternalId}|{commentId}";
                        patchedInPlace = uiBrowserView.TryApplyLikeUpdate(recordId, liked, newCount);
                        break;
                    }
            }

            action.Status = $"{source.TitleFull}: ок";
        }
        finally
        {
            action.ProgressPlus();
            action.Finish();
        }

        if (!patchedInPlace)
        {
            ApplyFilters();
        }
    }

    private IReadOnlyList<CommentAuthorView> ResolveCommentAuthors(string sourceId, string externalMediaId)
    {
        TriggerAuthorsLoad(sourceId, externalMediaId);
        return [];
    }

    private void TriggerAuthorsLoad(string sourceId, string externalMediaId)
    {
        if (_orcestrator == null || _commentsService == null)
        {
            return;
        }

        var key = (sourceId, externalMediaId);
        if (!_authorsInFlight.TryAdd(key, 0))
        {
            return;
        }

        var orcestrator = _orcestrator;
        var commentsService = _commentsService;

        _ = Task.Run(async () =>
        {
            try
            {
                var source = orcestrator.GetSources().FirstOrDefault(s => s.Id == sourceId);
                if (source?.Type is not ISupportsCommentAuthors)
                {
                    return;
                }

                var link = orcestrator.GetMedias()
                    .SelectMany(m => m.Sources)
                    .FirstOrDefault(l => l.SourceId == sourceId && l.ExternalId == externalMediaId);

                if (link == null)
                {
                    return;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var authors = await commentsService.GetAvailableAuthorsAsync(source, link, cts.Token);
                var view = authors
                    .Select(a => new CommentAuthorView(a.Id, a.Name, a.AvatarUrl, a.IsDefault))
                    .ToList();

                NotifyAuthorsReady(sourceId, externalMediaId, view);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Превышено время ожидания списка авторов для {Source}/{Media}", sourceId, externalMediaId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Не удалось получить список авторов для {Source}/{Media}", sourceId, externalMediaId);
            }
            finally
            {
                _authorsInFlight.TryRemove(key, out _);
            }
        });
    }

    private void NotifyAuthorsReady(string sourceId, string externalMediaId, IReadOnlyList<CommentAuthorView> view)
    {
        if (IsDisposed)
        {
            return;
        }

        try
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => uiBrowserView.NotifyAuthors(sourceId, externalMediaId, view));
            }
            else
            {
                uiBrowserView.NotifyAuthors(sourceId, externalMediaId, view);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void WarmAuthors(IReadOnlyList<CommentRecord> records)
    {
        if (_orcestrator == null)
        {
            return;
        }

        const int maxWarmPerCall = 20;
        var sourcesById = _orcestrator.GetSources().ToDictionary(s => s.Id);

        var keys = records
            .Select(r => (r.SourceId, r.ExternalMediaId))
            .Distinct()
            .Where(key => sourcesById.TryGetValue(key.SourceId, out var src) && src.Type is ISupportsCommentAuthors)
            .Take(maxWarmPerCall);

        foreach (var key in keys)
        {
            TriggerAuthorsLoad(key.SourceId, key.ExternalMediaId);
        }
    }

    private async Task FetchForExternalAsync(string sourceId, string externalId)
    {
        if (_orcestrator == null || _commentsService == null || _actionHolder == null)
        {
            return;
        }

        var source = _orcestrator.GetSources().FirstOrDefault(s => s.Id == sourceId);
        if (source == null || source.Type is not ISupportsComments)
        {
            return;
        }

        var media = _orcestrator.GetMedias()
            .FirstOrDefault(m => m.Sources.Any(l => l.SourceId == sourceId && l.ExternalId == externalId));

        var link = media?.Sources.FirstOrDefault(l => l.SourceId == sourceId && l.ExternalId == externalId);
        if (media == null || link == null)
        {
            MessageBox.Show("Медиа не найдено локально — нечего обновлять.",
                "Загрузка комментариев",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        var cts = new CancellationTokenSource();
        var action = _actionHolder.Register($"Комментарии: «{media.Title}»", "Запущена", 1, cts, kind: ActionKind.Comments);

        uiFetchProgressBar.Style = ProgressBarStyle.Marquee;
        uiFetchProgressBar.Visible = true;
        uiStatusLabel.Text = $"Загрузка комментариев: «{media.Title}»...";

        IProgress<string> progress = new Progress<string>(text =>
        {
            uiStatusLabel.Text = text;
            action.Status = text;
        });

        try
        {
            await Task.Run(async () =>
            {
                var count = await _commentsService.RefreshAsync(source, media, link, progress, cts.Token);
                action.Status = $"{source.TitleFull}: {count}";
            }, cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            uiStatusLabel.Text = "Загрузка отменена";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось загрузить комментарии для «{Title}»", media.Title);
            uiStatusLabel.Text = "Ошибка загрузки";
        }
        finally
        {
            action.ProgressPlus();
            action.Finish();
            uiFetchProgressBar.Visible = false;
            uiFetchProgressBar.Style = ProgressBarStyle.Blocks;
        }

        if (!TryApplyFetchedGroup(media, link))
        {
            ApplyFilters();
        }
    }

    private bool TryApplyFetchedGroup(Media media, MediaSourceLink link)
    {
        if (_orcestrator == null || _commentsService == null)
        {
            return false;
        }

        try
        {
            var records = _commentsService.GetByMedia(link.SourceId, link.ExternalId);
            BackfillOrphanParents(records, _commentsService, CancellationToken.None);

            var replyStatus = (uiReplyStatusComboBox.SelectedItem as ReplyStatusComboItem)?.Key ?? _settings.ReplyStatus;
            records = FilterByReplyStatus(records, replyStatus);

            var groupKey = CommentsBrowserView.BuildGroupKey(media, link.SourceId, link.ExternalId);
            var options = new CommentsRenderOptions
            {
                Search = uiSearchTextBox.Text.Trim(),
                Layout = GetActiveLayoutMode(),
            };

            return uiBrowserView.TryApplyFetched(_orcestrator, records, groupKey, options);
        }
        catch
        {
            return false;
        }
    }

    private async Task FetchAllForSourceAsync(Source source)
    {
        if (_orcestrator == null || _commentsService == null || _actionHolder == null)
        {
            return;
        }

        if (source.Type is not ISupportsComments)
        {
            MessageBox.Show($"Источник «{source.TitleFull}» не поддерживает комментарии.",
                "Невозможно загрузить",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        var sinceDays = _settings.FetchSinceDays > 0 ? (int?)_settings.FetchSinceDays : null;
        var since = sinceDays != null ? DateTime.UtcNow.AddDays(-sinceDays.Value) : (DateTime?)null;

        var takeRecent = _settings.FetchOnlyRecent > 0 ? (int?)_settings.FetchOnlyRecent : null;

        var allForSource = _orcestrator.GetMedias()
            .SelectMany(media => media.Sources
                .Where(link => link.SourceId == source.Id && !string.IsNullOrEmpty(link.ExternalId))
                .Select(link => (Media: media, Link: link)))
            .OrderByDescending(media => media.Link.SortNumber)
            .ToList();

        IEnumerable<(Media Media, MediaSourceLink Link)> filtered = allForSource;

        if (since != null)
        {
            filtered = filtered.Where(t => TryGetCreationDate(t.Media) is { } d && d >= since.Value);
        }

        if (takeRecent != null)
        {
            filtered = filtered.OrderByDescending(t => t.Link.SortNumber).Take(takeRecent.Value);
        }

        var targets = filtered.ToList();
        var hasFilters = since != null || takeRecent != null;
        var filterSummary = BuildFilterSummary(sinceDays, takeRecent);

        if (targets.Count == 0)
        {
            MessageBox.Show(hasFilters
                    ? $"В источнике «{source.TitleFull}» нет медиа, попадающих под фильтры ({filterSummary})."
                    : $"В источнике «{source.TitleFull}» нет медиа для загрузки.",
                "Нечего загружать",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        uiForceFetchAllButton.Enabled = false;
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var action = _actionHolder.Register($"Загрузка комментариев из «{source.TitleFull}»",
            "Запущена",
            targets.Count,
            cts,
            kind: ActionKind.Comments);

        uiFetchProgressBar.Style = ProgressBarStyle.Blocks;
        uiFetchProgressBar.Maximum = targets.Count;
        uiFetchProgressBar.Value = 0;
        uiFetchProgressBar.Visible = true;
        uiFetchCounterLabel.Text = $"0 / {targets.Count}";
        uiFetchCounterLabel.Visible = true;
        uiStatusLabel.Text = $"Загрузка комментариев из «{source.TitleFull}»...";

        var ok = 0;
        var failed = 0;
        var processed = 0;

        IProgress<FetchProgress> reporter = new Progress<FetchProgress>(p =>
        {
            uiFetchProgressBar.Value = p.Processed;
            uiStatusLabel.Text = p.StatusText;
            uiFetchCounterLabel.Text = p.CounterText;
        });

        var counterLock = new object();

        try
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3,
                CancellationToken = token,
            };

            await Parallel.ForEachAsync(targets, parallelOptions, async (item, ct) =>
            {
                var (media, link) = item;

                int currentIndex;
                string startStatus;
                lock (counterLock)
                {
                    currentIndex = processed + 1;
                    startStatus = $"[{currentIndex}/{targets.Count}] Загрузка: «{media.Title}»";
                    var startCounter = failed > 0
                        ? $"✓ {ok}  ✗ {failed}  /  {targets.Count}"
                        : $"✓ {ok}  /  {targets.Count}";

                    reporter.Report(new(processed, startStatus, startCounter));
                }

                action.Status = $"{media.Title}: загрузка...";

                try
                {
                    var count = await _commentsService.RefreshAsync(source, media, link, null, ct);

                    lock (counterLock)
                    {
                        ok++;
                    }

                    action.Status = $"{media.Title}: {count}";
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lock (counterLock)
                    {
                        failed++;
                    }

                    _logger?.LogError(ex, "Не удалось загрузить комментарии для «{Title}» ({SourceId}/{ExternalId})",
                        media.Title, link.SourceId, link.ExternalId);
                }

                action.ProgressPlus();

                lock (counterLock)
                {
                    processed++;
                    var counter = failed > 0
                        ? $"✓ {ok}  ✗ {failed}  /  {targets.Count}"
                        : $"✓ {ok}  /  {targets.Count}";

                    reporter.Report(new(processed, startStatus, counter));
                }
            });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger?.LogInformation("Загрузка комментариев отменена для «{Source}»", source.TitleFull);
        }
        finally
        {
            var summary = $"Источник «{source.TitleFull}»: успешно {ok} из {targets.Count}";
            if (failed > 0)
            {
                summary += $", ошибок {failed}";
            }

            action.Finish(summary);
            uiFetchProgressBar.Visible = false;
            uiFetchCounterLabel.Visible = false;
        }

        ApplyFilters();
        UpdateForceFetchButtonState();
    }

    private sealed record FetchProgress(int Processed, string StatusText, string CounterText);

    private sealed class SourceComboItem(string? sourceId, string label)
    {
        public string? SourceId { get; } = sourceId;
        public string Label { get; } = label;

        public override string ToString()
        {
            return Label;
        }
    }

    private sealed class ReplyStatusComboItem(CommentsReplyStatusFilter key, string label)
    {
        public CommentsReplyStatusFilter Key { get; } = key;
        public string Label { get; } = label;

        public override string ToString()
        {
            return Label;
        }
    }
}
