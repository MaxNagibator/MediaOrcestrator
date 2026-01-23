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
        var sources = _orcestrator.GetSources();

        _source = source;
        // todo ключа пока нет
        label1.Text = sources.First(x => x.Value.Name == source.TypeId).Value.Name;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        _orcestrator.RemoveSource(_source.Id);
        SourceDeleted?.Invoke(this, EventArgs.Empty);
    }
}
