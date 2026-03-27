using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Runner;

public partial class FilterToolStripControl : UserControl
{
    private const int SearchDebounceMs = 1000;
    private const string MetadataColumnsSettingKey = "metadata_columns";
    private readonly HashSet<SourceSyncRelation> _selectedRelations = [];
    private readonly HashSet<string> _availableMetadataFields = [];
    private readonly HashSet<string> _selectedMetadataFields = [];
    private SettingsManager? _settingsManager;

    public FilterToolStripControl()
    {
        InitializeComponent();

        uiMetadataDropDownButton.DropDown.Closing += (_, e) =>
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        };

        uiStatusFilterComboBox.Items.Clear();
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Все", Tag = null });

        var statuses = MediaStatusHelper.GetAll();
        foreach (var stat in statuses)
        {
            uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = stat.Text, Tag = stat.Id });
        }

        uiStatusFilterComboBox.SelectedIndex = 0;
    }

    public void SetSettingsManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        LoadSavedMetadataSelection();
    }

    public event EventHandler? FilterChanged;

    public bool ShowStatusFilter
    {
        get => uiStatusLabel.Visible;
        set
        {
            uiStatusLabel.Visible = value;
            uiStatusFilterComboBox.Visible = value;
        }
    }

    public void PopulateRelationsFilter(Orcestrator orcestrator)
    {
        uiRelationsDropDownButton.DropDownItems.Clear();
        _selectedRelations.Clear();
        var relations = orcestrator.GetRelations();

        foreach (var syncRelation in relations)
        {
            var item = new ToolStripMenuItem($"{syncRelation.From.TitleFull} -> {syncRelation.To.TitleFull}")
            {
                CheckOnClick = true,
                Tag = syncRelation,
            };

            item.CheckedChanged += (s, _) =>
            {
                if (s is ToolStripMenuItem { Tag: SourceSyncRelation relation } menuItem)
                {
                    if (menuItem.Checked)
                    {
                        _selectedRelations.Add(relation);
                    }
                    else
                    {
                        _selectedRelations.Remove(relation);
                    }
                }

                OnFilterChanged();
            };

            uiRelationsDropDownButton.DropDownItems.Add(item);
        }
    }

    public void UpdateMetadataFilter(List<MetadataColumnInfo> availableColumns)
    {
        var newSet = availableColumns.Select(c => c.ColumnId).ToHashSet();
        if (_availableMetadataFields.SetEquals(newSet))
        {
            return;
        }

        _availableMetadataFields.Clear();
        foreach (var id in newSet)
        {
            _availableMetadataFields.Add(id);
        }

        _selectedMetadataFields.IntersectWith(_availableMetadataFields);

        uiMetadataDropDownButton.DropDownItems.Clear();

        foreach (var col in availableColumns)
        {
            var isChecked = _selectedMetadataFields.Contains(col.ColumnId);

            var item = new ToolStripMenuItem(col.DisplayName)
            {
                CheckOnClick = true,
                Checked = isChecked,
                Tag = col.ColumnId,
            };

            item.CheckedChanged += (s, _) =>
            {
                if (s is ToolStripMenuItem { Tag: string columnId } menuItem)
                {
                    if (menuItem.Checked)
                    {
                        _selectedMetadataFields.Add(columnId);
                    }
                    else
                    {
                        _selectedMetadataFields.Remove(columnId);
                    }
                }

                SaveMetadataSelection();
                OnFilterChanged();
            };

            uiMetadataDropDownButton.DropDownItems.Add(item);
        }
    }

    public List<string> GetSelectedMetadataFields()
    {
        return _selectedMetadataFields.OrderBy(x => x).ToList();
    }

    public FilterState BuildFilterState(List<SourceSyncRelation>? selectedRelations = null)
    {
        var filterState = new FilterState
        {
            SearchText = uiSearchToolStripTextBox.Text,
        };

        if (uiStatusFilterComboBox.SelectedIndex > 0)
        {
            filterState.StatusFilter = (uiStatusFilterComboBox.SelectedItem as StatusFilterItem)?.Tag;
        }

        var activeRelations = selectedRelations ?? _selectedRelations.ToList();

        if (activeRelations.Count > 0)
        {
            filterState.SourceFilter = activeRelations
                .SelectMany(x => new[] { x.From.Id, x.To.Id })
                .Distinct()
                .ToHashSet();
        }

        return filterState;
    }

    private void uiSearchToolStripTextBox_TextChanged(object? sender, EventArgs e)
    {
        DebouncedSearch();
    }

    private void uiClearSearchButton_Click(object? sender, EventArgs e)
    {
        uiSearchToolStripTextBox.Text = string.Empty;
        OnFilterChanged();
    }

    private void uiStatusFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        OnFilterChanged();
    }

    private void DebouncedSearch()
    {
        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = new(_ =>
            {
                if (InvokeRequired)
                {
                    Invoke(OnFilterChanged);
                }
                else
                {
                    OnFilterChanged();
                }
            },
            null,
            SearchDebounceMs,
            Timeout.Infinite);
    }

    private void LoadSavedMetadataSelection()
    {
        var saved = _settingsManager?.GetStringValue(MetadataColumnsSettingKey);
        if (string.IsNullOrEmpty(saved))
        {
            return;
        }

        foreach (var columnId in saved.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            _selectedMetadataFields.Add(columnId);
        }
    }

    private void SaveMetadataSelection()
    {
        _settingsManager?.SetValue(MetadataColumnsSettingKey, string.Join(";", _selectedMetadataFields.OrderBy(x => x)));
    }

    private void OnFilterChanged()
    {
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed class StatusFilterItem
    {
        public required string Text { get; init; }
        public string? Tag { get; init; }

        public override string ToString()
        {
            return Text;
        }
    }

    public sealed class FilterState
    {
        public string SearchText { get; set; } = string.Empty;

        public string? StatusFilter { get; set; }

        public HashSet<string>? SourceFilter { get; set; }
    }
}
