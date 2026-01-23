namespace MediaOrcestrator.Domain;

public class MyMedia
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public List<MyMediaLinkToSource> Sources { get; set; }
}
