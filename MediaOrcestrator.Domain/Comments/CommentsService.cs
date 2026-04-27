using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain.Comments;

public sealed class CommentsService(
    LiteDatabase db,
    CommentsRepository repository,
    ILogger<CommentsService> logger)
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(int.MaxValue); // TimeSpan.FromHours(24);

    public List<CommentRecord> GetCached(MediaSourceLink link)
    {
        return repository.GetByMedia(link.SourceId, link.ExternalId);
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
            Raw = dto.Raw,
        };
    }
}
