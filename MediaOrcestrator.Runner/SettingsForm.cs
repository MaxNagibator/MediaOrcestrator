namespace MediaOrcestrator.Runner;

public sealed partial class SettingsForm : Form
{
    private const string PluginPathKey = "plugin_path";
    private const string DatabasePathKey = "database_path";
    private const string TempPathKey = "temp_path";
    private const string StatePathKey = "state_path";

    private readonly Dictionary<string, string?> _originalValues = new();
    private SettingsManager? _settingsManager;

    public SettingsForm()
    {
        InitializeComponent();
    }

    public bool RestartRequested { get; private set; }

    public void SetSettingsManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        _originalValues[PluginPathKey] = settingsManager.GetStringValue(PluginPathKey);
        _originalValues[DatabasePathKey] = settingsManager.GetStringValue(DatabasePathKey);
        _originalValues[TempPathKey] = settingsManager.GetStringValue(TempPathKey);
        _originalValues[StatePathKey] = settingsManager.GetStringValue(StatePathKey);
        _originalValues[SettingsManager.UploadIntervalMinutesKey] = settingsManager.GetStringValue(SettingsManager.UploadIntervalMinutesKey);

        uiPluginPathTextBox.Text = _originalValues[PluginPathKey] ?? "";
        uiDatabasePathTextBox.Text = _originalValues[DatabasePathKey] ?? "";
        uiTempPathTextBox.Text = _originalValues[TempPathKey] ?? "";
        uiStatePathTextBox.Text = _originalValues[StatePathKey] ?? "";
        uiGridUploadIntervalNumeric.Value = settingsManager.GetUploadIntervalMinutes();
    }

    private void uiPluginPathBrowseButton_Click(object sender, EventArgs e)
    {
        BrowseFolder(uiPluginPathTextBox);
    }

    private void uiDatabasePathBrowseButton_Click(object sender, EventArgs e)
    {
        BrowseFile(uiDatabasePathTextBox);
    }

    private void uiTempPathBrowseButton_Click(object sender, EventArgs e)
    {
        BrowseFolder(uiTempPathTextBox);
    }

    private void uiStatePathBrowseButton_Click(object sender, EventArgs e)
    {
        BrowseFolder(uiStatePathTextBox);
    }

    private void uiSaveButton_Click(object sender, EventArgs e)
    {
        if (!TryPersist())
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void uiSaveAndRestartButton_Click(object sender, EventArgs e)
    {
        if (!TryPersist())
        {
            return;
        }

        RestartRequested = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void uiCancelButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private static void BrowseFolder(TextBox textBox)
    {
        using var dialog = new FolderBrowserDialog();

        if (!string.IsNullOrWhiteSpace(textBox.Text) && Directory.Exists(textBox.Text))
        {
            dialog.SelectedPath = textBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            textBox.Text = dialog.SelectedPath;
        }
    }

    private static void BrowseFile(TextBox textBox)
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = false,
        };

        if (!string.IsNullOrWhiteSpace(textBox.Text))
        {
            var dir = Path.GetDirectoryName(textBox.Text);

            if (dir != null && Directory.Exists(dir))
            {
                dialog.InitialDirectory = dir;
            }

            dialog.FileName = Path.GetFileName(textBox.Text);
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            textBox.Text = dialog.FileName;
        }
    }

    private bool TryPersist()
    {
        if (_settingsManager == null)
        {
            return false;
        }

        var plugin = uiPluginPathTextBox.Text.Trim();
        var database = uiDatabasePathTextBox.Text.Trim();
        var temp = uiTempPathTextBox.Text.Trim();
        var state = uiStatePathTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(plugin) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(temp) || string.IsNullOrWhiteSpace(state))
        {
            MessageBox.Show("Все пути обязательны для заполнения.",
                "Настройки",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }

        PersistIfChanged(PluginPathKey, plugin);
        PersistIfChanged(DatabasePathKey, database);
        PersistIfChanged(TempPathKey, temp);
        PersistIfChanged(StatePathKey, state);
        PersistIfChanged(SettingsManager.UploadIntervalMinutesKey, decimal.ToInt32(uiGridUploadIntervalNumeric.Value).ToString());
        return true;
    }

    private void PersistIfChanged(string key, string value)
    {
        if (_originalValues[key] == value)
        {
            return;
        }

        _settingsManager!.SetValue(key, value);
        _originalValues[key] = value;
    }
}
