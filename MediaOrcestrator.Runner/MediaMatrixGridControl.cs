using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl
{
    private const int SearchDebounceMs = 300;

    private Orcestrator? _orcestrator;

    public MediaMatrixGridControl()
    {
        InitializeComponent();

        uiStatusFilterComboBox.Items.Clear();
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Все", Tag = null });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "OK", Tag = MediaSourceLink.StatusOk });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Ошибка", Tag = MediaSourceLink.StatusError });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Нет", Tag = MediaSourceLink.StatusNone });
        uiStatusFilterComboBox.SelectedIndex = 0;
    }

    public void Initialize(Orcestrator orcestrator)
    {
        _orcestrator = orcestrator;
    }

    public async void RefreshData(List<SourceSyncRelation>? selectedRelations = null)
    {
        if (_orcestrator == null)
        {
            return;
        }

        UpdateLoadingIndicator(true);

        try
        {
            var filterState = BuildFilterState(selectedRelations);

            var (mediaData, sources, allMediaCount) = await Task.Run(() =>
            {
                var allMedia = _orcestrator.GetMedias();
                var allSources = _orcestrator.GetSources();
                var (filteredMedia, filteredSources) = ApplyFilters(allMedia, allSources, filterState);
                return (filteredMedia, filteredSources, allMedia.Count);
            });

            uiMediaGrid.SetupColumns(sources);
            uiMediaGrid.PopulateGrid(sources, mediaData);
            UpdateStatusBar(allMediaCount, mediaData.Count);
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

        if (uiMediaGrid.GetMediaAtRow(ht.RowIndex) is { } media)
        {
            ShowContextMenu(media, uiMediaGrid.PointToScreen(e.Location));
        }
    }

    private void uiRefreshButton_Click(object sender, EventArgs e)
    {
        RefreshData();
    }

    private void uiSearchToolStripTextBox_TextChanged(object? sender, EventArgs e)
    {
        DebouncedSearch();
    }

    private void uiClearSearchButton_Click(object? sender, EventArgs e)
    {
        uiSearchToolStripTextBox.Text = string.Empty;
        RefreshData();
    }

    private void uiStatusFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshData();
    }

    private void uiSelectAllButton_Click(object? sender, EventArgs e)
    {
        uiMediaGrid.SelectAllRows();
    }

    private void uiDeselectAllButton_Click(object? sender, EventArgs e)
    {
        uiMediaGrid.DeselectAllRows();
    }

    private void uiMergerSelectedMediaButton_Click(object sender, EventArgs e)
    {
        var selectedMediaList = uiMediaGrid.GetCheckedMedia();

        if (selectedMediaList.Count < 2)
        {
            return;
        }

        var currentMediaSources = new List<MediaSourceLink>();
        foreach (var selectedMedia in selectedMediaList)
        {
            foreach (var selectedMediaSourceLink in selectedMedia.Sources)
            {
                var current = currentMediaSources.FirstOrDefault(x => x.SourceId == selectedMediaSourceLink.SourceId);
                if (current != null)
                {
                    var sources = uiMediaGrid.CurrentSources ?? _orcestrator.GetSources();
                    var fullSource = sources.First(x => x.Id == selectedMediaSourceLink.SourceId);
                    MessageBox.Show("Данное хранилище уже есть у медиа " + fullSource.TitleFull);
                    return;
                }

                // todo вот бы заполнять ссылку на него сразу и не приседать
                //var fullSource = sources.First(x => x.Id == selectedMediaSourceLink.SourceId);
                currentMediaSources.Add(selectedMediaSourceLink);
            }
        }

        selectedMediaList.First().Sources = currentMediaSources;
        _orcestrator.UpdateMedia(selectedMediaList.First());

        foreach (var media in selectedMediaList.Skip(1))
        {
            _orcestrator.RemoveMedia(media);
        }

        RefreshData();
    }

    private static (List<Media> mediaData, List<Source> sources) ApplyFilters(
        List<Media> allMedia,
        List<Source> allSources,
        FilterState filterState)
    {
        IEnumerable<Media> mediaQuery = allMedia;
        var sources = allSources;

        if (!string.IsNullOrEmpty(filterState.SearchText))
        {
            mediaQuery = mediaQuery.Where(x => x.Title.Contains(filterState.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (filterState.StatusFilter != null)
        {
            var status = filterState.StatusFilter;
            mediaQuery = mediaQuery.Where(m => m.Sources.Any(s => s.Status == status));
        }

        if (filterState.SourceFilter is { Count: > 0 })
        {
            sources = allSources.Where(x => filterState.SourceFilter.Contains(x.Id)).ToList();
            // TODO: При таком варианте не учитывается направление связи
            mediaQuery = mediaQuery.Where(m => m.Sources.Any(l => filterState.SourceFilter.Contains(l.SourceId)));
        }

        return (mediaQuery.ToList(), sources);
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

    private void DebouncedSearch()
    {
        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = new(_ =>
            {
                if (InvokeRequired)
                {
                    Invoke(() => RefreshData());
                }
                else
                {
                    RefreshData();
                }
            },
            null,
            SearchDebounceMs,
            Timeout.Infinite);
    }

    private FilterState BuildFilterState(List<SourceSyncRelation>? selectedRelations)
    {
        var filterState = new FilterState
        {
            SearchText = uiSearchToolStripTextBox.Text,
        };

        if (uiStatusFilterComboBox.SelectedIndex > 0)
        {
            filterState.StatusFilter = (uiStatusFilterComboBox.SelectedItem as StatusFilterItem)?.Tag;
        }

        if (selectedRelations is { Count: > 0 })
        {
            filterState.SourceFilter = selectedRelations
                .SelectMany(x => new[] { x.From.Id, x.To.Id })
                .Distinct()
                .ToHashSet();
        }

        return filterState;
    }

    private void ShowContextMenu(Media media, Point location)
    {
        _contextMenu?.Dispose();
        _contextMenu = new();

        foreach (var rel in _orcestrator.GetRelations())
        {
            var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
            var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
            var menuText = $"Синхронизировать {rel.From.TitleFull} -> {rel.To.TitleFull}";

            if (fromSource != null && toSource == null)
            {
                _contextMenu.Items.Add(menuText, null, async void (s, e) =>
                {
                    UpdateLoadingIndicator(true);
                    try
                    {
                        await _orcestrator.TransferByRelation(media, rel, fromSource.ExternalId);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Логирование
                        MessageBox.Show($"Ошибка при синхронизации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        UpdateLoadingIndicator(false);
                        RefreshData();
                    }
                });
            }
            else
            {
                var element = _contextMenu.Items.Add(menuText);
                element.Enabled = false;
            }
        }

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Show(location);
        }
    }

    private sealed class StatusFilterItem
    {
        public required string Text { get; init; }
        public string? Tag { get; init; }

        public override string ToString()
        {
            return Text;
        }
    }

    private sealed class FilterState
    {
        public string SearchText { get; set; } = string.Empty;

        public string? StatusFilter { get; set; }

        public HashSet<string>? SourceFilter { get; set; }
    }
}
