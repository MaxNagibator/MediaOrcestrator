using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class SourceControl : UserControl
{
    private readonly Orcestrator _orcestrator;
    private readonly StateManager _stateManager;
    private readonly ILogger<SourceControl> _logger;
    private Source? _source;

    public SourceControl(Orcestrator orcestrator, StateManager stateManager, ILogger<SourceControl> logger)
    {
        InitializeComponent();
        _orcestrator = orcestrator;
        _stateManager = stateManager;
        _logger = logger;
    }

    public event EventHandler? SourceDeleted;
    public event EventHandler? SourceUpdated;

    public void SetMediaSource(Source source)
    {
        _source = source;
        var sources = _orcestrator.GetSourceTypes();

        // todo ключа пока нет
        var pluginInfo = sources.Values.FirstOrDefault(x => x.Name == source.TypeId);

        uiTitleLabel.Text = source.Title;
        uiTypeLabel.Text = pluginInfo?.Name ?? source.TypeId;
    }

    private void uiDeleteButton_Click(object sender, EventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        var dialogResult = MessageBox.Show("Вы уверены, что хотите удалить этот источник?", "Удаление источника", MessageBoxButtons.YesNo);
        if (dialogResult != DialogResult.Yes)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_source.Id))
        {
            MessageBox.Show("Идентификатор источника не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _orcestrator.RemoveSource(_source.Id);
        SourceDeleted?.Invoke(this, EventArgs.Empty);
    }

    private void uiDuplicateButton_Click(object sender, EventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        var sourceType = ResolveSourceType(_source);
        if (sourceType == null)
        {
            return;
        }

        var newSourceId = Guid.NewGuid().ToString();
        var settings = SourceSettingsForm.ShowDuplicate(_source, sourceType, _stateManager, newSourceId, _orcestrator.GetSources(), _logger);

        if (settings == null)
        {
            return;
        }

        _orcestrator.AddSource(newSourceId, _source.TypeId, settings);
        SourceUpdated?.Invoke(this, EventArgs.Empty);
    }

    // TODO: Дублирование
    private void uiEditButton_Click(object sender, EventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        var sourceType = ResolveSourceType(_source);
        if (sourceType == null)
        {
            return;
        }

        if (!SourceSettingsForm.ShowEdit(_source, sourceType, _orcestrator.GetSources(), _logger))
        {
            return;
        }

        _orcestrator.UpdateSource(_source);
        SetMediaSource(_source);
        SourceUpdated?.Invoke(this, EventArgs.Empty);
    }

    private ISourceType? ResolveSourceType(Source source)
    {
        var sourceType = source.Type ?? _orcestrator.GetSourceTypes().Values.FirstOrDefault(x => x.Name == source.TypeId);

        if (sourceType == null)
        {
            MessageBox.Show("Плагин для этого типа источника не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return sourceType;
    }
}
