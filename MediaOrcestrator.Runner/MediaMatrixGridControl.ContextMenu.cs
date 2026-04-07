using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing.Text;
using System.Text;

namespace MediaOrcestrator.Runner;

// TODO: Костыль чтобы не передавать зависимости через конструктор контрола
public partial class MediaMatrixGridControl
{
    private static Bitmap? _syncIcon;
    private static Bitmap? _copyIcon;
    private static Bitmap? _deleteIcon;
    private static Bitmap? _renameIcon;
    private static Bitmap? _mergeIcon;
    private static Bitmap? _convertIcon;
    private static Bitmap? _infoIcon;
    private static Bitmap? _openIcon;

    private static Bitmap? SyncIcon => _syncIcon ??= CreateTextIcon("→", Color.Blue);
    private static Bitmap? CopyIcon => _copyIcon ??= CreateTextIcon("📋", Color.DarkGray);
    private static Bitmap? DeleteIcon => _deleteIcon ??= CreateTextIcon("✕", Color.Red);
    private static Bitmap? RenameIcon => _renameIcon ??= CreateTextIcon("✎", Color.DarkOrange);
    private static Bitmap? MergeIcon => _mergeIcon ??= CreateTextIcon("⊕", Color.Purple);
    private static Bitmap? ConvertIcon => _convertIcon ??= CreateTextIcon("↻", Color.Teal);
    private static Bitmap? InfoIcon => _infoIcon ??= CreateTextIcon("🔍", Color.SteelBlue);
    private static Bitmap? OpenIcon => _openIcon ??= CreateTextIcon("↗", Color.DodgerBlue);

    private static Bitmap? CreateTextIcon(string text, Color color)
    {
        try
        {
            var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using var font = new Font("Segoe UI Symbol", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);

            var size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, (16 - size.Width) / 2, (16 - size.Height) / 2);

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static DialogResult ShowDeleteConfirmation(Media media, Source source, bool isLastSource)
    {
        var message = isLastSource
            ? $"""
               Вы уверены, что хотите удалить медиа "{media.Title}" из {source.TitleFull}?

               ВНИМАНИЕ: Это последний источник для данного медиа. Запись будет полностью удалена из базы данных.
               """
            : $"""
               Вы уверены, что хотите удалить медиа "{media.Title}" из {source.TitleFull}?

               Медиа останется в других источниках.
               """;

        return MessageBox.Show(message,
            "Подтверждение удаления",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
    }

    private static void ShowBatchErrors(string bodyPrefix, string title, int succeeded, int total, List<(Media media, Exception ex)> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        var errorDetails = string.Join("\n", errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));

        MessageBox.Show($"""
                         {bodyPrefix}: {succeeded} из {total}

                         Ошибки:
                         {errorDetails}
                         """,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
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

    private async void ShowBatchContextMenu(List<Media> selectedMedia, Point location)
    {
        _contextMenu?.Dispose();
        _contextMenu = new();

        _contextMenu.Items.Add(new ToolStripMenuItem($"Выбрано: {selectedMedia.Count} медиа") { Enabled = false });
        _contextMenu.Items.Add(new ToolStripSeparator());

        AddBatchSyncMenuItems(selectedMedia);
        _contextMenu.Items.Add(new ToolStripSeparator());

        var updateMetaItem = new ToolStripMenuItem($"Обновить метаданные ({selectedMedia.Count})", SyncIcon);
        updateMetaItem.Click += async (_, _) => await HandleBatchUpdateMetadataAsync(selectedMedia);
        _contextMenu.Items.Add(updateMetaItem);

        var renameItem = new ToolStripMenuItem($"Пакетное переименование ({selectedMedia.Count})...", RenameIcon);
        renameItem.Click += (_, _) => HandleBatchRename(selectedMedia);
        _contextMenu.Items.Add(renameItem);

        if (selectedMedia.Count >= 2)
        {
            var mergeItem = new ToolStripMenuItem($"Объединить ({selectedMedia.Count})", MergeIcon);
            mergeItem.Click += (_, _) => MergeSelectedMedia(uiMediaGrid.GetSelectedMediaBySelectionOrder());
            _contextMenu.Items.Add(mergeItem);
        }

        var allSources = _orcestrator!.GetSources().Where(s => !s.IsDisable).ToList();

        _contextMenu.Items.Add(new ToolStripSeparator());
        AddBatchDeleteMenuItems(selectedMedia, allSources);

        var convertSeparator = new ToolStripSeparator();
        _contextMenu.Items.Add(convertSeparator);
        var loadingItem = new ToolStripMenuItem("Загрузка конвертаций...", ConvertIcon) { Enabled = false };
        _contextMenu.Items.Add(loadingItem);

        _contextMenu.Show(location);

        await AddBatchConvertMenuItemsAsync(selectedMedia, allSources);

        _contextMenu.Items.Remove(loadingItem);
        loadingItem.Dispose();

        if (!_contextMenu.Items.OfType<ToolStripMenuItem>().Any(i => i.Text?.StartsWith("Конвертировать") == true))
        {
            _contextMenu.Items.Remove(convertSeparator);
        }
    }

    private void AddBatchSyncMenuItems(List<Media> selectedMedia)
    {
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

            var menuItem = new ToolStripMenuItem($"Синхронизировать {rel.From.TitleFull} → {rel.To.TitleFull} ({eligibleMedia.Count})",
                SyncIcon);

            if (eligibleMedia.Count == 0)
            {
                menuItem.Enabled = false;
                menuItem.ToolTipText = "Нет подходящих медиа для синхронизации";
            }
            else
            {
                menuItem.Click += async (_, _) => await HandleBatchSyncAsync(eligibleMedia, rel);
            }

            _contextMenu!.Items.Add(menuItem);
        }
    }

    private void AddBatchDeleteMenuItems(List<Media> selectedMedia, List<Source> allSources)
    {
        foreach (var source in allSources)
        {
            var sourceMedia = selectedMedia
                .Where(m => m.Sources.Any(s => s.SourceId == source.Id))
                .ToList();

            if (sourceMedia.Count == 0)
            {
                continue;
            }

            var deleteItem = new ToolStripMenuItem($"Удалить из {source.TitleFull} ({sourceMedia.Count})", DeleteIcon);
            deleteItem.Click += async (_, _) => await HandleBatchDeleteAsync(sourceMedia, source);
            _contextMenu!.Items.Add(deleteItem);
        }
    }

    private async Task AddBatchConvertMenuItemsAsync(List<Media> selectedMedia, List<Source> allSources)
    {
        foreach (var source in allSources)
        {
            var convertTypes = source.Type.GetAvailableConvertTypes();
            if (convertTypes.Length == 0)
            {
                continue;
            }

            var sourceMedia = selectedMedia
                .Where(m => m.Sources.Any(s => s.SourceId == source.Id))
                .ToList();

            if (sourceMedia.Count == 0)
            {
                continue;
            }

            var mediaDtos = new Dictionary<Media, MediaDto?>();
            foreach (var media in sourceMedia)
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
                var eligibleForConvert = sourceMedia
                    .Where(m => mediaDtos.TryGetValue(m, out var dto)
                                && dto != null
                                && source.Type.CheckConvertAvailability(t.Id, dto).IsAvailable)
                    .ToList();

                var convertItem = new ToolStripMenuItem($"Конвертировать {t.Name} ({eligibleForConvert.Count})", ConvertIcon);

                if (eligibleForConvert.Count == 0)
                {
                    convertItem.Enabled = false;
                    convertItem.ToolTipText = "Нет подходящих медиа для конвертации";
                }
                else
                {
                    convertItem.Click += async (_, _) => await HandleBatchConvertAsync(eligibleForConvert, source, t);
                }

                _contextMenu!.Items.Add(convertItem);
            }
        }
    }

    private async void ShowContextMenu(Media media, Point location, Source? specificSource = null)
    {
        _contextMenu?.Dispose();
        _contextMenu = new();

        AddMetadataMenuItems(_contextMenu, media, specificSource);
        _contextMenu.Items.Add(new ToolStripSeparator());

        AddSyncMenuItems(_contextMenu, media, specificSource);
        AddDeleteMenuItems(media, specificSource);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var copyItem = new ToolStripMenuItem("Копировать детали в буфер обмена", CopyIcon);
        copyItem.Click += (_, _) => CopyMediaDetailsToClipboard(media);
        _contextMenu.Items.Add(copyItem);

        var allSources = _orcestrator!.GetSources();
        var relevantSourceLinks = specificSource != null
            ? media.Sources.Where(s => s.SourceId == specificSource.Id).ToList()
            : media.Sources;

        AddOpenExternalLinks(relevantSourceLinks, allSources);
        await AddConvertMenuItemsAsync(media, relevantSourceLinks, allSources);

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Show(location);
        }
    }

    private void AddMetadataMenuItems(ContextMenuStrip contextMenu, Media media, Source? specificSource)
    {
        var viewMetaItem = new ToolStripMenuItem("Просмотр метаданных медиа", InfoIcon);
        viewMetaItem.Click += (_, _) => ShowMediaDetail(media);
        contextMenu.Items.Add(viewMetaItem);

        var updateMetaItem = new ToolStripMenuItem("Принудительное обновление метаданных", SyncIcon);
        updateMetaItem.Click += async (_, _) => await HandleUpdateMetadataAsync(media);
        contextMenu.Items.Add(updateMetaItem);

        if (specificSource != null)
        {
            var clearSourceMetaItem = new ToolStripMenuItem($"Очистить метаданные источника ({specificSource.TitleFull})", DeleteIcon);
            clearSourceMetaItem.Click += (_, _) => HandleClearMetadata(media, specificSource.Id);
            contextMenu.Items.Add(clearSourceMetaItem);
        }
        else
        {
            var clearAllMetaItem = new ToolStripMenuItem("Очистить все метаданные", DeleteIcon);
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

            if (fromSource is { Status: MediaStatus.Ok }
                && toSource is not { Status: MediaStatus.Ok })
            {
                var menuItem = new ToolStripMenuItem(menuText, SyncIcon);
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
                var menuItem = new ToolStripMenuItem(menuText, SyncIcon)
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

    private void AddDeleteMenuItems(Media media, Source? specificSource)
    {
        if (specificSource != null)
        {
            var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == specificSource.Id);

            if (sourceLink == null || specificSource.IsDisable)
            {
                return;
            }

            _contextMenu!.Items.Add(new ToolStripSeparator());

            var deleteMenuItem = new ToolStripMenuItem($"Удалить из {specificSource.TitleFull}", DeleteIcon);
            deleteMenuItem.Click += async (_, _) => await HandleDeleteClickAsync(media, specificSource);
            _contextMenu.Items.Add(deleteMenuItem);
        }
        else
        {
            _contextMenu!.Items.Add(new ToolStripSeparator());

            foreach (var sourceLink in media.Sources)
            {
                var source = _orcestrator!.GetSources()
                    .FirstOrDefault(s => s.Id == sourceLink.SourceId);

                if (source is not { IsDisable: false })
                {
                    continue;
                }

                var deleteMenuItem = new ToolStripMenuItem($"Удалить из {source.TitleFull}", DeleteIcon);
                deleteMenuItem.Click += async (_, _) => await HandleDeleteClickAsync(media, source);
                _contextMenu.Items.Add(deleteMenuItem);
            }
        }
    }

    private void AddOpenExternalLinks(IEnumerable<MediaSourceLink> relevantSourceLinks, List<Source> allSources)
    {
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

            var openItem = new ToolStripMenuItem("Открыть: " + source.TitleFull, OpenIcon);
            openItem.Click += (_, _) => Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
            _contextMenu!.Items.Add(openItem);
        }
    }

    private async Task AddConvertMenuItemsAsync(Media media, IEnumerable<MediaSourceLink> relevantSourceLinks, List<Source> allSources)
    {
        foreach (var mediaSource in relevantSourceLinks)
        {
            var source = allSources.FirstOrDefault(x => x.Id == mediaSource.SourceId);
            if (source?.Type == null)
            {
                continue;
            }

            var convertTypes = source.Type.GetAvailableConvertTypes();
            if (convertTypes.Length == 0)
            {
                continue;
            }

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

                var convertItem = new ToolStripMenuItem($"Конвертировать {t.Name} ({source.TitleFull})", ConvertIcon)
                {
                    Enabled = availability.IsAvailable,
                    ToolTipText = availability.IsAvailable ? null : availability.Reason,
                };

                convertItem.Click += async (_, _) => await HandleSingleConvertAsync(media, mediaSource, source, t);
                _contextMenu!.Items.Add(convertItem);
            }
        }
    }

    private Task HandleBatchSyncAsync(List<Media> mediaList, SourceSyncRelation rel)
    {
        var result = MessageBox.Show($"Синхронизировать {mediaList.Count} медиа из {rel.From.TitleFull} в {rel.To.TitleFull}?",
            "Подтверждение пакетной синхронизации",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return Task.CompletedTask;
        }

        _logger?.LogInformation("Запуск пакетной синхронизации {Count} медиа: {From} → {To}",
            mediaList.Count, rel.From.TitleFull, rel.To.TitleFull);

        return RunBatchOperationAsync("Синхронизировано", "Пакетная синхронизация завершена с ошибками", mediaList, async media =>
        {
            await _orcestrator!.TransferByRelation(media, rel);
            _logger?.LogInformation("Синхронизировано: '{Title}'", media.Title);
        });
    }

    private Task HandleBatchDeleteAsync(List<Media> mediaList, Source source)
    {
        var result = MessageBox.Show($"Удалить {mediaList.Count} медиа из {source.TitleFull}?\n\nЭто действие нельзя отменить.",
            "Подтверждение пакетного удаления",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            return Task.CompletedTask;
        }

        _logger?.LogInformation("Запуск пакетного удаления {Count} медиа из '{Source}'",
            mediaList.Count, source.TitleFull);

        return RunBatchOperationAsync("Удалено", "Пакетное удаление завершено с ошибками", mediaList, async media =>
        {
            await _orcestrator!.DeleteMediaFromSourceAsync(media, source);
            _logger?.LogInformation("Удалено: '{Title}' из '{Source}'", media.Title, source.TitleFull);
        });
    }

    private void HandleBatchRename(List<Media> selectedMedia)
    {
        using var form = new BatchRenameForm(selectedMedia, _batchRenameService!);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            RefreshData();
        }
    }

    private Task HandleBatchUpdateMetadataAsync(List<Media> mediaList)
    {
        _logger?.LogInformation("Запуск пакетного обновления метаданных для {Count} медиа", mediaList.Count);

        return RunBatchOperationAsync("Обновлено", "Обновление метаданных завершено с ошибками", mediaList, async media =>
        {
            await _orcestrator!.ForceUpdateMetadataAsync(media);
            _logger?.LogInformation("Метаданные обновлены: '{Title}'", media.Title);
        });
    }

    private async Task HandleBatchConvertAsync(List<Media> eligibleMedia, Source source, ConvertType convertType)
    {
        _convertCts = new();
        var errors = new List<(Media media, Exception ex)>();
        var total = eligibleMedia.Count;
        var converted = 0;

        try
        {
            for (var i = 0; i < total; i++)
            {
                var media = eligibleMedia[i];
                var mediaSource = media.Sources.First(s => s.SourceId == source.Id);
                var index = i;

                var progress = new Progress<ConvertProgress>(p =>
                    ShowConvertProgress(p.Percent, $"{convertType.Name} [{index + 1}/{total}]: {media.Title} — {p.Percent:F0}%"));

                ShowConvertProgress(0, $"{convertType.Name} [{i + 1}/{total}]: {media.Title}...");

                try
                {
                    await source.Type.ConvertAsync(convertType.Id, mediaSource.ExternalId, source.Settings, progress, _convertCts.Token);
                    await _orcestrator!.ForceUpdateMetadataAsync(media, source.Id);
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

            ShowBatchErrors("Конвертировано", "Конвертация завершена с ошибками", converted, total, errors);
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
    }

    private async Task HandleSingleConvertAsync(Media media, MediaSourceLink mediaSource, Source source, ConvertType convertType)
    {
        try
        {
            var dto = await source.Type.GetMediaByIdAsync(mediaSource.ExternalId, source.Settings);
            if (dto == null)
            {
                MessageBox.Show("Не удалось получить метаданные", "Конвертация недоступна",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            var availability = source.Type.CheckConvertAvailability(convertType.Id, dto);
            if (!availability.IsAvailable)
            {
                MessageBox.Show(availability.Reason ?? "Конвертация недоступна", "Конвертация недоступна",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Не удалось проверить доступность конвертации для {Source}: {ExternalId}",
                source.TitleFull, mediaSource.ExternalId);

            MessageBox.Show($"Не удалось проверить доступность: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }

        _logger?.LogInformation("Запуск конвертации {Name} для {ExternalId} ({Source})",
            convertType.Name, mediaSource.ExternalId, source.TitleFull);

        _convertCts = new();
        try
        {
            var progress = new Progress<ConvertProgress>(p =>
                ShowConvertProgress(p.Percent, $"{convertType.Name}: {media.Title} — {p.Percent:F0}%"));

            ShowConvertProgress(0, $"{convertType.Name}: {media.Title}...");
            await source.Type.ConvertAsync(convertType.Id, mediaSource.ExternalId, source.Settings, progress, _convertCts.Token);
            _logger?.LogInformation("Конвертация {Name} завершена: {ExternalId}", convertType.Name, mediaSource.ExternalId);
            await _orcestrator!.ForceUpdateMetadataAsync(media, source.Id);
            RefreshData();
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Конвертация отменена пользователем: {ExternalId}", mediaSource.ExternalId);
            MessageBox.Show("Конвертация отменена", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка конвертации {Name}: {ExternalId}", convertType.Name, mediaSource.ExternalId);
            MessageBox.Show(ex.Message, "Ошибка конвертации", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            HideConvertProgress();
            _convertCts?.Dispose();
            _convertCts = null;
        }
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
            await _orcestrator!.DeleteMediaFromSourceAsync(media, source);
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

    private void ShowMediaDetail(Media media)
    {
        var sources = _orcestrator?.GetSources() ?? [];
        var form = new MediaDetailForm(media, sources, _logger);
        form.Show(this);
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

    private async Task RunBatchOperationAsync(string bodyPrefix, string errorTitle, List<Media> mediaList, Func<Media, Task> action)
    {
        UpdateLoadingIndicator(true);
        var errors = new List<(Media media, Exception ex)>();

        try
        {
            foreach (var media in mediaList)
            {
                try
                {
                    await action(media);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка операции для '{Title}'", media.Title);
                    errors.Add((media, ex));
                }
            }

            ShowBatchErrors(bodyPrefix, errorTitle, mediaList.Count - errors.Count, mediaList.Count, errors);
        }
        finally
        {
            UpdateLoadingIndicator(false);
            RefreshData();
        }
    }
}
