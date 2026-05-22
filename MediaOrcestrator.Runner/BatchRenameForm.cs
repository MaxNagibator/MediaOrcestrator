using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaOrcestrator.Runner;

public partial class BatchRenameForm : Form
{
    private const int EmGetScrollPos = 0x04DD;
    private const int EmSetScrollPos = 0x04DE;
    private const int WmSetRedraw = 0x000B;

    private readonly List<Media> _medias;
    private readonly BatchRenameService _service;
    private readonly ILogger _logger;
    private readonly ActionHolder _actionHolder;
    private readonly Dictionary<string, int> _rowIndexByMediaId = new();
    private readonly Dictionary<string, CheckBox> _sourceChecks = new();
    private readonly SynchronizationContext _uiContext;
    private readonly string? _specificSourceId;
    private readonly IReadOnlyCollection<string>? _gridSourceIds;

    private CancellationTokenSource? _applyCts;
    private bool _isApplying;
    private bool _suppressEvents;
    private bool _closeAfterCancel;
    private ActionHolder.RunningAction? _currentRunning;
    private SubtitleEtaTicker? _currentEtaTicker;

    public BatchRenameForm()
    {
        _medias = [];
        _service = null!;
        _logger = NullLogger.Instance;
        _actionHolder = null!;
        _uiContext = SynchronizationContext.Current ?? new();
        InitializeComponent();
        InitializeModeCombo();
    }

    public BatchRenameForm(
        List<Media> medias,
        BatchRenameService service,
        ILogger logger,
        ActionHolder actionHolder,
        Source? specificSource = null,
        IReadOnlyCollection<string>? gridSourceIds = null) : this()
    {
        _medias = medias;
        _service = service;
        _logger = logger;
        _actionHolder = actionHolder;
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
        uiRowSelectPanel.Visible = _medias.Count >= 2;
        RefreshPreview(resetRound: true);
        uiFindTextBox.Focus();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isApplying)
        {
            _closeAfterCancel = true;
            CancelApply();
            uiStatusLabel.Text = "Отмена...";
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
        RefreshPreview(resetRound: true);
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
            state.IsApplied = false;
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
                if (row.Tag is RowState { IsManuallyEdited: true } state)
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
        RefreshPreview(resetRound: true);
    }

    private async void uiApplyButton_Click(object? sender, EventArgs e)
    {
        var requests = BuildRequests();
        if (requests.Count == 0)
        {
            return;
        }

        _applyCts?.Dispose();
        _applyCts = new();
        var token = _applyCts.Token;

        SetApplyingState(true, requests.Count);

        var actionName = _medias.Count == 1
            ? $"Переименование: «{Truncate(_medias[0].Title, 50)}»"
            : $"Пакетное переименование ({requests.Count})";

        var running = _actionHolder.Register(actionName, "Подготовка", requests.Count, _applyCts, kind: ActionKind.Metadata);
        _currentRunning = running;
        _currentEtaTicker = new(running, new());

        using var actionScope = _actionHolder.BeginScope(running);

        var terminated = false;

        try
        {
            try
            {
                running.Status = "Авторизация площадок";
                if (!await EnsureSourcesAuthenticatedAsync(token))
                {
                    SetApplyingState(false);
                    uiStatusLabel.Text = "Применение отменено — нет авторизации площадок";
                    running.MarkCancelled("Нет авторизации площадок");
                    terminated = true;
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                if (IsDisposed)
                {
                    return;
                }

                SetApplyingState(false);
                uiStatusLabel.Text = "Отменено пользователем";
                LogLine(Stamp("Отменено пользователем"), Color.Firebrick);
                running.MarkCancelled();
                terminated = true;

                if (_closeAfterCancel)
                {
                    Close();
                }

                return;
            }
            catch (Exception ex)
            {
                if (IsDisposed)
                {
                    return;
                }

                SetApplyingState(false);
                uiStatusLabel.Text = $"Ошибка авторизации: {ex.Message}";
                LogLine(Stamp($"Ошибка авторизации: {ex.Message}"), Color.Firebrick);
                running.Fail($"Авторизация: {ex.Message}", ex);
                terminated = true;
                return;
            }

            if (token.IsCancellationRequested || IsDisposed)
            {
                if (!IsDisposed)
                {
                    SetApplyingState(false);
                    running.MarkCancelled();
                    terminated = true;

                    if (_closeAfterCancel)
                    {
                        Close();
                    }
                }

                return;
            }

            running.Status = $"0/{requests.Count}";
            LogLine(Stamp($"── Запуск: записей {requests.Count} ──"), Color.MidnightBlue);

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
                RefreshPreview(resetRound: false);
                uiStatusLabel.Text = "Отменено пользователем";
                LogLine(Stamp("── Прервано пользователем ──"), Color.Firebrick);
                running.MarkCancelled();
                terminated = true;

                if (_closeAfterCancel)
                {
                    Close();
                }

                return;
            }
            catch (Exception ex)
            {
                if (IsDisposed)
                {
                    return;
                }

                SetApplyingState(false);
                RefreshPreview(resetRound: false);
                uiStatusLabel.Text = $"Ошибка: {ex.Message}";
                LogLine(Stamp($"Ошибка: {ex.Message}"), Color.Firebrick);
                running.Fail(ex.Message, ex);
                terminated = true;
                return;
            }

            if (IsDisposed)
            {
                return;
            }

            SetApplyingState(false);
            RefreshPreview(resetRound: false);

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;

            uiStatusLabel.Text = failCount == 0
                ? $"Готово: применено {successCount} из {results.Count}"
                : $"Готово: {successCount} успешно, {failCount} с ошибками";

            LogLine(Stamp($"── Готово: успешно {successCount}, с ошибками {failCount} ──"),
                failCount == 0 ? Color.DarkGreen : Color.Firebrick);

            if (failCount == 0)
            {
                running.Finish($"Применено {successCount} из {results.Count}");
            }
            else
            {
                running.Fail($"Применено {successCount}, ошибок {failCount}");
            }

            terminated = true;

            if (successCount > 0)
            {
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            if (!terminated)
            {
                running.MarkCancelled("Прервано без явного исхода");
            }

            _currentRunning = null;
            _currentEtaTicker = null;
        }
    }

    private void uiCancelButton_Click(object? sender, EventArgs e)
    {
        if (_isApplying)
        {
            CancelApply();
            uiStatusLabel.Text = "Отмена...";
            return;
        }

        Close();
    }

    [DllImport("user32.dll")]
    private static extern void SendMessage(IntPtr hWnd, int msg, int wParam, ref Point lParam);

    [DllImport("user32.dll")]
    private static extern void SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);

    private static bool IsRowChecked(DataGridViewRow row)
    {
        return row.Cells[0].Value is bool b && b;
    }

    private static string Stamp(string message)
    {
        return $"[{DateTime.Now:HH:mm:ss}]  {message}";
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value ?? string.Empty;
        }

        return value.AsSpan(0, max - 1).ToString() + "…";
    }

    private static string OutcomeText(BatchRenameSourceResult source)
    {
        return source.Outcome switch
        {
            BatchRenameSourceOutcome.Updated => "обновлено",
            BatchRenameSourceOutcome.AlreadyUpToDate => "уже актуально",
            BatchRenameSourceOutcome.Skipped => $"пропуск ({source.Message})",
            BatchRenameSourceOutcome.NotSupported => "обновление не поддерживается",
            BatchRenameSourceOutcome.VerificationFailed => "название на площадке не изменилось",
            BatchRenameSourceOutcome.Failed => $"ошибка — {source.Message}",
            _ => source.Message ?? string.Empty,
        };
    }

    private static Color OutcomeColor(BatchRenameSourceOutcome outcome)
    {
        return outcome switch
        {
            BatchRenameSourceOutcome.Updated => Color.DarkGreen,
            BatchRenameSourceOutcome.AlreadyUpToDate => Color.Gray,
            BatchRenameSourceOutcome.Skipped => Color.Gray,
            BatchRenameSourceOutcome.NotSupported => Color.DarkOrange,
            BatchRenameSourceOutcome.VerificationFailed => Color.Firebrick,
            BatchRenameSourceOutcome.Failed => Color.Firebrick,
            _ => Color.Black,
        };
    }

    private static string BuildResultTooltip(BatchRenameResult result)
    {
        var tip = new StringBuilder();
        tip.AppendLine(result.Success
            ? "Применено"
            : "Ошибка: " + (result.ErrorMessage ?? "неизвестно"));

        foreach (var source in result.Sources)
        {
            tip.Append("• ").Append(source.SourceTitle).Append(" — ").AppendLine(OutcomeText(source));
        }

        return tip.ToString().TrimEnd();
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

        RefreshPreview(resetRound: true);
    }

    private void SetAllRows(bool value)
    {
        if (_isApplying)
        {
            return;
        }

        _suppressEvents = true;
        try
        {
            foreach (DataGridViewRow row in uiPreviewGrid.Rows)
            {
                row.Cells[uiApplyColumn.Index].Value = value;
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        RefreshPreview(resetRound: true);
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
                var title = media.Title ?? string.Empty;
                var idx = uiPreviewGrid.Rows.Add(true, title, title, string.Empty);
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
            return _gridSourceIds != null ? [] : null;
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

    private string EffectiveTarget(DataGridViewRow row)
    {
        return row.Cells[uiNewTitleColumn.Index].Value?.ToString() ?? string.Empty;
    }

    private bool IsRowActionable(DataGridViewRow row, RowState state)
    {
        if (!IsRowChecked(row) || state.PreviewError != null)
        {
            return false;
        }

        var target = EffectiveTarget(row);
        return state.Sources.Any(s => s.CanUpdate
                                      && !string.Equals(s.CurrentTitle, target, StringComparison.Ordinal));
    }

    private void RefreshPreview(bool resetRound)
    {
        var options = CurrentOptions();
        var previews = _service.Preview(_medias, options);
        var hasGlobalError = false;
        var actionable = 0;

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
                state.Sources = preview.Sources;

                if (resetRound)
                {
                    state.IsApplied = false;
                    state.LastApplyTooltip = null;
                }

                if (preview.Error != null)
                {
                    hasGlobalError = true;
                }

                if (!state.IsManuallyEdited)
                {
                    row.Cells[uiNewTitleColumn.Index].Value = preview.NewTitle;
                }

                if (IsRowActionable(row, state))
                {
                    actionable++;
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

        UpdateApplyButtonState(actionable);
        UpdateStatusLine(actionable, previews.Count);
        UpdateResetEditsVisibility();
    }

    private void UpdateApplyButtonState(int? cachedCount = null)
    {
        if (_isApplying)
        {
            return;
        }

        var count = cachedCount ?? CountActionableRows();
        uiApplyButton.Text = count > 0
            ? $"Применить ({count})"
            : "Применить";

        uiApplyButton.Enabled = count > 0 && string.IsNullOrEmpty(uiErrorLabel.Text);
    }

    private int CountActionableRows()
    {
        var count = 0;
        foreach (DataGridViewRow row in uiPreviewGrid.Rows)
        {
            if (row.Tag is RowState state && IsRowActionable(row, state))
            {
                count++;
            }
        }

        return count;
    }

    private void UpdateStatusLine(int actionable, int total)
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

        uiStatusLabel.Text = actionable == 0
            ? $"Применять нечего — площадки синхронны (записей: {total}){sourcesPart}"
            : $"К применению: {actionable} из {total}{sourcesPart}";
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

        PaintSourcesCell(row, state);

        var newCell = row.Cells[uiNewTitleColumn.Index];

        if (state.PreviewError != null)
        {
            newCell.Style.ForeColor = Color.Firebrick;
            newCell.ToolTipText = "Ошибка регулярного выражения";
            row.DefaultCellStyle.BackColor = Color.White;
            return;
        }

        if (state.IsApplied)
        {
            newCell.Style.ForeColor = state.LastApplySuccess ? Color.DarkGreen : Color.Firebrick;
            newCell.ToolTipText = state.LastApplyTooltip ?? string.Empty;
            row.DefaultCellStyle.BackColor = Color.White;
            return;
        }

        var target = EffectiveTarget(row);
        var updatable = state.Sources.Where(s => s.CanUpdate).ToList();
        var outOfSync = updatable
            .Where(s => !string.Equals(s.CurrentTitle, target, StringComparison.Ordinal))
            .ToList();

        if (updatable.Count == 0)
        {
            newCell.Style.ForeColor = Color.Gray;
            row.Cells[uiApplyColumn.Index].Style.ForeColor = Color.Gray;
            row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            newCell.ToolTipText = "Нет площадок, доступных для переименования";
            return;
        }

        row.DefaultCellStyle.BackColor = Color.White;

        if (!IsRowChecked(row))
        {
            newCell.Style.ForeColor = Color.Gray;
            newCell.ToolTipText = "Запись снята — переименование не применится";
            return;
        }

        if (outOfSync.Count == 0)
        {
            newCell.Style.ForeColor = Color.Gray;
            newCell.ToolTipText = "Все площадки уже несут это название";
            return;
        }

        newCell.Style.ForeColor = Color.Black;
        newCell.ToolTipText = "Будет отправлено на: " + string.Join(", ", outOfSync.Select(s => s.SourceTitle));
    }

    private void PaintSourcesCell(DataGridViewRow row, RowState state)
    {
        var cell = row.Cells[uiSourcesColumn.Index];
        var target = EffectiveTarget(row);

        if (state.Sources.Count == 0)
        {
            cell.Value = "—";
            cell.Style.ForeColor = Color.Gray;
            cell.ToolTipText = "Нет привязанных площадок";
            return;
        }

        var updatable = state.Sources.Where(s => s.CanUpdate).ToList();
        var drift = updatable
            .Where(s => !string.Equals(s.CurrentTitle, target, StringComparison.Ordinal))
            .Select(s => s.SourceTitle)
            .ToList();

        if (drift.Count > 0)
        {
            cell.Value = "✗ " + string.Join(", ", drift);
            cell.Style.ForeColor = Color.Firebrick;
        }
        else if (updatable.Count > 0)
        {
            cell.Value = "✓ синхронно";
            cell.Style.ForeColor = Color.Gray;
        }
        else
        {
            cell.Value = "—";
            cell.Style.ForeColor = Color.Gray;
        }

        var tip = new StringBuilder();
        foreach (var source in state.Sources)
        {
            var inSync = string.Equals(source.CurrentTitle, target, StringComparison.Ordinal);
            var mark = !source.CanUpdate ? "•" : inSync ? "✓" : "✗";

            tip.Append(mark).Append(' ').Append(source.SourceTitle);

            if (!source.CanUpdate)
            {
                tip.Append(" — ").Append(source.SkipReason);
            }
            else if (!inSync)
            {
                tip.Append(" — сейчас «").Append(Truncate(source.CurrentTitle, 50)).Append('»');
            }
            else
            {
                tip.Append(" — актуально");
            }

            tip.AppendLine();
        }

        cell.ToolTipText = tip.ToString().TrimEnd();
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
            if (row.Tag is not RowState state || !IsRowActionable(row, state))
            {
                continue;
            }

            requests.Add(new(state.Media, EffectiveTarget(row), null, allowed));
        }

        return requests;
    }

    private async Task<bool> EnsureSourcesAuthenticatedAsync(CancellationToken token)
    {
        var allowed = CurrentAllowedSourceIds() ?? _sourceChecks.Keys.ToHashSet(StringComparer.Ordinal);
        if (allowed.Count == 0)
        {
            return true;
        }

        var pending = _service.GetUnauthenticatedSources(allowed);
        if (pending.Count == 0)
        {
            return true;
        }

        var list = string.Join(Environment.NewLine, pending.Select(p => "• " + p.Title));
        var answer = MessageBox.Show(this,
            "Перед переименованием нужно войти на площадки:"
            + Environment.NewLine
            + list
            + Environment.NewLine
            + Environment.NewLine
            + "Открыть окно входа?",
            "Требуется авторизация",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (answer != DialogResult.OK)
        {
            LogLine(Stamp("Применение отменено: площадки не авторизованы"), Color.Firebrick);
            return false;
        }

        uiStatusLabel.Text = "Авторизация площадок...";
        uiProgressBar.Style = ProgressBarStyle.Marquee;
        LogLine(Stamp("Авторизация площадок: " + string.Join(", ", pending.Select(p => p.Title))), Color.MidnightBlue);

        var ui = new WinFormsAuthUI(this, _logger);
        try
        {
            await Task.Run(() => _service.AuthenticateSourcesAsync(allowed, ui, token), token);
        }
        finally
        {
            if (!IsDisposed)
            {
                uiProgressBar.Style = ProgressBarStyle.Continuous;
            }
        }

        return true;
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

        var current = string.IsNullOrEmpty(p.CurrentTitle) ? "..." : Truncate(p.CurrentTitle, 50);
        uiStatusLabel.Text = $"Применение {Math.Min(p.Processed + 1, p.Total)}/{p.Total}: {current}";

        if (_currentRunning is not { State: ActionState.Running } running || p.Total <= 0)
        {
            return;
        }

        running.ProgressMax = p.Total;
        running.SetProgress(Math.Min(p.Processed, p.Total));
        running.Status = $"{Math.Min(p.Processed, p.Total)}/{p.Total}";
        _currentEtaTicker?.Report(p.Processed * 100.0 / p.Total);
    }

    private void OnMediaProcessed(BatchRenameResult result)
    {
        if (IsDisposed || !_rowIndexByMediaId.TryGetValue(result.Media.Id, out var idx))
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
        state.LastApplyTooltip = BuildResultTooltip(result);

        var newCell = row.Cells[uiNewTitleColumn.Index];
        newCell.Style.ForeColor = result.Success ? Color.DarkGreen : Color.Firebrick;
        newCell.ToolTipText = state.LastApplyTooltip;

        LogResult(result);
    }

    private void LogResult(BatchRenameResult result)
    {
        if (result.Success)
        {
            var renamed = !string.Equals(result.OldTitle, result.NewTitle, StringComparison.Ordinal);
            LogLine(Stamp(renamed
                    ? $"✓ «{result.OldTitle}» → «{result.NewTitle}»"
                    : $"✓ «{result.NewTitle}» — площадки досинхронизированы"),
                Color.DarkGreen);
        }
        else
        {
            LogLine(Stamp($"✗ «{result.OldTitle}» — {result.ErrorMessage ?? "ошибка"}"), Color.Firebrick);
        }

        foreach (var source in result.Sources)
        {
            LogLine($"{source.SourceTitle} — {OutcomeText(source)}", OutcomeColor(source.Outcome), 1);
        }
    }

    private void LogLine(string text, Color color, int indent = 0)
    {
        if (IsDisposed)
        {
            return;
        }

        var prefix = indent > 0 ? new(' ', indent * 4) : string.Empty;
        AppendLog(prefix + text + Environment.NewLine, color);
    }

    private void AppendLog(string text, Color color)
    {
        var atBottom = IsLogAtBottom();
        var savedSelStart = uiLogBox.SelectionStart;
        var savedSelLength = uiLogBox.SelectionLength;
        var scroll = default(Point);
        SendMessage(uiLogBox.Handle, EmGetScrollPos, 0, ref scroll);

        SendMessage(uiLogBox.Handle, WmSetRedraw, false, 0);
        try
        {
            uiLogBox.SelectionStart = uiLogBox.TextLength;
            uiLogBox.SelectionLength = 0;
            uiLogBox.SelectionColor = color;
            uiLogBox.AppendText(text);
            uiLogBox.SelectionColor = uiLogBox.ForeColor;

            if (atBottom)
            {
                uiLogBox.SelectionStart = uiLogBox.TextLength;
                uiLogBox.SelectionLength = 0;
                uiLogBox.ScrollToCaret();
            }
            else
            {
                uiLogBox.SelectionStart = savedSelStart;
                uiLogBox.SelectionLength = savedSelLength;
                SendMessage(uiLogBox.Handle, EmSetScrollPos, 0, ref scroll);
            }
        }
        finally
        {
            SendMessage(uiLogBox.Handle, WmSetRedraw, true, 0);
            uiLogBox.Invalidate();
        }
    }

    private bool IsLogAtBottom()
    {
        if (uiLogBox.TextLength == 0)
        {
            return true;
        }

        var bottomChar = uiLogBox.GetCharIndexFromPosition(new(2, uiLogBox.ClientSize.Height - 2));
        return bottomChar >= uiLogBox.TextLength - Environment.NewLine.Length - 1;
    }

    private void SetApplyingState(bool applying, int totalForProgress = 0)
    {
        _isApplying = applying;

        uiFindTextBox.Enabled = !applying;
        uiReplaceTextBox.Enabled = !applying;
        uiModeCombo.Enabled = !applying;
        uiIgnoreCaseCheck.Enabled = !applying;
        uiSourcesPanel.Enabled = !applying;
        uiRowSelectPanel.Enabled = !applying;
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
            uiProgressBar.Style = ProgressBarStyle.Continuous;
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
        public IReadOnlyList<BatchRenameSourcePreview> Sources { get; set; } = [];
        public bool IsManuallyEdited { get; set; }
        public bool IsApplied { get; set; }
        public bool LastApplySuccess { get; set; }
        public string? LastApplyTooltip { get; set; }
    }
}
