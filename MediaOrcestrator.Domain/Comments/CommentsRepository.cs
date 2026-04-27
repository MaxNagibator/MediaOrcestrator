using LiteDB;

namespace MediaOrcestrator.Domain.Comments;

public sealed class CommentsRepository
{
    private const string CollectionName = "media_comments";

    private readonly LiteDatabase _db;

    public CommentsRepository(LiteDatabase db)
    {
        _db = db;

        var collection = Collection;
        collection.EnsureIndex(x => x.SourceId);
        collection.EnsureIndex(x => x.ExternalMediaId);
        collection.EnsureIndex(x => x.ParentExternalCommentId);
    }

    private ILiteCollection<CommentRecord> Collection => _db.GetCollection<CommentRecord>(CollectionName);

    public List<CommentRecord> GetByMedia(string sourceId, string externalMediaId)
    {
        return Collection
            .Find(x => x.SourceId == sourceId && x.ExternalMediaId == externalMediaId)
            .ToList();
    }

    public int CountByMedia(string sourceId, string externalMediaId)
    {
        return Collection.Count(x => x.SourceId == sourceId && x.ExternalMediaId == externalMediaId);
    }

    public List<CommentRecord> Query(
        string? sourceId = null,
        string? externalMediaId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? textContains = null,
        int limit = 1000)
    {
        var query = Collection.Query();

        if (sourceId != null)
        {
            query = query.Where(x => x.SourceId == sourceId);
        }

        if (externalMediaId != null)
        {
            query = query.Where(x => x.ExternalMediaId == externalMediaId);
        }

        if (from != null)
        {
            var fromValue = from.Value;
            query = query.Where(x => x.PublishedAt >= fromValue);
        }

        if (to != null)
        {
            var toValue = to.Value;
            query = query.Where(x => x.PublishedAt <= toValue);
        }

        var ordered = query.OrderByDescending(x => x.PublishedAt).Limit(limit).ToList();

        if (string.IsNullOrWhiteSpace(textContains))
        {
            return ordered;
        }

        return ordered
            .Where(x => x.Text != null && x.Text.Contains(textContains, StringComparison.OrdinalIgnoreCase)
                        || x.AuthorName != null && x.AuthorName.Contains(textContains, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public int CountAll()
    {
        return Collection.Count();
    }

    public void ReplaceAll(string sourceId, string externalMediaId, IReadOnlyList<CommentRecord> records)
    {
        _db.BeginTrans();

        try
        {
            var collection = Collection;
            collection.DeleteMany(x => x.SourceId == sourceId && x.ExternalMediaId == externalMediaId);

            if (records.Count > 0)
            {
                collection.InsertBulk(records);
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }
}
