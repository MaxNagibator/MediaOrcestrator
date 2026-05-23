using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class CoverProfilePicker : UserControl
{
    private const string NoProfileLabel = "— выбрать профиль —";

    private CoverTemplateStore? _store;
    private CoverGenerator? _generator;
    private bool _suppressEvents;

    public CoverProfilePicker()
    {
        InitializeComponent();
    }

    public event EventHandler? TemplateChanged;

    public CoverTemplate? Template { get; private set; }

    public string? ProfileName { get; private set; }

    public void Initialize(CoverTemplateStore store, CoverGenerator generator)
    {
        _store = store;
        _generator = generator;
        ReloadProfiles();
        UpdateEnabled();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        UpdateEnabled();
    }

    public void SetState(CoverTemplate? template, string? profileName)
    {
        Template = template;
        ProfileName = profileName;
        SyncComboSelection();
    }

    public void ReloadProfiles()
    {
        if (_store == null)
        {
            return;
        }

        _suppressEvents = true;

        try
        {
            uiCombo.BeginUpdate();
            uiCombo.Items.Clear();
            uiCombo.Items.Add(NoProfileLabel);

            foreach (var name in _store.List())
            {
                uiCombo.Items.Add(name);
            }

            SyncComboSelection();
            uiCombo.EndUpdate();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void uiCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || _store == null)
        {
            return;
        }

        var idx = uiCombo.SelectedIndex;

        if (idx <= 0)
        {
            if (Template == null && ProfileName == null)
            {
                return;
            }

            Template = null;
            ProfileName = null;
            TemplateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var name = uiCombo.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var loaded = _store.Load(name);

        if (loaded == null)
        {
            MessageBox.Show(FindForm(), $"Не удалось загрузить профиль «{name}»", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Template = loaded;
        ProfileName = name;
        TemplateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void uiSetupButton_Click(object? sender, EventArgs e)
    {
        if (_generator == null || _store == null)
        {
            return;
        }

        string? finalProfileName;

        using (var form = new CoverTemplateForm(_generator, _store, Template, ProfileName))
        {
            form.ShowDialog(FindForm());
            finalProfileName = form.CurrentProfileName;
        }

        ReloadProfiles();

        if (string.IsNullOrEmpty(finalProfileName))
        {
            return;
        }

        var loaded = _store.Load(finalProfileName);

        if (loaded == null)
        {
            return;
        }

        Template = loaded;
        ProfileName = finalProfileName;
        SyncComboSelection();
        TemplateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncComboSelection()
    {
        if (!string.IsNullOrEmpty(ProfileName))
        {
            var idx = uiCombo.Items.IndexOf(ProfileName);
            uiCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }
        else
        {
            uiCombo.SelectedIndex = uiCombo.Items.Count > 0 ? 0 : -1;
        }
    }

    private void UpdateEnabled()
    {
        uiCombo.Enabled = Enabled && _store != null;
        uiSetupButton.Enabled = Enabled && _store != null && _generator != null;
    }
}
