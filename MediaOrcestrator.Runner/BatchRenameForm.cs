using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class BatchRenameForm : Form
{
    private readonly List<Media> _medias;
    private readonly BatchRenameService _service;
    private CancellationTokenSource? _applyCts;

    public BatchRenameForm()
    {
        _medias = [];
        _service = null!;
        InitializeComponent();
    }

    public BatchRenameForm(List<Media> medias, BatchRenameService service) : this()
    {
        _medias = medias;
        _service = service;
        Text = $"Пакетное переименование ({medias.Count} видео)";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _applyCts?.Cancel();
        _applyCts?.Dispose();
        _applyCts = null;
        base.OnFormClosed(e);
    }

    private void uiPreviewButton_Click(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private async void uiApplyButton_Click(object? sender, EventArgs e)
    {
        await ApplyAsync();
    }

    private void UpdatePreview()
    {
        var find = uiFindTextBox.Text;
        uiPreviewGrid.Rows.Clear();

        if (string.IsNullOrEmpty(find))
        {
            uiApplyButton.Enabled = false;
            return;
        }

        var previews = _service.Preview(_medias, find, uiReplaceTextBox.Text);
        var hasChanges = false;

        foreach (var preview in previews)
        {
            if (!preview.HasChanges)
            {
                var row = uiPreviewGrid.Rows.Add(preview.OldTitle, preview.OldTitle, "(без изменений)");
                uiPreviewGrid.Rows[row].DefaultCellStyle.ForeColor = Color.Gray;
            }
            else
            {
                uiPreviewGrid.Rows.Add(preview.OldTitle, preview.NewTitle, "");
                hasChanges = true;
            }
        }

        uiApplyButton.Enabled = hasChanges;
    }

    private async Task ApplyAsync()
    {
        var find = uiFindTextBox.Text;
        var replace = uiReplaceTextBox.Text;

        uiApplyButton.Enabled = false;
        uiPreviewButton.Enabled = false;
        uiFindTextBox.Enabled = false;
        uiReplaceTextBox.Enabled = false;
        uiStatusLabel.Text = "Применение изменений...";

        _applyCts?.Dispose();
        _applyCts = new();
        var token = _applyCts.Token;

        List<BatchRenameResult> results;

        try
        {
            results = await Task.Run(() => _service.ApplyAsync(_medias, find, replace, token),
                token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (IsDisposed)
            {
                return;
            }

            uiStatusLabel.Text = $"Ошибка: {ex.Message}";
            uiPreviewButton.Enabled = true;
            uiFindTextBox.Enabled = true;
            uiReplaceTextBox.Enabled = true;
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        uiPreviewGrid.Rows.Clear();

        foreach (var result in results)
        {
            var statusText = result.Success ? "Готово" : $"Ошибка: {result.ErrorMessage}";
            var row = uiPreviewGrid.Rows.Add(result.OldTitle, result.NewTitle, statusText);
            uiPreviewGrid.Rows[row].DefaultCellStyle.ForeColor = result.Success
                ? Color.DarkGreen
                : Color.DarkRed;
        }

        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);
        uiStatusLabel.Text = $"Готово: {successCount} успешно, {failCount} с ошибками";

        uiPreviewButton.Enabled = true;
        uiFindTextBox.Enabled = true;
        uiReplaceTextBox.Enabled = true;

        DialogResult = successCount > 0 ? DialogResult.OK : DialogResult.None;
    }
}
