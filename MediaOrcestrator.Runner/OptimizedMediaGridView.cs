using MediaOrcestrator.Domain;
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
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 35;
        ReadOnly = false;
        RowHeadersVisible = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        CellFormatting += OnCellFormatting;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<Source>? CurrentSources { get; private set; }

    public void SetupColumns(List<Source> sources)
    {
        if (Columns.Count == sources.Count + FirstSourceColumnIndex)
        {
            var columnsMatch = true;
            for (var i = 0; i < sources.Count; i++)
            {
                if (Columns[i + FirstSourceColumnIndex].Name == sources[i].Id)
                {
                    continue;
                }

                columnsMatch = false;
                break;
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
        Columns[TitleColumnIndex].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        Columns[TitleColumnIndex].ReadOnly = true;
        Columns[TitleColumnIndex].HeaderCell.Style.Font = _headerFont;

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

    public void PopulateGrid(List<Source> sources, List<Media> mediaData)
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

                var platformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status);

                for (var i = 0; i < sources.Count; i++)
                {
                    var status = platformStatuses.GetValueOrDefault(sources[i].Id, MediaSourceLink.StatusNone);
                    var cell = row.Cells[i + FirstSourceColumnIndex];
                    cell.Value = GetStatusSymbol(status);
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

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < FirstSourceColumnIndex || e.RowIndex < 0)
        {
            return;
        }

        if (Rows[e.RowIndex].Cells[e.ColumnIndex].Tag is not string status)
        {
            return;
        }

        if (e.CellStyle != null)
        {
            e.CellStyle.ForeColor = GetStatusColor(status);
        }
    }

    private static string GetStatusSymbol(string? status)
    {
        return status switch
        {
            MediaSourceLink.StatusOk => "✔",
            MediaSourceLink.StatusError => "✘",
            MediaSourceLink.StatusNone => "○",
            null => "○",
            _ => "●",
        };
    }

    private static Color GetStatusColor(string? status)
    {
        return status switch
        {
            MediaSourceLink.StatusOk => Color.Green,
            MediaSourceLink.StatusError => Color.Red,
            MediaSourceLink.StatusNone => Color.Gray,
            null => Color.Gray,
            _ => Color.Blue,
        };
    }
}
