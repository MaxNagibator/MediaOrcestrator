using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        _orcestrator.Init();
        DrawSources();
        // TODO: SetZalupaV2
        uiMediaMatrixGridControl.Initialize(_orcestrator);
        uiMediaMatrixGridControl.RefreshData();
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
            uiMediaMatrixGridControl.RefreshData(GetSelectedRelations());
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

    private void uiAddSourceButton_Click(object sender, EventArgs e)
    {
        if (uiSourcesComboBox.SelectedItem is not ISourceType selectedPlugin)
        {
            return;
        }

        using var settingsForm = new SourceSettingsForm();
        settingsForm.SetSettings(selectedPlugin.SettingsKeys);
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
        DrawSources();
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

        _orcestrator.AddLink(from, to);
        DrawRelations();
    }

    private void uiRelationViewModeCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        uiMediaMatrixGridControl.RefreshData(GetSelectedRelations());
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

    private List<SourceSyncRelation> GetSelectedRelations()
    {
        if (!uiRelationViewModeCheckBox.Checked)
        {
            return [];
        }

        // TODO: Шляпа
        return uiRelationsPanel.Controls.OfType<RelationControl>()
            .Where(x => x is { Selected: true, Relation: not null })
            .Select(x => x.Relation!)
            .ToList();
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
        var offset = -1;
        foreach (var source in _orcestrator.GetSources())
        {
            offset++;
            var control = _serviceProvider.GetRequiredService<SourceControl>();
            control.SetMediaSource(source);
            control.Width = uiMediaSourcePanel.Width;
            control.Top = offset * control.Height;
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
            control.RelationSelectionChanged += (_, _) => uiMediaMatrixGridControl.RefreshData(GetSelectedRelations());

            uiRelationsPanel.Controls.Add(control);
            control.SendToBack();
        }
    }
}
