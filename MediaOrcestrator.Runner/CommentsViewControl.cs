using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class CommentsViewControl : UserControl
{
    private const string AnySourceLabel = "(любой)";
    private const int MaxTextPreviewLength = 200;

    private Orcestrator? _orcestrator;
    private CommentsService? _commentsService;
    private ActionHolder? _actionHolder;
    private ILogger<CommentsViewControl>? _logger;
    private bool _loaded;

    public CommentsViewControl()
    {
        InitializeComponent();
    }

    public void Initialize(
        Orcestrator orcestrator,
        CommentsService commentsService,
        ActionHolder actionHolder,
        ILogger<CommentsViewControl> logger)
    {
        _orcestrator = orcestrator;
        _commentsService = commentsService;
        _actionHolder = actionHolder;
        _logger = logger;

        using (Splash.Current.StartSpan("Колонки таблицы"))
        {
            ConfigureColumns();
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

    private void uiRefreshButton_Click(object? sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void uiSourceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateForceFetchButtonState();
    }

    private async void uiForceFetchAllButton_Click(object? sender, EventArgs e)
    {
        if (_orcestrator == null || _commentsService == null || _actionHolder == null)
        {
            return;
        }

        var sourceItem = uiSourceComboBox.SelectedItem as SourceComboItem;
        if (sourceItem?.SourceId == null)
        {
            return;
        }

        var source = _orcestrator.GetSources().FirstOrDefault(s => s.Id == sourceItem.SourceId);
        if (source == null)
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

    private void uiSearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        ApplyFilters();
    }

    private void uiCommentsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _orcestrator == null || _actionHolder == null)
        {
            return;
        }

        var row = uiCommentsGrid.Rows[e.RowIndex].DataBoundItem as CommentRow;
        if (row?.MediaId == null)
        {
            return;
        }

        var media = _orcestrator.GetMedias().FirstOrDefault(m => m.Id == row.MediaId);
        if (media == null)
        {
            return;
        }

        var detail = new MediaDetailForm(media, _orcestrator, _actionHolder, _commentsService, _logger);
        detail.Show(this);
    }

    private static string BuildPreview(CommentRecord record)
    {
        if (record.IsDeleted)
        {
            return "[удалён]";
        }

        var text = record.Text ?? string.Empty;
        text = text.Replace("\r", " ").Replace("\n", " ");

        if (text.Length > MaxTextPreviewLength)
        {
            text = text[..MaxTextPreviewLength] + "…";
        }

        return text;
    }

    private void ConfigureColumns()
    {
        if (uiCommentsGrid.Columns.Count > 0)
        {
            return;
        }

        uiCommentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CommentRow.PublishedAt),
            HeaderText = "Дата",
            DefaultCellStyle = new()
                { Format = "g" },
            FillWeight = 12,
        });

        uiCommentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CommentRow.SourceTitle),
            HeaderText = "Источник",
            FillWeight = 14,
        });

        uiCommentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CommentRow.MediaTitle),
            HeaderText = "Медиа",
            FillWeight = 22,
        });

        uiCommentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CommentRow.AuthorName),
            HeaderText = "Автор",
            FillWeight = 12,
        });

        uiCommentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CommentRow.Preview),
            HeaderText = "Текст",
            FillWeight = 35,
            DefaultCellStyle = new()
                { WrapMode = DataGridViewTriState.False },
        });

        uiCommentsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CommentRow.Likes),
            HeaderText = "♥",
            FillWeight = 5,
            DefaultCellStyle = new()
                { Alignment = DataGridViewContentAlignment.MiddleRight },
        });
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
        if (_orcestrator == null || _commentsService == null)
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
                limit: limit);

            var rows = MapToRows(records);

            uiCommentsGrid.DataSource = null;
            uiCommentsGrid.DataSource = rows;
            uiCountLabel.Text = $"Найдено: {rows.Count}";
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

    private List<CommentRow> MapToRows(IReadOnlyList<CommentRecord> records)
    {
        if (records.Count == 0 || _orcestrator == null)
        {
            return [];
        }

        var sourceTitles = _orcestrator.GetSources().ToDictionary(x => x.Id, x => x.TitleFull);
        var mediaByLink = new Dictionary<(string, string), Media>();

        foreach (var media in _orcestrator.GetMedias())
        {
            foreach (var link in media.Sources)
            {
                if (link is { SourceId: not null, ExternalId: not null })
                {
                    mediaByLink[(link.SourceId, link.ExternalId)] = media;
                }
            }
        }

        var rows = new List<CommentRow>(records.Count);
        foreach (var record in records)
        {
            mediaByLink.TryGetValue((record.SourceId, record.ExternalMediaId), out var media);
            sourceTitles.TryGetValue(record.SourceId, out var sourceTitle);

            rows.Add(new()
            {
                PublishedAt = record.PublishedAt.ToLocalTime(),
                SourceTitle = sourceTitle ?? record.SourceId,
                MediaTitle = media?.Title ?? $"<{record.ExternalMediaId}>",
                AuthorName = record.AuthorName,
                Preview = BuildPreview(record),
                Likes = record.LikeCount,
                MediaId = media?.Id,
            });
        }

        return rows;
    }

    private void UpdateForceFetchButtonState()
    {
        var sourceItem = uiSourceComboBox.SelectedItem as SourceComboItem;
        uiForceFetchAllButton.Enabled = sourceItem?.SourceId != null;
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

    private sealed class CommentRow
    {
        public DateTime PublishedAt { get; set; }
        public string SourceTitle { get; set; } = string.Empty;
        public string MediaTitle { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public int? Likes { get; set; }
        public string? MediaId { get; set; }
    }
}
