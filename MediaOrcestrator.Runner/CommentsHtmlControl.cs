using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;

namespace MediaOrcestrator.Runner;

public partial class CommentsHtmlControl : UserControl
{
    private const string AnySourceLabel = "(любой)";
    private const int SearchDebounceMs = 300;

    private readonly Timer _searchDebounce = new() { Interval = SearchDebounceMs };

    private Orcestrator? _orcestrator;
    private CommentsService? _commentsService;
    private ActionHolder? _actionHolder;
    private ILogger? _logger;
    private bool _invertMediaSort;
    private bool _invertCommentSort;
    private bool _loaded;

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

        using (Splash.Current.StartSpan("Сортировки"))
        {
            PopulateSortCombos();
        }

        using (Splash.Current.StartSpan("Список источников"))
        {
            ReloadSourcesCombo();
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
        ApplyFilters();
    }

    private void uiSortChanged(object? sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void uiRefreshButton_Click(object? sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void uiSourceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateForceFetchButtonState();
        ApplyFilters();
    }

    private void uiSearchTextBox_TextChanged(object? sender, EventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private void uiMediaSearchTextBox_TextChanged(object? sender, EventArgs e)
    {
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

    private void uiMediaSortInvertButton_Click(object? sender, EventArgs e)
    {
        _invertMediaSort = !_invertMediaSort;
        uiMediaSortInvertButton.Text = _invertMediaSort ? "↑" : "↓";
        ApplyFilters();
    }

    private void uiCommentSortInvertButton_Click(object? sender, EventArgs e)
    {
        _invertCommentSort = !_invertCommentSort;
        uiCommentSortInvertButton.Text = _invertCommentSort ? "↑" : "↓";
        ApplyFilters();
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

        await FetchAllForSourceAsync(source);
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

    private void PopulateSortCombos()
    {
        uiMediaSortComboBox.BeginUpdate();
        try
        {
            uiMediaSortComboBox.DataSource = new List<SortItem<MediaSort>>
            {
                new(MediaSort.ByTitle, "По названию"),
                new(MediaSort.BySortNumber, "По номеру (SortNumber)"),
            };

            uiMediaSortComboBox.DisplayMember = nameof(SortItem<MediaSort>.Label);
            uiMediaSortComboBox.SelectedIndex = 0;
        }
        finally
        {
            uiMediaSortComboBox.EndUpdate();
        }

        uiCommentSortComboBox.BeginUpdate();
        try
        {
            uiCommentSortComboBox.DataSource = new List<SortItem<CommentSort>>
            {
                new(CommentSort.OldestFirst, "Старые сверху"),
                new(CommentSort.NewestFirst, "Новые сверху"),
                new(CommentSort.MostLiked, "Популярные сверху"),
                new(CommentSort.MostReplied, "Обсуждаемые сверху"),
                new(CommentSort.NoAuthorReply, "Без ответа автора сверху"),
            };

            uiCommentSortComboBox.DisplayMember = nameof(SortItem<CommentSort>.Label);
            uiCommentSortComboBox.SelectedIndex = 0;
        }
        finally
        {
            uiCommentSortComboBox.EndUpdate();
        }
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

    private void ApplyFilters()
    {
        if (!_loaded || _orcestrator == null || _commentsService == null)
        {
            return;
        }

        try
        {
            var sourceItem = uiSourceComboBox.SelectedItem as SourceComboItem;
            var search = uiSearchTextBox.Text.Trim();
            var limit = (int)uiLimitNumeric.Value;

            var records = _commentsService.Query(sourceItem?.SourceId,
                textContains: string.IsNullOrEmpty(search) ? null : search,
                limit: limit + 1);

            var truncated = records.Count > limit;
            if (truncated)
            {
                records = records.Take(limit).ToList();
            }

            var mediaSort = (uiMediaSortComboBox.SelectedItem as SortItem<MediaSort>)?.Value ?? MediaSort.ByTitle;
            var commentSort = (uiCommentSortComboBox.SelectedItem as SortItem<CommentSort>)?.Value ?? CommentSort.OldestFirst;
            var mediaSearch = uiMediaSearchTextBox.Text.Trim();

            uiBrowserView.Render(_orcestrator, records, new()
            {
                Search = search,
                CommentSort = commentSort,
                InvertCommentSort = _invertCommentSort,
                MediaSort = mediaSort,
                InvertMediaSort = _invertMediaSort,
                MediaSearch = mediaSearch,
            });

            uiCountLabel.Text = truncated
                ? $"Найдено: ≥{records.Count} (увеличьте лимит)"
                : $"Найдено: {records.Count}";

            uiStatusLabel.Text = records.Count == 0
                ? "Ничего не найдено"
                : $"Комментариев: {records.Count}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось загрузить комментарии");
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
            var uri = source.Type.GetExternalUri(externalId, source.Settings);
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

    private void OpenCommentExternal(string sourceId, string externalMediaId, string externalCommentId)
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

        var allComments = _commentsService.GetByMedia(sourceId, externalMediaId);
        var rootCommentId = FindRootCommentId(allComments, externalCommentId);

        try
        {
            var uri = permalinks.GetCommentExternalUri(externalMediaId,
                externalCommentId,
                rootCommentId,
                source.Settings);

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
        var action = _actionHolder.Register($"{label}: «{media.Title}»", "Запущена", 1, cts);

        var patchedInPlace = false;

        try
        {
            switch (request.Kind)
            {
                case CommentMutationKind.Create:
                    {
                        var record = await Task.Run(() =>
                                _commentsService.CreateCommentAsync(source, link, request.ParentExternalCommentId, request.Text, cts.Token),
                            cts.Token);

                        var groupKey = CommentsBrowserView.BuildGroupKey(media, source.Id, link.ExternalId);
                        var parentCompositeId = string.IsNullOrEmpty(request.ParentExternalCommentId)
                            ? null
                            : $"{source.Id}|{link.ExternalId}|{request.ParentExternalCommentId}";

                        patchedInPlace = uiBrowserView.TryApplyCreate(_orcestrator, record, groupKey, parentCompositeId);
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
        var action = _actionHolder.Register($"Комментарии: «{media.Title}»", "Запущена", 1, cts);

        try
        {
            await Task.Run(async () =>
            {
                var count = await _commentsService.RefreshAsync(source, media, link, null, cts.Token);
                action.Status = $"{source.TitleFull}: {count}";
            }, cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось загрузить комментарии для «{Title}»", media.Title);
        }
        finally
        {
            action.ProgressPlus();
            action.Finish();
        }

        ApplyFilters();
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

        var targets = _orcestrator.GetMedias()
            .SelectMany(media => media.Sources
                .Where(link => link.SourceId == source.Id && !string.IsNullOrEmpty(link.ExternalId))
                .Select(link => (Media: media, Link: link)))
            .ToList();

        if (targets.Count == 0)
        {
            MessageBox.Show($"В источнике «{source.TitleFull}» нет медиа для загрузки.",
                "Нечего загружать",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        var confirm = MessageBox.Show($"Загрузить комментарии для всех медиа источника «{source.TitleFull}»? Будет обработано {targets.Count} медиа.",
            "Подтверждение",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.OK)
        {
            return;
        }

        uiForceFetchAllButton.Enabled = false;
        var cts = new CancellationTokenSource();
        var action = _actionHolder.Register($"Загрузка комментариев из «{source.TitleFull}»",
            "Запущена",
            targets.Count,
            cts);

        var ok = 0;
        var failed = 0;

        try
        {
            await Task.Run(async () =>
            {
                foreach (var (media, link) in targets)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    action.Status = $"{media.Title}: загрузка...";

                    try
                    {
                        var count = await _commentsService.RefreshAsync(source, media, link, null, cts.Token);
                        ok++;
                        action.Status = $"{media.Title}: {count}";
                    }
                    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger?.LogError(ex, "Не удалось загрузить комментарии для «{Title}» ({SourceId}/{ExternalId})",
                            media.Title, link.SourceId, link.ExternalId);
                    }

                    action.ProgressPlus();
                }
            }, cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            _logger?.LogInformation("Загрузка комментариев отменена для «{Source}»", source.TitleFull);
        }
        finally
        {
            action.Finish();
        }

        ApplyFilters();
        UpdateForceFetchButtonState();

        var summary = $"Источник «{source.TitleFull}»: успешно {ok} из {targets.Count}";
        if (failed > 0)
        {
            summary += $", ошибок {failed}";
        }

        MessageBox.Show(summary,
            "Загрузка завершена",
            MessageBoxButtons.OK,
            failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private sealed class SourceComboItem(string? sourceId, string label)
    {
        public string? SourceId { get; } = sourceId;
        public string Label { get; } = label;

        public override string ToString()
        {
            return Label;
        }
    }

    private sealed class SortItem<T>(T value, string label) where T : Enum
    {
        public T Value { get; } = value;
        public string Label { get; } = label;

        public override string ToString()
        {
            return Label;
        }
    }
}
