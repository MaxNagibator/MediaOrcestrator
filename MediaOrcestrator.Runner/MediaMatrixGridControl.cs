using MediaOrcestrator.Domain;
using System.Reflection;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl
{
    private Orcestrator? _orcestrator;
    private Font? _statusFont;

    public MediaMatrixGridControl()
    {
        InitializeComponent();
        SetDoubleBuffered(uMediaGrid);
    }

    public void Initialize(Orcestrator orcestrator)
    {
        _orcestrator = orcestrator;
    }

    public void RefreshData(List<SourceSyncRelation>? selectedRelations = null)
    {
        if (_orcestrator == null)
        {
            return;
        }

        var mediaData = _orcestrator.GetMedias().ToList();
        if (!string.IsNullOrEmpty(textBox1.Text))
        {
            mediaData = mediaData.Where(x => x.Title.ToLower().Contains(textBox1.Text.ToLower())).ToList();
        }

        var allSources = _orcestrator.GetSources();
        //mediaData = mediaData.Take(20).ToList();

        List<Source> sources;

        if (selectedRelations is { Count: > 0 })
        {
            var selectedSourceIds = selectedRelations
                .SelectMany(x => new[] { x.From.Id, x.To.Id })
                .Distinct()
                .ToHashSet();

            sources = allSources
                .Where(x => selectedSourceIds.Contains(x.Id))
                .ToList();

            // TODO: При таком варианте не учитывается направление связи, а только наличие источника в связи. Альтернатива использовать только From для media
            mediaData = mediaData
                .Where(m => m.Sources.Any(l => selectedSourceIds.Contains(l.SourceId)))
                .ToList();
        }
        else
        {
            sources = allSources;
        }

        SetupColumns(sources);
        PopulateGrid(sources, mediaData);
    }

    private void uMediaGrid_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var ht = uMediaGrid.HitTest(e.X, e.Y);
        if (ht.Type != DataGridViewHitTestType.Cell || ht.RowIndex < 0)
        {
            return;
        }

        var row = uMediaGrid.Rows[ht.RowIndex];
        if (!row.Selected)
        {
            uMediaGrid.ClearSelection();
            row.Selected = true;
        }

        if (row.Tag is Media media)
        {
            ShowContextMenu(media, uMediaGrid.PointToScreen(e.Location));
        }
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
        RefreshData();
    }

    private void uiMergerSelectedMediaButton_Click(object sender, EventArgs e)
    {
        var selectedMediaList = new List<Media>();
        foreach (DataGridViewRow row in uMediaGrid.Rows)
        {
            if (row.Cells[0].Value is not true)
            {
                continue;
            }

            if (row.Tag is Media media)
            {
                selectedMediaList.Add(media);
            }
        }

        if (selectedMediaList.Count < 2)
        {
            return;
        }

        var currentMediaSources = new List<MediaSourceLink>();
        foreach (var selectedMedia in selectedMediaList)
        {
            foreach (var selectedMediaSourceLink in selectedMedia.Sources)
            {
                var current = currentMediaSources.FirstOrDefault(x => x.SourceId == selectedMediaSourceLink.SourceId);
                if (current != null)
                {
                    var sources = _orcestrator.GetSources();
                    var fullSource = sources.First(x => x.Id == selectedMediaSourceLink.SourceId);
                    MessageBox.Show("Данное хранилище уже есть у медиа " + fullSource.TitleFull);
                    return;
                }

                // todo вот бы заполнять ссылку на него сразу и не приседать
                //var fullSource = sources.First(x => x.Id == selectedMediaSourceLink.SourceId);
                currentMediaSources.Add(selectedMediaSourceLink);
            }
        }

        selectedMediaList.First().Sources = currentMediaSources;
        _orcestrator.UpdateMedia(selectedMediaList.First());

        foreach (var media in selectedMediaList.Skip(1))
        {
            _orcestrator.RemoveMedia(media);
        }
    }

    private static void SetDoubleBuffered(Control control)
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            null, control, [true]);
    }

    private void SetupColumns(List<Source> sources)
    {
        uMediaGrid.Columns.Clear();

        var checkColumn = new DataGridViewCheckBoxColumn
        {
            HeaderText = "",
            Width = 30,
            ReadOnly = false,
        };

        uMediaGrid.Columns.Add(checkColumn);

        uMediaGrid.Columns.Add("Title", "Название");
        uMediaGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        uMediaGrid.Columns[1].ReadOnly = true;
        uMediaGrid.Columns[1].HeaderCell.Style.Font = new(Font, FontStyle.Bold);

        foreach (var source in sources)
        {
            var colIndex = uMediaGrid.Columns.Add(source.Id, source.Title.Length > 5 ? source.Title[..5] : source.Title);
            uMediaGrid.Columns[colIndex].Width = 80;
            uMediaGrid.Columns[colIndex].ReadOnly = true;
            uMediaGrid.Columns[colIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            uMediaGrid.Columns[colIndex].HeaderCell.Style.Font = new(Font, FontStyle.Bold);
            uMediaGrid.Columns[colIndex].HeaderCell.ToolTipText = source.Title;
        }
    }

    private void PopulateGrid(List<Source> sources, List<Media> mediaData)
    {
        uMediaGrid.SuspendLayout();
        uMediaGrid.Rows.Clear();

        if (mediaData.Count > 0)
        {
            uMediaGrid.Rows.Add(mediaData.Count);
            _statusFont ??= new(Font.FontFamily, 12, FontStyle.Bold);

            for (var r = 0; r < mediaData.Count; r++)
            {
                var media = mediaData[r];
                var row = uMediaGrid.Rows[r];
                row.Tag = media;

                row.Cells[0].Value = false;
                row.Cells[1].Value = media.Title;
                row.Cells[1].ToolTipText = media.Title;

                var platformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status);

                for (var i = 0; i < sources.Count; i++)
                {
                    var source = sources[i];
                    var status = platformStatuses.GetValueOrDefault(source.Id, "None");
                    var cell = row.Cells[i + 2];
                    cell.Value = GetStatusSymbol(status);
                    cell.Style.ForeColor = GetStatusColor(status);
                    cell.Style.Font = _statusFont;
                    cell.ToolTipText = $"Источник: {source.Title}\nСтатус: {status}";
                }
            }
        }

        uMediaGrid.ResumeLayout();
    }

    private string GetStatusSymbol(string? status)
    {
        return status switch
        {
            "OK" => "✔",
            "Error" => "✘",
            "None" => "○",
            null => "○",
            _ => "●",
        };
    }

    private Color GetStatusColor(string? status)
    {
        return status switch
        {
            "OK" => Color.Green,
            "Error" => Color.Red,
            "None" => Color.Gray,
            null => Color.Gray,
            _ => Color.Blue,
        };
    }

    private void ShowContextMenu(Media media, Point location)
    {
        var contextMenu = new ContextMenuStrip();

        foreach (var rel in _orcestrator.GetRelations())
        {
            var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
            var toSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
            if (fromSource != null && toSource == null)
            {
                contextMenu.Items.Add("Синк " + rel.From.TitleFull + " -> " + rel.To.TitleFull, null, (s, e) =>
                {
                    Task.Run(async () =>
                    {
                        await _orcestrator.TransferByRelation(media, rel, fromSource.ExternalId);
                    });
                });
            }
            else
            {
                var element = contextMenu.Items.Add("Синк " + rel.From.TitleFull + " -> " + rel.To.TitleFull, null, (s, e) =>
                {
                });

                element.Enabled = false;
            }
        }

        if (contextMenu.Items.Count > 0)
        {
            contextMenu.Show(location);
        }
    }
}
