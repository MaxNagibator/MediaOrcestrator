using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Domain.Merging;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl
{
    private Orcestrator? _orcestrator;
    private SyncRetryRunner? _retryRunner;
    private ILogger<MediaMatrixGridControl>? _logger;
    private BatchRenameService? _batchRenameService;
    private BatchPreviewService? _batchPreviewService;
    private CoverGenerator? _coverGenerator;
    private CoverTemplateStore? _coverTemplateStore;
    private MediaMergeService? _mergeService;
    private ActionHolder _actionHolder;
    private CommentsService? _commentsService;
    private ILoggerFactory? _loggerFactory;
    private CancellationTokenSource? _convertCts;

    public MediaMatrixGridControl()
    {
        InitializeComponent();
        uiFilterControl.FilterChanged += (_, _) => RefreshData();
        uiCancelConvertItem.Click += (_, _) => _convertCts?.Cancel();
    }

    public void Initialize(MediaMatrixContext context)
    {
        _orcestrator = context.Orcestrator;
        _retryRunner = context.RetryRunner;
        _logger = context.Logger;
        _batchRenameService = context.BatchRenameService;
        _batchPreviewService = context.BatchPreviewService;
        _coverGenerator = context.CoverGenerator;
        _coverTemplateStore = context.CoverTemplateStore;
        _mergeService = context.MergeService;
        _actionHolder = context.ActionHolder;
        _commentsService = context.CommentsService;
        _loggerFactory = context.LoggerFactory;

        using (Splash.Current.StartSpan("Фильтр"))
        {
            uiFilterControl.SetSettingsManager(context.SettingsManager);
            uiFilterControl.PopulateRelationsFilter(context.Orcestrator);
        }
    }

    public void PopulateRelationsFilter()
    {
        if (_orcestrator != null)
        {
            uiFilterControl.PopulateRelationsFilter(_orcestrator);
        }
    }

    public async void RefreshData(List<SourceSyncRelation>? selectedRelations = null)
    {
        if (_orcestrator == null)
        {
            return;
        }

        _logger?.LogInformation("Начато обновление списка медиа...");
        UpdateLoadingIndicator(true);

        try
        {
            var filterState = uiFilterControl.BuildFilterState(selectedRelations);

            var (mediaData, sources, allMediaCount) = await Task.Run(() =>
            {
                var allMedia = _orcestrator.GetMedias();
                var allSources = _orcestrator.GetSources();
                var (filteredMedia, filteredSources) = ApplyFilters(allMedia, allSources, filterState);
                return (filteredMedia, filteredSources, allMedia.Count);
            });

            var allMetadataColumns = BuildMetadataColumns(mediaData, sources);

            uiFilterControl.UpdateMetadataFilter(allMetadataColumns);
            var selectedColumnIds = uiFilterControl.GetSelectedMetadataFields();
            var selectedMetadata = allMetadataColumns.Where(c => selectedColumnIds.Contains(c.ColumnId)).ToList();

            uiMediaGrid.SaveState();
            uiMediaGrid.SetupColumns(sources, selectedMetadata);
            uiMediaGrid.PopulateGrid(sources, mediaData, selectedMetadata);
            uiMediaGrid.RestoreState();
            UpdateStatusBar(allMediaCount, mediaData.Count);

            _logger?.LogInformation("Список медиа успешно обновлен. Отображается: {Count} из {Total}", mediaData.Count, allMediaCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при обновлении списка медиа.");
        }
        finally
        {
            UpdateLoadingIndicator(false);
        }
    }

    private void uiMediaGrid_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var ht = uiMediaGrid.HitTest(e.X, e.Y);
        if (ht.Type != DataGridViewHitTestType.Cell || ht.RowIndex < 0)
        {
            return;
        }

        var row = uiMediaGrid.Rows[ht.RowIndex];
        if (!row.Selected)
        {
            uiMediaGrid.ClearSelection();
            row.Selected = true;
        }

        var selectedMedia = uiMediaGrid.GetSelectedMedia();
        var screenLocation = uiMediaGrid.PointToScreen(e.Location);

        if (selectedMedia.Count > 1)
        {
            ShowBatchContextMenu(selectedMedia, screenLocation);
            return;
        }

        if (uiMediaGrid.GetMediaAtRow(ht.RowIndex) is { } media)
        {
            Source? specificSource = null;
            var sources = uiMediaGrid.CurrentSources;
            if (sources != null)
            {
                var sourceStartIdx = uiMediaGrid.Columns.Count - sources.Count;
                if (ht.ColumnIndex >= sourceStartIdx)
                {
                    var sourceColumnIndex = ht.ColumnIndex - sourceStartIdx;
                    if (sourceColumnIndex < sources.Count)
                    {
                        specificSource = sources[sourceColumnIndex];
                    }
                }
            }

            ShowContextMenu(media, screenLocation, specificSource);
        }
    }

    private void uiRefreshButton_Click(object sender, EventArgs e)
    {
        RefreshData();
    }

    private void uiMergerSelectedMediaButton_Click(object sender, EventArgs e)
    {
        var selectedMediaList = uiMediaGrid.GetSelectedMediaBySelectionOrder();

        if (selectedMediaList.Count < 2)
        {
            MessageBox.Show("Выберите как минимум 2 медиа для объединения",
                "Недостаточно выбранных элементов",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        MergeSelectedMedia(selectedMediaList);
    }

    private void uiMergeAssistantButton_Click(object sender, EventArgs e)
    {
        if (_orcestrator == null || _mergeService == null || _loggerFactory == null)
        {
            return;
        }

        using var form = new MergeAssistantForm(_orcestrator,
            _mergeService,
            _loggerFactory.CreateLogger<MergeAssistantForm>());

        var result = form.ShowDialog(FindForm());

        if (result == DialogResult.OK && form.AppliedCount > 0)
        {
            RefreshData();
        }
    }

    private void uiConvertProgressBar_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            uiConvertCancelMenu.Show(Cursor.Position);
        }
    }

    private static (List<Media> mediaData, List<Source> sources) ApplyFilters(
        List<Media> allMedia,
        List<Source> allSources,
        FilterToolStripControl.FilterState filterState)
    {
        IEnumerable<Media> mediaQuery = allMedia;
        var sources = allSources;

        if (!string.IsNullOrEmpty(filterState.SearchText))
        {
            mediaQuery = mediaQuery.Where(x => x.Title != null && x.Title.Contains(filterState.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (filterState.SourceFilter is { Count: > 0 })
        {
            sources = allSources.Where(x => filterState.SourceFilter.Contains(x.Id)).ToList();
            // TODO: При таком варианте не учитывается направление связи
            mediaQuery = mediaQuery.Where(m => m.Sources.Any(l => filterState.SourceFilter.Contains(l.SourceId)));
        }

        if (filterState.StatusFilter != null)
        {
            var status = filterState.StatusFilter;
            var sourceFilter = filterState.SourceFilter;
            mediaQuery = sourceFilter is { Count: > 0 }
                ? mediaQuery.Where(m => m.Sources.Any(s => sourceFilter.Contains(s.SourceId) && s.Status == status))
                : mediaQuery.Where(m => m.Sources.Any(s => s.Status == status));
        }

        return (mediaQuery.ToList(), sources);
    }

    private static List<MetadataColumnInfo> BuildMetadataColumns(List<Media> mediaData, List<Source> sources)
    {
        // TODO: Жидкое место
        var sourceNameMap = sources.ToDictionary(s => s.Id, s => s.Title);

        var pairs = mediaData
            .SelectMany(m => m.Metadata)
            .Where(m => m.SourceId != null)
            .GroupBy(m => (m.Key, m.SourceId))
            .Select(g => g.First())
            .ToList();

        var duplicateKeys = pairs
            .GroupBy(m => m.Key)
            .Where(g => g.Select(m => m.SourceId).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var result = new List<MetadataColumnInfo>();

        foreach (var group in pairs.GroupBy(m => m.Key).OrderBy(g => g.Key))
        {
            if (duplicateKeys.Contains(group.Key))
            {
                foreach (var meta in group.OrderBy(m => m.SourceId))
                {
                    var sourceName = sourceNameMap.GetValueOrDefault(meta.SourceId!, meta.SourceId!);
                    var displayName = (meta.DisplayName ?? meta.Key) + $" ({sourceName})";
                    var columnId = $"{meta.Key} ({sourceName})";
                    result.Add(new(columnId, meta.Key, meta.SourceId, displayName, meta.DisplayType));
                }
            }
            else
            {
                var meta = group.First();
                result.Add(new(meta.Key, meta.Key, null, meta.DisplayName ?? meta.Key, meta.DisplayType));
            }
        }

        return result;
    }

    private void MergeSelectedMedia(List<Media> selectedMediaList)
    {
        if (_mergeService == null || _orcestrator == null)
        {
            return;
        }

        _logger?.LogInformation("Запуск операции объединения медиа. Выбрано элементов: {Count}", selectedMediaList.Count);

        try
        {
            var mergePreview = _mergeService.BuildPreview(selectedMediaList);
            var allSources = _orcestrator.GetSources();

            var mediaList = string.Join("\n", mergePreview.SourceMedias.Select(FormatMedia));
            var conflictsNote = mergePreview.HasConflicts
                ? "\n⚠ Дублирующиеся источники будут пропущены."
                : string.Empty;

            var confirmationMessage = $"""
                                       Целевое (сохранится):
                                       {FormatMedia(mergePreview.TargetMedia)}

                                       Будут присоединены и удалены:
                                       {mediaList}
                                       {conflictsNote}
                                       """;

            var result = MessageBox.Show(confirmationMessage,
                $"Объединение {selectedMediaList.Count} медиа",
                MessageBoxButtons.YesNo,
                mergePreview.HasConflicts ? MessageBoxIcon.Warning : MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                _logger?.LogInformation("Объединение медиа отменено пользователем.");
                return;
            }

            _mergeService.Apply(mergePreview, true);
            RefreshData();

            string FormatMedia(Media m)
            {
                var sourceNames = m.Sources
                    .Select(s => allSources.FirstOrDefault(x => x.Id == s.SourceId)?.Title ?? s.SourceId)
                    .ToList();

                return $"- {m.Title} [{string.Join(", ", sourceNames)}]";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при объединении медиа.");
            MessageBox.Show($"""
                             Произошла ошибка при объединении медиа:

                             {ex.Message}
                             """,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UpdateLoadingIndicator(bool isLoading)
    {
        if (uiLoadingLabel.InvokeRequired)
        {
            uiLoadingLabel.Invoke(() => uiLoadingLabel.Visible = isLoading);
        }
        else
        {
            uiLoadingLabel.Visible = isLoading;
        }
    }

    private void UpdateStatusBar(int total, int filtered)
    {
        if (uiStatusStrip.InvokeRequired)
        {
            uiStatusStrip.Invoke(() =>
            {
                uiTotalCountLabel.Text = $"Всего: {total}";
                uiFilteredCountLabel.Text = $"Отфильтровано: {filtered}";
            });
        }
        else
        {
            uiTotalCountLabel.Text = $"Всего: {total}";
            uiFilteredCountLabel.Text = $"Отфильтровано: {filtered}";
        }
    }
}
