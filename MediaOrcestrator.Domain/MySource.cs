namespace MediaOrcestrator.Domain;

public class MySource
{
    public string Id { get; set; }
    public string TypeId { get; set; }
    public Dictionary<string, string> Settings { get; set; }
}
