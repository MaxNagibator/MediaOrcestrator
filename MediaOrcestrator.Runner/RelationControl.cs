using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner;

public partial class RelationControl : UserControl
{
    private readonly Orcestrator _orcestrator;
    private SourceRelation? _relation;

    public RelationControl(Orcestrator orcestrator)
    {
        _orcestrator = orcestrator;
        InitializeComponent();
    }

    public event EventHandler? RelationDeleted;

    public void SetRelation(SourceRelation relation)
    {
        _relation = relation;

        uiFromTitleLabel.Text = relation.From.Title;
        uiToTitleLabel.Text = relation.To.Title;

        uiFromTypeLabel.Text = relation.From.TypeId;
        uiToTypeLabel.Text = relation.To.TypeId;
    }

    private void uiDeleteButton_Click(object sender, EventArgs e)
    {
        if (_relation == null)
        {
            return;
        }

        var dialogResult = MessageBox.Show("Вы уверены, что хотите удалить эту связь?", "Удаление связи", MessageBoxButtons.YesNo);
        if (dialogResult != DialogResult.Yes)
        {
            return;
        }

        _orcestrator.RemoveLink(_relation.From, _relation.To);
        RelationDeleted?.Invoke(this, EventArgs.Empty);
    }
}
