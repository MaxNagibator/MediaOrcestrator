using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class MediaItemControl : UserControl
{
    public MediaItemControl()
    {
        InitializeComponent();
    }

    public void SetData(MediaGridRowDto data, List<Source> platformIds)
    {
        tableLayoutPanel1.Controls.Clear();
        tableLayoutPanel1.ColumnCount = platformIds.Count + 1;
        tableLayoutPanel1.ColumnStyles.Clear();

        tableLayoutPanel1.ColumnStyles.Add(new(SizeType.Percent, 100F));
        var lblTitle = new Label
        {
            Text = data.Title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new(Font, FontStyle.Bold),
        };

        tableLayoutPanel1.Controls.Add(lblTitle, 0, 0);

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

            tableLayoutPanel1.ColumnStyles.Add(new(SizeType.Absolute, 80F));
            tableLayoutPanel1.Controls.Add(lblStatus, i + 1, 0);
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
