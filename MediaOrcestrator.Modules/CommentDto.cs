namespace MediaOrcestrator.Modules;

/// <summary>
/// Один комментарий к медиа из внешнего источника.
/// </summary>
public sealed class CommentDto
{
    /// <summary>
    /// Идентификатор комментария в источнике.
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Идентификатор родительского комментария; <see langword="null" /> у корневых.
    /// </summary>
    public string? ParentExternalId { get; set; }

    /// <summary>
    /// Отображаемое имя автора.
    /// </summary>
    public required string AuthorName { get; set; }

    /// <summary>
    /// Идентификатор автора в источнике; <see langword="null" />, если недоступен.
    /// </summary>
    public string? AuthorExternalId { get; set; }

    /// <summary>
    /// URL аватара автора; <see langword="null" />, если отсутствует.
    /// </summary>
    public string? AuthorAvatarUrl { get; set; }

    /// <summary>
    /// Текст комментария.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Время публикации в UTC.
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Количество лайков; <see langword="null" />, если источник не предоставляет.
    /// </summary>
    public int? LikeCount { get; set; }

    /// <summary>
    /// Признак удалённого комментария — текст автора скрыт, но ветка ответов сохраняется.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Дополнительные поля, специфичные для источника.
    /// </summary>
    public Dictionary<string, string>? Raw { get; set; }
}
