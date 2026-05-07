namespace MediaOrcestrator.Modules;

/// <summary>
/// Поддержка лайков на комментариях.
/// </summary>
/// <remarks>
/// Реализуется отдельно от <see cref="ISupportsCommentMutations" />: источник может
/// уметь лайкать без права писать/править комментарии (или наоборот).
/// </remarks>
public interface ISupportsCommentLikes
{
    /// <summary>
    /// Ставит лайк от текущего авторизованного пользователя/группы.
    /// </summary>
    /// <returns>Свежий счётчик лайков комментария.</returns>
    Task<int> LikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Снимает лайк.
    /// </summary>
    /// <returns>Свежий счётчик лайков комментария.</returns>
    Task<int> UnlikeCommentAsync(
        string externalMediaId,
        string externalCommentId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}
