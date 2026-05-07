using MediaOrcestrator.Domain;
using MediaOrcestrator.Domain.Comments;
using MediaOrcestrator.Domain.Merging;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace MediaOrcestrator.Runner;

public partial class MainForm : Form
{
    private readonly Orcestrator _orcestrator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainForm> _logger;
    private readonly AppUpdateManager _updateManager;
    private readonly Dictionary<string, AuditSourceRow> _auditRows = new();
    private readonly PublishControl? _publishControl;
    private bool _isSyncRunning;

    public MainForm(Orcestrator orcestrator, IServiceProvider serviceProvider, ILogger<MainForm> logger, RichTextBox logControl, AppUpdateManager updateManager)
    {
        _orcestrator = orcestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _updateManager = updateManager;

        InitializeComponent();
        uiLogsTabPage.Controls.Add(logControl);

        // TODO: Сомнительно
        _publishControl = _serviceProvider.GetRequiredService<PublishControl>();
        _publishControl.Dock = DockStyle.Fill;
        _publishControl.MediaPublished += OnMediaPublished;
        uiPublishTabPage.Controls.Add(_publishControl);
    }

    public Action? StartupCompleted { get; set; }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        TopMost = true;
        TopMost = false;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        Text = $"Медиа оркестратор v{_updateManager.CurrentVersion}";
        uiAuditSourcesPanel.SizeChanged += (_, _) => ResizeAuditRows();
        uiRelationsGraphControl.InvertRequested += OnGraphInvertRequested;
        uiRelationsGraphControl.DeleteRequested += OnGraphDeleteRequested;
        uiRelationsGraphControl.CreateRequested += OnGraphCreateRequested;
        uiRelationsGraphControl.RefreshRequested += (_, _) => DrawRelations();

        using (Splash.Current.StartSpan("Подготовка списка источников..."))
        {
            DrawSources();
        }

        using (Splash.Current.StartSpan("Подготовка связей между источниками..."))
        {
            DrawRelations();
        }

        using (Splash.Current.StartSpan("Загрузка медиа-матрицы..."))
        {
            // TODO: SetZalupaV2
            uiMediaMatrixGridControl.Initialize(new(_orcestrator,
                _serviceProvider.GetRequiredService<SyncRetryRunner>(),
                _serviceProvider.GetRequiredService<ILogger<MediaMatrixGridControl>>(),
                _serviceProvider.GetRequiredService<SettingsManager>(),
                _serviceProvider.GetRequiredService<BatchRenameService>(),
                _serviceProvider.GetRequiredService<BatchPreviewService>(),
                _serviceProvider.GetRequiredService<CoverGenerator>(),
                _serviceProvider.GetRequiredService<CoverTemplateStore>(),
                _serviceProvider.GetRequiredService<MediaMergeService>(),
                _serviceProvider.GetRequiredService<ActionHolder>(),
                _serviceProvider.GetRequiredService<CommentsService>(),
                _serviceProvider.GetRequiredService<ILoggerFactory>()));

            uiMediaMatrixGridControl.RefreshData();
        }

        using (Splash.Current.StartSpan("Подготовка комментариев..."))
        {
            uiCommentsViewControl.Initialize(_orcestrator,
                _serviceProvider.GetRequiredService<CommentsService>(),
                _serviceProvider.GetRequiredService<ActionHolder>(),
                _serviceProvider.GetRequiredService<ILogger<CommentsViewControl>>());

            uiCommentsHtmlControl.Initialize(_orcestrator,
                _serviceProvider.GetRequiredService<CommentsService>(),
                _serviceProvider.GetRequiredService<ActionHolder>(),
                _serviceProvider.GetRequiredService<ILogger<CommentsHtmlControl>>());
        }

        uiMainTabControl.Selected += OnMainTabSelected;

        if (uiClearTypeComboBox.Items.Count > 0)
        {
            uiClearTypeComboBox.SelectedIndex = 0;
        }

        using (Splash.Current.StartSpan("Подготовка плана синхронизации..."))
        {
            var planner = _serviceProvider.GetRequiredService<SyncPlanner>();
            uiSyncTreeControl.Initialize(planner,
                _orcestrator,
                _serviceProvider.GetRequiredService<SyncRetryRunner>(),
                _serviceProvider.GetRequiredService<ActionHolder>(),
                _serviceProvider.GetRequiredService<ILogger<SyncTreeControl>>());
        }

        StartupCompleted?.Invoke();

        CheckToolUpdatesInBackground();
        CheckAppUpdateInBackground();
    }

    private void OnMainTabSelected(object? sender, TabControlEventArgs e)
    {
        if (e.TabPage == uiCommentsTabPage)
        {
            uiCommentsViewControl.EnsureLoaded();
        }
        else if (e.TabPage == uiCommentsHtmlTabPage)
        {
            uiCommentsHtmlControl.EnsureLoaded();
        }
    }

    private async void uiSyncButton_Click(object sender, EventArgs e)
    {
        await RunSyncAsync(null, AuditSyncMode.Full);
    }

    private async void uiQuickSyncButton_Click(object sender, EventArgs e)
    {
        await RunSyncAsync(null, AuditSyncMode.Quick);
    }

    private async void uiSyncNewButton_Click(object sender, EventArgs e)
    {
        await RunSyncAsync(null, AuditSyncMode.New);
    }

    private async void OnAuditRowSyncRequested(object? sender, AuditSyncRequestedEventArgs e)
    {
        await RunSyncAsync(e.Source, e.Mode);
    }

    private void uiAddSourceButton_Click(object sender, EventArgs e)
    {
        if (uiSourcesComboBox.SelectedItem is not ISourceType selectedPlugin)
        {
            return;
        }

        // TODO: Немного шляпная тема мне кажется
        var newSourceId = Guid.NewGuid().ToString();
        var stateManager = _serviceProvider.GetRequiredService<StateManager>();
        var settings = SourceSettingsForm.ShowAdd(selectedPlugin, stateManager, newSourceId, _orcestrator.GetSources(), _logger);

        if (settings == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedPlugin.Name))
        {
            MessageBox.Show("Имя типа источника не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _orcestrator.AddSource(newSourceId, selectedPlugin.Name, settings);
        DrawSources();
    }

    private void uiMediaSourcePanel_SizeChanged(object sender, EventArgs e)
    {
    }

    private void UiAddRelationButton_Click(object sender, EventArgs e)
    {
        if (uiRelationFromComboBox.SelectedItem is not Source from)
        {
            MessageBox.Show("Пожалуйста, выберите источник для связи.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (uiRelationToComboBox.SelectedItem is not Source to)
        {
            MessageBox.Show("Пожалуйста, выберите целевой источник для связи.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _orcestrator.AddRelation(from, to);
        DrawRelations();
    }

    private void uiForceScanButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show("отладочное");
        return;

        Task.Run(async () =>
        {
            try
            {
                var media = _orcestrator.GetMedias().Skip(0).First();
                var source = media.Sources.First();

                var sadasd = _orcestrator.GetRelations().First();
                await sadasd.From.Type.DownloadAsync(source.ExternalId, sadasd.From.Settings);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Ошибка при загрузке: {Message}", exception.Message);
            }
        });
    }

    private void uiClearDatabaseButton_Click(object sender, EventArgs e)
    {
        var result = MessageBox.Show("Вы уверены, что хотите очистить базу данных?\nЭта операция необратима и удалит все данные.",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _logger.LogInformation("Пользователь запустил очистку базы данных.");
        uiClearDatabaseButton.Enabled = false;
        try
        {
            _orcestrator.ClearDatabase();
            _logger.LogInformation("Очистка базы данных завершена через UI.");
            MessageBox.Show("База данных успешно очищена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DrawSources();
            uiMediaMatrixGridControl.RefreshData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке базы данных через UI.");
            MessageBox.Show($"Ошибка при очистке базы данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            uiClearDatabaseButton.Enabled = true;
        }
    }

    private void uiClearSpecificTypeButton_Click(object sender, EventArgs e)
    {
        if (uiClearTypeComboBox.SelectedItem is not string selectedType)
        {
            MessageBox.Show("Пожалуйста, выберите тип для очистки.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show($"Вы уверены, что хотите очистить коллекцию '{selectedType}'?\nЭта операция необратима.",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _logger.LogInformation("Пользователь запустил очистку коллекции: {Type}", selectedType);
        uiClearSpecificTypeButton.Enabled = false;
        try
        {
            _orcestrator.ClearCollection(selectedType);
            _logger.LogInformation("Очистка коллекции {Type} завершена через UI.", selectedType);
            MessageBox.Show($"Коллекция '{selectedType}' успешно очищена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DrawSources();
            uiMediaMatrixGridControl.RefreshData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке коллекции {Type} через UI.", selectedType);
            MessageBox.Show($"Ошибка при очистке коллекции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            uiClearSpecificTypeButton.Enabled = true;
        }
    }

    private void uiRubuteAuthStateOpenBrowserButton_Click(object sender, EventArgs e)
    {
        GetCookie("https://studio.rutube.ru/", uiRubuteAuthStatePathTextBox, false);
    }

    private void uiYoutubeAuthStateOpenBrowserButton_Click(object sender, EventArgs e)
    {
        GetCookie("https://studio.youtube.com/", uiYoutubeAuthStatePathTextBox, true);
    }

    private void uiVkVideoAuthStateOpenBrowserButton_Click(object sender, EventArgs e)
    {
        GetCookie("https://cabinet.vkvideo.ru/", uiVkVideoAuthStatePathTextBox, false);
    }

    private void uiManageToolsButton_Click(object sender, EventArgs e)
    {
        var toolManager = _serviceProvider.GetRequiredService<ToolManager>();
        using var form = new ToolsForm(toolManager);
        form.ShowDialog(this);
    }

    private void uiOpenSettingsButton_Click(object sender, EventArgs e)
    {
        var settingsManager = _serviceProvider.GetRequiredService<SettingsManager>();
        using var form = new SettingsForm();
        form.SetSettingsManager(settingsManager);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (form.RestartRequested)
        {
            Application.Restart();
        }
    }

    private void uiCheckUpdatesButton_Click(object? sender, EventArgs e)
    {
        CheckAppUpdateInBackground();
    }

    private void uiReportIssueButton_Click(object? sender, EventArgs e)
    {
        var errorReportService = _serviceProvider.GetRequiredService<ErrorReportService>();
        using var form = new ErrorReportForm(errorReportService);
        form.ShowDialog(this);
    }

    private void OnGraphInvertRequested(object? sender, RelationGraphEdgeEventArgs e)
    {
        _orcestrator.InvertRelation(e.FromSourceId, e.ToSourceId);
        DrawRelations();
    }

    private void OnGraphCreateRequested(object? sender, RelationGraphEdgeEventArgs e)
    {
        _orcestrator.AddRelation(e.FromSourceId, e.ToSourceId);
        DrawRelations();
    }

    private void OnGraphDeleteRequested(object? sender, RelationGraphEdgeEventArgs e)
    {
        var confirm = MessageBox.Show("Удалить выбранную связь?",
            "Подтверждение",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.OK)
        {
            return;
        }

        _orcestrator.RemoveRelation(e.FromSourceId, e.ToSourceId);
        DrawRelations();
    }

    private void OnMediaPublished(object? sender, EventArgs e)
    {
        uiMediaMatrixGridControl.RefreshData();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        uiRunningActionsFlowLayoutPanel.Controls.Clear();
        // todo shlyapa
        var actionHolder = _serviceProvider.GetRequiredService<ActionHolder>();
        //actionHolder.Register("Синкаем пипку коня Никиты " + actionHolder.Actions.Count, "в процессе", 10, new CancellationTokenSource());
        var i = -1;
        foreach (var action in actionHolder.Actions)
        {
            action.Value.ProgressPlus();
            i++;
            var btn = new ActionUserControl();
            btn.SetAction(action.Value);

            btn.AutoSize = false;
            btn.Width = uiRunningActionsFlowLayoutPanel.Width - 10;
            btn.Left = 5;
            btn.Top = 5 + i * (btn.Height + 5);
            uiRunningActionsFlowLayoutPanel.Controls.Add(btn);
        }
    }

    private async Task RunSyncAsync(Source? filterSource, AuditSyncMode mode)
    {
        if (_isSyncRunning)
        {
            return;
        }

        var (isFull, onlyNew) = mode switch
        {
            AuditSyncMode.Full => (true, false),
            AuditSyncMode.Quick => (false, false),
            AuditSyncMode.New => (false, true),
            _ => (false, false),
        };

        var modeName = mode switch
        {
            AuditSyncMode.Full => "полной",
            AuditSyncMode.Quick => "быстрой",
            AuditSyncMode.New => "новых",
            _ => "",
        };

        var scope = filterSource != null ? $"источника «{filterSource.TitleFull}»" : "всех источников";
        _logger.LogInformation("Запуск {Mode} синхронизации {Scope}.", modeName, scope);

        _isSyncRunning = true;
        SetAllSyncControlsBusy(true);

        var targetRow = filterSource != null && _auditRows.TryGetValue(filterSource.Id, out var row) ? row : null;
        targetRow?.ReportProgress("Старт...");
        if (targetRow == null)
        {
            uiBulkProgressLabel.Text = "Старт...";
        }

        var progress = new Progress<string>(message =>
        {
            if (targetRow != null)
            {
                targetRow.ReportProgress(message);
            }
            else
            {
                uiBulkProgressLabel.Text = message;
            }
        });

        try
        {
            await _orcestrator.GetStorageFullInfo(isFull, filterSource, onlyNew, progress);
            _logger.LogInformation("Синхронизация через UI завершена.");
            uiMediaMatrixGridControl.RefreshData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации через UI.");
            if (targetRow != null)
            {
                targetRow.ReportProgress($"Ошибка: {ex.Message}");
            }
            else
            {
                uiBulkProgressLabel.Text = $"Ошибка: {ex.Message}";
            }

            MessageBox.Show($"Ошибка при синхронизации: {ex.Message}");
        }
        finally
        {
            _isSyncRunning = false;
            SetAllSyncControlsBusy(false);
            DrawSources();
        }
    }

    private void SetAllSyncControlsBusy(bool busy)
    {
        uiSyncButton.Enabled = !busy;
        uiQuickSyncButton.Enabled = !busy;
        uiSyncNewButton.Enabled = !busy;

        foreach (var row in _auditRows.Values)
        {
            row.SetBusy(busy);
        }
    }

    private void ResizeAuditRows()
    {
        var width = uiAuditSourcesPanel.ClientSize.Width - 6;
        if (width <= 0)
        {
            return;
        }

        foreach (var row in _auditRows.Values)
        {
            row.Width = width;
        }
    }

    private void GetCookie(string openPage, TextBox pathTextbox, bool transformCookie)
    {
        var jsonPath = transformCookie ? pathTextbox.Text + ".tmp.json" : pathTextbox.Text;
        var csvPath = pathTextbox.Text;

        Task.Run(async () =>
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false,
                    Args = ["--disable-blink-features=AutomationControlled"],
                });

                var contextOptions = new BrowserNewContextOptions();
                if (File.Exists(jsonPath))
                {
                    contextOptions.StorageStatePath = jsonPath;
                }
                else
                {
                    var directory = Path.GetDirectoryName(jsonPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }

                await using var context = await browser.NewContextAsync(contextOptions);
                var page = await context.NewPageAsync();

                _logger.LogInformation("Navigating to " + openPage + "...");
                await page.GotoAsync(openPage, new()
                {
                    Timeout = 0,
                });

                var msg = "Зайдите в свой профиль и нажмие OK, или отмена, если передумали";
                if (MessageBox.Show(msg, "Сохранить куку в фаил?", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    _logger.LogInformation("Browser is open.");
                    _logger.LogInformation("Press any key to save log and exit...");

                    await context.StorageStateAsync(new()
                    {
                        Path = jsonPath,
                    });

                    if (transformCookie)
                    {
                        _logger.LogInformation("Auth state saved to temp auth_state.json");

                        CookieTransformator.Run(jsonPath, csvPath, _logger);
                        _logger.LogInformation("Auth state convert to auth_state");
                    }
                    else
                    {
                        _logger.LogInformation("Auth state ");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при работе с браузером для {Url}", openPage);
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });
    }

    private void DrawSources()
    {
        uiRelationFromComboBox.Items.Clear();
        uiRelationToComboBox.Items.Clear();
        uiAuditSourcesPanel.Controls.Clear();
        _auditRows.Clear();
        uiRelationFromComboBox.DisplayMember = "TitleFull";
        uiRelationToComboBox.DisplayMember = "TitleFull";

        uiSourcesComboBox.Items.Clear();
        uiSourcesComboBox.DisplayMember = "Name";
        uiSourcesComboBox.Items.Add("Выберите тип хранилища");

        var sources = _orcestrator.GetSourceTypes();
        if (sources == null)
        {
            return;
        }

        foreach (var source in sources)
        {
            uiSourcesComboBox.Items.Add(source.Value);
        }

        if (uiSourcesComboBox.Items.Count > 0)
        {
            uiSourcesComboBox.SelectedIndex = 0;
        }

        uiMediaSourcePanel.Controls.Clear();
        foreach (var source in _orcestrator.GetSources())
        {
            using (Splash.Current.StartSpan(source.TitleFull))
            {
                var control = _serviceProvider.GetRequiredService<SourceControl>();
                control.SetMediaSource(source);
                control.Dock = DockStyle.Top;
                control.SourceDeleted += (_, _) => DrawSources();
                control.SourceUpdated += (_, _) => DrawSources();

                uiMediaSourcePanel.Controls.Add(control);
                control.SendToBack();

                uiRelationFromComboBox.Items.Add(source);
                uiRelationToComboBox.Items.Add(source);

                var row = new AuditSourceRow
                {
                    Width = uiAuditSourcesPanel.ClientSize.Width - 6,
                    Margin = new(3),
                };

                row.SetSource(source);
                row.SetBusy(_isSyncRunning);
                row.SyncRequested += OnAuditRowSyncRequested;

                uiAuditSourcesPanel.Controls.Add(row);
                _auditRows[source.Id] = row;
            }
        }

        DrawRelations();
        _publishControl?.ReloadSources();
    }

    private async void CheckToolUpdatesInBackground()
    {
        try
        {
            var toolManager = _serviceProvider.GetRequiredService<ToolManager>();
            var statuses = await Task.Run(() => toolManager.CheckForUpdatesAsync());
            var updatesAvailable = statuses.Where(s => s.UpdateAvailable).ToList();

            if (updatesAvailable.Count <= 0)
            {
                return;
            }

            var names = string.Join(", ", updatesAvailable.Select(s => s.Name));
            _logger.LogInformation("Доступны обновления для: {Tools}", names);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Фоновая проверка обновлений инструментов не удалась");
        }
    }

    private async void CheckAppUpdateInBackground()
    {
        try
        {
            var update = await Task.Run(() => _updateManager.CheckForUpdateAsync());

            if (update is null)
            {
                return;
            }

            _logger.LogInformation("Доступно обновление приложения: {Version}", update.Version);
            ShowUpdateDialog(update);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Фоновая проверка обновлений приложения не удалась");
        }
    }

    private void ShowUpdateDialog(AppUpdateInfo update)
    {
        using var form = new UpdateForm(update, (progress, ct) => _updateManager.DownloadUpdateAsync(update, progress, ct));

        switch (form.ShowDialog(this))
        {
            case DialogResult.OK:
                MessageBox.Show("Обновление скачано. Приложение будет перезапущено.",
                    "Обновление", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _updateManager.ApplyUpdate(form.DownloadedZipPath!);
                break;

            case DialogResult.Abort:
                _logger.LogError(form.DownloadError, "Не удалось скачать обновление");

                MessageBox.Show($"""
                                 Не удалось скачать обновление:

                                 {form.DownloadError?.Message}
                                 """,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

                break;

            case DialogResult.Cancel:
                _logger.LogInformation("Скачивание обновления отменено пользователем");
                break;
        }
    }

    private void DrawRelations()
    {
        uiRelationsPanel.Controls.Clear();
        var relations = _orcestrator.GetRelations();

        foreach (var rel in relations)
        {
            using (Splash.Current.StartSpan($"{rel.From.TitleFull} → {rel.To.TitleFull}"))
            {
                var control = _serviceProvider.GetRequiredService<RelationControl>();
                control.SetRelation(rel);
                control.RelationDeleted += (_, _) => DrawRelations();
                control.RelationSelectionChanged += (_, _) => uiMediaMatrixGridControl.RefreshData();
                control.Dock = DockStyle.Top;

                uiRelationsPanel.Controls.Add(control);
                control.SendToBack();
            }
        }

        uiRelationsGraphControl.SetRelations(relations);
        uiMediaMatrixGridControl.PopulateRelationsFilter();
    }
}
