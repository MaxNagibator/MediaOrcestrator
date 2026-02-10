using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class SourceControl : UserControl
{
    private readonly Orcestrator _orcestrator;
    private Source? _source;

    public SourceControl(Orcestrator orcestrator)
    {
        InitializeComponent();
        _orcestrator = orcestrator;
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

    // TODO: Дублирование
    private void uiEditButton_Click(object sender, EventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        var sources = _orcestrator.GetSourceTypes();
        var pluginInfo = sources.Values.FirstOrDefault(x => x.Name == _source.TypeId);

        if (pluginInfo == null)
        {
            MessageBox.Show("Плагин для этого типа источника не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var settingsForm = new SourceSettingsForm();
        // TODO: Подумать _source.Type
        settingsForm.SetSettings(pluginInfo.SettingsKeys, _source.Type);
        settingsForm.SetEditSource(_source);

        if (settingsForm.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        if (_source == null)
        {
            MessageBox.Show("Источник не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _orcestrator.UpdateSource(_source);
        SetMediaSource(_source);
        SourceUpdated?.Invoke(this, EventArgs.Empty);
    }
}
