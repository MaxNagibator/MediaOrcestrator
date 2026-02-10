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
        mediaData = mediaData.Take(20).ToList();

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

        SetHeaderColumns(sources);
        SetGridContent(sources, mediaData);
    }

    private void SetHeaderColumns(List<Source> sources)
    {
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
    }

    private void SetGridContent(List<Source> sources, List<Media> mediaData)
    {
        uMediaGridPanel.Controls.Clear();
        uMediaGridPanel.RowCount = 0;

        foreach (var media in mediaData)
        {
            //var dto = new MediaGridRowDto
            //{
            //    Id = media.Id,
            //    Title = media.Title,
            //    PlatformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status),
            //};

            var control = new MediaItemControl();
            control.SetData(media, sources, _orcestrator!);
            control.Dock = DockStyle.Top;
            uMediaGridPanel.RowCount++;
            uMediaGridPanel.Controls.Add(control);
        }
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
        RefreshData();
    }

    private void uiMergerSelectedMediaButton_Click(object sender, EventArgs e)
    {

        var selectedMediaList = new List<Media>();
        foreach (var m in uMediaGridPanel.Controls)
        {
            var mediaControl = m as MediaItemControl;
            if (mediaControl != null && mediaControl.IsMediaSelected)
            {
                selectedMediaList.Add(mediaControl.Media);
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
}
