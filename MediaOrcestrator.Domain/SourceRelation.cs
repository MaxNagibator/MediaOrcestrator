using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Domain;

public class SourceRelation
{
    public MySource To { get; set; }
    public MySource From { get; set; }
}
