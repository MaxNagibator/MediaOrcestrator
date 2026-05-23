using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;

namespace MediaOrcestrator.Runner;

public partial class BatchPreviewForm : Form
{
    private const int EmGetScrollPos = 0x04DD;
    private const int EmSetScrollPos = 0x04DE;
    private const int WmSetRedraw = 0x000B;

    private readonly List<Media> _medias;
    private readonly BatchPreviewService _service;
    private readonly CoverGenerator _coverGenerator;
    private readonly CoverTemplateStore _coverTemplateStore;
    private readonly ILogger _logger;
    private readonly ActionHolder _actionHolder;
    private readonly Dictionary<string, DataGridViewRow> _rowsByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CheckBox> _targetChecks = new(StringComparer.Ordinal);

    private List<Source> _donors = [];
    private List<Source> _allTargets = [];
    private CoverTemplate? _coverTemplate;
    private string? _currentProfileName;
    private bool _suppressEvents;
    private bool _suppressProfileComboEvents;
    private bool _isApplying;
    private bool _closeAfterCancel;
    private CancellationTokenSource? _applyCts;
    private ActionHolder.RunningAction? _currentRunning;
    private SubtitleEtaTicker? _currentEtaTicker;

    public BatchPreviewForm()
    {
        _medias = [];
        _service = null!;
        _coverGenerator = null!;
        _coverTemplateStore = null!;
        _logger = NullLogger.Instance;
        _actionHolder = null!;
        InitializeComponent();
    }

    public BatchPreviewForm(
        List<Media> medias,
        BatchPreviewService service,
        CoverGenerator coverGenerator,
        CoverTemplateStore coverTemplateStore,
        ILogger logger,
        ActionHolder actionHolder) : this()
    {
        _medias = medias;
        _service = service;
        _coverGenerator = coverGenerator;
        _coverTemplateStore = coverTemplateStore;
        _logger = logger;
        _actionHolder = actionHolder;
    }

    public event EventHandler? DataChanged;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        Text = _medias.Count == 1
            ? $"Обновление превью «{Truncate(_medias[0].Title, 60)}»"
            : $"Обновление превью ({_medias.Count} видео)";

        ApplyInitialSplitterDistance();
        PopulateDonors();
        OnModeChanged();
        RefreshProfilesCombo();
        RefreshCoverThumbnail();
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
        uiCoverThumbnail.Image?.Dispose();
        uiCoverThumbnail.Image = null;
        base.OnFormClosed(e);
    }

    private void OnFromSourceCheckedChanged(object? sender, EventArgs e)
    {
        if (!uiFromSourceRadio.Checked || _suppressEvents || _isApplying)
        {
            return;
        }

        uiFromFileRadio.Checked = false;
        uiFromTemplateRadio.Checked = false;
        OnModeChanged();
    }

    private void OnFromFileCheckedChanged(object? sender, EventArgs e)
    {
        if (!uiFromFileRadio.Checked || _suppressEvents || _isApplying)
        {
            return;
        }

        uiFromSourceRadio.Checked = false;
        uiFromTemplateRadio.Checked = false;
        OnModeChanged();
    }

    private void OnFromTemplateCheckedChanged(object? sender, EventArgs e)
    {
        if (!uiFromTemplateRadio.Checked || _suppressEvents || _isApplying)
        {
            return;
        }

        uiFromSourceRadio.Checked = false;
        uiFromFileRadio.Checked = false;
        OnModeChanged();
    }

    private void OnDonorComboSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || _isApplying)
        {
            return;
        }

        OnDonorChanged();
    }

    private void OnBrowseButtonClick(object? sender, EventArgs e)
    {
        BrowseFile();
    }

    private void OnTemplateButtonClick(object? sender, EventArgs e)
    {
        OpenTemplateEditor();
    }

    private void OnProfileComboSelectedIndexChanged(object? sender, EventArgs e)
    {
        OnProfileComboChanged();
    }

    private void uiResultGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (uiResultGrid.CurrentCell is DataGridViewCheckBoxCell)
        {
            uiResultGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void uiResultGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressEvents || e.RowIndex < 0 || e.ColumnIndex != uiApplyColumn.Index)
        {
            return;
        }

        UpdateApplyButtonState();
        UpdateStatusLine();
    }

    private async void uiApplyButton_Click(object? sender, EventArgs e)
    {
        await ApplyAsync();
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

    private void OnTargetCheckChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || _isApplying)
        {
            return;
        }

        RebuildGrid();
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

    private static string RowKey(string mediaId, string sourceId)
    {
        return $"{mediaId}|{sourceId}";
    }

    private void ApplyInitialSplitterDistance()
    {
        const double GridShare = 0.62;
        var available = uiGridLogSplit.Height - uiGridLogSplit.SplitterWidth;
        if (available <= uiGridLogSplit.Panel1MinSize + uiGridLogSplit.Panel2MinSize)
        {
            return;
        }

        var target = (int)(available * GridShare);
        var min = uiGridLogSplit.Panel1MinSize;
        var max = available - uiGridLogSplit.Panel2MinSize;
        uiGridLogSplit.SplitterDistance = Math.Clamp(target, min, max);
    }

    private void RefreshCoverThumbnail()
    {
        uiCoverThumbnail.Image?.Dispose();
        uiCoverThumbnail.Image = null;

        if (_coverTemplate == null || string.IsNullOrEmpty(_coverTemplate.TemplatePath) || !File.Exists(_coverTemplate.TemplatePath))
        {
            return;
        }

        try
        {
            var sampleTitle = _medias.Count > 0 ? _medias[0].Title : null;
            var sampleNumber = CoverNumberResolver.Resolve(_coverTemplate, sampleTitle, 0);
            using var skBitmap = _coverGenerator.Render(_coverTemplate, sampleNumber);
            uiCoverThumbnail.Image = SkiaInterop.ToBitmap(skBitmap);
        }
        catch
        {
        }
    }

    private void RefreshProfilesCombo()
    {
        _suppressProfileComboEvents = true;
        uiProfileCombo.Items.Clear();
        uiProfileCombo.Items.Add("— выбрать профиль —");

        foreach (var name in _coverTemplateStore.List())
        {
            uiProfileCombo.Items.Add(name);
        }

        if (!string.IsNullOrEmpty(_currentProfileName))
        {
            var idx = uiProfileCombo.Items.IndexOf(_currentProfileName);
            uiProfileCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }
        else
        {
            uiProfileCombo.SelectedIndex = 0;
        }

        _suppressProfileComboEvents = false;
    }

    private void OnProfileComboChanged()
    {
        if (_suppressProfileComboEvents)
        {
            return;
        }

        var idx = uiProfileCombo.SelectedIndex;

        if (idx <= 0)
        {
            return;
        }

        var name = uiProfileCombo.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var loaded = _coverTemplateStore.Load(name);

        if (loaded == null)
        {
            MessageBox.Show(this, $"Не удалось загрузить профиль «{name}»", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _coverTemplate = loaded;
        _currentProfileName = name;
        RefreshCoverThumbnail();
        UpdateApplyButtonState();
        UpdateStatusLine();
    }

    private void PopulateDonors()
    {
        _donors = _service.GetAvailableDonors(_medias);

        _suppressEvents = true;
        try
        {
            uiDonorComboBox.Items.Clear();

            foreach (var donor in _donors)
            {
                uiDonorComboBox.Items.Add(donor.TitleFull);
            }

            if (uiDonorComboBox.Items.Count > 0)
            {
                uiDonorComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void RebuildTargets(Source? excludeDonor)
    {
        _allTargets = _service.GetAvailableTargets(_medias, excludeDonor);

        _suppressEvents = true;
        try
        {
            uiTargetsPanel.Controls.Clear();
            _targetChecks.Clear();

            foreach (var target in _allTargets)
            {
                var checkbox = new CheckBox
                {
                    Text = target.TitleFull,
                    Checked = true,
                    AutoSize = true,
                    Tag = target.Id,
                    Margin = new(0, 4, 12, 4),
                };

                checkbox.CheckedChanged += OnTargetCheckChanged;
                uiTargetsPanel.Controls.Add(checkbox);
                _targetChecks[target.Id] = checkbox;
            }

            if (_allTargets.Count >= 2)
            {
                uiTargetsPanel.Controls.Add(uiTargetsAllLink);
                uiTargetsPanel.Controls.Add(uiTargetsNoneLink);
            }

            uiTargetsGroup.Visible = _allTargets.Count > 0;
        }
        finally
        {
            _suppressEvents = false;
        }

        RebuildGrid();
    }

    private void SetAllTargets(bool value)
    {
        if (_isApplying)
        {
            return;
        }

        _suppressEvents = true;
        try
        {
            foreach (var checkbox in _targetChecks.Values)
            {
                checkbox.Checked = value;
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        RebuildGrid();
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
            foreach (DataGridViewRow row in uiResultGrid.Rows)
            {
                row.Cells[uiApplyColumn.Index].Value = value;
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        UpdateApplyButtonState();
        UpdateStatusLine();
    }

    private List<Source> GetCheckedTargets()
    {
        var selected = new List<Source>(_allTargets.Count);
        foreach (var target in _allTargets)
        {
            if (_targetChecks.TryGetValue(target.Id, out var checkbox) && checkbox.Checked)
            {
                selected.Add(target);
            }
        }

        return selected;
    }

    private void RebuildGrid()
    {
        _suppressEvents = true;
        try
        {
            uiResultGrid.Rows.Clear();
            _rowsByKey.Clear();
            var targets = GetCheckedTargets();

            foreach (var media in _medias)
            {
                foreach (var target in targets)
                {
                    if (media.Sources.All(s => s.SourceId != target.Id))
                    {
                        continue;
                    }

                    var idx = uiResultGrid.Rows.Add(true, media.Title, target.TitleFull, "Ожидание");
                    var row = uiResultGrid.Rows[idx];
                    var key = RowKey(media.Id, target.Id);
                    row.Tag = new RowState(media, target);
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                    _rowsByKey[key] = row;
                }
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        UpdateApplyButtonState();
        UpdateStatusLine();
    }

    private void OnModeChanged()
    {
        uiDonorComboBox.Enabled = uiFromSourceRadio.Checked && !_isApplying;
        uiFilePathTextBox.Enabled = uiFromFileRadio.Checked && !_isApplying;
        uiBrowseButton.Enabled = uiFromFileRadio.Checked && !_isApplying;
        uiTemplateButton.Enabled = uiFromTemplateRadio.Checked && !_isApplying;
        uiProfileCombo.Enabled = uiFromTemplateRadio.Checked && !_isApplying;

        if (uiFromSourceRadio.Checked)
        {
            OnDonorChanged();
        }
        else
        {
            RebuildTargets(null);
        }
    }

    private void OnDonorChanged()
    {
        var donor = GetSelectedDonor();
        RebuildTargets(donor);
    }

    private Source? GetSelectedDonor()
    {
        var index = uiDonorComboBox.SelectedIndex;
        return index >= 0 && index < _donors.Count ? _donors[index] : null;
    }

    private void BrowseFile()
    {
        using var dialog = new OpenFileDialog();
        dialog.Title = "Выберите файл превью";
        dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp|Все файлы|*.*";

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        uiFilePathTextBox.Text = dialog.FileName;
        UpdateApplyButtonState();
        UpdateStatusLine();
    }

    private void OpenTemplateEditor()
    {
        using var form = new CoverTemplateForm(_coverGenerator, _coverTemplateStore, _coverTemplate, _currentProfileName);

        if (form.ShowDialog(this) != DialogResult.OK || form.Result == null)
        {
            return;
        }

        _coverTemplate = form.Result;
        _currentProfileName = form.CurrentProfileName;
        RefreshProfilesCombo();
        RefreshCoverThumbnail();
        UpdateApplyButtonState();
        UpdateStatusLine();
    }

    private bool HasSelectedSource()
    {
        if (uiFromSourceRadio.Checked)
        {
            return uiDonorComboBox.SelectedIndex >= 0;
        }

        if (uiFromFileRadio.Checked)
        {
            return !string.IsNullOrEmpty(uiFilePathTextBox.Text) && File.Exists(uiFilePathTextBox.Text);
        }

        return uiFromTemplateRadio.Checked && _coverTemplate != null;
    }

    private int CountActionableRows()
    {
        var count = 0;
        foreach (DataGridViewRow row in uiResultGrid.Rows)
        {
            if (row.Tag is RowState state && IsRowChecked(row) && !state.IsApplied)
            {
                count++;
            }
        }

        return count;
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

        uiApplyButton.Enabled = count > 0 && HasSelectedSource();
    }

    private void UpdateStatusLine()
    {
        if (_isApplying)
        {
            return;
        }

        var actionable = CountActionableRows();
        var total = uiResultGrid.Rows.Count;
        var targetsCount = _targetChecks.Count;
        var checkedTargets = _targetChecks.Values.Count(c => c.Checked);

        var targetsPart = targetsCount > 0
            ? $" · площадки: {checkedTargets}/{targetsCount}"
            : string.Empty;

        if (!HasSelectedSource())
        {
            if (uiFromSourceRadio.Checked)
            {
                uiStatusLabel.Text = "Выберите площадку-донор";
            }
            else
            {
                uiStatusLabel.Text = uiFromFileRadio.Checked
                    ? "Выберите файл превью"
                    : "Настройте шаблон обложки";
            }

            return;
        }

        uiStatusLabel.Text = actionable == 0
            ? $"Применять нечего (строк: {total}){targetsPart}"
            : $"К применению: {actionable} из {total}{targetsPart}";
    }

    private List<BatchPreviewRequest> BuildRequests()
    {
        var byMedia = new Dictionary<string, (Media Media, List<Source> Targets)>(StringComparer.Ordinal);

        foreach (DataGridViewRow row in uiResultGrid.Rows)
        {
            if (row.Tag is not RowState state || !IsRowChecked(row) || state.IsApplied)
            {
                continue;
            }

            if (!byMedia.TryGetValue(state.Media.Id, out var bucket))
            {
                bucket = (state.Media, []);
                byMedia[state.Media.Id] = bucket;
            }

            bucket.Targets.Add(state.Target);
        }

        var requests = new List<BatchPreviewRequest>(byMedia.Count);
        foreach (var media in _medias)
        {
            if (byMedia.TryGetValue(media.Id, out var bucket))
            {
                requests.Add(new(bucket.Media, bucket.Targets));
            }
        }

        return requests;
    }

    private async Task ApplyAsync()
    {
        var requests = BuildRequests();
        if (requests.Count == 0)
        {
            return;
        }

        var donor = uiFromSourceRadio.Checked ? GetSelectedDonor() : null;
        var localFilePath = uiFromFileRadio.Checked ? uiFilePathTextBox.Text : null;
        var coverTemplate = uiFromTemplateRadio.Checked ? _coverTemplate : null;

        var totalUnits = requests.Sum(r => r.Targets.Count);

        _applyCts?.Dispose();
        _applyCts = new();
        var token = _applyCts.Token;

        SetApplyingState(true, totalUnits);

        var actionName = _medias.Count == 1
            ? $"Обновление превью: «{Truncate(_medias[0].Title, 50)}»"
            : $"Обновление превью ({requests.Count} видео)";

        var running = _actionHolder.Register(actionName, "Подготовка", totalUnits, _applyCts, kind: ActionKind.Metadata);
        _currentRunning = running;
        _currentEtaTicker = new(running, new());

        using var actionScope = _actionHolder.BeginScope(running);

        var processedUnits = 0;
        var terminated = false;

        IReadOnlyList<BatchPreviewResult> results;
        try
        {
            try
            {
                running.Status = "Авторизация площадок";
                if (!await EnsureSourcesAuthenticatedAsync(donor, requests, token))
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
                if (IsDisposed)
                {
                    return;
                }

                SetApplyingState(false);
                running.MarkCancelled();
                terminated = true;

                if (_closeAfterCancel)
                {
                    Close();
                }

                return;
            }

            running.Status = $"0/{totalUnits}";
            LogLine(Stamp($"── Запуск: видео {requests.Count}, заливок {totalUnits} ──"), Color.MidnightBlue);

            var progress = new Progress<BatchPreviewProgress>(p => OnMediaProgress(p, totalUnits));
            var uiContext = SynchronizationContext.Current ?? new();

            try
            {
                results = await Task.Run(() => _service.ApplyAsync(requests,
                        donor,
                        localFilePath,
                        coverTemplate,
                        progress,
                        result =>
                        {
                            Interlocked.Increment(ref processedUnits);
                            var snapshot = processedUnits;
                            uiContext.Post(_ => OnResultReported(result, snapshot, totalUnits), null);
                        },
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

    private async Task<bool> EnsureSourcesAuthenticatedAsync(Source? donor, IReadOnlyList<BatchPreviewRequest> requests, CancellationToken token)
    {
        var needed = new HashSet<string>(StringComparer.Ordinal);
        if (donor != null)
        {
            needed.Add(donor.Id);
        }

        foreach (var target in requests.SelectMany(request => request.Targets))
        {
            needed.Add(target.Id);
        }

        if (needed.Count == 0)
        {
            return true;
        }

        var pending = _service.GetUnauthenticatedSources(needed);
        if (pending.Count == 0)
        {
            return true;
        }

        var list = string.Join(Environment.NewLine, pending.Select(p => "• " + p.Title));
        var answer = MessageBox.Show(this,
            "Перед обновлением превью нужно войти на площадки:"
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
            await Task.Run(() => _service.AuthenticateSourcesAsync(needed, ui, token), token);
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

    private void OnMediaProgress(BatchPreviewProgress p, int totalUnits)
    {
        if (IsDisposed || _currentRunning is not { State: ActionState.Running } running)
        {
            return;
        }

        var current = string.IsNullOrEmpty(p.CurrentTitle) ? "..." : Truncate(p.CurrentTitle, 50);

        if (p.Processed >= p.Total)
        {
            return;
        }

        uiStatusLabel.Text = $"Обработка {p.Processed + 1}/{p.Total}: {current}";
        running.Subtitle = current;
    }

    private void OnResultReported(BatchPreviewResult result, int processedUnits, int totalUnits)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        var key = RowKey(result.Media.Id, result.Target.Id);
        var statusText = result.Success ? "Готово" : $"Ошибка: {result.ErrorMessage}";
        var color = result.Success ? Color.DarkGreen : Color.DarkRed;

        if (_rowsByKey.TryGetValue(key, out var matchingRow))
        {
            matchingRow.Cells[uiStatusColumn.Name].Value = statusText;
            matchingRow.DefaultCellStyle.ForeColor = color;

            if (matchingRow.Tag is RowState state)
            {
                state.IsApplied = true;
            }
        }
        else
        {
            var idx = uiResultGrid.Rows.Add(true, result.Media.Title, result.Target.TitleFull, statusText);
            var row = uiResultGrid.Rows[idx];
            row.Tag = new RowState(result.Media, result.Target) { IsApplied = true };
            row.DefaultCellStyle.ForeColor = color;
            _rowsByKey[key] = row;
        }

        if (result.Success)
        {
            LogLine(Stamp($"✓ «{Truncate(result.Media.Title, 50)}» → {result.Target.TitleFull}"), Color.DarkGreen);
        }
        else
        {
            LogLine(Stamp($"✗ «{Truncate(result.Media.Title, 50)}» → {result.Target.TitleFull} — {result.ErrorMessage ?? "ошибка"}"), Color.Firebrick);
        }

        uiProgressBar.Maximum = Math.Max(totalUnits, 1);
        uiProgressBar.Value = Math.Min(processedUnits, uiProgressBar.Maximum);

        if (_currentRunning is not { State: ActionState.Running } running)
        {
            return;
        }

        running.ProgressMax = totalUnits;
        running.SetProgress(Math.Min(processedUnits, totalUnits));
        running.Status = $"{Math.Min(processedUnits, totalUnits)}/{totalUnits}";
        _currentEtaTicker?.Report(processedUnits * 100.0 / Math.Max(totalUnits, 1));
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

    private void SetApplyingState(bool applying, int totalForProgress = 0)
    {
        _isApplying = applying;

        uiFromSourceRadio.Enabled = !applying;
        uiFromFileRadio.Enabled = !applying;
        uiFromTemplateRadio.Enabled = !applying;
        uiDonorComboBox.Enabled = !applying && uiFromSourceRadio.Checked;
        uiFilePathTextBox.Enabled = !applying && uiFromFileRadio.Checked;
        uiBrowseButton.Enabled = !applying && uiFromFileRadio.Checked;
        uiTemplateButton.Enabled = !applying && uiFromTemplateRadio.Checked;
        uiProfileCombo.Enabled = !applying && uiFromTemplateRadio.Checked;
        uiTargetsPanel.Enabled = !applying;
        uiRowSelectPanel.Enabled = !applying;
        uiResultGrid.ReadOnly = applying;
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

    private sealed class RowState(Media media, Source target)
    {
        public Media Media { get; } = media;
        public Source Target { get; } = target;
        public bool IsApplied { get; set; }
    }
}
