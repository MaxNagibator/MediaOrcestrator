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
        TryLoadPreview(media);
        PopulateSources(media, sourceDict);

        uiTitleLabel.ContextMenuStrip = CreateCopyMenu(uiTitleLabel);
        uiDescriptionLabel.ContextMenuStrip = CreateCopyMenu(uiDescriptionLabel);
        uiHeaderPanel_Resize(null, EventArgs.Empty);
    }

    private void uiHeaderPanel_Resize(object? sender, EventArgs e)
    {
        if (_titleFont == null)
        {
            return;
        }

        var padding = uiHeaderPanel.Padding;
        var contentWidth = uiHeaderPanel.ClientSize.Width - padding.Left - padding.Right;

        uiPreviewBox.Location = new(padding.Left, padding.Top);
        uiPreviewBox.Width = contentWidth;

        if (uiPreviewBox.Image != null)
        {
            var aspectRatio = (double)uiPreviewBox.Image.Height / uiPreviewBox.Image.Width;
            uiPreviewBox.Height = Math.Clamp((int)(contentWidth * aspectRatio), 100, 500);
        }

        uiTitleLabel.Location = new(padding.Left, uiPreviewBox.Bottom + 8);
        uiTitleLabel.Size = new(contentWidth, _titleFont.Height + 4);

        uiDescriptionLabel.Location = new(padding.Left, uiTitleLabel.Bottom + 4);
        uiDescriptionLabel.Size = new(contentWidth, _regularFont.Height * 2);

        var desiredHeight = uiDescriptionLabel.Bottom + padding.Bottom;
        if (Math.Abs(uiHeaderPanel.Height - desiredHeight) > 1)
        {
            uiHeaderPanel.Height = desiredHeight;
        }
    }

    private static Label CreateLabel(string text, Font font, Color? foreColor = null, string? copyValue = null)
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

        label.ContextMenuStrip = CreateCopyMenu(label, copyValue);
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

        if (meta.DisplayType == "Bitrate" && long.TryParse(meta.Value, out var bitsPerSecond))
        {
            return FormatBitrate(bitsPerSecond);
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

    private static ContextMenuStrip CreateCopyMenu(Control control, string? copyValue = null)
    {
        var menu = new ContextMenuStrip();

        if (copyValue != null)
        {
            menu.Items.Add("Копировать значение", null, (_, _) => Clipboard.SetText(copyValue));
            menu.Items.Add("Копировать целиком", null, (_, _) => Clipboard.SetText(control.Text));
        }
        else
        {
            menu.Items.Add("Копировать", null, (_, _) => Clipboard.SetText(control.Text));
        }

        return menu;
    }

    private static string FormatBitrate(long bitsPerSecond)
    {
        if (bitsPerSecond < 1000)
        {
            return $"{bitsPerSecond} бит/с";
        }

        if (bitsPerSecond < 1_000_000)
        {
            return $"{bitsPerSecond / 1000.0:F0} Кбит/с";
        }

        return $"{bitsPerSecond / 1_000_000.0:F2} Мбит/с";
    }

    private void AdjustPreviewSize()
    {
        uiHeaderPanel_Resize(null, EventArgs.Empty);
    }

    private void TryLoadPreview(Media media)
    {
        foreach (var meta in media.Metadata.Where(m => m.Key == "PreviewUrl" && !string.IsNullOrEmpty(m.Value)))
        {
            var path = meta.Value;

            if (File.Exists(path))
            {
                try
                {
                    using var image = Image.Load(path);
                    using var ms = new MemoryStream();
                    image.Save(ms, new BmpEncoder());
                    ms.Position = 0;
                    uiPreviewBox.Image = new Bitmap(ms);
                    AdjustPreviewSize();
                    _logger?.LogDebug("Превью загружено: {Path}", path);
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Не удалось загрузить превью: {Path}", path);
                }
            }

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            _ = LoadOnlinePreviewAsync(path);
            return;
        }
    }

    private async Task LoadOnlinePreviewAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            var data = await httpClient.GetByteArrayAsync(url);
            using var image = Image.Load(data);
            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new BmpEncoder());
            ms.Position = 0;
            var bitmap = new Bitmap(ms);

            if (!IsDisposed)
            {
                uiPreviewBox.Image = bitmap;
                AdjustPreviewSize();
                _logger?.LogDebug("Онлайн превью загружено: {Url}", url);
            }
            else
            {
                bitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Не удалось загрузить онлайн превью: {Url}", url);
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
                Color.FromArgb(100, 100, 100),
                sourceLink.ExternalId));

            var sourceMetadata = media.Metadata
                .Where(m => m.SourceId == sourceLink.SourceId && m.Key != "PreviewUrl")
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
                        _regularFont, copyValue: displayValue));
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
