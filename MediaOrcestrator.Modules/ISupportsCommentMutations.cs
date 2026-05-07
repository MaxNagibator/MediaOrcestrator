namespace MediaOrcestrator.Modules;

/// <summary>
/// Поддержка изменения комментариев в источнике: создание, редактирование, удаление,
/// восстановление.
/// </summary>
/// <remarks>
/// Плагин реализует интерфейс дополнительно к <see cref="ISupportsComments" />,
/// если внешний сервис позволяет писать в ленту комментариев от имени авторизованного
/// пользователя/группы.
/// </remarks>
public interface ISupportsCommentMutations
{
    /// <summary>
    /// Создаёт новый корневой комментарий или ответ.
    /// </summary>
    /// <param name="externalMediaId">Идентификатор медиа в источнике.</param>
    /// <param name="parentExternalCommentId">
    /// Идентификатор комментария, на который отвечаем; <see langword="null" /> для корневого.
    /// </param>
    /// <param name="text">Текст нового комментария.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Полный DTO созданного комментария — со всеми полями автора и идентификатором.</returns>
    Task<CommentDto> CreateCommentAsync(
        string externalMediaId,
        string? parentExternalCommentId,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Меняет текст существующего комментария.
    /// </summary>
    /// <returns>Обновлённый DTO, если источник возвращает свежее состояние; иначе <see langword="null" />.</returns>
    Task<CommentDto?> EditCommentAsync(
        string externalMediaId,
        string externalCommentId,
        string text,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает комментарий удалённым.
    /// </summary>
    Task DeleteCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Снимает с комментария отметку удаления.
    /// </summary>
    Task RestoreCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}
