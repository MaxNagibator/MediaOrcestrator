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
        var platformIds = sources.Keys.ToList();

        var toolTip = new ToolTip();
        uiMediaHeaderPanel.Controls.Clear();
        uiMediaHeaderPanel.ColumnCount = platformIds.Count + 1;
        uiMediaHeaderPanel.ColumnStyles.Clear();

        uiMediaHeaderPanel.ColumnStyles.Add(new(SizeType.Percent, 100F));
        uiMediaHeaderPanel.Controls.Add(new Label
        {
            Text = "Название",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new(Font, FontStyle.Bold),
        }, 0, 0);

        for (var i = 0; i < platformIds.Count; i++)
        {
            var source = sources[platformIds[i]];
            var displayName = source.Name.Length > 2 ? source.Name[..1] : source.Name;

            var label = new Label
            {
                Text = displayName,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new(Font, FontStyle.Bold),
            };

            toolTip.SetToolTip(label, source.Name);

            uiMediaHeaderPanel.ColumnStyles.Add(new(SizeType.Absolute, 40F));
            uiMediaHeaderPanel.Controls.Add(label, i + 1, 0);
        }

        uiMediaGridPanel.Controls.Clear();
        uiMediaGridPanel.RowCount = 0;
        var mediaData = _orcestrator.GetMediaData();

        foreach (var media in mediaData)
        {
            var dto = new MediaGridRowDto
            {
                Id = media.Id,
                Title = media.Title,
                PlatformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status),
            };

            var control = new MediaItemControl();
            control.SetData(dto, platformIds);
            control.Dock = DockStyle.Top;
            uiMediaGridPanel.RowCount++;
            uiMediaGridPanel.Controls.Add(control);
        }
    }
}
