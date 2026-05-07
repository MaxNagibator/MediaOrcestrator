using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MediaOrcestrator.Runner;

// TODO: Переиспользовать CoverGenerator / CoverTemplateStore для генерации обложки из шаблона по названию (как в BatchPreviewForm) - чтобы пользователь не искал файл руками.
// TODO: Подсказывать имя серии (следующий номер эпизода) по шаблону из BatchRenameService, когда выбран источник с уже существующей нумерацией.
public partial class PublishControl : UserControl
{
    private const int MaxTitleSuggestions = 20;
    private const int MaxDescriptionSuggestions = 10;
    private const int DescriptionPreviewLength = 100;

    private readonly Orcestrator? _orcestrator;
    private readonly ILogger<PublishControl>? _logger;
    private readonly List<string> _titleSuggestions = [];
    private readonly Dictionary<string, string> _titleToDescription = new(StringComparer.Ordinal);
    private CancellationTokenSource? _publishCts;
    private string? _videoPath;
    private string? _coverPath;
    private string? _autoFilledTitle;
    private string? _titleBeforeDropDown;
    private string? _videoDescription;
    private bool _isPublishing;
    private bool _suppressTitleFilter;

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

    private enum DescriptionSource
    {
        History,
        VideoFile,
    }

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
        ReloadSourceSuggestions();
        UpdateVideoLabel();
        UpdateCoverLabel();
        UpdateDescriptionCounter();
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

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Enter) && uiPublishButton.Enabled && !_isPublishing)
        {
            uiPublishButton.PerformClick();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
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

    private void uiClearVideoButton_Click(object? sender, EventArgs e)
    {
        SetVideo(null);
    }

    private void uiClearCoverButton_Click(object? sender, EventArgs e)
    {
        SetCover(null);
    }

    private void uiVideoDropPanel_Click(object? sender, EventArgs e)
    {
        if (_isPublishing)
        {
            return;
        }

        uiBrowseVideoButton.PerformClick();
    }

    private void uiCoverDropPanel_Click(object? sender, EventArgs e)
    {
        if (_isPublishing)
        {
            return;
        }

        uiBrowseCoverButton.PerformClick();
    }

    private void uiSourceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdatePublishButtonState();
        ReloadSourceSuggestions();
    }

    private void uiDescriptionTemplateComboBox_SelectionChangeCommitted(object? sender, EventArgs e)
    {
        if (uiDescriptionTemplateComboBox.SelectedItem is not DescriptionTemplate template)
        {
            return;
        }

        var current = uiDescriptionTextBox.Text;
        if (!string.IsNullOrWhiteSpace(current) && !string.Equals(current, template.Text, StringComparison.Ordinal))
        {
            var result = MessageBox.Show(this,
                "Заменить текущее описание выбранным шаблоном?",
                "Замена описания",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                uiDescriptionTemplateComboBox.SelectedIndex = -1;
                return;
            }
        }

        uiDescriptionTextBox.Text = template.Text;
        uiDescriptionTemplateComboBox.SelectedIndex = -1;
    }

    private void uiTitleComboBox_TextChanged(object? sender, EventArgs e)
    {
        UpdatePublishButtonState();

        if (_suppressTitleFilter)
        {
            return;
        }

        FilterTitleSuggestions();
    }

    private void uiTitleComboBox_DropDown(object? sender, EventArgs e)
    {
        _titleBeforeDropDown = uiTitleComboBox.Text;
    }

    private void uiTitleComboBox_Enter(object? sender, EventArgs e)
    {
        if (_titleSuggestions.Count == 0 || !string.IsNullOrEmpty(uiTitleComboBox.Text))
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (uiTitleComboBox.Focused && !uiTitleComboBox.DroppedDown && string.IsNullOrEmpty(uiTitleComboBox.Text))
            {
                uiTitleComboBox.DroppedDown = true;
                Cursor.Current = Cursors.Default;
            }
        });
    }

    private void uiTitleComboBox_SelectionChangeCommitted(object? sender, EventArgs e)
    {
        var previous = _titleBeforeDropDown;
        var selected = uiTitleComboBox.Text;

        var hasUserText = !string.IsNullOrWhiteSpace(previous)
                          && !string.Equals(previous, selected, StringComparison.Ordinal)
                          && !string.Equals(previous, _autoFilledTitle, StringComparison.Ordinal);

        if (hasUserText)
        {
            var result = MessageBox.Show(this,
                "Заменить введённое название выбранным вариантом?",
                "Замена названия",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                uiTitleComboBox.Text = previous;
                return;
            }
        }

        _autoFilledTitle = null;

        if (!string.IsNullOrEmpty(selected)
            && string.IsNullOrWhiteSpace(uiDescriptionTextBox.Text)
            && _titleToDescription.TryGetValue(selected, out var description))
        {
            uiDescriptionTextBox.Text = description;
        }
    }

    private void uiDescriptionTextBox_TextChanged(object? sender, EventArgs e)
    {
        UpdateDescriptionCounter();
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

        var title = uiTitleComboBox.Text.Trim();
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

    private static string? TryFormatFileSize(string path)
    {
        try
        {
            return FormatFileSize(new FileInfo(path).Length);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["Б", "КБ", "МБ", "ГБ"];
        double size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:F1} {units[unitIndex]}";
    }

    private void FilterTitleSuggestions()
    {
        if (_titleSuggestions.Count == 0)
        {
            return;
        }

        var query = uiTitleComboBox.Text;
        var caret = uiTitleComboBox.SelectionStart;

        var matches = string.IsNullOrEmpty(query)
            ? _titleSuggestions.ToArray()
            : _titleSuggestions
                .Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (uiTitleComboBox.Items.Count == matches.Length)
        {
            var equal = true;
            for (var i = 0; i < matches.Length; i++)
            {
                if (string.Equals((string)uiTitleComboBox.Items[i]!, matches[i], StringComparison.Ordinal))
                {
                    continue;
                }

                equal = false;
                break;
            }

            if (equal)
            {
                return;
            }
        }

        _suppressTitleFilter = true;
        try
        {
            uiTitleComboBox.BeginUpdate();
            // Сброс SelectedIndex обязателен ДО Items.Clear(): пока выпадашка открыта, нативный listbox
            // шлёт reflected-команду, ComboBox.get_Text → Items[SelectedIndex] → ArgumentOutOfRangeException на пустом списке.
            uiTitleComboBox.SelectedIndex = -1;
            uiTitleComboBox.Items.Clear();
            if (matches.Length > 0)
            {
                uiTitleComboBox.Items.AddRange(matches);
            }

            if (!string.Equals(uiTitleComboBox.Text, query, StringComparison.Ordinal))
            {
                uiTitleComboBox.Text = query;
            }

            uiTitleComboBox.SelectionStart = Math.Min(caret, query.Length);
            uiTitleComboBox.SelectionLength = 0;
            uiTitleComboBox.EndUpdate();
        }
        finally
        {
            _suppressTitleFilter = false;
        }

        if (!uiTitleComboBox.Focused)
        {
            return;
        }

        if (matches.Length > 0 && !uiTitleComboBox.DroppedDown)
        {
            uiTitleComboBox.DroppedDown = true;
            uiTitleComboBox.Select(query.Length, 0);
            Cursor.Current = Cursors.Default;
        }
        else if (matches.Length == 0 && uiTitleComboBox.DroppedDown)
        {
            uiTitleComboBox.SelectedIndex = -1;
            uiTitleComboBox.DroppedDown = false;
        }
    }

    private void SetVideo(string? path)
    {
        _videoPath = path;
        UpdateVideoLabel();

        if (!string.IsNullOrEmpty(path) && string.IsNullOrWhiteSpace(uiTitleComboBox.Text))
        {
            var fromFile = Path.GetFileNameWithoutExtension(path);
            uiTitleComboBox.Text = fromFile;
            _autoFilledTitle = fromFile;
        }

        UpdateVideoDescriptionSuggestion(path);
        UpdatePublishButtonState();
    }

    private void UpdateVideoDescriptionSuggestion(string? videoPath)
    {
        var newDescription = string.IsNullOrEmpty(videoPath) ? null : TryReadVideoDescription(videoPath);
        if (string.Equals(_videoDescription, newDescription, StringComparison.Ordinal))
        {
            return;
        }

        for (var i = uiDescriptionTemplateComboBox.Items.Count - 1; i >= 0; i--)
        {
            if (uiDescriptionTemplateComboBox.Items[i] is DescriptionTemplate { Source: DescriptionSource.VideoFile })
            {
                uiDescriptionTemplateComboBox.Items.RemoveAt(i);
            }
        }

        _videoDescription = newDescription;

        if (newDescription == null)
        {
            uiDescriptionTemplateComboBox.Enabled = !_isPublishing && uiDescriptionTemplateComboBox.Items.Count > 0;
            return;
        }

        uiDescriptionTemplateComboBox.Items.Insert(0, new DescriptionTemplate(newDescription, DescriptionSource.VideoFile));
        uiDescriptionTemplateComboBox.Enabled = !_isPublishing;

        if (string.IsNullOrWhiteSpace(uiDescriptionTextBox.Text))
        {
            uiDescriptionTextBox.Text = newDescription;
        }
    }

    private string? TryReadVideoDescription(string videoPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(videoPath);
            var name = Path.GetFileNameWithoutExtension(videoPath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var descriptionPath = Path.Combine(dir, name + ".description");
            if (File.Exists(descriptionPath))
            {
                var text = File.ReadAllText(descriptionPath).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            var infoPath = Path.Combine(dir, name + ".info.json");
            if (File.Exists(infoPath))
            {
                using var stream = File.OpenRead(infoPath);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("description", out var element)
                    && element.ValueKind == JsonValueKind.String)
                {
                    var text = element.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }

            var txtPath = Path.Combine(dir, name + ".txt");
            if (File.Exists(txtPath))
            {
                var text = File.ReadAllText(txtPath).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }
        catch (Exception exception)
        {
            _logger?.LogWarning(exception, "Не удалось прочитать описание из файла рядом с видео {Path}", videoPath);
        }

        return null;
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
        if (string.IsNullOrEmpty(_videoPath))
        {
            uiVideoDropLabel.Text = "Перетащите видеофайл сюда или нажмите для выбора";
            uiClearVideoButton.Enabled = false;
            return;
        }

        var fileName = Path.GetFileName(_videoPath);
        var sizeText = TryFormatFileSize(_videoPath);
        uiVideoDropLabel.Text = sizeText != null ? $"{fileName}\r\n{sizeText}" : fileName;
        uiClearVideoButton.Enabled = !_isPublishing;
    }

    private void UpdateCoverLabel()
    {
        if (string.IsNullOrEmpty(_coverPath))
        {
            uiCoverDropLabel.Text = "Перетащите изображение сюда или нажмите для выбора";
            uiClearCoverButton.Enabled = false;
            return;
        }

        uiCoverDropLabel.Text = Path.GetFileName(_coverPath);
        uiClearCoverButton.Enabled = !_isPublishing;
    }

    private void UpdateDescriptionCounter()
    {
        uiDescriptionCounterLabel.Text = uiDescriptionTextBox.TextLength.ToString();
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
                                  && !string.IsNullOrWhiteSpace(uiTitleComboBox.Text)
                                  && !string.IsNullOrEmpty(_videoPath);
    }

    private void SetControlsBusy(bool busy)
    {
        uiSourceComboBox.Enabled = !busy;
        uiTitleComboBox.Enabled = !busy;
        uiDescriptionTextBox.Enabled = !busy;
        uiDescriptionTemplateComboBox.Enabled = !busy && uiDescriptionTemplateComboBox.Items.Count > 0;
        uiBrowseVideoButton.Enabled = !busy;
        uiBrowseCoverButton.Enabled = !busy;
        uiClearVideoButton.Enabled = !busy && !string.IsNullOrEmpty(_videoPath);
        uiClearCoverButton.Enabled = !busy && !string.IsNullOrEmpty(_coverPath);

        var dropCursor = busy ? Cursors.Default : Cursors.Hand;
        uiVideoDropPanel.Cursor = dropCursor;
        uiVideoDropLabel.Cursor = dropCursor;
        uiCoverDropPanel.Cursor = dropCursor;
        uiCoverDropLabel.Cursor = dropCursor;
        uiCoverPreviewPictureBox.Cursor = dropCursor;

        uiPublishProgressBar.Visible = busy;

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
        uiTitleComboBox.Text = string.Empty;
        _autoFilledTitle = null;
        _titleBeforeDropDown = null;
        uiDescriptionTextBox.Clear();
        SetVideo(null);
        SetCover(null);
        ReloadSourceSuggestions();
    }

    private void ReloadSourceSuggestions()
    {
        _suppressTitleFilter = true;
        try
        {
            uiTitleComboBox.BeginUpdate();
            var preservedTitle = uiTitleComboBox.Text;
            uiTitleComboBox.Items.Clear();
            _titleSuggestions.Clear();
            _titleToDescription.Clear();

            uiDescriptionTemplateComboBox.BeginUpdate();
            uiDescriptionTemplateComboBox.Items.Clear();

            if (_videoDescription != null)
            {
                uiDescriptionTemplateComboBox.Items.Add(new DescriptionTemplate(_videoDescription, DescriptionSource.VideoFile));
            }

            if (_orcestrator == null || uiSourceComboBox.SelectedItem is not Source source)
            {
                uiTitleComboBox.Text = preservedTitle;
                uiTitleComboBox.EndUpdate();
                uiDescriptionTemplateComboBox.EndUpdate();
                uiDescriptionTemplateComboBox.Enabled = !_isPublishing && uiDescriptionTemplateComboBox.Items.Count > 0;
                return;
            }

            var links = _orcestrator.GetMedias()
                .SelectMany(x => x.Sources ?? [])
                .Where(x => x.SourceId == source.Id)
                .OrderByDescending(x => x.SortNumber)
                .ToList();

            foreach (var title in links.Select(x => x.Title)
                         .Where(x => !string.IsNullOrEmpty(x))
                         .Distinct(StringComparer.Ordinal)
                         .Take(MaxTitleSuggestions))
            {
                _titleSuggestions.Add(title);
                uiTitleComboBox.Items.Add(title);
            }

            foreach (var link in links)
            {
                if (string.IsNullOrEmpty(link.Title) || string.IsNullOrWhiteSpace(link.Description))
                {
                    continue;
                }

                _titleToDescription.TryAdd(link.Title, link.Description);
            }

            uiTitleComboBox.Text = preservedTitle;
            uiTitleComboBox.EndUpdate();

            foreach (var description in links.Select(x => x.Description)
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Where(x => !string.Equals(x, _videoDescription, StringComparison.Ordinal))
                         .Distinct(StringComparer.Ordinal)
                         .Take(MaxDescriptionSuggestions))
            {
                uiDescriptionTemplateComboBox.Items.Add(new DescriptionTemplate(description));
            }

            uiDescriptionTemplateComboBox.EndUpdate();
            uiDescriptionTemplateComboBox.Enabled = !_isPublishing && uiDescriptionTemplateComboBox.Items.Count > 0;
        }
        finally
        {
            _suppressTitleFilter = false;
        }

        if (!string.IsNullOrEmpty(uiTitleComboBox.Text))
        {
            FilterTitleSuggestions();
        }
    }

    private sealed class DescriptionTemplate(string text, DescriptionSource source = DescriptionSource.History)
    {
        public string Text { get; } = text;

        public DescriptionSource Source { get; } = source;

        public override string ToString()
        {
            var newLineIndex = Text.IndexOfAny(['\r', '\n']);
            var firstLine = newLineIndex >= 0 ? Text[..newLineIndex] : Text;
            firstLine = firstLine.Trim();
            var preview = firstLine.Length > DescriptionPreviewLength
                ? firstLine[..DescriptionPreviewLength] + "..."
                : firstLine;

            return Source == DescriptionSource.VideoFile ? "[Из файла видео] " + preview : preview;
        }
    }
}
