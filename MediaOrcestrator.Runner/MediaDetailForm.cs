using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Bmp;
using System.ComponentModel;
using System.Diagnostics;
using Image = SixLabors.ImageSharp.Image;

namespace MediaOrcestrator.Runner;

public partial class MediaDetailForm : Form
{
    private readonly ILogger? _logger;
    private readonly Font _titleFont;
    private readonly Font _headerFont;
    private readonly Font _groupFont;
    private readonly Font _boldFont;
    private readonly Font _regularFont;

    public MediaDetailForm(Media media, List<Source> sources, ILogger? logger = null)
    {
        _logger = logger;
        InitializeComponent();

        _titleFont = new(Font.FontFamily, 11, FontStyle.Bold);
        _headerFont = new(Font.FontFamily, 10, FontStyle.Bold);
        _groupFont = new(Font.FontFamily, 9.5f, FontStyle.Bold);
        _boldFont = new(Font.FontFamily, 9, FontStyle.Bold);
        _regularFont = new(Font.FontFamily, 9, FontStyle.Regular);

        _logger?.LogDebug("Открытие окна деталей для медиа '{Title}' (Sources: {Count}, Metadata: {MetaCount})",
            media.Title, media.Sources.Count, media.Metadata.Count);

        Text = $"Подробная информация: {media.Title}";
        uiTitleLabel.Text = media.Title ?? "";
        uiTitleLabel.Font = _titleFont;
        uiDescriptionLabel.Text = media.Description ?? "";
        uiSourcesHeaderLabel.Font = _headerFont;

        var sourceDict = sources.ToDictionary(s => s.Id);
        TryLoadPreview(media, sourceDict);
        PopulateSources(media, sourceDict);
    }

    private void uiHeaderPanel_Resize(object? sender, EventArgs e)
    {
        var textLeft = uiPreviewBox.Right + 12;
        var textWidth = Math.Max(100, uiHeaderPanel.ClientSize.Width - textLeft - 12);
        uiTitleLabel.Location = new(textLeft, uiPreviewBox.Top);
        uiTitleLabel.Size = new(textWidth, uiHeaderPanel.Height / 3);
        uiDescriptionLabel.Location = new(textLeft, uiTitleLabel.Bottom + 4);
        uiDescriptionLabel.Size = new(textWidth, uiHeaderPanel.Height - uiTitleLabel.Bottom - 4 - uiHeaderPanel.Padding.Bottom);
    }

    private static Label CreateLabel(string text, Font font, Color? foreColor = null)
    {
        var label = new Label
        {
            Text = text,
            Font = font,
            AutoSize = true,
            Padding = new(2),
        };

        if (foreColor.HasValue)
        {
            label.ForeColor = foreColor.Value;
        }

        return label;
    }

    private static string FormatMetadataValue(MetadataItem meta)
    {
        if (string.IsNullOrEmpty(meta.Value) || string.IsNullOrEmpty(meta.DisplayType))
        {
            return meta.Value ?? "";
        }

        if (meta.DisplayType == "ByteSize" && long.TryParse(meta.Value, out var bytes))
        {
            return OptimizedMediaGridView.FormatFileSize(bytes);
        }

        var targetType = Type.GetType(meta.DisplayType);
        if (targetType == null)
        {
            return meta.Value;
        }

        try
        {
            var converted = TypeDescriptor.GetConverter(targetType).ConvertFromInvariantString(meta.Value);
            return converted switch
            {
                DateTime dt => dt.ToString("g"),
                int n => n.ToString("N0"),
                long l => l.ToString("N0"),
                double d => d.ToString("N0"),
                _ => converted?.ToString() ?? meta.Value,
            };
        }
        catch (Exception)
        {
            return meta.Value;
        }
    }

    private void TryLoadPreview(Media media, Dictionary<string, Source> sourceDict)
    {
        foreach (var sourceLink in media.Sources)
        {
            if (!sourceDict.TryGetValue(sourceLink.SourceId, out var source))
            {
                continue;
            }

            if (source.TypeId != "HardDiskDrive" || !source.Settings.TryGetValue("path", out var basePath))
            {
                continue;
            }

            var folder = Path.Combine(basePath, sourceLink.ExternalId);
            if (!Directory.Exists(folder))
            {
                continue;
            }

            var thumbnailPath = Directory.GetFiles(folder, "thumbnail.*").FirstOrDefault();
            if (thumbnailPath == null)
            {
                continue;
            }

            try
            {
                using var image = Image.Load(thumbnailPath);
                using var ms = new MemoryStream();
                image.Save(ms, new BmpEncoder());
                ms.Position = 0;
                uiPreviewBox.Image = new Bitmap(ms);
                _logger?.LogDebug("Превью загружено: {Path}", thumbnailPath);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Не удалось загрузить превью: {Path}", thumbnailPath);
            }
        }
    }

    private void PopulateSources(Media media, Dictionary<string, Source> sourceDict)
    {
        if (media.Sources.Count == 0)
        {
            uiContentPanel.Controls.Add(new Label
            {
                Text = "Нет источников",
                ForeColor = Color.Gray,
                AutoSize = true,
                Padding = new(4),
            });

            return;
        }

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };

        flow.Resize += (_, _) =>
        {
            flow.SuspendLayout();
            var width = flow.ClientSize.Width - flow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            foreach (Control child in flow.Controls)
            {
                child.Width = Math.Max(100, width);
            }

            flow.ResumeLayout();
        };

        foreach (var sourceLink in media.Sources)
        {
            var source = sourceDict.GetValueOrDefault(sourceLink.SourceId);
            var sourceName = source?.TitleFull ?? sourceLink.SourceId;
            var status = MediaStatusHelper.GetById(sourceLink.Status);

            var groupBox = new GroupBox
            {
                Text = sourceName,
                Font = _groupFont,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new(0, 50),
                Padding = new(8, 4, 8, 8),
            };

            var innerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
            };

            innerFlow.Controls.Add(CreateLabel($"{status.IconText} {status.Text}",
                _boldFont,
                status.IconColor));

            innerFlow.Controls.Add(CreateLabel($"ID: {sourceLink.ExternalId}",
                _regularFont,
                Color.FromArgb(100, 100, 100)));

            var sourceMetadata = media.Metadata
                .Where(m => m.SourceId == sourceLink.SourceId)
                .OrderBy(m => m.Key)
                .ToList();

            if (sourceMetadata.Count == 0)
            {
                innerFlow.Controls.Add(CreateLabel("(нет метаданных)",
                    _regularFont,
                    Color.Gray));
            }
            else
            {
                foreach (var meta in sourceMetadata)
                {
                    var displayName = meta.DisplayName ?? meta.Key;
                    var displayValue = FormatMetadataValue(meta);

                    innerFlow.Controls.Add(CreateLabel($"{displayName}: {displayValue}",
                        _regularFont));
                }
            }

            if (source?.Type != null)
            {
                var uri = source.Type.GetExternalUri(sourceLink.ExternalId, source.Settings);
                if (uri != null)
                {
                    var linkLabel = new LinkLabel
                    {
                        Text = "Открыть",
                        Font = _regularFont,
                        AutoSize = true,
                        Padding = new(2),
                    };

                    linkLabel.LinkClicked += (_, _) =>
                    {
                        Process.Start(new ProcessStartInfo(uri.ToString())
                        {
                            UseShellExecute = true,
                        });
                    };

                    innerFlow.Controls.Add(linkLabel);
                }
            }

            groupBox.Controls.Add(innerFlow);
            flow.Controls.Add(groupBox);

            _logger?.LogDebug("Добавлен источник '{Source}': {MetaCount} метаданных",
                sourceName, sourceMetadata.Count);
        }

        uiContentPanel.Controls.Add(flow);
    }
}
