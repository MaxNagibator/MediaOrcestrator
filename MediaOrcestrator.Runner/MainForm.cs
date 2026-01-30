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

    public MainForm(Orcestrator orcestrator, IServiceProvider serviceProvider, ILogger<MainForm> logger)
    {
        _orcestrator = orcestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;

        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        _orcestrator.Init();
        DrawSources();
        // TODO: SetZalupaV2
        mediaMatrixGridControl1.Initialize(_orcestrator);
        mediaMatrixGridControl1.RefreshData();
    }

    private async void btnSync_Click(object sender, EventArgs e)
    {
        _logger.LogInformation("Пользователь нажал кнопку синхронизации.");
        btnSync.Enabled = false;
        try
        {
            await _orcestrator.GetStorageFullInfo();
            _logger.LogInformation("Синхронизация через UI завершена.");
            DrawSources();
            mediaMatrixGridControl1.RefreshData(GetSelectedRelations());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации через UI.");
            MessageBox.Show($"Ошибка при синхронизации: {ex.Message}");
        }
        finally
        {
            btnSync.Enabled = true;
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

        _orcestrator.AddSource(selectedPlugin.Name, settingsForm.Settings);
        DrawSources();
    }

    private void uMediaSourcePanel_SizeChanged(object sender, EventArgs e)
    {
        DrawSources();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        var from = (Source)uiRelationFromComboBox.SelectedItem;
        var to = (Source)uiRelationToComboBox.SelectedItem;

        _orcestrator.AddLink(from, to);
        DrawRelations();
    }

    private void uiRelationViewModeCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        mediaMatrixGridControl1.RefreshData(GetSelectedRelations());
    }

    private List<SourceSyncRelation> GetSelectedRelations()
    {
        if (!uiRelationViewModeCheckBox.Checked)
        {
            return [];
        }

        // TODO: Шляпа
        return panel1.Controls.OfType<RelationControl>()
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

        uMediaSourcePanel.Controls.Clear();
        var offset = -1;
        foreach (var source in _orcestrator.GetSources())
        {
            offset++;
            var control = _serviceProvider.GetRequiredService<SourceControl>();
            control.SetMediaSource(source);
            control.Width = uMediaSourcePanel.Width;
            control.Top = offset * control.Height;
            control.SourceDeleted += (_, _) => DrawSources();
            control.SourceUpdated += (_, _) => DrawSources();

            uMediaSourcePanel.Controls.Add(control);
            control.SendToBack();

            uiRelationFromComboBox.Items.Add(source);
            uiRelationToComboBox.Items.Add(source);
        }

        DrawRelations();
    }

    private void DrawRelations()
    {
        panel1.Controls.Clear();
        var relations = _orcestrator.GetRelations();

        foreach (var rel in relations)
        {
            var control = _serviceProvider.GetRequiredService<RelationControl>();
            control.SetRelation(rel);
            control.RelationDeleted += (_, _) => DrawRelations();
            control.RelationSelectionChanged += (_, _) => mediaMatrixGridControl1.RefreshData(GetSelectedRelations());

            panel1.Controls.Add(control);
            control.SendToBack();
        }
    }
}
