using Google;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MediaOrcestrator.Youtube;

internal sealed class YoutubeCommentsReadService(ILogger<YoutubeCommentsReadService> logger)
{
    private const string ThreadParts = "snippet,replies";
    private const string CommentParts = "snippet";
    private const string VideoOwnerParts = "snippet";
    private const long PageSize = 100L;

    public async IAsyncEnumerable<CommentDto> GetCommentsAsync(
        YouTubeService service,
        string videoId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ownerChannelId = await GetVideoOwnerChannelIdAsync(service, videoId, cancellationToken);

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

            CommentThreadListResponse response;
            try
            {
                response = await request.ExecuteAsync(cancellationToken);
            }
            catch (GoogleApiException ex) when (IsCommentsDisabled(ex))
            {
                logger.CommentsDisabled(videoId);
                yield break;
            }

            if (response.Items is null || response.Items.Count == 0)
            {
                break;
            }

            foreach (var thread in response.Items)
            {
                var topComment = thread.Snippet?.TopLevelComment;
                if (topComment is not null)
                {
                    yield return MapComment(topComment, null, ownerChannelId);

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
                        yield return MapComment(reply, thread.Id, ownerChannelId);

                        totalReplies++;
                    }
                }
                else
                {
                    await foreach (var reply in FetchAllRepliesAsync(service, thread.Id, ownerChannelId, cancellationToken))
                    {
                        yield return reply;

                        totalReplies++;
                    }
                }
            }

            pageToken = response.NextPageToken;
        } while (pageToken is not null);

        logger.CommentsCompleted(videoId, totalThreads, totalReplies);
    }

    private static async IAsyncEnumerable<CommentDto> FetchAllRepliesAsync(
        YouTubeService service,
        string parentId,
        string? ownerChannelId,
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
                yield return MapComment(reply, parentId, ownerChannelId);
            }

            pageToken = response.NextPageToken;
        } while (pageToken is not null);
    }

    private static CommentDto MapComment(
        Comment comment,
        string? parentExternalId,
        string? ownerChannelId)
    {
        var snippet = comment.Snippet;
        var authorChannelId = ExtractAuthorChannelId(snippet);

        return new()
        {
            ExternalId = comment.Id,
            ParentExternalId = parentExternalId ?? snippet?.ParentId,
            AuthorName = NormalizeAuthorName(snippet?.AuthorDisplayName),
            AuthorExternalId = authorChannelId,
            AuthorAvatarUrl = snippet?.AuthorProfileImageUrl,
            Text = snippet?.TextOriginal ?? snippet?.TextDisplay ?? string.Empty,
            PublishedAt = snippet?.PublishedAtDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow,
            LikeCount = snippet?.LikeCount is { } likes ? (int)likes : null,
            IsDeleted = false,
            IsAuthor = ownerChannelId is not null
                       && authorChannelId is not null
                       && string.Equals(ownerChannelId, authorChannelId, StringComparison.Ordinal),
            LikedByAuthor = false,
        };
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

    private static string? ExtractAuthorChannelId(CommentSnippet? snippet)
    {
        var holder = snippet?.AuthorChannelId;
        var valueProp = holder?.GetType().GetProperty("Value");
        return valueProp?.GetValue(holder) as string;
    }

    private static bool IsCommentsDisabled(GoogleApiException ex)
    {
        return ex.Error?.Errors?.Any(static e => string.Equals(e.Reason, "commentsDisabled", StringComparison.Ordinal)) == true;
    }

    private async Task<string?> GetVideoOwnerChannelIdAsync(
        YouTubeService service,
        string videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = service.Videos.List(VideoOwnerParts);
            request.Id = videoId;

            var response = await request.ExecuteAsync(cancellationToken);
            return response.Items?.FirstOrDefault()?.Snippet?.ChannelId;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.OwnerLookupFailed(videoId, ex);
            return null;
        }
    }
}
