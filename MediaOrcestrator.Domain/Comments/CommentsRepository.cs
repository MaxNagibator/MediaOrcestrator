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
        IReadOnlyCollection<string>? sourceIds = null,
        string? externalMediaId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? textContains = null,
        int limit = 1000)
    {
        var query = Collection.Query();

        if (sourceIds is { Count: > 0 })
        {
            if (sourceIds.Count == 1)
            {
                var only = sourceIds.First();
                query = query.Where(x => x.SourceId == only);
            }
            else
            {
                var set = sourceIds as HashSet<string> ?? new HashSet<string>(sourceIds, StringComparer.Ordinal);
                query = query.Where(x => set.Contains(x.SourceId));
            }
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

        var ordered = query.OrderByDescending(x => x.PublishedAt);

        if (string.IsNullOrWhiteSpace(textContains))
        {
            return ordered.Limit(limit).ToList();
        }

        var result = new List<CommentRecord>(Math.Min(limit, 256));
        foreach (var record in ordered.ToEnumerable())
        {
            var matches = record.Text != null && record.Text.Contains(textContains, StringComparison.OrdinalIgnoreCase)
                          || record.AuthorName != null && record.AuthorName.Contains(textContains, StringComparison.OrdinalIgnoreCase);

            if (!matches)
            {
                continue;
            }

            result.Add(record);

            if (result.Count >= limit)
            {
                break;
            }
        }

        return result;
    }

    public int CountAll()
    {
        return Collection.Count();
    }

    public CommentRecord? GetById(string id)
    {
        return Collection.FindById(id);
    }

    public void Upsert(CommentRecord record)
    {
        Collection.Upsert(record);
    }

    public void UpsertMany(IReadOnlyList<CommentRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        _db.BeginTrans();

        try
        {
            var collection = Collection;

            foreach (var record in records)
            {
                collection.Upsert(record);
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
