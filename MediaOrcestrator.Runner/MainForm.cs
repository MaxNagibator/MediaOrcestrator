using MediaOrcestrator.Domain;
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

    public MainForm(Orcestrator orcestrator, IServiceProvider serviceProvider, ILogger<MainForm> logger, RichTextBox logControl)
    {
        _orcestrator = orcestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;

        InitializeComponent();
        uiLogsTabPage.Controls.Add(logControl);
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        DrawSources();
        DrawRelations();
        // TODO: SetZalupaV2
        uiMediaMatrixGridControl.Initialize(_orcestrator);
        uiMediaMatrixGridControl.RefreshData();

        if (uiClearTypeComboBox.Items.Count > 0)
        {
            uiClearTypeComboBox.SelectedIndex = 0;
        }
    }

    private async void uiSyncButton_Click(object sender, EventArgs e)
    {
        _logger.LogInformation("Пользователь нажал кнопку синхронизации.");
        uiSyncButton.Enabled = false;
        try
        {
            await _orcestrator.GetStorageFullInfo();
            _logger.LogInformation("Синхронизация через UI завершена.");
            DrawSources();
            uiMediaMatrixGridControl.RefreshData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации через UI.");
            MessageBox.Show($"Ошибка при синхронизации: {ex.Message}");
        }
        finally
        {
            uiSyncButton.Enabled = true;
        }
    }

    private void uiPlanSyncButton_Click(object sender, EventArgs e)
    {
        var planner = _serviceProvider.GetRequiredService<SyncPlanner>();
        var medias = _orcestrator.GetMedias();
        var relations = _orcestrator.GetRelations();

        var intents = planner.Plan(medias, relations);

        if (intents.Count == 0)
        {
            MessageBox.Show("Нет доступных задач для синхронизации.");
            return;
        }

        // TODO: DI
        uiSyncTreeControl.Initialize(intents, _orcestrator, _serviceProvider.GetRequiredService<ILogger<SyncTreeControl>>());
        uiMainTabControl.SelectedTab = uiSyncTreeTabPage;
    }

    private void uiAddSourceButton_Click(object sender, EventArgs e)
    {
        if (uiSourcesComboBox.SelectedItem is not ISourceType selectedPlugin)
        {
            return;
        }

        using var settingsForm = new SourceSettingsForm();
        settingsForm.SetSettings(selectedPlugin.SettingsKeys, selectedPlugin);
        if (settingsForm.ShowDialog() != DialogResult.OK || settingsForm.Settings == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedPlugin.Name))
        {
            MessageBox.Show("Имя типа источника не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _orcestrator.AddSource(selectedPlugin.Name, settingsForm.Settings);
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
        Task.Run(async () =>
        {
            try
            {
                var media = _orcestrator.GetMedias().Skip(0).First();
                var source = media.Sources.First();

                var sadasd = _orcestrator.GetRelations().First();
                await sadasd.From.Type.Download(source.ExternalId, sadasd.From.Settings);
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

    private void DrawSources()
    {
        uiRelationFromComboBox.Items.Clear();
        uiRelationToComboBox.Items.Clear();
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
            var control = _serviceProvider.GetRequiredService<SourceControl>();
            control.SetMediaSource(source);
            control.Dock = DockStyle.Top;
            control.SourceDeleted += (_, _) => DrawSources();
            control.SourceUpdated += (_, _) => DrawSources();

            uiMediaSourcePanel.Controls.Add(control);
            control.SendToBack();

            uiRelationFromComboBox.Items.Add(source);
            uiRelationToComboBox.Items.Add(source);
        }

        DrawRelations();
    }

    private void DrawRelations()
    {
        uiRelationsPanel.Controls.Clear();
        var relations = _orcestrator.GetRelations();

        foreach (var rel in relations)
        {
            var control = _serviceProvider.GetRequiredService<RelationControl>();
            control.SetRelation(rel);
            control.RelationDeleted += (_, _) => DrawRelations();
            control.RelationSelectionChanged += (_, _) => uiMediaMatrixGridControl.RefreshData();
            control.Dock = DockStyle.Top;

            uiRelationsPanel.Controls.Add(control);
            control.SendToBack();
        }

        uiMediaMatrixGridControl.PopulateRelationsFilter();
    }

    private void uiRubuteAuthStateOpenBrowserButton_Click(object sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = false,
            });

            var contextOptions = new BrowserNewContextOptions();

            if (File.Exists(uiRubuteAuthStatePathTextBox.Text))
            {
                contextOptions.StorageStatePath = uiRubuteAuthStatePathTextBox.Text;
            }

            await using var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();

            var captureLog = new List<object>();

            page.Request += (_, request) =>
            {
                var reqData = new
                {
                    Timestamp = DateTime.UtcNow,
                    Type = "Request",
                    request.Method,
                    request.Url,
                    request.Headers,
                    request.PostData,
                };

                lock (captureLog)
                {
                    captureLog.Add(reqData);
                    _logger.LogInformation($"[REQ] {request.Method} {request.Url}");
                }
            };

            page.Response += async (_, response) =>
            {
                string? body = null;
                try
                {
                    if (response.Headers.TryGetValue("content-type", out var contentType) && (contentType.Contains("application/json") || contentType.Contains("text/")))
                    {
                        body = await response.TextAsync();
                    }
                }
                catch
                {
                }

                var resData = new
                {
                    Timestamp = DateTime.UtcNow,
                    Type = "Response",
                    response.Status,
                    response.Url,
                    response.Headers,
                    Body = body,
                };

                lock (captureLog)
                {
                    captureLog.Add(resData);
                    _logger.LogInformation($"[RES] {response.Status} {response.Url}");
                }
            };

            _logger.LogInformation("Navigating to Rutube...");
            await page.GotoAsync("https://studio.rutube.ru/", new()
            {
                Timeout = 0,
            });

            var msg = "Зайдите в свой рутуб студия и нажмие OK, или отмена, если передумали";
            if (MessageBox.Show(msg, "Rutube auth state", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                _logger.LogInformation("Browser is open.");
                _logger.LogInformation("Press any key to save log and exit...");

                await context.StorageStateAsync(new()
                {
                    Path = uiRubuteAuthStatePathTextBox.Text,
                });

                _logger.LogInformation("Log saved to capture_log.json");
                _logger.LogInformation("Auth state saved to auth_state.json");
            }
        });
    }
}
