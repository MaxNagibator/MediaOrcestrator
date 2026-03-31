using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using System.ComponentModel;

namespace MediaOrcestrator.Runner;

public class OptimizedMediaGridView : DataGridView
{
    public const int FirstSourceColumnIndex = 2;
    private const int TitleColumnIndex = 1;
    private const int CheckboxColumnIndex = 0;
    private const int SourceTitleMaxLength = 20;

    private Font? _statusFont;
    private Font? _headerFont;

    public OptimizedMediaGridView()
    {
        DoubleBuffered = true;

        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToResizeColumns = true;
        AllowUserToOrderColumns = true;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 35;
        ReadOnly = false;
        RowHeadersVisible = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        CellFormatting += OnCellFormatting;
        SortCompare += OnSortCompare;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<Source>? CurrentSources { get; private set; }

    public void SetupColumns(List<Source> sources, List<MetadataColumnInfo> selectedMetadataColumns)
    {
        var expectedColumnCount = FirstSourceColumnIndex + selectedMetadataColumns.Count + sources.Count;
        if (Columns.Count == expectedColumnCount)
        {
            var columnsMatch = true;
            for (var i = 0; i < selectedMetadataColumns.Count; i++)
            {
                if (Columns[i + FirstSourceColumnIndex].Name == "Meta_" + selectedMetadataColumns[i].ColumnId)
                {
                    continue;
                }

                columnsMatch = false;
                break;
            }

            if (columnsMatch)
            {
                for (var i = 0; i < sources.Count; i++)
                {
                    if (Columns[i + FirstSourceColumnIndex + selectedMetadataColumns.Count].Name == sources[i].Id)
                    {
                        continue;
                    }

                    columnsMatch = false;
                    break;
                }
            }

            if (columnsMatch)
            {
                return;
            }
        }

        SuspendLayout();

        CurrentCell = null;
        Rows.Clear();
        Columns.Clear();

        _headerFont ??= new(Font, FontStyle.Bold);
        _statusFont ??= new(Font.FontFamily, 8.25f, FontStyle.Bold);

        var checkColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Check",
            HeaderText = string.Empty,
            Width = 30,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
        };

        Columns.Add(checkColumn);

        Columns.Add("Title", "Название");
        //Columns[TitleColumnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        Columns[TitleColumnIndex].Width = Width - 80 * sources.Count - 100 * selectedMetadataColumns.Count;

        Columns[TitleColumnIndex].ReadOnly = true;
        Columns[TitleColumnIndex].HeaderCell.Style.Font = _headerFont;

        foreach (var col in selectedMetadataColumns)
        {
            var displayType = col.DisplayType;
            var colIndex = Columns.Add("Meta_" + col.ColumnId, col.DisplayName);
            Columns[colIndex].Width = 100;
            Columns[colIndex].ReadOnly = true;
            Columns[colIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            Columns[colIndex].HeaderCell.Style.Font = _headerFont;

            if (string.IsNullOrEmpty(displayType))
            {
                continue;
            }

            if (displayType == "ByteSize")
            {
                Columns[colIndex].ValueType = typeof(long);
                Columns[colIndex].Tag = "ByteSize";
                continue;
            }

            var targetType = Type.GetType(displayType);
            if (targetType == null)
            {
                continue;
            }

            Columns[colIndex].ValueType = targetType;
            if (targetType == typeof(DateTime))
            {
                Columns[colIndex].DefaultCellStyle.Format = "g";
            }
            else if (targetType == typeof(int) || targetType == typeof(long) || targetType == typeof(double))
            {
                Columns[colIndex].DefaultCellStyle.Format = "N0";
            }
        }

        foreach (var source in sources)
        {
            var displayTitle = source.Title.Length > SourceTitleMaxLength
                ? source.Title[..SourceTitleMaxLength]
                : source.Title;

            var colIndex = Columns.Add(source.Id, displayTitle);
            Columns[colIndex].Width = 80;
            Columns[colIndex].ReadOnly = true;
            Columns[colIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Columns[colIndex].DefaultCellStyle.Font = _statusFont;
            Columns[colIndex].HeaderCell.Style.Font = _headerFont;
            Columns[colIndex].HeaderCell.ToolTipText = source.TitleFull;
        }

        ResumeLayout();
    }

    public void PopulateGrid(List<Source> sources, List<Media> mediaData, List<MetadataColumnInfo> selectedMetadataColumns)
    {
        CurrentSources = sources;

        SuspendLayout();
        CurrentCell = null;
        Rows.Clear();

        if (mediaData.Count > 0)
        {
            Rows.Add(mediaData.Count);

            for (var r = 0; r < mediaData.Count; r++)
            {
                var media = mediaData[r];
                var row = Rows[r];
                row.Tag = media;

                row.Cells[CheckboxColumnIndex].Value = false;
                row.Cells[TitleColumnIndex].Value = media.Title;
                row.Cells[TitleColumnIndex].ToolTipText = media.Title;

                for (var i = 0; i < selectedMetadataColumns.Count; i++)
                {
                    var col = selectedMetadataColumns[i];
                    var metaItem = col.SourceId != null
                        ? media.Metadata.FirstOrDefault(m => m.Key == col.Key && m.SourceId == col.SourceId)
                        : media.Metadata.FirstOrDefault(m => m.Key == col.Key);

                    object? cellValue = metaItem?.Value;

                    if (metaItem != null && !string.IsNullOrEmpty(metaItem.DisplayType) && !string.IsNullOrEmpty(metaItem.Value))
                    {
                        if (metaItem.DisplayType == "ByteSize")
                        {
                            cellValue = long.TryParse(metaItem.Value, out var bytes) ? bytes : null;
                        }
                        else
                        {
                            var targetType = Type.GetType(metaItem.DisplayType);
                            if (targetType != null)
                            {
                                try
                                {
                                    // TODO: Шляпно. Возможно стоит сделать явную типизацию
                                    cellValue = TypeDescriptor.GetConverter(targetType).ConvertFromInvariantString(metaItem.Value);
                                }
                                catch
                                {
                                    cellValue = null;
                                }
                            }
                        }
                    }

                    row.Cells[i + FirstSourceColumnIndex].Value = cellValue;
                }

                if (false)
                {
                    // экспресс починка дублей
                    var sourceId = new List<string>();
                    var delete = new List<string>();
                    foreach (var source in media.Sources)
                    {
                        if (sourceId.Contains(source.SourceId))
                        {
                            //   media.Sources = media.Sources.Take(3).ToList();
                            delete.Add(source.ExternalId);
                            continue;
                        }

                        sourceId.Add(source.SourceId);
                    }
                    //media.Sources = media.Sources.Where(x => !delete.Contains(x.ExternalId)).ToList();
                }

                var platformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status);
                var platformSortNumbers = media.Sources.ToDictionary(x => x.SourceId, x => x.SortNumber);

                var sourceStartIdx = FirstSourceColumnIndex + selectedMetadataColumns.Count;

                for (var i = 0; i < sources.Count; i++)
                {
                    var status = platformStatuses.GetValueOrDefault(sources[i].Id, MediaStatus.None);
                    var sort = platformSortNumbers.GetValueOrDefault(sources[i].Id, -1);
                    var cell = row.Cells[i + sourceStartIdx];
                    cell.Value = sort;
                    cell.Tag = status;
                    cell.ToolTipText =
                        $"""
                         Источник: {sources[i].Title}
                         Статус: {status}
                         """;
                }
            }
        }

        ResumeLayout();
    }

    public void SelectAllRows()
    {
        SuspendLayout();
        foreach (DataGridViewRow row in Rows)
        {
            row.Cells[CheckboxColumnIndex].Value = true;
        }

        ResumeLayout();
    }

    public void DeselectAllRows()
    {
        SuspendLayout();
        foreach (DataGridViewRow row in Rows)
        {
            row.Cells[CheckboxColumnIndex].Value = false;
        }

        ResumeLayout();
    }

    public List<Media> GetCheckedMedia()
    {
        var result = new List<Media>();
        foreach (DataGridViewRow row in Rows)
        {
            if (row.Cells[CheckboxColumnIndex].Value is true && row.Tag is Media media)
            {
                result.Add(media);
            }
        }

        return result;
    }

    public List<Media> GetSelectedMedia()
    {
        var result = new List<Media>();
        foreach (var row in SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index))
        {
            if (row.Tag is Media media)
            {
                result.Add(media);
            }
        }

        return result;
    }

    public Media? GetMediaAtRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            return null;
        }

        return Rows[rowIndex].Tag as Media;
    }

    protected override void OnCellClick(DataGridViewCellEventArgs e)
    {
        base.OnCellClick(e);

        if (e is not { RowIndex: >= 0, ColumnIndex: CheckboxColumnIndex })
        {
            return;
        }

        var cell = Rows[e.RowIndex].Cells[CheckboxColumnIndex];
        cell.Value = !(bool)(cell.Value ?? false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusFont?.Dispose();
            _statusFont = null;
            _headerFont?.Dispose();
            _headerFont = null;
        }

        base.Dispose(disposing);
    }

    private static void OnSortCompare(object? sender, DataGridViewSortCompareEventArgs e)
    {
        if (e.Column.Index != TitleColumnIndex)
        {
            return;
        }

        e.SortResult = NaturalStringComparer.Instance.Compare(e.CellValue1?.ToString(), e.CellValue2?.ToString());
        e.Handled = true;
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < FirstSourceColumnIndex || e.RowIndex < 0)
        {
            return;
        }

        if (Columns[e.ColumnIndex].Tag is "ByteSize" && e.Value is long byteValue)
        {
            e.Value = FormatFileSize(byteValue);
            e.FormattingApplied = true;
            return;
        }

        if (Rows[e.RowIndex].Cells[e.ColumnIndex].Tag is not string status)
        {
            return;
        }

        if (e.CellStyle != null)
        {
            var cc = MediaStatusHelper.GetById(status);
            e.Value = cc.IconText;
            e.CellStyle.ForeColor = cc.IconColor;
        }
    }

    internal static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} Б";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} КБ";
        }

        if (bytes < 1024L * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024):F1} МБ";
        }

        return $"{bytes / (1024.0 * 1024 * 1024):F2} ГБ";
    }
}
