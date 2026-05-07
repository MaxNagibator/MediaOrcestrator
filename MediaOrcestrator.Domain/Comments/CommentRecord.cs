namespace MediaOrcestrator.Domain.Comments;

/// <summary>
/// Запись комментария в локальном кэше LiteDB.
/// </summary>
/// <remarks>
/// Хранится в коллекции <c>media_comments</c>. Привязывается к
/// <see cref="MediaSourceLink" /> через пару (<see cref="SourceId" />,
/// <see cref="ExternalMediaId" />).
/// </remarks>
public sealed class CommentRecord
{
    /// <summary>
    /// Композитный первичный ключ: <c>{SourceId}|{ExternalMediaId}|{ExternalCommentId}</c>.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Идентификатор источника (см. <see cref="Source.Id" />).
    /// </summary>
    public required string SourceId { get; set; }

    /// <summary>
    /// Внешний идентификатор медиа в источнике.
    /// </summary>
    public required string ExternalMediaId { get; set; }

    /// <summary>
    /// Внешний идентификатор комментария в источнике.
    /// </summary>
    public required string ExternalCommentId { get; set; }

    /// <summary>
    /// Внешний идентификатор родительского комментария;
    /// <see langword="null" /> у корневых.
    /// </summary>
    public string? ParentExternalCommentId { get; set; }

    public required string AuthorName { get; set; }
    public string? AuthorExternalId { get; set; }
    public string? AuthorAvatarUrl { get; set; }

    public required string Text { get; set; }
    public DateTime PublishedAt { get; set; }
    public int? LikeCount { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsAuthor { get; set; }
    public bool LikedByAuthor { get; set; }
    public bool LikedByMe { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>
    /// Дополнительные поля, специфичные для источника.
    /// </summary>
    public Dictionary<string, string>? Raw { get; set; }
}
