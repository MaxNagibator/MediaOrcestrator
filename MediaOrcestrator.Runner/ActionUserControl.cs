using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class ActionUserControl : UserControl
{
    private ActionHolder.RunningAction _act;
    private bool _isCanceled;

    public ActionUserControl()
    {
        InitializeComponent();
    }

    public void SetAction(ActionHolder.RunningAction act)
    {
        _act = act;
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        label1.Text = _act.Name + " " + _act.Status;
        if (_isCanceled)
        {
            BackColor = Color.DarkGray;
            button1.Visible = false;
            return;
        }

        if (_act.ProgressMax > 0)
        {
            progressBar1.Visible = true;
            progressBar1.Maximum = _act.ProgressMax;
            progressBar1.Value = Math.Clamp(_act.ProgressValue, 0, _act.ProgressMax);
            label2.Visible = true;
            label2.Text = _act.ProgressValue + " / " + _act.ProgressMax;
        }
        else
        {
            progressBar1.Visible = false;
            label2.Visible = false;
        }
    }

    private void button1_Click(object sender, EventArgs e)
    {
        _act.Status = "Отменено";
        _act.ProgressMax = 0;
        _isCanceled = true;
        UpdateStatus();
        _act.Cancel();
    }
}
