using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class MediaSourceControl : UserControl
{
    private readonly Orcestrator _orcestrator;
    private MySource? _source;

    public MediaSourceControl(Orcestrator orcestrator)
    {
        InitializeComponent();
        _orcestrator = orcestrator;
    }

    public event EventHandler? SourceDeleted;

    public void SetMediaSource(MySource source)
    {
        _source = source;
        var sources = _orcestrator.GetSources();

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

        _orcestrator.RemoveSource(_source.Id);
        SourceDeleted?.Invoke(this, EventArgs.Empty);
    }
}
