using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class RelationControl : UserControl
{
    private readonly Orcestrator _orcestrator;

    public RelationControl(Orcestrator orcestrator)
    {
        _orcestrator = orcestrator;
        InitializeComponent();
    }

    public event EventHandler? RelationDeleted;
    public event EventHandler? RelationSelectionChanged;

    public bool Selected => uiSelectCheckBox.Checked;

    public SourceSyncRelation? Relation { get; private set; }

    public void SetRelation(SourceSyncRelation relation)
    {
        Relation = relation;

        uiFromTitleLabel.Text = relation.From.Title;
        uiToTitleLabel.Text = relation.To.Title;

        uiFromTypeLabel.Text = relation.From.TypeId;
        uiToTypeLabel.Text = relation.To.TypeId;
    }

    private void uiDeleteButton_Click(object sender, EventArgs e)
    {
        if (Relation == null)
        {
            return;
        }

        var dialogResult = MessageBox.Show("Вы уверены, что хотите удалить эту связь?", "Удаление связи", MessageBoxButtons.YesNo);
        if (dialogResult != DialogResult.Yes)
        {
            return;
        }

        _orcestrator.RemoveLink(Relation.From, Relation.To);
        RelationDeleted?.Invoke(this, EventArgs.Empty);
    }

    private void uiSelectCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        BackColor = uiSelectCheckBox.Checked ? Color.LightCyan : Color.WhiteSmoke;
        RelationSelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
