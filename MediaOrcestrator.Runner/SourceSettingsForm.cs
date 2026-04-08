using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner;

public partial class SourceSettingsForm : Form
{
    private readonly Dictionary<string, Control> _controls = [];
    private readonly List<Button> _loadButtons = [];
    private IEnumerable<SourceSettings> _settingsKeys = [];
    private Source? _editSource;
    private ISourceType? _sourceType;
    private ILogger? _logger;
    private Button? _authButton;
    private Label? _authStatusLabel;
    private string[]? _docFiles;

    public SourceSettingsForm()
    {
        InitializeComponent();
    }

    public Dictionary<string, string>? Settings { get; private set; }

    public void SetSettings(IEnumerable<SourceSettings> settingsKeys, ISourceType? sourceType = null, ILogger? logger = null)
    {
        _settingsKeys = settingsKeys;
        _sourceType = sourceType;
        _logger = logger;
    }

    public void SetEditSource(Source source)
    {
        _editSource = source;
        uiNameTextBox.Text = source.Title;
        uiCreateButton.Text = "Сохранить изменения";
    }

    private void SourceSettingsForm_Load(object sender, EventArgs e)
    {
        uiSettingsPanel.Controls.Clear();
        _controls.Clear();
        _loadButtons.Clear();

        _docFiles = FindDocFiles();

        if (_docFiles.Length > 0)
        {
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new(0, 5, 0, 5),
            };

            var uiDocsButton = new Button
            {
                Text = "Руководство",
                Width = 140,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.System,
            };

            uiDocsButton.Click += uiDocsButton_Click;

            uiMainLayout.Controls.Remove(uiCreateButton);
            uiCreateButton.Dock = DockStyle.Right;

            bottomPanel.Controls.Add(uiCreateButton);
            bottomPanel.Controls.Add(uiDocsButton);
            uiMainLayout.Controls.Add(bottomPanel, 0, 3);
        }

        // TODO: Костыль. Можно подумать над перемешением в IStorageType
        if (_sourceType is IAuthenticatable auth && _logger != null)
        {
            var authPanel = CreateAuthPanel(auth);
            authPanel.Dock = DockStyle.Fill;

            var bottomControl = uiMainLayout.GetControlFromPosition(0, 3);
            uiMainLayout.RowCount = 5;

            if (bottomControl != null)
            {
                uiMainLayout.SetRow(bottomControl, 4);
            }

            uiMainLayout.SetRow(uiSettingsPanel, 3);
            uiMainLayout.Controls.Add(authPanel, 0, 2);

            uiMainLayout.RowStyles.Clear();
            uiMainLayout.RowStyles.Add(new(SizeType.AutoSize));
            uiMainLayout.RowStyles.Add(new(SizeType.AutoSize));
            uiMainLayout.RowStyles.Add(new(SizeType.Absolute, 45F));
            uiMainLayout.RowStyles.Add(new(SizeType.Percent, 100F));
            uiMainLayout.RowStyles.Add(new(SizeType.Absolute, 45F));

        }

        var settingsTable = CreateSettingsTable();
        uiSettingsPanel.Controls.Add(settingsTable);

        if (_settingsKeys == null)
        {
            return;
        }

        foreach (var setting in _settingsKeys)
        {
            var card = CreateSettingCard(setting);
            settingsTable.RowStyles.Add(new(SizeType.AutoSize));
            settingsTable.Controls.Add(card, 0, settingsTable.RowCount++);
        }

        if (_sourceType is IAuthenticatable authForStatus && _logger != null)
        {
            UpdateAuthStatus(authForStatus);
        }
    }

    private void uiCreateButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(uiNameTextBox.Text))
        {
            MessageBox.Show("Имя обязательно", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Settings = _editSource?.Settings ?? [];
        Settings["_system_name"] = uiNameTextBox.Text;

        foreach (var (key, control) in _controls)
        {
            Settings[key] = GetControlValue(control);
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void uiDocsButton_Click(object? sender, EventArgs e)
    {
        if (_docFiles == null || _docFiles.Length == 0)
        {
            return;
        }

        var selectedFile = _docFiles[0];

        if (_docFiles.Length > 1)
        {
            var names = _docFiles.Select(Path.GetFileNameWithoutExtension).ToArray();

            using var selectForm = new Form
            {
                Text = "Выберите руководство",
                Size = new(350, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new("Segoe UI", 10F),
            };

            listBox.Items.AddRange(names!);
            listBox.SelectedIndex = 0;

            listBox.DoubleClick += (_, _) =>
            {
                selectForm.DialogResult = DialogResult.OK;
                selectForm.Close();
            };

            var okButton = new Button
            {
                Text = "Открыть",
                Dock = DockStyle.Bottom,
                Height = 35,
                DialogResult = DialogResult.OK,
            };

            selectForm.Controls.Add(listBox);
            selectForm.Controls.Add(okButton);
            selectForm.AcceptButton = okButton;

            if (selectForm.ShowDialog(this) != DialogResult.OK || listBox.SelectedIndex < 0)
            {
                return;
            }

            selectedFile = _docFiles[listBox.SelectedIndex];
        }

        var markdown = File.ReadAllText(selectedFile);
        var title = Path.GetFileNameWithoutExtension(selectedFile);
        var basePath = Path.GetDirectoryName(selectedFile)!;

        var docForm = new DocumentationForm(title, markdown, basePath);
        docForm.Show(this);
    }

    private static Label CreateLabel(string text)
    {
        return new()
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(64, 64, 64),
            Padding = new(0, 0, 0, 4),
        };
    }

    private static Label CreateDescriptionLabel(string text)
    {
        var descriptionLabel = new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = false,
            Font = new("Segoe UI", 8F),
            ForeColor = Color.FromArgb(128, 128, 128),
            Padding = new(0, 3, 0, 1),
        };

        descriptionLabel.SizeChanged += (s, _) =>
        {
            var label = (Label)s!;
            if (label.Width <= 0)
            {
                return;
            }

            var proposedSize = new Size(label.Width, int.MaxValue);
            var measureText = TextRenderer.MeasureText(label.Text, label.Font, proposedSize, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            var measuredHeight = measureText.Height + label.Padding.Vertical;

            if (label.Height != measuredHeight)
            {
                label.Height = measuredHeight;
            }
        };

        return descriptionLabel;
    }

    private static string GetControlValue(Control control)
    {
        return control switch
        {
            TextBox textBox => textBox.Text,
            ComboBox comboBox => (comboBox.SelectedItem as ComboBoxItem)?.Value ?? "",
            _ => "",
        };
    }

    private TableLayoutPanel CreateSettingsTable()
    {
        var table = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0,
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new(0, 2, 0, 2),
        };

        table.ColumnStyles.Add(new(SizeType.Percent, 100F));
        return table;
    }

    private Panel CreateAuthPanel(IAuthenticatable auth)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            BackColor = Color.White,
            Padding = new(10, 8, 10, 8),
            Margin = new(5, 3, 5, 3),
            BorderStyle = BorderStyle.FixedSingle,
        };

        _authButton = new()
        {
            Text = "Авторизовать",
            Width = 120,
            Height = 28,
            Dock = DockStyle.Left,
        };

        _authStatusLabel = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new(10, 0, 0, 0),
        };

        _authButton.Click += async (_, _) => await RunAuthAsync(auth);

        panel.Controls.Add(_authStatusLabel);
        panel.Controls.Add(_authButton);

        return panel;
    }

    private async Task RunAuthAsync(IAuthenticatable auth)
    {
        _authButton!.Enabled = false;
        _authButton.Text = "Авторизация...";

        try
        {
            var settings = GetCurrentSettings();
            var ui = new WinFormsAuthUI(this, _logger!);
            await Task.Run(() => auth.AuthenticateAsync(settings, ui, CancellationToken.None));
            UpdateAuthStatus(auth);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _authButton.Enabled = true;
            _authButton.Text = "Авторизовать";
        }
    }

    private void UpdateAuthStatus(IAuthenticatable auth)
    {
        var settings = GetCurrentSettings();
        var isAuth = auth.IsAuthenticated(settings);

        if (_authStatusLabel != null)
        {
            _authStatusLabel.Text = isAuth ? "Авторизован" : "Не авторизован";
            _authStatusLabel.ForeColor = isAuth ? Color.Green : Color.Red;
        }

        foreach (var btn in _loadButtons)
        {
            btn.Enabled = isAuth;
        }
    }

    private Panel CreateSettingCard(SourceSettings setting)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new(10, 8, 10, 8),
            Margin = new(5, 3, 5, 3),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var labelText = setting.IsRequired ? $"{setting.Title} *" : setting.Title;
        var label = CreateLabel(labelText);
        var inputControl = CreateInputControl(setting);

        _controls.Add(setting.Key, inputControl);

        if (!string.IsNullOrEmpty(setting.Description))
        {
            card.Controls.Add(CreateDescriptionLabel(setting.Description));
        }

        if (setting.Type == SettingType.Dropdown)
        {
            var comboBox = (ComboBox)inputControl;
            PopulateComboBox(setting, comboBox);

            if (setting.Options == null)
            {
                card.Controls.Add(CreateLoadButton(setting, comboBox));
            }
        }

        card.Controls.Add(inputControl);
        card.Controls.Add(label);

        return card;
    }

    private Control CreateInputControl(SourceSettings setting)
    {
        return setting.Type == SettingType.Dropdown
            ? CreateComboBox()
            : CreateTextBox(setting);
    }

    private static ComboBox CreateComboBox()
    {
        return new()
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new("Segoe UI", 10F),
        };
    }

    private void PopulateComboBox(SourceSettings setting, ComboBox comboBox)
    {
        var savedValue = _editSource?.Settings.GetValueOrDefault(setting.Key) ?? setting.DefaultValue;

        if (setting.Options == null)
        {
            if (string.IsNullOrEmpty(savedValue))
            {
                return;
            }

            comboBox.Items.Add(new ComboBoxItem { Value = savedValue, Label = savedValue });
            comboBox.SelectedIndex = 0;

            return;
        }

        foreach (var option in setting.Options)
        {
            comboBox.Items.Add(new ComboBoxItem { Value = option.Value, Label = option.Label });
        }

        if (string.IsNullOrEmpty(savedValue))
        {
            return;
        }

        var index = setting.Options.FindIndex(o => o.Value == savedValue);

        if (index >= 0)
        {
            comboBox.SelectedIndex = index;
        }
    }

    private TextBox CreateTextBox(SourceSettings setting)
    {
        return new()
        {
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new("Segoe UI", 10F),
            Text = _editSource?.Settings.GetValueOrDefault(setting.Key) ?? setting.DefaultValue ?? "",
        };
    }

    private Button CreateLoadButton(SourceSettings setting, ComboBox comboBox)
    {
        var loadButton = new Button
        {
            Text = "Загрузить",
            Dock = DockStyle.Top,
            Height = 28,
            Margin = new(0, 3, 0, 0),
        };

        loadButton.Click += async (_, _) => await LoadOptionsAsync(setting, comboBox, loadButton);

        _loadButtons.Add(loadButton);

        return loadButton;
    }

    private async Task LoadOptionsAsync(SourceSettings setting, ComboBox comboBox, Button loadButton)
    {
        if (_sourceType == null)
        {
            return;
        }

        loadButton.Enabled = false;
        loadButton.Text = "Загрузка...";

        try
        {
            var currentSettings = GetCurrentSettings();
            var options = await _sourceType.GetSettingOptionsAsync(setting.Key, currentSettings);

            comboBox.Items.Clear();
            foreach (var option in options)
            {
                comboBox.Items.Add(new ComboBoxItem { Value = option.Value, Label = option.Label });
            }

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки опций: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadButton.Enabled = true;
            loadButton.Text = "Загрузить";
        }
    }

    private Dictionary<string, string> GetCurrentSettings()
    {
        var settings = new Dictionary<string, string>();

        foreach (var (key, control) in _controls)
        {
            settings[key] = GetControlValue(control);
        }

        return settings;
    }

    private string[] FindDocFiles()
    {
        if (_sourceType == null)
        {
            return [];
        }

        var assemblyDir = Path.GetDirectoryName(_sourceType.GetType().Assembly.Location);

        if (assemblyDir == null)
        {
            return [];
        }

        var docsDir = Path.Combine(assemblyDir, "docs");

        return Directory.Exists(docsDir)
            ? Directory.GetFiles(docsDir, "*.md")
            : [];
    }
}

file sealed class ComboBoxItem
{
    public required string Value { get; set; }
    public required string Label { get; set; }

    public override string ToString()
    {
        return Label;
    }
}
