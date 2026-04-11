using MediaOrcestrator.Domain;

// TODO: Скрестить с UpdateProgressForm
namespace MediaOrcestrator.Runner;

public partial class UpdateInfoForm : Form
{
    public UpdateInfoForm()
    {
        InitializeComponent();
    }

    public UpdateInfoForm(AppUpdateInfo update) : this()
    {
        uiVersionLabel.Text = $"Доступна новая версия {update.Version}";
        uiSizeLabel.Text = $"Размер: {FormatSize(update.Size)}";
        FormatReleaseNotes(uiReleaseNotesTextBox, update.ReleaseNotes);
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
