using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Runner;

public class SourceSettingsForm : Form
{
    private readonly IEnumerable<SourceSettings> _settingsKeys;
    private readonly Dictionary<string, TextBox> _textBoxes = new();

    public SourceSettingsForm(IEnumerable<SourceSettings> settingsKeys)
    {
        _settingsKeys = settingsKeys;
        InitializeComponent();
    }

    public Dictionary<string, string>? Settings { get; private set; }

    // TODO: Чисто черновой набросок
    private void InitializeComponent()
    {
        Text = "Настройки источника";
        Size = new(400, 200);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            Padding = new(10),
        };

        foreach (var setting in _settingsKeys)
        {
            var label = new Label { Text = setting.Title, AutoSize = true };
            var textBox = new TextBox { Width = 350 };
            _textBoxes.Add(setting.Key, textBox);

            panel.Controls.Add(label);
            panel.Controls.Add(textBox);
        }

        var btnOk = new Button { Text = "ОК", DialogResult = DialogResult.OK };
        btnOk.Click += (_, _) =>
        {
            Settings = new();
            foreach (var (key, value) in _textBoxes)
            {
                Settings.Add(key, value.Text);
            }

            Close();
        };

        panel.Controls.Add(btnOk);
        Controls.Add(panel);
    }
}
