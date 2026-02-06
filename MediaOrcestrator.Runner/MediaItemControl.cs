using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class MediaItemControl : UserControl
{
    private Orcestrator? _orcestrator;
    private Media _media;

    public MediaItemControl()
    {
        InitializeComponent();
    }

    public void SetData(Media media, List<Source> platformIds, Orcestrator? orcestrator)
    {
        _media = media;
        var data = new MediaGridRowDto
        {
            Id = media.Id,
            Title = media.Title,
            PlatformStatuses = media.Sources.ToDictionary(x => x.SourceId, x => x.Status),
        };

        _orcestrator = orcestrator;
        uiMainLayout.Controls.Clear();
        uiMainLayout.ColumnCount = platformIds.Count + 1;
        uiMainLayout.ColumnStyles.Clear();

        uiMainLayout.ColumnStyles.Add(new(SizeType.Percent, 100F));
        var lblTitle = new Label
        {
            Text = data.Title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new(Font, FontStyle.Bold),
        };

        uiMainLayout.Controls.Add(lblTitle, 0, 0);


        ContextMenuStrip contextMenu = new ContextMenuStrip();

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
                        var tempMedia = await rel.From.Type.Download(fromSource.ExternalId, rel.From.Settings);
                        tempMedia.Id = media.Id;
                        var externalId = await rel.To.Type.Upload(tempMedia, rel.To.Settings);

                        var toMediaSource = new MediaSourceLink
                        {
                            MediaId = media.Id,
                            Media = media,
                            ExternalId = externalId,
                            Status = "OK",
                            SourceId = rel.To.Id,
                        };

                        media.Sources.Add(toMediaSource);
                        orcestrator.UpdateMedia(media);
                        //logger.Information("Успешно синхронизировано медиа {Media} в {ToSource}. ExternalId: {ExternalId}", media, rel.To, externalId);
                    });
                });
            }
            else
            {
                var element = contextMenu.Items.Add("Синк " + rel.From.TitleFull + " -> " + rel.To.TitleFull, null, (s, e) =>
                { });
                element.Enabled = false;
            }
        }
        uiMainLayout.ContextMenuStrip = contextMenu;

        var toolTip = new ToolTip();

        for (var i = 0; i < platformIds.Count; i++)
        {
            var platformId = platformIds[i];

            var status = data.PlatformStatuses.GetValueOrDefault(platformId.Id, "None");
            var lblStatus = new Label
            {
                Text = GetStatusSymbol(status),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = GetStatusColor(status),
                Font = new(Font.FontFamily, 12, FontStyle.Bold),
            };

            toolTip.SetToolTip(lblStatus, $"Источник: {platformId.Title}\nСтатус: {status}");

            uiMainLayout.ColumnStyles.Add(new(SizeType.Absolute, 80F));
            uiMainLayout.Controls.Add(lblStatus, i + 1, 0);
        }
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
}
