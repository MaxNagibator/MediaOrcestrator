using LiteDB;

namespace MediaOrcestrator.Domain;

/// <summary>
/// Статус медии в конкретном хранилище
/// </summary>
public class MediaSourceLink
{
    public string SourceId { get; set; }

    public string Status { get; set; }

    public string MediaId { get; set; }

    [BsonIgnore]
    public Media Media { get; set; }
}
