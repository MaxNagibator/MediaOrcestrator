using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Runner;

public partial class SourceSettingsForm : Form
{
    private readonly Dictionary<string, Control> _controls = [];
    private IEnumerable<SourceSettings> _settingsKeys = [];
    private Source? _editSource;
    private ISourceType? _sourceType;

    public SourceSettingsForm()
    {
        InitializeComponent();
    }

    public Dictionary<string, string>? Settings { get; private set; }

    public void SetSettings(IEnumerable<SourceSettings> settingsKeys, ISourceType? sourceType = null)
    {
        _settingsKeys = settingsKeys;
        _sourceType = sourceType;
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

    private static Label CreateLabel(string text)
    {
        return new()
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(64, 64, 64),
            Padding = new(0, 0, 0, 8),
        };
    }

    private static Label CreateDescriptionLabel(string text)
    {
        return new()
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new("Segoe UI", 8F),
            ForeColor = Color.FromArgb(128, 128, 128),
            Padding = new(0, 5, 0, 0),
        };
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
            Padding = new(0, 5, 0, 5),
        };

        table.ColumnStyles.Add(new(SizeType.Percent, 100F));
        return table;
    }

    private Panel CreateSettingCard(SourceSettings setting)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new(15, 10, 15, 10),
            Margin = new(10, 5, 10, 5),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var label = CreateLabel(setting.Title);
        var inputControl = CreateInputControl(setting);

        _controls.Add(setting.Key, inputControl);

        if (!string.IsNullOrEmpty(setting.Description))
        {
            card.Controls.Add(CreateDescriptionLabel(setting.Description));
        }

        if (setting.Type == SettingType.Dropdown)
        {
            card.Controls.Add(CreateLoadButton(setting, (ComboBox)inputControl));
        }

        card.Controls.Add(inputControl);
        card.Controls.Add(label);

        return card;
    }

    private Control CreateInputControl(SourceSettings setting)
    {
        return setting.Type == SettingType.Dropdown
            ? CreateComboBox(setting)
            : CreateTextBox(setting);
    }

    private ComboBox CreateComboBox(SourceSettings setting)
    {
        var comboBox = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new("Segoe UI", 10F),
        };

        var savedValue = _editSource?.Settings.GetValueOrDefault(setting.Key) ?? setting.DefaultValue;
        if (!string.IsNullOrEmpty(savedValue))
        {
            comboBox.Items.Add(new ComboBoxItem { Value = savedValue, Label = $"ID: {savedValue}" });
            comboBox.SelectedIndex = 0;
        }

        return comboBox;
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
            Height = 50,
            AutoSize = true,
            Margin = new(0, 5, 0, 0),
        };

        loadButton.Click += async (_, _) => await LoadOptionsAsync(setting, comboBox, loadButton);

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
}

internal class ComboBoxItem
{
    public required string Value { get; set; }
    public required string Label { get; set; }

    public override string ToString()
    {
        return Label;
    }
}
