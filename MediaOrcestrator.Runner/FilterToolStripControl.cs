using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Runner;

public partial class FilterToolStripControl : UserControl
{
    private const int SearchDebounceMs = 1000;
    private readonly HashSet<SourceSyncRelation> _selectedRelations = [];
    private readonly HashSet<string> _availableMetadataFields = [];
    private readonly HashSet<string> _selectedMetadataFields = [];
    private bool _metadataInitialized;

    public FilterToolStripControl()
    {
        InitializeComponent();

        uiStatusFilterComboBox.Items.Clear();
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Все", Tag = null });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "OK", Tag = MediaSourceLink.StatusOk });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Ошибка", Tag = MediaSourceLink.StatusError });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Пропал", Tag = MediaSourceLink.StatusMissing });
        uiStatusFilterComboBox.Items.Add(new StatusFilterItem { Text = "Нет", Tag = MediaSourceLink.StatusNone });
        uiStatusFilterComboBox.SelectedIndex = 0;
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

    public void UpdateMetadataFilter(List<MetadataItem> availableMetadata)
    {
        var newSet = availableMetadata.Select(m => m.Key).ToHashSet();
        if (_availableMetadataFields.SetEquals(newSet))
        {
            return;
        }

        _availableMetadataFields.Clear();
        foreach (var k in newSet)
        {
            _availableMetadataFields.Add(k);
        }

        // TODO: Автоматический выбор всех по умолчанию. Возможно стоит убрать
        if (!_metadataInitialized)
        {
            foreach (var k in newSet)
            {
                _selectedMetadataFields.Add(k);
            }

            _metadataInitialized = true;
        }

        uiMetadataDropDownButton.DropDownItems.Clear();

        foreach (var meta in availableMetadata)
        {
            var key = meta.Key;
            var isChecked = _selectedMetadataFields.Contains(key);

            var item = new ToolStripMenuItem(meta.DisplayName ?? key)
            {
                CheckOnClick = true,
                Checked = isChecked,
                Tag = key,
            };

            item.CheckedChanged += (s, _) =>
            {
                if (s is ToolStripMenuItem { Tag: string mk } menuItem)
                {
                    if (menuItem.Checked)
                    {
                        _selectedMetadataFields.Add(mk);
                    }
                    else
                    {
                        _selectedMetadataFields.Remove(mk);
                    }
                }

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
