using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

// TODO: Drag-n-drop видео и обложки.
// TODO: Переиспользовать CoverGenerator / CoverTemplateStore для генерации обложки из шаблона по названию (как в BatchPreviewForm) - чтобы пользователь не искал файл руками.
// TODO: Подсказывать имя серии (следующий номер эпизода) по шаблону из BatchRenameService, когда выбран источник с уже существующей нумерацией.
public partial class PublishControl : UserControl
{
    private readonly Orcestrator? _orcestrator;
    private readonly ILogger<PublishControl>? _logger;
    private CancellationTokenSource? _publishCts;
    private string? _videoPath;
    private string? _coverPath;
    private bool _isPublishing;

    public PublishControl()
    {
        InitializeComponent();
    }

    public PublishControl(Orcestrator orcestrator, ILogger<PublishControl> logger) : this()
    {
        _orcestrator = orcestrator;
        _logger = logger;
    }

    public event EventHandler? MediaPublished;

    public void ReloadSources()
    {
        if (_orcestrator == null)
        {
            return;
        }

        var selectedId = (uiSourceComboBox.SelectedItem as Source)?.Id;

        uiSourceComboBox.BeginUpdate();
        uiSourceComboBox.Items.Clear();
        uiSourceComboBox.DisplayMember = nameof(Source.TitleFull);

        var sources = _orcestrator.GetSources()
            .Where(x => x is { IsDisable: false, Type: not null } && x.Type.ChannelType != SyncDirection.OnlyDownload)
            .ToList();

        foreach (var source in sources)
        {
            uiSourceComboBox.Items.Add(source);
        }

        if (selectedId != null)
        {
            var restored = sources.FindIndex(x => x.Id == selectedId);
            if (restored >= 0)
            {
                uiSourceComboBox.SelectedIndex = restored;
            }
        }

        if (uiSourceComboBox.SelectedIndex < 0 && uiSourceComboBox.Items.Count > 0)
        {
            uiSourceComboBox.SelectedIndex = 0;
        }

        uiSourceComboBox.EndUpdate();
        UpdatePublishButtonState();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ReloadSources();
        UpdateVideoLabel();
        UpdateCoverLabel();
        SetStatus("Готов к публикации.", false);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        var image = uiCoverPreviewPictureBox.Image;
        uiCoverPreviewPictureBox.Image = null;
        image?.Dispose();

        _publishCts?.Cancel();
        _publishCts?.Dispose();
        _publishCts = null;

        base.OnHandleDestroyed(e);
    }

    private void uiBrowseVideoButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите видеофайл",
            Filter = "Видео|*.mp4;*.mkv;*.webm;*.mov;*.avi;*.m4v;*.wmv;*.flv;*.ts|Все файлы|*.*",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetVideo(dialog.FileName);
        }
    }

    private void uiBrowseCoverButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите обложку",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif|Все файлы|*.*",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetCover(dialog.FileName);
        }
    }

    private void uiClearCoverButton_Click(object? sender, EventArgs e)
    {
        SetCover(null);
    }

    private void uiSourceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdatePublishButtonState();
    }

    private void uiTitleTextBox_TextChanged(object? sender, EventArgs e)
    {
        UpdatePublishButtonState();
    }

    private async void uiPublishButton_Click(object? sender, EventArgs e)
    {
        if (_isPublishing)
        {
            uiPublishButton.Enabled = false;
            SetStatus("Отмена...", false);

            if (_publishCts != null)
            {
                await _publishCts.CancelAsync();
            }

            return;
        }

        if (_orcestrator == null || _logger == null)
        {
            return;
        }

        if (uiSourceComboBox.SelectedItem is not Source source)
        {
            SetStatus("Выберите источник.", true);
            return;
        }

        var title = uiTitleTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            SetStatus("Укажите название.", true);
            return;
        }

        if (string.IsNullOrEmpty(_videoPath) || !File.Exists(_videoPath))
        {
            SetStatus("Выберите видеофайл.", true);
            return;
        }

        _isPublishing = true;
        _publishCts?.Dispose();
        _publishCts = new();
        var token = _publishCts.Token;

        SetControlsBusy(true);
        SetStatus($"Публикация в «{source.TitleFull}»...", false);

        var description = uiDescriptionTextBox.Text;
        var videoPath = _videoPath;
        var coverPath = _coverPath;

        try
        {
            await Task.Run(() => _orcestrator.PublishMediaAsync(source, title, description, videoPath, coverPath, token), token);

            SetStatus($"Опубликовано в «{source.TitleFull}».", false);
            _logger.LogInformation("Медиа «{Title}» опубликовано в {Source}", title, source.TitleFull);
            ResetForm();
            MediaPublished?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogInformation(exception, "Публикация «{Title}» в {Source} отменена пользователем", title, source.TitleFull);
            SetStatus("Публикация отменена.", false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ошибка при публикации «{Title}» в {Source}", title, source.TitleFull);
            SetStatus($"Ошибка: {exception.Message}", true);
        }
        finally
        {
            _isPublishing = false;
            SetControlsBusy(false);
            UpdatePublishButtonState();
        }
    }

    private void SetVideo(string? path)
    {
        _videoPath = path;
        UpdateVideoLabel();

        if (!string.IsNullOrEmpty(path) && string.IsNullOrWhiteSpace(uiTitleTextBox.Text))
        {
            uiTitleTextBox.Text = Path.GetFileNameWithoutExtension(path);
        }

        UpdatePublishButtonState();
    }

    private void SetCover(string? path)
    {
        _coverPath = path;

        var previousImage = uiCoverPreviewPictureBox.Image;
        uiCoverPreviewPictureBox.Image = null;
        previousImage?.Dispose();

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var temp = Image.FromStream(stream);
                uiCoverPreviewPictureBox.Image = new Bitmap(temp);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(exception, "Не удалось отобразить превью обложки {Path}", path);
            }
        }

        UpdateCoverLabel();
    }

    private void UpdateVideoLabel()
    {
        uiVideoDropLabel.Text = string.IsNullOrEmpty(_videoPath)
            ? "Нажмите «Обзор...» чтобы выбрать видеофайл"
            : Path.GetFileName(_videoPath);
    }

    private void UpdateCoverLabel()
    {
        uiCoverDropLabel.Text = string.IsNullOrEmpty(_coverPath)
            ? "Нажмите «Обзор...» чтобы выбрать обложку"
            : Path.GetFileName(_coverPath);

        uiClearCoverButton.Enabled = !string.IsNullOrEmpty(_coverPath);
    }

    private void UpdatePublishButtonState()
    {
        if (_isPublishing)
        {
            uiPublishButton.Text = "Отменить";
            uiPublishButton.Enabled = true;
            return;
        }

        uiPublishButton.Text = "Опубликовать";
        uiPublishButton.Enabled = uiSourceComboBox.SelectedItem is Source
                                  && !string.IsNullOrWhiteSpace(uiTitleTextBox.Text)
                                  && !string.IsNullOrEmpty(_videoPath);
    }

    private void SetControlsBusy(bool busy)
    {
        uiSourceComboBox.Enabled = !busy;
        uiTitleTextBox.Enabled = !busy;
        uiDescriptionTextBox.Enabled = !busy;
        uiBrowseVideoButton.Enabled = !busy;
        uiBrowseCoverButton.Enabled = !busy;
        uiClearCoverButton.Enabled = !busy && !string.IsNullOrEmpty(_coverPath);

        uiPublishButton.Text = busy ? "Отменить" : "Опубликовать";
        uiPublishButton.Enabled = true;
    }

    private void SetStatus(string message, bool isError)
    {
        uiStatusLabel.Text = message;
        uiStatusLabel.ForeColor = isError ? Color.Firebrick : Color.DimGray;
    }

    private void ResetForm()
    {
        uiTitleTextBox.Clear();
        uiDescriptionTextBox.Clear();
        SetVideo(null);
        SetCover(null);
    }
}
