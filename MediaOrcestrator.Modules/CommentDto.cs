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
    /// Комментарий написан владельцем медиа (автором канала, владельцем группы и т. п.).
    /// </summary>
    public bool IsAuthor { get; set; }

    /// <summary>
    /// Лайк/сердечко от автора медиа. Источники, у которых такого признака нет, всегда оставляют <see langword="false" />.
    /// </summary>
    public bool LikedByAuthor { get; set; }

    /// <summary>
    /// Текущий авторизованный пользователь/группа уже поставили лайк этому комментарию.
    /// </summary>
    public bool LikedByMe { get; set; }

    /// <summary>
    /// Текущий пользователь может править этот комментарий через <see cref="ISupportsCommentMutations" />.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Текущий пользователь может удалить этот комментарий через <see cref="ISupportsCommentMutations" />.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Дополнительные поля, специфичные для источника.
    /// </summary>
    public Dictionary<string, string>? Raw { get; set; }
}
