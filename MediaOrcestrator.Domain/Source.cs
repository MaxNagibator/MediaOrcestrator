using LiteDB;
using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Domain;

public class Source
{
    public string Id { get; set; }
    public string TypeId { get; set; }
    public Dictionary<string, string> Settings { get; set; }

    public string Title => Settings.GetValueOrDefault("_system_name", "<noname>");

    public string TitleFull => Title + " (" + TypeId + ")";

    [BsonIgnore]
    public ISourceType Type { get; set; }
}
