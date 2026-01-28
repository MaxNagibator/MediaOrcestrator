using LiteDB;

namespace MediaOrcestrator.Domain;

public class Media
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public List<MediaSourceLink> Sources { get; set; }
}
