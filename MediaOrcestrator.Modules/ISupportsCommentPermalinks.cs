namespace MediaOrcestrator.Modules;

/// <summary>
/// Поддержка прямых ссылок на конкретные комментарии во внешнем сервисе.
/// </summary>
/// <remarks>
/// Плагин реализует этот интерфейс дополнительно к <see cref="ISupportsComments" />,
/// если внешний сервис умеет адресовать комментарии.
/// </remarks>
public interface ISupportsCommentPermalinks
{
    /// <summary>
    /// Формирует ссылку на конкретный комментарий во внешнем сервисе.
    /// </summary>
    /// <param name="externalMediaId">Идентификатор медиа в источнике.</param>
    /// <param name="externalCommentId">Идентификатор комментария в источнике.</param>
    /// <param name="rootExternalCommentId">
    /// Идентификатор корневого комментария всей цепочки (предок самого верхнего уровня);
    /// <see langword="null" />, если сам комментарий корневой.
    /// </param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <returns>URI или <see langword="null" />, если ссылку построить не удалось.</returns>
    Uri? GetCommentExternalUri(
        string externalMediaId,
        string externalCommentId,
        string? rootExternalCommentId,
        Dictionary<string, string> settings);
}
