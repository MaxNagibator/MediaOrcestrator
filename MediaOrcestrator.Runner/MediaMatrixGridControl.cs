using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Merging;
using MediaOrcestrator.Runner.MediaContextMenu;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl, IMediaActionUi
{
    private Orcestrator? _orcestrator;
    private ILogger<MediaMatrixGridControl>? _logger;
    private ILoggerFactory? _loggerFactory;
    private MediaMergeService? _mergeService;
    private MediaActionContext? _menuContext;
    private MediaContextMenuController? _menuController;
    private CancellationTokenSource? _convertCts;

    public MediaMatrixGridControl()
    {
        InitializeComponent();
        uiFilterControl.FilterChanged += (_, _) => RefreshData();
        uiCancelConvertItem.Click += (_, _) => _convertCts?.Cancel();
    }

    IWin32Window? IMediaActionUi.Owner => FindForm() ?? (IWin32Window)this;

    public void Initialize(MediaMatrixContext context)
    {
        _orcestrator = context.Orcestrator;
        _logger = context.Logger;
        _loggerFactory = context.LoggerFactory;
        _mergeService = context.MergeService;

        _menuContext = new(context.Orcestrator,
            context.RetryRunner,
            context.BatchRenameService,
            context.BatchPreviewService,
            context.CoverGenerator,
            context.CoverTemplateStore,
            context.MergeService,
            context.ActionHolder,
            context.CommentsService,
            context.LoggerFactory,
            context.Logger,
            this);

        _menuController = new(_menuContext);

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
        ((IMediaActionUi)this).SetLoading(true);

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
            ((IMediaActionUi)this).SetLoading(false);
        }
    }

    private void uiMediaGrid_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || _menuController == null || _orcestrator == null)
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
        if (selectedMedia.Count == 0)
        {
            return;
        }

        var screenLocation = uiMediaGrid.PointToScreen(e.Location);
        var specificSource = ResolveSpecificSource(ht.ColumnIndex, selectedMedia.Count);

        var selection = new MediaSelection(selectedMedia, specificSource)
        {
            InSelectionOrder = uiMediaGrid.GetSelectedMediaBySelectionOrder(),
            GridSources = uiMediaGrid.CurrentSources,
        };

        _menuController.Show(selection, screenLocation);
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

        if (_menuContext != null)
        {
            MergeRunner.Run(selectedMediaList, _menuContext);
        }
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

    void IMediaActionUi.SetLoading(bool isLoading)
    {
        UpdateLoadingIndicator(isLoading);
    }

    void IMediaActionUi.NotifyDataChanged()
    {
        RefreshData();
    }

    void IMediaActionUi.ShowConvertProgress(double percent, string text)
    {
        ShowConvertProgress(percent, text);
    }

    void IMediaActionUi.HideConvertProgress()
    {
        HideConvertProgress();
    }

    void IMediaActionUi.RegisterConvertCancellation(CancellationTokenSource? cts)
    {
        _convertCts = cts;
    }

    private Source? ResolveSpecificSource(int columnIndex, int selectedCount)
    {
        if (selectedCount > 1)
        {
            return null;
        }

        var sources = uiMediaGrid.CurrentSources;
        if (sources == null)
        {
            return null;
        }

        var sourceStartIdx = uiMediaGrid.Columns.Count - sources.Count;
        if (columnIndex < sourceStartIdx)
        {
            return null;
        }

        var sourceColumnIndex = columnIndex - sourceStartIdx;
        return sourceColumnIndex < sources.Count ? sources[sourceColumnIndex] : null;
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

    private void ShowConvertProgress(double percent, string text)
    {
        if (uiStatusStrip.InvokeRequired)
        {
            uiStatusStrip.Invoke(() => ShowConvertProgress(percent, text));
            return;
        }

        uiConvertProgressBar.Value = (int)Math.Min(percent, 100);
        uiConvertStatusLabel.Text = text;
        uiConvertProgressBar.Visible = true;
        uiConvertStatusLabel.Visible = true;
    }

    private void HideConvertProgress()
    {
        if (uiStatusStrip.InvokeRequired)
        {
            uiStatusStrip.Invoke(HideConvertProgress);
            return;
        }

        uiConvertProgressBar.Visible = false;
        uiConvertStatusLabel.Visible = false;
        uiConvertProgressBar.Value = 0;
        uiConvertStatusLabel.Text = "";
    }
}
