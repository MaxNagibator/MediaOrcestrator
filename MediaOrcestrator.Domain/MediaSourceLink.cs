using LiteDB;

namespace MediaOrcestrator.Domain;

/// <summary>
/// Статус медии в конкретном хранилище
/// </summary>
public class MediaSourceLink
{
    // TODO: Прибрать
    public const string StatusOk = "OK";
    public const string StatusError = "Error";
    public const string StatusNone = "None";

    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    public string SourceId { get; set; }

    public string Status { get; set; }

    /// <summary>
    /// Идентификатор медии в источнике.
    /// </summary>
    /// <remarks>
    /// В каждом источники свой айди.
    /// </remarks>
    public string ExternalId { get; set; }

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
