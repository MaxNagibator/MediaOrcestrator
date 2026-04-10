using System.Text;

namespace MediaOrcestrator.Runner;

public sealed class SettingsManager
{
    private readonly string _settingsPath;
    private readonly Dictionary<string, string> _settings;

    public SettingsManager(string settingsPath)
    {
        _settingsPath = settingsPath;
        _settings = new(StringComparer.OrdinalIgnoreCase);

        LoadSettings();
    }

    public string SettingsDirectory => Path.GetDirectoryName(Path.GetFullPath(_settingsPath)) ?? AppContext.BaseDirectory;

    public string? GetStringValue(string key)
    {
        return _settings.GetValueOrDefault(key);
    }

    public void SetValue(string key, string value)
    {
        _settings[key] = value;
        SaveSettings();
    }

    private void LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return;
        }

        var settingsLines = File.ReadAllLines(_settingsPath);
        foreach (var line in settingsLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                _settings[parts[0]] = parts[1];
            }
        }
    }

    private void SaveSettings()
    {
        var saveFileOutPut = new StringBuilder();
        foreach (var kv in _settings)
        {
            saveFileOutPut.AppendLine($"{kv.Key} {kv.Value}");
        }

        File.WriteAllText(_settingsPath, saveFileOutPut.ToString());
    }
}
