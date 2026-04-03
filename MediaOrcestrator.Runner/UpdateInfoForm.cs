using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

// TODO: Скрестить с UpdateProgressForm
public sealed class UpdateInfoForm : Form
{
    public UpdateInfoForm(AppUpdateInfo update)
    {
        Text = "Обновление приложения";
        MinimumSize = new(400, 350);
        Size = new(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(12),
            RowCount = 5,
            ColumnCount = 1,
        };

        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));

        var versionLabel = new Label
        {
            Text = $"Доступна новая версия {update.Version}",
            Font = new(Font.FontFamily, 12, FontStyle.Bold),
            AutoSize = true,
            Margin = new(0, 0, 0, 4),
        };

        var sizeLabel = new Label
        {
            Text = $"Размер: {FormatSize(update.Size)}",
            AutoSize = true,
            Margin = new(0, 0, 0, 8),
        };

        var releaseNotesLabel = new Label
        {
            Text = "Что нового:",
            AutoSize = true,
            Margin = new(0, 0, 0, 2),
        };

        var releaseNotesTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Window,
            Font = new("Segoe UI", 10),
        };

        FormatReleaseNotes(releaseNotesTextBox, update.ReleaseNotes);

        var buttonsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new(0, 4, 0, 0),
        };

        var laterButton = new Button
        {
            Text = "Позже",
            DialogResult = DialogResult.No,
            AutoSize = true,
        };

        var updateButton = new Button
        {
            Text = "Обновить",
            DialogResult = DialogResult.Yes,
            AutoSize = true,
        };

        buttonsPanel.Controls.AddRange([laterButton, updateButton]);

        layout.Controls.Add(versionLabel, 0, 0);
        layout.Controls.Add(sizeLabel, 0, 1);
        layout.Controls.Add(releaseNotesLabel, 0, 2);
        layout.Controls.Add(releaseNotesTextBox, 0, 3);
        layout.Controls.Add(buttonsPanel, 0, 4);

        AcceptButton = updateButton;
        CancelButton = laterButton;

        Controls.Add(layout);
    }

    private static void FormatReleaseNotes(RichTextBox rtb, string markdown)
    {
        rtb.Clear();
        var boldFont = new Font(rtb.Font, FontStyle.Bold);
        var headingFont = new Font(rtb.Font.FontFamily, 13, FontStyle.Bold);

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (rtb.TextLength > 0)
            {
                rtb.AppendText(Environment.NewLine);
            }

            if (line.StartsWith("## "))
            {
                AppendStyled(rtb, line[3..], headingFont);
            }
            else if (line.StartsWith("- "))
            {
                rtb.AppendText("\u2022 ");
                AppendInline(rtb, line[2..], boldFont);
            }
            else
            {
                AppendInline(rtb, line, boldFont);
            }
        }
    }

    private static void AppendStyled(RichTextBox rtb, string text, Font font)
    {
        var start = rtb.TextLength;
        rtb.AppendText(text);
        rtb.Select(start, text.Length);
        rtb.SelectionFont = font;
        rtb.SelectionLength = 0;
    }

    private static void AppendInline(RichTextBox rtb, string text, Font boldFont)
    {
        var remaining = text.AsSpan();

        while (!remaining.IsEmpty)
        {
            var boldStart = remaining.IndexOf("**");

            if (boldStart < 0)
            {
                rtb.AppendText(remaining.ToString());
                break;
            }

            if (boldStart > 0)
            {
                rtb.AppendText(remaining[..boldStart].ToString());
            }

            remaining = remaining[(boldStart + 2)..];
            var boldEnd = remaining.IndexOf("**");

            if (boldEnd < 0)
            {
                rtb.AppendText("**");
                rtb.AppendText(remaining.ToString());
                break;
            }

            AppendStyled(rtb, remaining[..boldEnd].ToString(), boldFont);
            remaining = remaining[(boldEnd + 2)..];
        }
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} Б",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} КБ",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} МБ",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} ГБ",
        };
    }
}
