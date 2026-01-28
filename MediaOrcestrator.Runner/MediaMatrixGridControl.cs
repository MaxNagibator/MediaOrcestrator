using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class MediaMatrixGridControl : UserControl
{
    private Orcestrator? _orcestrator;

    public MediaMatrixGridControl()
    {
        InitializeComponent();
    }

    public void Initialize(Orcestrator orcestrator)
    {
        _orcestrator = orcestrator;
    }

    public void RefreshData()
    {
        if (_orcestrator == null)
        {
            return;
        }

        var sources = _orcestrator.GetSources();

        var toolTip = new ToolTip();
        uMediaHeaderPanel.Controls.Clear();
        uMediaHeaderPanel.ColumnCount = sources.Count + 1;
        uMediaHeaderPanel.ColumnStyles.Clear();

        uMediaHeaderPanel.ColumnStyles.Add(new(SizeType.Percent, 100F));
        uMediaHeaderPanel.Controls.Add(new Label
        {
            Text = "Название",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new(Font, FontStyle.Bold),
        }, 0, 0);

        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];

            var title = source.Title;
            var displayName = title.Length > 5 ? title.Substring(0, 5) : title;

            var label = new Label
            {
                Text = displayName,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new(Font, FontStyle.Bold),
            };

            toolTip.SetToolTip(label, title);

            uMediaHeaderPanel.ColumnStyles.Add(new(SizeType.Absolute, 80F));
            uMediaHeaderPanel.Controls.Add(label, i + 1, 0);
        }

        uMediaGridPanel.Controls.Clear();
        uMediaGridPanel.RowCount = 0;
        var mediaData = _orcestrator.GetMedias();

        foreach (var media in mediaData)
        {
            var dto = new MediaGridRowDto
            {
                Id = media.Id,
                Title = media.Title,
                PlatformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status),
            };

            var control = new MediaItemControl();
            control.SetData(dto, sources);
            control.Dock = DockStyle.Top;
            uMediaGridPanel.RowCount++;
            uMediaGridPanel.Controls.Add(control);
        }
    }
}
