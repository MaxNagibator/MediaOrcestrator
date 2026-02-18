using MediaOrcestrator.Domain;
using System.Text;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl
{
    private const int SearchDebounceMs = 300;
    private readonly HashSet<SourceSyncRelation> _selectedRelations = [];

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
        PopulateRelationsFilter();
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
            Source? specificSource = null;
            if (ht.ColumnIndex >= OptimizedMediaGridView.FirstSourceColumnIndex)
            {
                var sourceColumnIndex = ht.ColumnIndex - OptimizedMediaGridView.FirstSourceColumnIndex;
                var sources = uiMediaGrid.CurrentSources;
                if (sources != null && sourceColumnIndex < sources.Count)
                {
                    specificSource = sources[sourceColumnIndex];
                }
            }

            ShowContextMenu(media, uiMediaGrid.PointToScreen(e.Location), specificSource);
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
            MessageBox.Show("Выберите как минимум 2 медиа для объединения",
                "Недостаточно выбранных элементов",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            var mergePreview = ValidateMergeOperation(selectedMediaList);

            var mediaList = string.Join("\n", mergePreview.SourceMedias.Select(x => $"- {x.Title}"));
            var conflictsList = mergePreview.HasConflicts
                ? $"""
                   ⚠ ВНИМАНИЕ! Обнаружены конфликты:
                   {string.Join("\n", mergePreview.Conflicts.Select(x => $"- {x}"))}

                   Дублирующиеся источники будут пропущены.
                   """
                : string.Empty;

            var confirmationMessage = $"""
                                       Вы собираетесь объединить следующие медиа:

                                       Целевое медиа (сохранится):
                                       - {mergePreview.TargetMedia.Title}

                                       Медиа, которые будут присоединены и удалены:
                                       {mediaList}

                                       {conflictsList}
                                       Это действие нельзя отменить. Продолжить?
                                       """;

            var result = MessageBox.Show(confirmationMessage,
                "Подтверждение объединения",
                MessageBoxButtons.YesNo,
                mergePreview.HasConflicts ? MessageBoxIcon.Warning : MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            mergePreview.TargetMedia.Sources = mergePreview.ResultingSources;
            _orcestrator.UpdateMedia(mergePreview.TargetMedia);

            foreach (var media in mergePreview.SourceMedias)
            {
                _orcestrator.RemoveMedia(media);
            }

            RefreshData();

            MessageBox.Show($"""
                             Медиа успешно объединены.

                             Итоговое количество источников: {mergePreview.TotalSourcesCount}
                             """,
                "Объединение завершено",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"""
                             Произошла ошибка при объединении медиа:

                             {ex.Message}
                             """,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
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

    private static Bitmap? GetSyncIcon()
    {
        try
        {
            var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            using var pen = new Pen(Color.Blue, 2);

            g.Clear(Color.Transparent);
            g.DrawLine(pen, 2, 8, 12, 8);
            g.DrawLine(pen, 9, 5, 12, 8);
            g.DrawLine(pen, 9, 11, 12, 8);

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? GetCopyIcon()
    {
        try
        {
            var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            using var pen = new Pen(Color.DarkGray, 1);

            g.Clear(Color.Transparent);
            g.DrawRectangle(pen, 2, 2, 8, 8);
            g.DrawRectangle(pen, 5, 5, 8, 8);

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? GetDeleteIcon()
    {
        try
        {
            var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            using var pen = new Pen(Color.Red, 2);

            g.Clear(Color.Transparent);
            g.DrawLine(pen, 4, 4, 12, 12);
            g.DrawLine(pen, 12, 4, 4, 12);

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public void PopulateRelationsFilter()
    {
        if (_orcestrator == null)
        {
            return;
        }

        uiRelationsDropDownButton.DropDownItems.Clear();
        _selectedRelations.Clear();
        var relations = _orcestrator.GetRelations();

        foreach (var syncRelation in relations)
        {
            var item = new ToolStripMenuItem($"{syncRelation.From.TitleFull} -> {syncRelation.To.TitleFull}")
            {
                CheckOnClick = true,
                Tag = syncRelation,
            };

            item.CheckedChanged += (s, _) =>
            {
                if (s is ToolStripMenuItem { Tag: SourceSyncRelation relation } menuItem)
                {
                    if (menuItem.Checked)
                    {
                        _selectedRelations.Add(relation);
                    }
                    else
                    {
                        _selectedRelations.Remove(relation);
                    }
                }

                RefreshData();
            };

            uiRelationsDropDownButton.DropDownItems.Add(item);
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

        var activeRelations = selectedRelations ?? _selectedRelations.ToList();

        if (activeRelations.Count > 0)
        {
            filterState.SourceFilter = activeRelations
                .SelectMany(x => new[] { x.From.Id, x.To.Id })
                .Distinct()
                .ToHashSet();
        }

        return filterState;
    }

    // TODO: Прибрать дублирование + портянка
    private void ShowContextMenu(Media media, Point location, Source? specificSource = null)
    {
        _contextMenu?.Dispose();
        _contextMenu = new();

        if (specificSource != null)
        {
            var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == specificSource.Id);

            foreach (var rel in _orcestrator.GetRelations())
            {
                if (rel.From.Id != specificSource.Id && rel.To.Id != specificSource.Id)
                {
                    continue;
                }

                var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
                var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
                var menuText = $"Синхронизировать {rel.From.TitleFull} -> {rel.To.TitleFull}";

                if (fromSource != null && toSource == null)
                {
                    var menuItem = new ToolStripMenuItem(menuText, GetSyncIcon());
                    menuItem.Click += async (_, _) =>
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
                    };

                    _contextMenu.Items.Add(menuItem);
                }
                else
                {
                    var menuItem = new ToolStripMenuItem(menuText, GetSyncIcon())
                    {
                        Enabled = false,
                    };

                    if (fromSource == null && toSource == null)
                    {
                        menuItem.ToolTipText = "Медиа отсутствует в исходном хранилище";
                    }
                    else if (fromSource == null)
                    {
                        menuItem.ToolTipText = "Исходное хранилище недоступно";
                    }
                    else if (toSource != null)
                    {
                        menuItem.ToolTipText = "Медиа уже существует в целевом хранилище";
                    }

                    _contextMenu.Items.Add(menuItem);
                }
            }

            if (sourceLink != null && !specificSource.IsDisable)
            {
                if (_contextMenu.Items.Count > 0)
                {
                    _contextMenu.Items.Add(new ToolStripSeparator());
                }

                var deleteMenuItem = new ToolStripMenuItem($"Удалить из {specificSource.TitleFull}", GetDeleteIcon());

                deleteMenuItem.Click += async (s, e) => await HandleDeleteClickAsync(media, specificSource);

                _contextMenu.Items.Add(deleteMenuItem);
            }

            if (_contextMenu.Items.Count > 0)
            {
                _contextMenu.Items.Add(new ToolStripSeparator());
            }

            var copyItem = new ToolStripMenuItem("Копировать детали в буфер обмена", GetCopyIcon());
            copyItem.Click += (s, e) => CopyMediaDetailsToClipboard(media);
            _contextMenu.Items.Add(copyItem);

            if (_contextMenu.Items.Count > 0)
            {
                _contextMenu.Show(location);
            }

            return;
        }

        foreach (var rel in _orcestrator.GetRelations())
        {
            var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
            var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
            var menuText = $"Синхронизировать {rel.From.TitleFull} -> {rel.To.TitleFull}";

            if (fromSource != null && toSource == null)
            {
                var menuItem = new ToolStripMenuItem(menuText, GetSyncIcon());
                menuItem.Click += async (s, e) =>
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
                };

                _contextMenu.Items.Add(menuItem);
            }
            else
            {
                var menuItem = new ToolStripMenuItem(menuText, GetSyncIcon())
                {
                    Enabled = false,
                };

                if (fromSource == null && toSource == null)
                {
                    menuItem.ToolTipText = "Медиа отсутствует в исходном хранилище";
                }
                else if (fromSource == null)
                {
                    menuItem.ToolTipText = "Исходное хранилище недоступно";
                }
                else if (toSource != null)
                {
                    menuItem.ToolTipText = "Медиа уже существует в целевом хранилище";
                }

                _contextMenu.Items.Add(menuItem);
            }
        }

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Items.Add(new ToolStripSeparator());
        }

        foreach (var sourceLink in media.Sources)
        {
            var source = _orcestrator.GetSources()
                .FirstOrDefault(s => s.Id == sourceLink.SourceId);

            if (source == null || source.IsDisable)
            {
                continue;
            }

            var deleteMenuItem = new ToolStripMenuItem($"Удалить из {source.TitleFull}", GetDeleteIcon());
            deleteMenuItem.Click += async (_, _) => await HandleDeleteClickAsync(media, source);

            _contextMenu.Items.Add(deleteMenuItem);
        }

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Items.Add(new ToolStripSeparator());
        }

        var copyDetailsItem = new ToolStripMenuItem("Копировать детали в буфер обмена", GetCopyIcon());
        copyDetailsItem.Click += (s, e) => CopyMediaDetailsToClipboard(media);
        _contextMenu.Items.Add(copyDetailsItem);

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Show(location);
        }
    }


    private DialogResult ShowDeleteConfirmation(Media media, Source source, bool isLastSource)
    {
        string message;
        if (isLastSource)
        {
            message = $"""
                       Вы уверены, что хотите удалить медиа "{media.Title}" из {source.TitleFull}?

                       ВНИМАНИЕ: Это последний источник для данного медиа. Запись будет полностью удалена из базы данных.
                       """;
        }
        else
        {
            message = $"""
                       Вы уверены, что хотите удалить медиа "{media.Title}" из {source.TitleFull}?

                       Медиа останется в других источниках.
                       """;
        }

        return MessageBox.Show(message,
            "Подтверждение удаления",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
    }


    private async Task HandleDeleteClickAsync(Media media, Source source)
    {
        var isLastSource = media.Sources.Count == 1;

        var result = ShowDeleteConfirmation(media, source, isLastSource);
        if (result != DialogResult.Yes)
        {
            return;
        }

        UpdateLoadingIndicator(true);

        try
        {
            await _orcestrator.DeleteMediaFromSourceAsync(media, source);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message,
                "Ошибка удаления",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"""
                             Ошибка авторизации при удалении из {source.TitleFull}.

                             Проверьте настройки источника и обновите учётные данные.

                             Детали: {ex.Message}
                             """,
                "Ошибка авторизации",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"""
                             Ошибка файловой системы при удалении из {source.TitleFull}.

                             Проверьте права доступа и наличие файла.

                             Детали: {ex.Message}
                             """,
                "Ошибка файловой системы",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"""
                             Неожиданная ошибка при удалении из {source.TitleFull}.

                             Детали: {ex.Message}
                             """,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UpdateLoadingIndicator(false);
            RefreshData();
        }
    }

    private void CopyMediaDetailsToClipboard(Media media)
    {
        try
        {
            var sources = _orcestrator?.GetSources() ?? [];
            var details = new StringBuilder();

            details.AppendLine($"Название: {media.Title}");

            if (!string.IsNullOrEmpty(media.Description))
            {
                details.AppendLine($"Описание: {media.Description}");
            }

            details.AppendLine();
            details.AppendLine("Источники:");

            if (media.Sources.Count == 0)
            {
                details.AppendLine(" Нет источников");
            }
            else
            {
                foreach (var sourceLink in media.Sources)
                {
                    var source = sources.FirstOrDefault(s => s.Id == sourceLink.SourceId);
                    var sourceName = source?.TitleFull ?? "Неизвестный источник";
                    var status = sourceLink.Status switch
                    {
                        MediaSourceLink.StatusOk => "✔ OK",
                        MediaSourceLink.StatusError => "✘ Ошибка",
                        MediaSourceLink.StatusNone => "○ Нет",
                        _ => "● Неизвестно",
                    };

                    details.AppendLine($"  {sourceName}: {status}");
                    if (!string.IsNullOrEmpty(sourceLink.ExternalId))
                    {
                        details.AppendLine($"    ID: {sourceLink.ExternalId}");
                    }
                }
            }

            Clipboard.SetText(details.ToString());
            MessageBox.Show("Детали медиа скопированы в буфер обмена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при копировании: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private MergePreviewData ValidateMergeOperation(List<Media> selectedMedia)
    {
        if (selectedMedia.Count < 2)
        {
            throw new InvalidOperationException("Для объединения необходимо выбрать как минимум 2 медиа");
        }

        var targetMedia = selectedMedia.First();
        var sourceMedias = selectedMedia.Skip(1).ToList();
        var conflicts = new List<string>();
        var resultingSources = new List<MediaSourceLink>();

        var sourceDict = new Dictionary<string, MediaSourceLink>();

        foreach (var sourceLink in targetMedia.Sources)
        {
            sourceDict[sourceLink.SourceId] = sourceLink;
        }

        var allSources = _orcestrator?.GetSources() ?? [];

        foreach (var media in sourceMedias)
        {
            foreach (var sourceLink in media.Sources)
            {
                if (sourceDict.TryAdd(sourceLink.SourceId, sourceLink))
                {
                    continue;
                }

                var source = allSources.FirstOrDefault(s => s.Id == sourceLink.SourceId);
                var sourceName = source?.TitleFull ?? sourceLink.SourceId;
                conflicts.Add($"Источник '{sourceName}' присутствует в нескольких медиа");
            }
        }

        resultingSources.AddRange(sourceDict.Values);

        return new()
        {
            TargetMedia = targetMedia,
            SourceMedias = sourceMedias,
            ResultingSources = resultingSources,
            Conflicts = conflicts,
        };
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

    private sealed class MergePreviewData
    {
        public required Media TargetMedia { get; init; }

        public required List<Media> SourceMedias { get; init; }

        public required List<MediaSourceLink> ResultingSources { get; init; }

        public required List<string> Conflicts { get; init; }

        public int TotalSourcesCount => ResultingSources.Count;

        public bool HasConflicts => Conflicts.Count > 0;
    }
}
