namespace MediaOrcestrator.Modules;

/// <summary>
/// Поддержка выбора автора при создании комментария: источник позволяет писать
/// от имени личного профиля или одной из связанных групп/каналов.
/// </summary>
/// <remarks>
/// Реализуется дополнительно к <see cref="ISupportsCommentMutations" />.
/// Если у источника один-единственный возможный автор, интерфейс реализовывать
/// не нужно — UI просто использует обычный <see cref="ISupportsCommentMutations.CreateCommentAsync" />.
/// </remarks>
public interface ISupportsCommentAuthors
{
    /// <summary>
    /// Возвращает список авторов, от имени которых можно опубликовать комментарий
    /// к указанному медиа.
    /// </summary>
    /// <remarks>
    /// Список должен содержать как минимум одного автора. Один из элементов помечается
    /// <see cref="CommentAuthorOption.IsDefault" /> — он используется по умолчанию,
    /// если пользователь не выбрал другой.
    /// </remarks>
    /// <param name="externalMediaId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<IReadOnlyList<CommentAuthorOption>> GetAvailableAuthorsAsync(
        string externalMediaId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт комментарий от имени конкретного автора.
    /// </summary>
    /// <param name="externalMediaId">Идентификатор медиа в источнике.</param>
    /// <param name="parentExternalCommentId">
    /// Идентификатор комментария, на который отвечаем (адресат ответа); <see langword="null" /> для корневого.
    /// </param>
    /// <param name="rootExternalCommentId">
    /// Идентификатор корневого комментария всей цепочки; <see langword="null" />, если сам ответ корневой.
    /// </param>
    /// <param name="replyToAuthorName">
    /// Отображаемое имя автора <paramref name="parentExternalCommentId" /> для @-меншена; <see langword="null" />, если не
    /// нужно.
    /// </param>
    /// <param name="text">Текст нового комментария.</param>
    /// <param name="authorId">
    /// Идентификатор автора из <see cref="GetAvailableAuthorsAsync" />.
    /// <see langword="null" /> или пустая строка — автор по умолчанию.
    /// </param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<CommentDto> CreateCommentAsAsync(
        string externalMediaId,
        string? parentExternalCommentId,
        string? rootExternalCommentId,
        string? replyToAuthorName,
        string text,
        string? authorId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Описание автора, от имени которого можно создать комментарий.
/// </summary>
/// <param name="Id">
/// Стабильный идентификатор автора (например, "user" или "group:-238153973").
/// Передаётся обратно в <see cref="ISupportsCommentAuthors.CreateCommentAsAsync" />.
/// </param>
/// <param name="Name">Отображаемое имя — личный профиль или название группы.</param>
/// <param name="AvatarUrl">URL аватара или <see langword="null" />.</param>
/// <param name="IsDefault">Автор по умолчанию.</param>
public sealed record CommentAuthorOption(
    string Id,
    string Name,
    string? AvatarUrl,
    bool IsDefault);
