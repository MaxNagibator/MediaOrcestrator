using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MediaOrcestrator.Youtube;

public sealed class YoutubeCommentsReadService(ILogger logger, YoutubeAuthService authService)
{
    private const string ThreadParts = "snippet,replies";
    private const string CommentParts = "snippet";
    private const long PageSize = 100L;

    public async IAsyncEnumerable<CommentDto> GetCommentsAsync(
        string videoId,
        Dictionary<string, string> settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!YoutubeAuthService.IsConfigured(settings))
        {
            throw new InvalidOperationException("Для получения комментариев YouTube необходимо настроить OAuth (client_id и client_secret)");
        }

        using var service = await authService.CreateServiceAsync(settings, cancellationToken);

        string? pageToken = null;
        var totalThreads = 0;
        var totalReplies = 0;

        do
        {
            var request = service.CommentThreads.List(ThreadParts);
            request.VideoId = videoId;
            request.MaxResults = PageSize;
            request.TextFormat = CommentThreadsResource.ListRequest.TextFormatEnum.PlainText;
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(cancellationToken);
            if (response.Items is null || response.Items.Count == 0)
            {
                break;
            }

            foreach (var thread in response.Items)
            {
                var topComment = thread.Snippet?.TopLevelComment;
                if (topComment is not null)
                {
                    yield return MapComment(topComment, null);

                    totalThreads++;
                }

                var totalReplyCount = thread.Snippet?.TotalReplyCount ?? 0;
                var loadedReplies = thread.Replies?.Comments?.Count ?? 0;

                if (totalReplyCount == 0)
                {
                    continue;
                }

                if (totalReplyCount <= loadedReplies && thread.Replies?.Comments is { } inline)
                {
                    foreach (var reply in inline)
                    {
                        yield return MapComment(reply, thread.Id);

                        totalReplies++;
                    }
                }
                else
                {
                    await foreach (var reply in FetchAllRepliesAsync(service, thread.Id, cancellationToken))
                    {
                        yield return reply;

                        totalReplies++;
                    }
                }
            }

            pageToken = response.NextPageToken;
        } while (pageToken is not null);

        logger.LogInformation("YouTube комментарии: видео {VideoId}, верхних {Threads}, ответов {Replies}",
            videoId, totalThreads, totalReplies);
    }

    private static async IAsyncEnumerable<CommentDto> FetchAllRepliesAsync(
        YouTubeService service,
        string parentId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? pageToken = null;
        do
        {
            var request = service.Comments.List(CommentParts);
            request.ParentId = parentId;
            request.MaxResults = PageSize;
            request.TextFormat = CommentsResource.ListRequest.TextFormatEnum.PlainText;
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(cancellationToken);
            if (response.Items is null)
            {
                yield break;
            }

            foreach (var reply in response.Items)
            {
                yield return MapComment(reply, parentId);
            }

            pageToken = response.NextPageToken;
        } while (pageToken is not null);
    }

    private static CommentDto MapComment(Comment comment, string? parentExternalId)
    {
        var snippet = comment.Snippet;

        return new()
        {
            ExternalId = comment.Id,
            ParentExternalId = parentExternalId ?? snippet?.ParentId,
            AuthorName = snippet?.AuthorDisplayName ?? string.Empty,
            AuthorExternalId = ExtractAuthorChannelId(snippet),
            AuthorAvatarUrl = snippet?.AuthorProfileImageUrl,
            Text = snippet?.TextDisplay ?? snippet?.TextOriginal ?? string.Empty,
            PublishedAt = snippet?.PublishedAtDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow,
            LikeCount = snippet?.LikeCount is { } likes ? (int)likes : null,
            IsDeleted = false,
        };
    }

    private static string? ExtractAuthorChannelId(CommentSnippet? snippet)
    {
        var holder = snippet?.AuthorChannelId;
        var valueProp = holder?.GetType().GetProperty("Value");
        return valueProp?.GetValue(holder) as string;
    }
}
