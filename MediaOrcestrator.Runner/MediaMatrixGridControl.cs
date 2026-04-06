using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl
{
    private Orcestrator? _orcestrator;
    private ILogger<MediaMatrixGridControl>? _logger;
    private BatchRenameService? _batchRenameService;
    private CancellationTokenSource? _convertCts;

    public MediaMatrixGridControl()
    {
        InitializeComponent();
        uiFilterControl.FilterChanged += (_, _) => RefreshData();
        uiCancelConvertItem.Click += (_, _) => _convertCts?.Cancel();
    }

    public void Initialize(Orcestrator orcestrator, ILogger<MediaMatrixGridControl> logger, SettingsManager settingsManager, BatchRenameService batchRenameService)
    {
        _orcestrator = orcestrator;
        _logger = logger;
        _batchRenameService = batchRenameService;
        uiFilterControl.SetSettingsManager(settingsManager);
        uiFilterControl.PopulateRelationsFilter(orcestrator);
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

    private void MergeSelectedMedia(List<Media> selectedMediaList)
    {
        _logger?.LogInformation("Запуск операции объединения медиа. Выбрано элементов: {Count}", selectedMediaList.Count);

        try
        {
            var mergePreview = ValidateMergeOperation(selectedMediaList);
            var allSources = _orcestrator!.GetSources();

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

            _logger?.LogInformation("Выполняется объединение медиа. Целевое медиа: '{TargetMedia}'", mergePreview.TargetMedia.Title);
            mergePreview.TargetMedia.Sources = mergePreview.ResultingSources;
            _orcestrator.UpdateMedia(mergePreview.TargetMedia);

            foreach (var media in mergePreview.SourceMedias)
            {
                _orcestrator.RemoveMedia(media);
            }

            RefreshData();

            _logger?.LogInformation("Объединение медиа успешно завершено. Итоговое количество источников: {TotalSourcesCount}", mergePreview.TotalSourcesCount);

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

    private async void ShowBatchContextMenu(List<Media> selectedMedia, Point location)
    {
        _contextMenu?.Dispose();
        _contextMenu = new();

        var headerItem = new ToolStripMenuItem($"Выбрано: {selectedMedia.Count} медиа") { Enabled = false };
        _contextMenu.Items.Add(headerItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        foreach (var rel in _orcestrator!.GetRelations())
        {
            var eligibleMedia = selectedMedia
                .Where(m =>
                {
                    var from = m.Sources.FirstOrDefault(s => s.SourceId == rel.From.Id);
                    var to = m.Sources.FirstOrDefault(s => s.SourceId == rel.To.Id);
                    return from is { Status: MediaStatus.Ok } && to is not { Status: MediaStatus.Ok };
                })
                .ToList();

            var menuText = $"Синхронизировать {rel.From.TitleFull} → {rel.To.TitleFull} ({eligibleMedia.Count})";
            var menuItem = new ToolStripMenuItem(menuText, GetSyncIcon());

            if (eligibleMedia.Count == 0)
            {
                menuItem.Enabled = false;
                menuItem.ToolTipText = "Нет подходящих медиа для синхронизации";
            }
            else
            {
                var capturedRel = rel;
                var capturedMedia = eligibleMedia;
                menuItem.Click += async (_, _) => await HandleBatchSyncAsync(capturedMedia, capturedRel);
            }

            _contextMenu.Items.Add(menuItem);
        }

        _contextMenu.Items.Add(new ToolStripSeparator());

        var updateMetaItem = new ToolStripMenuItem($"Обновить метаданные ({selectedMedia.Count})", GetSyncIcon());
        updateMetaItem.Click += async (_, _) => await HandleBatchUpdateMetadataAsync(selectedMedia);
        _contextMenu.Items.Add(updateMetaItem);

        var renameItem = new ToolStripMenuItem($"Пакетное переименование ({selectedMedia.Count})...");
        var capturedMediaForRename = selectedMedia;
        renameItem.Click += (_, _) => HandleBatchRename(capturedMediaForRename);
        _contextMenu.Items.Add(renameItem);

        if (selectedMedia.Count >= 2)
        {
            var mergeItem = new ToolStripMenuItem($"Объединить ({selectedMedia.Count})");
            mergeItem.Click += (_, _) => MergeSelectedMedia(uiMediaGrid.GetSelectedMediaBySelectionOrder());
            _contextMenu.Items.Add(mergeItem);
        }

        _contextMenu.Items.Add(new ToolStripSeparator());

        var allSources = _orcestrator.GetSources().Where(s => !s.IsDisable).ToList();
        foreach (var source in allSources)
        {
            var deletableMedia = selectedMedia
                .Where(m => m.Sources.Any(s => s.SourceId == source.Id))
                .ToList();

            if (deletableMedia.Count == 0)
            {
                continue;
            }

            var deleteItem = new ToolStripMenuItem($"Удалить из {source.TitleFull} ({deletableMedia.Count})", GetDeleteIcon());
            var capturedSource = source;
            deleteItem.Click += async (_, _) => await HandleBatchDeleteAsync(deletableMedia, capturedSource);
            _contextMenu.Items.Add(deleteItem);

            var convertTypes = source.Type.GetAvailabelConvertTypes();
            if (convertTypes.Length <= 0)
            {
                continue;
            }

            var mediaDtos = new Dictionary<Media, MediaDto?>();
            foreach (var media in deletableMedia)
            {
                var mediaSource = media.Sources.FirstOrDefault(s => s.SourceId == source.Id);
                if (mediaSource == null)
                {
                    continue;
                }

                try
                {
                    mediaDtos[media] = await source.Type.GetMediaByIdAsync(mediaSource.ExternalId, source.Settings);
                }
                catch
                {
                    mediaDtos[media] = null;
                }
            }

            foreach (var t in convertTypes)
            {
                var eligibleForConvert = deletableMedia
                    .Where(m =>
                    {
                        if (!mediaDtos.TryGetValue(m, out var dto) || dto == null)
                        {
                            return false;
                        }

                        return source.Type.CheckConvertAvailability(t.Id, dto).IsAvailable;
                    })
                    .ToList();

                var convertItem = new ToolStripMenuItem($"Конвертировать {t.Name} ({eligibleForConvert.Count})");

                if (eligibleForConvert.Count == 0)
                {
                    convertItem.Enabled = false;
                    convertItem.ToolTipText = "Нет подходящих медиа для конвертации";
                }
                else
                {
                    convertItem.Click += async (_, _) =>
                    {
                        _convertCts = new();
                        var errors = new List<(Media media, Exception ex)>();
                        var total = eligibleForConvert.Count;
                        var converted = 0;

                        try
                        {
                            for (var i = 0; i < total; i++)
                            {
                                var media = eligibleForConvert[i];
                                var mediaSource = media.Sources.First(s => s.SourceId == source.Id);

                                var i1 = i;
                                var progress = new Progress<ConvertProgress>(p =>
                                    ShowConvertProgress(p.Percent, $"{t.Name} [{i1 + 1}/{total}]: {media.Title} — {p.Percent:F0}%"));

                                ShowConvertProgress(0, $"{t.Name} [{i + 1}/{total}]: {media.Title}...");

                                try
                                {
                                    await source.Type.ConvertAsync(t.Id, mediaSource.ExternalId, source.Settings, progress, _convertCts.Token);
                                    await _orcestrator.ForceUpdateMetadataAsync(media, source.Id);
                                    converted++;
                                }
                                catch (OperationCanceledException)
                                {
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    errors.Add((media, ex));
                                }
                            }

                            if (errors.Count <= 0)
                            {
                                return;
                            }

                            var errorDetails = string.Join("\n", errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));
                            MessageBox.Show($"""
                                             Конвертировано: {converted} из {total}

                                             Ошибки ({errors.Count}):
                                             {errorDetails}
                                             """,
                                "Конвертация завершена с ошибками",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (OperationCanceledException)
                        {
                            MessageBox.Show("Конвертация отменена", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        finally
                        {
                            HideConvertProgress();
                            RefreshData();
                            _convertCts?.Dispose();
                            _convertCts = null;
                        }
                    };
                }

                _contextMenu.Items.Add(convertItem);
            }
        }

        _contextMenu.Show(location);
    }

    private async Task HandleBatchSyncAsync(List<Media> mediaList, SourceSyncRelation rel)
    {
        var result = MessageBox.Show($"Синхронизировать {mediaList.Count} медиа из {rel.From.TitleFull} в {rel.To.TitleFull}?",
            "Подтверждение пакетной синхронизации",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _logger?.LogInformation("Запуск пакетной синхронизации {Count} медиа: {From} → {To}",
            mediaList.Count, rel.From.TitleFull, rel.To.TitleFull);

        UpdateLoadingIndicator(true);
        var errors = new List<(Media media, Exception ex)>();

        try
        {
            foreach (var media in mediaList)
            {
                try
                {
                    await _orcestrator!.TransferByRelation(media, rel);
                    _logger?.LogInformation("Синхронизировано: '{Title}'", media.Title);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка синхронизации '{Title}'", media.Title);
                    errors.Add((media, ex));
                }
            }

            if (errors.Count > 0)
            {
                var errorDetails = string.Join("\n", errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));
                MessageBox.Show($"Синхронизировано: {mediaList.Count - errors.Count} из {mediaList.Count}\n\nОшибки:\n{errorDetails}",
                    "Пакетная синхронизация завершена с ошибками",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            UpdateLoadingIndicator(false);
            RefreshData();
        }
    }

    private async Task HandleBatchDeleteAsync(List<Media> mediaList, Source source)
    {
        var result = MessageBox.Show($"Удалить {mediaList.Count} медиа из {source.TitleFull}?\n\nЭто действие нельзя отменить.",
            "Подтверждение пакетного удаления",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _logger?.LogInformation("Запуск пакетного удаления {Count} медиа из '{Source}'",
            mediaList.Count, source.TitleFull);

        UpdateLoadingIndicator(true);
        var errors = new List<(Media media, Exception ex)>();

        try
        {
            foreach (var media in mediaList)
            {
                try
                {
                    await _orcestrator!.DeleteMediaFromSourceAsync(media, source);
                    _logger?.LogInformation("Удалено: '{Title}' из '{Source}'", media.Title, source.TitleFull);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка удаления '{Title}' из '{Source}'", media.Title, source.TitleFull);
                    errors.Add((media, ex));
                }
            }

            if (errors.Count > 0)
            {
                var errorDetails = string.Join("\n", errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));
                MessageBox.Show($"Удалено: {mediaList.Count - errors.Count} из {mediaList.Count}\n\nОшибки:\n{errorDetails}",
                    "Пакетное удаление завершено с ошибками",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            UpdateLoadingIndicator(false);
            RefreshData();
        }
    }

    private void HandleBatchRename(List<Media> selectedMedia)
    {
        using var form = new BatchRenameForm(selectedMedia, _batchRenameService!);
        var result = form.ShowDialog(this);

        if (result == DialogResult.OK)
        {
            RefreshData();
        }
    }

    private async Task HandleBatchUpdateMetadataAsync(List<Media> mediaList)
    {
        _logger?.LogInformation("Запуск пакетного обновления метаданных для {Count} медиа", mediaList.Count);

        UpdateLoadingIndicator(true);
        var errors = new List<(Media media, Exception ex)>();

        try
        {
            foreach (var media in mediaList)
            {
                try
                {
                    await _orcestrator!.ForceUpdateMetadataAsync(media);
                    _logger?.LogInformation("Метаданные обновлены: '{Title}'", media.Title);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка обновления метаданных '{Title}'", media.Title);
                    errors.Add((media, ex));
                }
            }

            if (errors.Count > 0)
            {
                var errorDetails = string.Join("\n", errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));
                MessageBox.Show($"Обновлено: {mediaList.Count - errors.Count} из {mediaList.Count}\n\nОшибки:\n{errorDetails}",
                    "Обновление метаданных завершено с ошибками",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            UpdateLoadingIndicator(false);
            RefreshData();
        }
    }

    private async void ShowContextMenu(Media media, Point location, Source? specificSource = null)
    {
        _contextMenu?.Dispose();
        _contextMenu = new();

        AddMetadataMenuItems(_contextMenu, media, specificSource);

        _contextMenu.Items.Add(new ToolStripSeparator());

        if (specificSource != null)
        {
            var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == specificSource.Id);
            AddSyncMenuItems(_contextMenu, media, specificSource);

            if (sourceLink != null && !specificSource.IsDisable)
            {
                if (_contextMenu.Items.Count > 0)
                {
                    _contextMenu.Items.Add(new ToolStripSeparator());
                }

                var deleteMenuItem = new ToolStripMenuItem($"Удалить из {specificSource.TitleFull}", GetDeleteIcon());
                deleteMenuItem.Click += async (_, _) => await HandleDeleteClickAsync(media, specificSource);
                _contextMenu.Items.Add(deleteMenuItem);
            }
        }
        else
        {
            AddSyncMenuItems(_contextMenu, media, null);

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
        }

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Items.Add(new ToolStripSeparator());
        }

        var copyItem = new ToolStripMenuItem("Копировать детали в буфер обмена", GetCopyIcon());
        copyItem.Click += (_, _) => CopyMediaDetailsToClipboard(media);
        _contextMenu.Items.Add(copyItem);

        var allSources = _orcestrator.GetSources();
        var relevantSourceLinks = specificSource != null
            ? media.Sources.Where(s => s.SourceId == specificSource.Id).ToList()
            : media.Sources;

        foreach (var mediaSource in relevantSourceLinks)
        {
            var source = allSources.FirstOrDefault(x => x.Id == mediaSource.SourceId);
            if (source?.Type == null)
            {
                continue;
            }

            var uri = source.Type.GetExternalUri(mediaSource.ExternalId, source.Settings);
            if (uri == null)
            {
                continue;
            }

            var openItem = new ToolStripMenuItem("Открыть: " + source.TitleFull, GetCopyIcon());
            openItem.Click += (_, _) =>
            {
                Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
            };

            _contextMenu.Items.Add(openItem);
        }

        foreach (var mediaSource in relevantSourceLinks)
        {
            var source = allSources.FirstOrDefault(x => x.Id == mediaSource.SourceId);
            if (source?.Type == null)
            {
                continue;
            }

            var convertTypes = source.Type.GetAvailabelConvertTypes();
            if (convertTypes.Length == 0)
            {
                continue;
            }

            _logger?.LogDebug("Загрузка метаданных для проверки конвертации: {Source}, ExternalId={ExternalId}",
                source.TitleFull, mediaSource.ExternalId);

            MediaDto? mediaDto = null;
            try
            {
                mediaDto = await source.Type.GetMediaByIdAsync(mediaSource.ExternalId, source.Settings);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Не удалось загрузить метаданные для {Source}: {ExternalId}",
                    source.TitleFull, mediaSource.ExternalId);
            }

            foreach (var t in convertTypes)
            {
                var availability = mediaDto != null
                    ? source.Type.CheckConvertAvailability(t.Id, mediaDto)
                    : new(false, "Не удалось получить метаданные");

                _logger?.LogDebug("Конвертация {Name} ({Source}): доступна={Available}, причина={Reason}",
                    t.Name, source.TitleFull, availability.IsAvailable, availability.Reason);

                var convertItem = new ToolStripMenuItem($"Конвертировать {t.Name} ({source.TitleFull})");
                convertItem.Enabled = availability.IsAvailable;
                if (!availability.IsAvailable)
                {
                    convertItem.ToolTipText = availability.Reason;
                }

                convertItem.Click += async (_, _) =>
                {
                    _logger?.LogInformation("Запуск конвертации {Name} для {ExternalId} ({Source})",
                        t.Name, mediaSource.ExternalId, source.TitleFull);

                    _convertCts = new();
                    try
                    {
                        var title = media.Title;
                        var progress = new Progress<ConvertProgress>(p =>
                            ShowConvertProgress(p.Percent, $"{t.Name}: {title} — {p.Percent:F0}%"));

                        ShowConvertProgress(0, $"{t.Name}: {title}...");
                        await source.Type.ConvertAsync(t.Id, mediaSource.ExternalId, source.Settings, progress, _convertCts.Token);
                        _logger?.LogInformation("Конвертация {Name} завершена: {ExternalId}", t.Name, mediaSource.ExternalId);
                        await _orcestrator.ForceUpdateMetadataAsync(media, source.Id);
                        RefreshData();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogInformation("Конвертация отменена пользователем: {ExternalId}", mediaSource.ExternalId);
                        MessageBox.Show("Конвертация отменена", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Ошибка конвертации {Name}: {ExternalId}", t.Name, mediaSource.ExternalId);
                        MessageBox.Show(ex.Message, "Ошибка конвертации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        HideConvertProgress();
                        _convertCts?.Dispose();
                        _convertCts = null;
                    }
                };

                _contextMenu.Items.Add(convertItem);
            }
        }

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Show(location);
        }
    }

    private void AddMetadataMenuItems(ContextMenuStrip contextMenu, Media media, Source? specificSource)
    {
        var viewMetaItem = new ToolStripMenuItem("Просмотр метаданных медиа", null);
        viewMetaItem.Click += (_, _) => ShowMediaDetail(media);
        contextMenu.Items.Add(viewMetaItem);

        var updateMetaItem = new ToolStripMenuItem("Принудительное обновление метаданных", GetSyncIcon());
        updateMetaItem.Click += async (_, _) => await HandleUpdateMetadataAsync(media);
        contextMenu.Items.Add(updateMetaItem);

        if (specificSource != null)
        {
            var clearSourceMetaItem = new ToolStripMenuItem($"Очистить метаданные источника ({specificSource.TitleFull})", GetDeleteIcon());
            clearSourceMetaItem.Click += (_, _) => HandleClearMetadata(media, specificSource.Id);
            contextMenu.Items.Add(clearSourceMetaItem);
        }
        else
        {
            var clearAllMetaItem = new ToolStripMenuItem("Очистить все метаданные", GetDeleteIcon());
            clearAllMetaItem.Click += (_, _) => HandleClearMetadata(media, null);
            contextMenu.Items.Add(clearAllMetaItem);
        }
    }

    private void AddSyncMenuItems(ContextMenuStrip contextMenu, Media media, Source? specificSource)
    {
        foreach (var rel in _orcestrator!.GetRelations())
        {
            if (specificSource != null && rel.From.Id != specificSource.Id && rel.To.Id != specificSource.Id)
            {
                continue;
            }

            var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
            var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
            var menuText = $"Синхронизировать {rel.From.TitleFull} -> {rel.To.TitleFull}";

            if (fromSource != null
                && fromSource.Status == MediaStatus.Ok
                && (toSource == null || toSource.Status != MediaStatus.Ok))
            {
                var menuItem = new ToolStripMenuItem(menuText, GetSyncIcon());
                menuItem.Click += async (_, _) =>
                {
                    UpdateLoadingIndicator(true);
                    try
                    {
                        _logger?.LogInformation("Запуск синхронизации медиа '{Title}' из {From} в {To}", media.Title, rel.From.TitleFull, rel.To.TitleFull);
                        await _orcestrator.TransferByRelation(media, rel);
                        _logger?.LogInformation("Синхронизация медиа '{Title}' успешно завершена", media.Title);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Ошибка при синхронизации медиа '{Title}' из {From} в {To}.", media.Title, rel.From.TitleFull, rel.To.TitleFull);
                        MessageBox.Show($"Ошибка при синхронизации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        UpdateLoadingIndicator(false);
                        RefreshData();
                    }
                };

                contextMenu.Items.Add(menuItem);
            }
            else
            {
                // после песенки отвечу
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
                else if (toSource != null && toSource.Status == MediaStatus.Ok)
                {
                    menuItem.ToolTipText = "Медиа уже существует в целевом хранилище";
                }

                contextMenu.Items.Add(menuItem);
            }
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

        _logger?.LogInformation("Попытка удаления медиа '{Title}' из источника '{Source}'", media.Title, source.TitleFull);
        var result = ShowDeleteConfirmation(media, source, isLastSource);
        if (result != DialogResult.Yes)
        {
            _logger?.LogInformation("Пользователь отменил удаление медиа '{Title}'", media.Title);
            return;
        }

        UpdateLoadingIndicator(true);

        try
        {
            await _orcestrator.DeleteMediaFromSourceAsync(media, source);
            _logger?.LogInformation("Медиа '{Title}' успешно удалено из источника '{Source}'", media.Title, source.TitleFull);
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
            _logger?.LogError(ex, "Ошибка авторизации при удалении медиа '{Title}' из '{Source}'.", media.Title, source.TitleFull);
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
            _logger?.LogError(ex, "Ошибка файловой системы при удалении медиа '{Title}' из '{Source}'.", media.Title, source.TitleFull);
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

    private void ShowMediaDetail(Media media)
    {
        var sources = _orcestrator?.GetSources() ?? [];
        var form = new MediaDetailForm(media, sources, _logger);
        form.Show(this);
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

    private async Task HandleUpdateMetadataAsync(Media media)
    {
        _logger?.LogInformation("Запуск принудительного обновления метаданных для медиа: '{Title}'", media.Title);
        UpdateLoadingIndicator(true);
        try
        {
            if (_orcestrator != null)
            {
                await _orcestrator.ForceUpdateMetadataAsync(media);
            }

            _logger?.LogInformation("Метаданные для медиа '{Title}' успешно обновлены", media.Title);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при обновлении метаданных для медиа '{Title}'.", media.Title);
            MessageBox.Show($"Ошибка при обновлении метаданных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateLoadingIndicator(false);
            RefreshData();
        }
    }

    private void HandleClearMetadata(Media media, string? sourceId)
    {
        if (_orcestrator == null)
        {
            return;
        }

        if (sourceId != null)
        {
            _logger?.LogInformation("Очистка метаданных для медиа '{Title}', источник: {SourceId}", media.Title, sourceId);
            _orcestrator.ClearSourceMetadata(media, sourceId);
        }
        else
        {
            _logger?.LogInformation("Очистка всех метаданных для медиа '{Title}'", media.Title);
            _orcestrator.ClearAllMetadata(media);
        }

        RefreshData();
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

            if (media.Metadata.Count > 0)
            {
                details.AppendLine();
                details.AppendLine("Метаданные:");
                foreach (var meta in media.Metadata)
                {
                    details.AppendLine($"  {meta.Key}: {meta.Value}");
                }
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
                    var status = MediaStatusHelper.GetById(sourceLink.Status);

                    details.AppendLine($"  {sourceName}: {status.IconText + " " + status.Text}");
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

    private sealed class MergePreviewData
    {
        public required Media TargetMedia { get; init; }

        public required List<Media> SourceMedias { get; init; }

        public required List<MediaSourceLink> ResultingSources { get; init; }

        public required List<string> Conflicts { get; init; }

        public int TotalSourcesCount => ResultingSources.Count;

        public bool HasConflicts => Conflicts.Count > 0;
    }

    private void uiConvertProgressBar_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            uiConvertCancelMenu.Show(Cursor.Position);
        }
    }
}
