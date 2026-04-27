namespace MediaOrcestrator.Modules;

/// <summary>
/// Поддержка просмотра комментариев для плагина-источника.
/// </summary>
/// <remarks>
/// Плагин реализует этот интерфейс дополнительно к <see cref="ISourceType" />,
/// если внешний сервис предоставляет комментарии к медиа.
/// </remarks>
public interface ISupportsComments
{
    /// <summary>
    /// Потоковое перечисление всего дерева комментариев к указанному медиа.
    /// </summary>
    /// <remarks>
    /// Реализация должна заполнять <see cref="CommentDto.ParentExternalId" />
    /// для построения дерева. Порядок элементов значения не имеет — дерево
    /// собирается на стороне потребителя по идентификаторам.
    /// </remarks>
    /// <param name="externalId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Асинхронная последовательность комментариев.</returns>
    IAsyncEnumerable<CommentDto> GetCommentsAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}
