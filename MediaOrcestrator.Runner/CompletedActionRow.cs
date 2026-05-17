using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public sealed partial class CompletedActionRow : UserControl
{
    private static readonly Color SucceededAccent = Color.FromArgb(0x2E, 0x8B, 0x57);
    private static readonly Color SucceededBackColor = Color.FromArgb(0xF1, 0xF8, 0xF1);
    private static readonly Color SucceededForeColor = Color.FromArgb(0x55, 0x6B, 0x55);
    private static readonly Color FailedAccent = Color.IndianRed;
    private static readonly Color FailedBackColor = Color.FromArgb(0xFB, 0xEF, 0xEF);
    private static readonly Color CanceledAccent = Color.DimGray;
    private static readonly Color CanceledBackColor = Color.FromArgb(0xF4, 0xF4, 0xF4);

    private string _name = string.Empty;
    private string? _error;

    public CompletedActionRow()
    {
        InitializeComponent();
    }

    public void SetAction(ActionHolder.RunningAction action)
    {
        _name = action.Name;
        _error = action.Error;

        var duration = ActionFormatting.FormatDuration(action.Duration);
        var status = action.Status;
        uiLabel.Text = string.IsNullOrEmpty(status)
            ? $"{action.Name} · {duration}"
            : $"{action.Name} — {status} · {duration}";

        switch (action.State)
        {
            case ActionState.Failed:
                uiAccentStrip.BackColor = FailedAccent;
                BackColor = FailedBackColor;
                uiLabel.ForeColor = SystemColors.ControlText;
                uiDetailsButton.Visible = !string.IsNullOrEmpty(_error);
                break;

            case ActionState.Cancelled:
                uiAccentStrip.BackColor = CanceledAccent;
                BackColor = CanceledBackColor;
                uiLabel.ForeColor = CanceledAccent;
                uiDetailsButton.Visible = false;
                break;

            default:
                uiAccentStrip.BackColor = SucceededAccent;
                BackColor = SucceededBackColor;
                uiLabel.ForeColor = SucceededForeColor;
                uiDetailsButton.Visible = false;
                break;
        }
    }

    private void uiDetailsButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_error))
        {
            return;
        }

        using var form = new TaskErrorForm(_name, _error);
        form.ShowDialog(this);
    }
}
