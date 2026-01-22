using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class MainForm : Form
{
    private Orcestrator _orcestrator;

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        _orcestrator = OrcestratorBuilder.Construct();
        _orcestrator.Init();
        DrawSources();
    }

    private void DrawSources()
    {
        uiMediaSourcePanel.Controls.Clear();

        var shift = 10;
        foreach (var source in _orcestrator.GetSources())
        {
            var control = new MediaSourceControl();
            control.SetMediaSource(source.Value);
            control.SetZalup(_orcestrator);
            control.Width = uiMediaSourcePanel.Width - 20;
            control.Height = 80;
            control.Left = 10;
            control.Top = shift;
            shift += 100;
            uiMediaSourcePanel.Controls.Add(control);
        }
    }
}
