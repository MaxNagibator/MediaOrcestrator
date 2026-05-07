using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain.Comments;

public sealed class CommentsService(
    LiteDatabase db,
    CommentsRepository repository,
    ILogger<CommentsService> logger)
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.MaxValue; // TimeSpan.FromHours(24);

    public List<CommentRecord> GetCached(MediaSourceLink link)
    {
        return repository.GetByMedia(link.SourceId, link.ExternalId);
    }

    public List<CommentRecord> GetByMedia(string sourceId, string externalMediaId)
    {
        return repository.GetByMedia(sourceId, externalMediaId);
    }

    public List<CommentRecord> Query(
        string? sourceId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? textContains = null,
        int limit = 1000)
    {
        return repository.Query(sourceId, null, from, to, textContains, limit);
    }

    public bool IsStale(MediaSourceLink link, TimeSpan? ttl = null)
    {
        if (link.CommentsFetchedAt == null)
        {
            return true;
        }

        var effective = ttl ?? DefaultCacheTtl;
        return DateTime.UtcNow - link.CommentsFetchedAt.Value > effective;
    }

    public async Task<int> RefreshAsync(
        Source source,
        Media media,
        MediaSourceLink link,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (source.Type is not ISupportsComments commentsSource)
        {
            throw new NotSupportedException($"Источник «{source.TitleFull}» не поддерживает комментарии");
        }

        progress?.Report($"Загрузка комментариев из «{source.TitleFull}»...");
        logger.LogInformation("Загрузка комментариев media={MediaId} source={SourceId} external={ExternalId}",
            media.Id, source.Id, link.ExternalId);

        var fetched = new List<CommentRecord>();
        await foreach (var dto in commentsSource.GetCommentsAsync(link.ExternalId, source.Settings, cancellationToken))
        {
            fetched.Add(MapToRecord(source.Id, link.ExternalId, dto));

            if (fetched.Count % 50 == 0)
            {
                progress?.Report($"«{source.TitleFull}»: получено {fetched.Count} комментариев");
            }
        }

        repository.ReplaceAll(source.Id, link.ExternalId, fetched);

        link.CommentsFetchedAt = DateTime.UtcNow;
        link.CommentsCount = fetched.Count;

        db.GetCollection<Media>("medias").Update(media);

        progress?.Report($"«{source.TitleFull}»: загружено {fetched.Count} комментариев");
        logger.LogInformation("Загружено {Count} комментариев media={MediaId} source={SourceId}",
            fetched.Count, media.Id, source.Id);

        return fetched.Count;
    }

    public async Task<CommentRecord> CreateCommentAsync(
        Source source,
        MediaSourceLink link,
        string? parentExternalCommentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var mutator = RequireMutator(source);

        var dto = await mutator.CreateCommentAsync(link.ExternalId,
            parentExternalCommentId,
            text,
            source.Settings,
            cancellationToken);

        var record = MapToRecord(source.Id, link.ExternalId, dto);
        repository.Upsert(record);

        logger.LogInformation("Создан комментарий {CommentId} в media={ExternalMediaId} source={SourceId}",
            record.ExternalCommentId, link.ExternalId, source.Id);

        return record;
    }

    public async Task<CommentRecord> EditCommentAsync(
        Source source,
        MediaSourceLink link,
        string externalCommentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var mutator = RequireMutator(source);

        var updated = await mutator.EditCommentAsync(link.ExternalId,
            externalCommentId,
            text,
            source.Settings,
            cancellationToken);

        var existing = repository.GetById($"{source.Id}|{link.ExternalId}|{externalCommentId}");

        CommentRecord record;
        if (updated != null)
        {
            record = MapToRecord(source.Id, link.ExternalId, updated);

            if (string.IsNullOrEmpty(record.ParentExternalCommentId)
                && existing != null
                && !string.IsNullOrEmpty(existing.ParentExternalCommentId))
            {
                record.ParentExternalCommentId = existing.ParentExternalCommentId;
            }
        }
        else
        {
            record = existing
                     ?? throw new InvalidOperationException($"Комментарий {externalCommentId} не найден в локальном кэше и источник не вернул свежее состояние");

            record.Text = text;
        }

        repository.Upsert(record);

        logger.LogInformation("Обновлён комментарий {CommentId} в media={ExternalMediaId} source={SourceId}",
            externalCommentId, link.ExternalId, source.Id);

        return record;
    }

    public async Task DeleteCommentAsync(
        Source source,
        MediaSourceLink link,
        string externalCommentId,
        CancellationToken cancellationToken = default)
    {
        var mutator = RequireMutator(source);

        await mutator.DeleteCommentAsync(link.ExternalId,
            externalCommentId,
            source.Settings,
            cancellationToken);

        var existing = repository.GetById($"{source.Id}|{link.ExternalId}|{externalCommentId}");
        if (existing != null)
        {
            existing.IsDeleted = true;
            repository.Upsert(existing);
        }

        logger.LogInformation("Удалён комментарий {CommentId} в media={ExternalMediaId} source={SourceId}",
            externalCommentId, link.ExternalId, source.Id);
    }

    public async Task RestoreCommentAsync(
        Source source,
        MediaSourceLink link,
        string externalCommentId,
        CancellationToken cancellationToken = default)
    {
        var mutator = RequireMutator(source);

        await mutator.RestoreCommentAsync(link.ExternalId,
            externalCommentId,
            source.Settings,
            cancellationToken);

        var existing = repository.GetById($"{source.Id}|{link.ExternalId}|{externalCommentId}");
        if (existing != null)
        {
            existing.IsDeleted = false;
            repository.Upsert(existing);
        }

        logger.LogInformation("Восстановлен комментарий {CommentId} в media={ExternalMediaId} source={SourceId}",
            externalCommentId, link.ExternalId, source.Id);
    }

    public async Task<int> LikeCommentAsync(
        Source source,
        MediaSourceLink link,
        string externalCommentId,
        bool liked,
        CancellationToken cancellationToken = default)
    {
        if (source.Type is not ISupportsCommentLikes likes)
        {
            throw new NotSupportedException($"Источник «{source.TitleFull}» не поддерживает лайки на комментариях");
        }

        var newCount = liked
            ? await likes.LikeCommentAsync(link.ExternalId, externalCommentId, source.Settings, cancellationToken)
            : await likes.UnlikeCommentAsync(link.ExternalId, externalCommentId, source.Settings, cancellationToken);

        var existing = repository.GetById($"{source.Id}|{link.ExternalId}|{externalCommentId}");
        if (existing != null)
        {
            existing.LikedByMe = liked;
            existing.LikeCount = newCount;
            repository.Upsert(existing);
        }

        logger.LogInformation("{Action} комментария {CommentId} в media={ExternalMediaId} source={SourceId}, новый счётчик={Count}",
            liked ? "Поставлен лайк" : "Снят лайк", externalCommentId, link.ExternalId, source.Id, newCount);

        return newCount;
    }

    private static ISupportsCommentMutations RequireMutator(Source source)
    {
        if (source.Type is not ISupportsCommentMutations mutator)
        {
            throw new NotSupportedException($"Источник «{source.TitleFull}» не поддерживает изменение комментариев");
        }

        return mutator;
    }

    private static CommentRecord MapToRecord(string sourceId, string externalMediaId, CommentDto dto)
    {
        return new()
        {
            Id = $"{sourceId}|{externalMediaId}|{dto.ExternalId}",
            SourceId = sourceId,
            ExternalMediaId = externalMediaId,
            ExternalCommentId = dto.ExternalId,
            ParentExternalCommentId = dto.ParentExternalId,
            AuthorName = dto.AuthorName,
            AuthorExternalId = dto.AuthorExternalId,
            AuthorAvatarUrl = dto.AuthorAvatarUrl,
            Text = dto.Text,
            PublishedAt = dto.PublishedAt,
            LikeCount = dto.LikeCount,
            IsDeleted = dto.IsDeleted,
            IsAuthor = dto.IsAuthor,
            LikedByAuthor = dto.LikedByAuthor,
            LikedByMe = dto.LikedByMe,
            CanEdit = dto.CanEdit,
            CanDelete = dto.CanDelete,
            Raw = dto.Raw,
        };
    }
}
