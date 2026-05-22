using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class BatchRenameForm : Form
{
    private readonly List<Media> _medias;
    private readonly BatchRenameService _service;
    private readonly Dictionary<string, int> _rowIndexByMediaId = new();
    private readonly Dictionary<string, CheckBox> _sourceChecks = new();
    private readonly SynchronizationContext _uiContext;
    private readonly string? _specificSourceId;
    private readonly IReadOnlyCollection<string>? _gridSourceIds;

    private CancellationTokenSource? _applyCts;
    private bool _isApplying;
    private bool _suppressEvents;

    public BatchRenameForm()
    {
        _medias = [];
        _service = null!;
        _uiContext = SynchronizationContext.Current ?? new();
        InitializeComponent();
        InitializeModeCombo();
    }

    public BatchRenameForm(
        List<Media> medias,
        BatchRenameService service,
        Source? specificSource = null,
        IReadOnlyCollection<string>? gridSourceIds = null) : this()
    {
        _medias = medias;
        _service = service;
        _specificSourceId = specificSource?.Id;
        _gridSourceIds = gridSourceIds;
        Text = medias.Count == 1
            ? $"Переименование «{Truncate(medias[0].Title, 60)}»"
            : $"Пакетное переименование ({medias.Count})";
    }

    public event EventHandler? DataChanged;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        BuildSourcePicker();
        PopulateRows();
        RefreshPreview();
        uiFindTextBox.Focus();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isApplying)
        {
            CancelApply();
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _applyCts?.Dispose();
        _applyCts = null;
        base.OnFormClosed(e);
    }

    private void uiAnyOption_Changed(object? sender, EventArgs e)
    {
        if (_suppressEvents || _isApplying)
        {
            return;
        }

        uiPreviewTimer.Stop();
        uiPreviewTimer.Start();
    }

    private void uiPreviewTimer_Tick(object? sender, EventArgs e)
    {
        uiPreviewTimer.Stop();
        RefreshPreview();
    }

    private void uiPreviewGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (uiPreviewGrid.CurrentCell is DataGridViewCheckBoxCell)
        {
            uiPreviewGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void uiPreviewGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressEvents || e.RowIndex < 0)
        {
            return;
        }

        var row = uiPreviewGrid.Rows[e.RowIndex];

        if (e.ColumnIndex == uiNewTitleColumn.Index && row.Tag is RowState state)
        {
            var newValue = row.Cells[uiNewTitleColumn.Index].Value?.ToString() ?? string.Empty;
            state.IsManuallyEdited = !string.Equals(newValue, state.PreviewNewTitle, StringComparison.Ordinal);
            UpdateResetEditsVisibility();
            UpdateApplyButtonState();
            RepaintRow(row, state);
        }
        else if (e.ColumnIndex == uiApplyColumn.Index)
        {
            UpdateApplyButtonState();
            RepaintRow(row, row.Tag as RowState);
        }
    }

    private void uiResetEditsLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        _suppressEvents = true;
        try
        {
            foreach (DataGridViewRow row in uiPreviewGrid.Rows)
            {
                if (row.Tag is RowState state && state.IsManuallyEdited)
                {
                    state.IsManuallyEdited = false;
                    row.Cells[uiNewTitleColumn.Index].Value = state.PreviewNewTitle;
                }
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        UpdateResetEditsVisibility();
        UpdateApplyButtonState();
        RefreshPreview();
    }

    private async void uiApplyButton_Click(object? sender, EventArgs e)
    {
        var requests = BuildRequests();
        if (requests.Count == 0)
        {
            return;
        }

        SetApplyingState(true, requests.Count);
        Log($"Запуск переименования — записей: {requests.Count}");

        _applyCts?.Dispose();
        _applyCts = new();
        var token = _applyCts.Token;

        var progress = new Progress<BatchRenameProgress>(OnProgress);

        IReadOnlyList<BatchRenameResult> results;
        try
        {
            results = await Task.Run(() => _service.ApplyAsync(requests,
                    progress,
                    result => _uiContext.Post(_ => OnMediaProcessed(result), null),
                    token),
                token);
        }
        catch (OperationCanceledException)
        {
            if (IsDisposed)
            {
                return;
            }

            SetApplyingState(false);
            uiStatusLabel.Text = "Отменено пользователем";
            Log("Отменено пользователем");
            return;
        }
        catch (Exception ex)
        {
            if (IsDisposed)
            {
                return;
            }

            SetApplyingState(false);
            uiStatusLabel.Text = $"Ошибка: {ex.Message}";
            Log($"Ошибка: {ex.Message}");
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        SetApplyingState(false);

        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);
        uiStatusLabel.Text = failCount == 0
            ? $"Готово: переименовано {successCount} из {results.Count}"
            : $"Готово: {successCount} успешно, {failCount} с ошибками";

        Log(uiStatusLabel.Text);

        if (successCount > 0)
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void uiCancelButton_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private static bool IsRowChecked(DataGridViewRow row)
    {
        return row.Cells[0].Value is bool b && b;
    }

    private static string FormatResultLine(BatchRenameResult result)
    {
        if (result.Sources.Count == 0)
        {
            return $"—  «{result.OldTitle}»: без изменений";
        }

        var sources = "  ·  " + JoinOutcomes(result.Sources);

        return result.Success
            ? $"✓  «{result.OldTitle}» → «{result.NewTitle}»{sources}"
            : $"✘  «{result.OldTitle}»: {result.ErrorMessage ?? "ошибка"}{sources}";
    }

    private static string JoinOutcomes(IReadOnlyList<BatchRenameSourceResult> sources)
    {
        return string.Join(", ",
            sources.Select(s => s.Outcome switch
            {
                BatchRenameSourceOutcome.Updated => $"{s.SourceTitle}: ок",
                BatchRenameSourceOutcome.Skipped => $"{s.SourceTitle}: пропуск",
                BatchRenameSourceOutcome.NotSupported => $"{s.SourceTitle}: не поддерж.",
                BatchRenameSourceOutcome.Failed => $"{s.SourceTitle}: ошибка",
                BatchRenameSourceOutcome.VerificationFailed => $"{s.SourceTitle}: не применилось",
                _ => s.SourceTitle,
            }));
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value ?? string.Empty;
        }

        return value.AsSpan(0, max - 1).ToString() + "…";
    }

    private void InitializeModeCombo()
    {
        uiModeCombo.Items.Add(new ModeItem(BatchRenameMode.Plain, "Замена"));
        uiModeCombo.Items.Add(new ModeItem(BatchRenameMode.Regex, "Регулярное выражение"));
        uiModeCombo.SelectedIndex = 0;
    }

    private void BuildSourcePicker()
    {
        _suppressEvents = true;
        try
        {
            uiSourcesPanel.Controls.Clear();
            _sourceChecks.Clear();

            var referenced = _service.GetReferencedSources(_medias);

            if (_gridSourceIds != null)
            {
                referenced = referenced.Where(r => _gridSourceIds.Contains(r.SourceId)).ToList();
            }

            var restrictToSpecific = _specificSourceId != null
                                     && referenced.Any(r => r.SourceId == _specificSourceId);

            foreach (var info in referenced)
            {
                var checkbox = new CheckBox
                {
                    Text = info.Title,
                    Checked = !restrictToSpecific || info.SourceId == _specificSourceId,
                    AutoSize = true,
                    Tag = info.SourceId,
                    Margin = new(0, 4, 12, 4),
                };

                checkbox.CheckedChanged += uiAnyOption_Changed;

                uiSourcesPanel.Controls.Add(checkbox);
                _sourceChecks[info.SourceId] = checkbox;
            }

            if (referenced.Count >= 2)
            {
                uiSourcesPanel.Controls.Add(uiSourcesAllLink);
                uiSourcesPanel.Controls.Add(uiSourcesNoneLink);
            }

            uiSourcesLabel.Visible = referenced.Count > 0;
            uiSourcesPanel.Visible = referenced.Count > 0;
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void SetAllSources(bool value)
    {
        _suppressEvents = true;
        try
        {
            foreach (var checkbox in _sourceChecks.Values)
            {
                checkbox.Checked = value;
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        RefreshPreview();
    }

    private void PopulateRows()
    {
        _suppressEvents = true;
        try
        {
            uiPreviewGrid.Rows.Clear();
            _rowIndexByMediaId.Clear();

            foreach (var media in _medias)
            {
                var idx = uiPreviewGrid.Rows.Add(true, media.Title ?? string.Empty, media.Title ?? string.Empty);
                var row = uiPreviewGrid.Rows[idx];
                row.Tag = new RowState(media);
                _rowIndexByMediaId[media.Id] = idx;
            }
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private HashSet<string>? CurrentAllowedSourceIds()
    {
        if (_sourceChecks.Count == 0)
        {
            if (_gridSourceIds != null)
            {
                return [];
            }

            return null;
        }

        var all = _sourceChecks.Values.All(c => c.Checked);

        if (all && _gridSourceIds == null)
        {
            return null;
        }

        return _sourceChecks
            .Where(kv => kv.Value.Checked)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    private BatchRenameOptions CurrentOptions()
    {
        var mode = uiModeCombo.SelectedItem is ModeItem item ? item.Mode : BatchRenameMode.Plain;
        return new(uiFindTextBox.Text,
            uiReplaceTextBox.Text,
            mode,
            uiIgnoreCaseCheck.Checked,
            CurrentAllowedSourceIds());
    }

    private void RefreshPreview()
    {
        var options = CurrentOptions();
        var previews = _service.Preview(_medias, options);
        var hasGlobalError = false;
        var changes = 0;

        _suppressEvents = true;
        try
        {
            foreach (var preview in previews)
            {
                if (!_rowIndexByMediaId.TryGetValue(preview.Media.Id, out var idx))
                {
                    continue;
                }

                var row = uiPreviewGrid.Rows[idx];
                var state = row.Tag as RowState ?? new RowState(preview.Media);
                row.Tag = state;

                state.PreviewNewTitle = preview.NewTitle;
                state.PreviewError = preview.Error;
                state.UpdatableSources = preview.Sources.Where(s => s.CanUpdate).Select(s => s.SourceTitle).ToArray();
                state.BlockedSources = preview.Sources.Where(s => !s.CanUpdate)
                    .Select(s => $"{s.SourceTitle}: {s.SkipReason}")
                    .ToArray();

                if (preview.Error != null)
                {
                    hasGlobalError = true;
                }

                if (!state.IsManuallyEdited)
                {
                    row.Cells[uiNewTitleColumn.Index].Value = preview.NewTitle;
                }

                var effectiveNewTitle = row.Cells[uiNewTitleColumn.Index].Value?.ToString() ?? string.Empty;
                var titleChanges = !string.Equals(effectiveNewTitle, preview.OldTitle, StringComparison.Ordinal);

                if (titleChanges && IsRowChecked(row) && state.UpdatableSources.Length > 0)
                {
                    changes++;
                }

                RepaintRow(row, state);
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        uiErrorLabel.Text = hasGlobalError
            ? previews.FirstOrDefault(p => p.Error != null)?.Error ?? string.Empty
            : string.Empty;

        UpdateApplyButtonState(changes);
        UpdateStatusLine(changes, previews.Count);
        UpdateResetEditsVisibility();
    }

    private void UpdateApplyButtonState(int? cachedChangeCount = null)
    {
        if (_isApplying)
        {
            return;
        }

        var changes = cachedChangeCount ?? CountPendingChanges();
        uiApplyButton.Text = changes > 0
            ? $"Применить ({changes})"
            : "Применить";

        var noRegexError = string.IsNullOrEmpty(uiErrorLabel.Text);
        uiApplyButton.Enabled = changes > 0 && noRegexError;
    }

    private int CountPendingChanges()
    {
        var changes = 0;
        foreach (DataGridViewRow row in uiPreviewGrid.Rows)
        {
            if (row.Tag is not RowState state)
            {
                continue;
            }

            if (!IsRowChecked(row) || state.UpdatableSources.Length == 0)
            {
                continue;
            }

            var newTitle = row.Cells[uiNewTitleColumn.Index].Value?.ToString() ?? string.Empty;
            var oldTitle = state.Media.Title ?? string.Empty;

            if (!string.Equals(newTitle, oldTitle, StringComparison.Ordinal))
            {
                changes++;
            }
        }

        return changes;
    }

    private void UpdateStatusLine(int changes, int total)
    {
        if (_isApplying)
        {
            return;
        }

        var allowed = CurrentAllowedSourceIds();
        var totalSourcesCount = _sourceChecks.Count;
        var allowedCount = allowed?.Count ?? totalSourcesCount;

        var sourcesPart = totalSourcesCount > 0
            ? $" · площадки: {allowedCount}/{totalSourcesCount}"
            : string.Empty;

        uiStatusLabel.Text = changes == 0
            ? $"Изменений нет (всего записей: {total}){sourcesPart}"
            : $"К изменению: {changes} из {total}{sourcesPart}";
    }

    private void UpdateResetEditsVisibility()
    {
        var anyEdited = false;
        foreach (DataGridViewRow row in uiPreviewGrid.Rows)
        {
            if (row.Tag is RowState { IsManuallyEdited: true })
            {
                anyEdited = true;
                break;
            }
        }

        uiResetEditsLink.Visible = anyEdited;
    }

    private void RepaintRow(DataGridViewRow row, RowState? state)
    {
        if (state == null)
        {
            return;
        }

        var newCell = row.Cells[uiNewTitleColumn.Index];
        var checkCell = row.Cells[uiApplyColumn.Index];

        var newValue = newCell.Value?.ToString() ?? string.Empty;
        var oldValue = state.Media.Title ?? string.Empty;
        var rowChecked = IsRowChecked(row);
        var hasUpdatable = state.UpdatableSources.Length > 0;
        var titleChanged = !string.Equals(newValue, oldValue, StringComparison.Ordinal);

        if (state.IsApplied)
        {
            newCell.Style.ForeColor = state.LastApplySuccess ? Color.DarkGreen : Color.DarkRed;
            newCell.ToolTipText = string.Empty;
            row.DefaultCellStyle.BackColor = Color.White;
            return;
        }

        if (state.PreviewError != null)
        {
            newCell.Style.ForeColor = Color.DarkRed;
            newCell.ToolTipText = "Ошибка регулярного выражения";
            return;
        }

        if (!hasUpdatable)
        {
            newCell.Style.ForeColor = Color.Gray;
            checkCell.Style.ForeColor = Color.Gray;
            row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            newCell.ToolTipText = state.BlockedSources.Length > 0
                ? "Недоступные площадки:" + Environment.NewLine + string.Join(Environment.NewLine, state.BlockedSources)
                : "Нет привязанных площадок";

            return;
        }

        row.DefaultCellStyle.BackColor = Color.White;

        if (!rowChecked)
        {
            newCell.Style.ForeColor = Color.Gray;
            newCell.ToolTipText = "Запись снята — переименование не применится";
            return;
        }

        if (!titleChanged)
        {
            newCell.Style.ForeColor = Color.Gray;
            newCell.ToolTipText = "Название не меняется";
            return;
        }

        newCell.Style.ForeColor = Color.Black;

        var tooltip = "Будет применено к площадкам: " + string.Join(", ", state.UpdatableSources);

        if (state.BlockedSources.Length > 0)
        {
            tooltip += Environment.NewLine + "Недоступны: " + string.Join(", ", state.BlockedSources);
        }

        newCell.ToolTipText = tooltip;
    }

    private void CancelApply()
    {
        try
        {
            _applyCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private List<BatchRenameRequest> BuildRequests()
    {
        var allowed = CurrentAllowedSourceIds();
        var requests = new List<BatchRenameRequest>();

        foreach (DataGridViewRow row in uiPreviewGrid.Rows)
        {
            if (row.Tag is not RowState state)
            {
                continue;
            }

            if (!IsRowChecked(row) || state.UpdatableSources.Length == 0 || state.PreviewError != null)
            {
                continue;
            }

            var newTitle = row.Cells[uiNewTitleColumn.Index].Value?.ToString() ?? string.Empty;
            var oldTitle = state.Media.Title ?? string.Empty;

            if (string.Equals(newTitle, oldTitle, StringComparison.Ordinal))
            {
                continue;
            }

            requests.Add(new(state.Media, newTitle, null, allowed));
        }

        return requests;
    }

    private void OnProgress(BatchRenameProgress p)
    {
        if (IsDisposed)
        {
            return;
        }

        if (p.Total > 0)
        {
            uiProgressBar.Maximum = p.Total;
            uiProgressBar.Value = Math.Min(p.Processed, p.Total);
        }

        var current = string.IsNullOrEmpty(p.CurrentTitle) ? "..." : Truncate(p.CurrentTitle!, 50);
        uiStatusLabel.Text = $"Применение {p.Processed + 1}/{p.Total}: {current}";
    }

    private void OnMediaProcessed(BatchRenameResult result)
    {
        if (IsDisposed)
        {
            return;
        }

        if (!_rowIndexByMediaId.TryGetValue(result.Media.Id, out var idx))
        {
            return;
        }

        var row = uiPreviewGrid.Rows[idx];
        if (row.Tag is not RowState state)
        {
            return;
        }

        state.IsApplied = true;
        state.LastApplySuccess = result.Success;

        if (result.Success && !string.Equals(result.OldTitle, result.NewTitle, StringComparison.Ordinal))
        {
            row.Cells[uiNewTitleColumn.Index].Value = result.NewTitle;
        }

        var newCell = row.Cells[uiNewTitleColumn.Index];
        newCell.Style.ForeColor = result.Success ? Color.DarkGreen : Color.DarkRed;
        row.Cells[uiApplyColumn.Index].ReadOnly = true;

        Log(FormatResultLine(result));
    }

    private void Log(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        uiLogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}]  {message}{Environment.NewLine}");
    }

    private void SetApplyingState(bool applying, int totalForProgress = 0)
    {
        _isApplying = applying;

        uiFindTextBox.Enabled = !applying;
        uiReplaceTextBox.Enabled = !applying;
        uiModeCombo.Enabled = !applying;
        uiIgnoreCaseCheck.Enabled = !applying;
        uiSourcesPanel.Enabled = !applying;
        uiPreviewGrid.ReadOnly = applying;
        uiApplyButton.Enabled = !applying;

        if (applying)
        {
            uiProgressBar.Visible = true;
            uiProgressBar.Style = ProgressBarStyle.Continuous;
            uiProgressBar.Maximum = Math.Max(totalForProgress, 1);
            uiProgressBar.Value = 0;
            uiCancelButton.Text = "Прервать";
            uiApplyButton.Text = "Применение...";
        }
        else
        {
            uiProgressBar.Visible = false;
            uiCancelButton.Text = "Закрыть";
            UpdateApplyButtonState();
        }
    }

    private sealed record ModeItem(BatchRenameMode Mode, string Display)
    {
        public override string ToString()
        {
            return Display;
        }
    }

    private sealed class RowState(Media media)
    {
        public Media Media { get; } = media;
        public string PreviewNewTitle { get; set; } = media.Title ?? string.Empty;
        public string? PreviewError { get; set; }
        public string[] UpdatableSources { get; set; } = [];
        public string[] BlockedSources { get; set; } = [];
        public bool IsManuallyEdited { get; set; }
        public bool IsApplied { get; set; }
        public bool LastApplySuccess { get; set; }
    }
}
