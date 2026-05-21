namespace MediaOrcestrator.Runner;

public partial class CommentsFetchOptionsDialog : Form
{
    public CommentsFetchOptionsDialog()
    {
        InitializeComponent();
    }

    public CommentsFetchOptionsDialog(CommentsViewSettings settings) : this()
    {
        uiSinceNumeric.Value = Math.Clamp(settings.FetchSinceDays, (int)uiSinceNumeric.Minimum, (int)uiSinceNumeric.Maximum);
        uiOnlyRecentNumeric.Value = Math.Clamp(settings.FetchOnlyRecent, (int)uiOnlyRecentNumeric.Minimum, (int)uiOnlyRecentNumeric.Maximum);

        var parallelism = settings.FetchMaxParallelism <= 0
            ? CommentsViewSettings.DefaultFetchMaxParallelism
            : settings.FetchMaxParallelism;

        uiMaxParallelismNumeric.Value = Math.Clamp(parallelism,
            (int)uiMaxParallelismNumeric.Minimum,
            (int)uiMaxParallelismNumeric.Maximum);
    }

    public int SinceDays => (int)uiSinceNumeric.Value;
    public int OnlyRecent => (int)uiOnlyRecentNumeric.Value;
    public int MaxParallelism => (int)uiMaxParallelismNumeric.Value;

    private void uiOkButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void uiCancelButton_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
