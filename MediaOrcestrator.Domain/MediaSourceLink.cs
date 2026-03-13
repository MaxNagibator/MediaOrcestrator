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
    /// Как будто бы есть, но в источнике не найден.
    /// </summary>
    /// <remarks>
    /// После загрузки на рутуб, ошибок нету, но видео пропадает.
    /// </remarks>
    public const string StatusMissing = "Missing";

    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    public string SourceId { get; set; }

    public string Status { get; set; }

    public int SortNumber { get; set; }

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
