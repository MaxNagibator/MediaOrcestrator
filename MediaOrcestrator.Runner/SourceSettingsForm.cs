using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Runner;

public partial class SourceSettingsForm : Form
{
    private readonly Dictionary<string, TextBox> _textBoxes = new();
    private IEnumerable<SourceSettings> _settingsKeys = [];
    private Source? _editSource;

    public SourceSettingsForm()
    {
        InitializeComponent();
    }

    public Dictionary<string, string>? Settings { get; private set; }

    public void SetSettings(IEnumerable<SourceSettings> settingsKeys)
    {
        _settingsKeys = settingsKeys;
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
        _textBoxes.Clear();

        var settingsTable = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0,
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new(0, 5, 0, 5),
        };

        settingsTable.ColumnStyles.Add(new(SizeType.Percent, 100F));
        uiSettingsPanel.Controls.Add(settingsTable);

        if (_settingsKeys == null)
        {
            return;
        }

        foreach (var setting in _settingsKeys)
        {
            var card = new Panel
            {
                Height = 85,
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new(15, 10, 15, 10),
                Margin = new(10, 5, 10, 5),
                BorderStyle = BorderStyle.FixedSingle,
            };

            var label = new Label
            {
                Text = setting.Title,
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new("Segoe UI Semibold", 9F),
                ForeColor = Color.FromArgb(64, 64, 64),
                Padding = new(0, 0, 0, 8),
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new("Segoe UI", 10F),
                Text = _editSource?.Settings.GetValueOrDefault(setting.Key) ?? "",
            };

            _textBoxes.Add(setting.Key, textBox);
            card.Controls.Add(textBox);
            card.Controls.Add(label);

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

        if (_editSource != null)
        {
            Settings = _editSource.Settings;
            Settings["_system_name"] = uiNameTextBox.Text;
            foreach (var (key, value) in _textBoxes)
            {
                Settings[key] = value.Text;
            }
        }
        else
        {
            Settings = new();
            Settings.Add("_system_name", uiNameTextBox.Text);
            foreach (var (key, value) in _textBoxes)
            {
                Settings.Add(key, value.Text);
            }
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
