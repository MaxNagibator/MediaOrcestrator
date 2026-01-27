using MediaOrcestrator.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaOrcestrator.Runner;
public partial class SourceSettingsForm : Form
{
    private IEnumerable<SourceSettings> _settingsKeys;
    private readonly Dictionary<string, TextBox> _textBoxes = new();

    public SourceSettingsForm()
    {
        InitializeComponent();
    }

    internal void SetSettings(IEnumerable<SourceSettings> settingsKeys)
    {
        _settingsKeys = settingsKeys;
    }

    public Dictionary<string, string>? Settings { get; private set; }

    // TODO: Чисто черновой набросок
    private void Bla()
    {
        //SuspendLayout();
        // 
        // SourceSettingsForm
        // 
        ClientSize = new Size(284, 261);
        Name = "SourceSettingsForm";
        //ResumeLayout(false);
        Text = "Настройки источника";
        Size = new(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        if(_settingsKeys == null)
        {
            return;
        }
        foreach (var setting in _settingsKeys)
        {
            var label = new Label { Text = setting.Title, AutoSize = true };
            var textBox = new TextBox { Width = 350 };
            _textBoxes.Add(setting.Key, textBox);

            panel1.Controls.Add(label);
            panel1.Controls.Add(textBox);
        }
    }

    private void SourceSettingsForm_Load(object sender, EventArgs e)
    {
        Bla();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        Settings = new();
        if (string.IsNullOrEmpty(uiNameTextBox.Text))
        {
            MessageBox.Show("имя обязательно");
            return;
        }

        Settings.Add("_system_name", uiNameTextBox.Text);
        foreach (var (key, value) in _textBoxes)
        {
            Settings.Add(key, value.Text);
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
