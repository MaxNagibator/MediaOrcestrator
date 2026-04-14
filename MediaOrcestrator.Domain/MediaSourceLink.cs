using LiteDB;

namespace MediaOrcestrator.Domain;

/// <summary>
/// Статус медии в конкретном хранилище
/// </summary>
public class MediaSourceLink
{
    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    public string SourceId { get; set; }

    public string Status { get; set; }

    public string? StatusMessage { get; set; }

    public int SortNumber { get; set; }

    /// <summary>
    /// Идентификатор медии в источнике.
    /// </summary>
    /// <remarks>
    /// В каждом источники свой айди.
    /// </remarks>
    public string ExternalId { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    /// <summary>
    /// Внутренний идентификатор медии.
    /// </summary>
    public string MediaId { get; set; }

    [BsonIgnore]
    public Media Media { get; set; }

    public override string ToString()
    {
        return $"{SourceId}: {ExternalId} ({Status})";
    }
}
