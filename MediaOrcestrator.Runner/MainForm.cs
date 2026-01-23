using MediaOrcestrator.Domain;
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
        InitializeComponent();
        _orcestrator = orcestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            await _orcestrator.Sync();
            _logger.LogInformation("Синхронизация через UI завершена.");
            DrawSources();
            mediaMatrixGridControl1.RefreshData();
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

    private void DrawSources()
    {
        uiMediaSourcePanel.Controls.Clear();

        var shift = 10;
        foreach (var source in _orcestrator.GetSources())
        {
            // TODO: Сомнительно
            var control = _serviceProvider.GetRequiredService<MediaSourceControl>();
            control.SetMediaSource(source.Value);
            control.Width = uiMediaSourcePanel.Width - 20;
            control.Height = 80;
            control.Left = 10;
            control.Top = shift;
            shift += 100;
            uiMediaSourcePanel.Controls.Add(control);
        }
    }
}
