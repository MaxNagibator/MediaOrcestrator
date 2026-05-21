using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeYtDlpCommentsReadService(ILogger<YoutubeYtDlpCommentsReadService> logger)
{
    private const string RootParent = "root";

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(
        string videoId,
        YtDlp ytDlp,
        CancellationToken cancellationToken)
    {
        var url = string.Format(YoutubeChannel.VideoUrlTemplate, videoId);
        logger.YtDlpCommentsFetching(videoId);

        var rawComments = await ytDlp.GetCommentsAsync(url, cancellationToken);

        if (rawComments.Count == 0)
        {
            logger.YtDlpCommentsEmpty(videoId);
            return [];
        }

        var result = new List<CommentDto>(rawComments.Count);
        var depthById = new Dictionary<string, int>(rawComments.Count, StringComparer.Ordinal);
        var topLevel = 0;
        var replies = 0;
        var maxDepth = 0;

        foreach (var comment in rawComments)
        {
            if (string.IsNullOrEmpty(comment.Id))
            {
                continue;
            }

            var parentId = NormalizeParent(comment.Parent);
            var depth = parentId is null
                ? 0
                : depthById.TryGetValue(parentId, out var parentDepth)
                    ? parentDepth + 1
                    : 1;

            depthById[comment.Id] = depth;

            if (depth == 0)
            {
                topLevel++;
            }
            else
            {
                replies++;

                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }

            result.Add(Map(comment, parentId));
        }

        logger.YtDlpCommentsCompleted(videoId, topLevel, replies, maxDepth);
        return result;
    }

    private static CommentDto Map(YtDlpCommentJson comment, string? parentId)
    {
        var publishedAt = comment.Timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(comment.Timestamp.Value).UtcDateTime
            : DateTime.UtcNow;

        return new()
        {
            ExternalId = comment.Id ?? string.Empty,
            ParentExternalId = parentId,
            AuthorName = NormalizeAuthorName(comment.Author),
            AuthorExternalId = comment.AuthorId,
            AuthorAvatarUrl = comment.AuthorThumbnail,
            Text = comment.Text ?? string.Empty,
            PublishedAt = publishedAt,
            LikeCount = comment.LikeCount,
            IsDeleted = false,
            IsAuthor = comment.AuthorIsUploader == true,
            LikedByAuthor = comment.IsFavorited == true,
        };
    }

    private static string? NormalizeParent(string? parent)
    {
        if (string.IsNullOrEmpty(parent) || string.Equals(parent, RootParent, StringComparison.Ordinal))
        {
            return null;
        }

        return parent;
    }

    private static string NormalizeAuthorName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var i = 0;
        while (i < name.Length && name[i] == '@')
        {
            i++;
        }

        return i == 0 ? name : name[i..];
    }
}
