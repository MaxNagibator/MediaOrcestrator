using System.Text.Json.Serialization;

namespace MediaOrcestrator.VkVideo;

public sealed class WebTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires")]
    public long Expires { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}

public sealed class CatalogResponse
{
    [JsonPropertyName("catalog")]
    public CatalogData Catalog { get; set; } = new();

    [JsonPropertyName("videos")]
    public List<VideoItem> Videos { get; set; } = [];

    [JsonPropertyName("groups")]
    public List<GroupItem> Groups { get; set; } = [];
}

public sealed class CatalogData
{
    [JsonPropertyName("default_section")]
    public string? DefaultSection { get; set; }

    [JsonPropertyName("sections")]
    public List<CatalogSection> Sections { get; set; } = [];
}

public sealed class CatalogSection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("next_from")]
    public string? NextFrom { get; set; }

    [JsonPropertyName("blocks")]
    public List<CatalogBlock>? Blocks { get; set; }
}

public sealed class CatalogBlock
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("next_from")]
    public string? NextFrom { get; set; }

    [JsonPropertyName("videos_ids")]
    public List<string>? VideosIds { get; set; }
}

public sealed class CatalogSectionResponse
{
    [JsonPropertyName("section")]
    public CatalogSection Section { get; set; } = new();

    [JsonPropertyName("videos")]
    public List<VideoItem> Videos { get; set; } = [];
}

public sealed record CatalogFirstPageResult(
    List<VideoItem> Videos,
    string? VideoSectionId,
    string? VideoNextFrom,
    string? ClipsSectionId);

public sealed record CatalogSectionPageResult(
    List<VideoItem> Videos,
    string? NextFrom);

public sealed class VideoItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("views")]
    public int Views { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("comments")]
    public int Comments { get; set; }

    [JsonPropertyName("image")]
    public List<VideoImage> Image { get; set; } = [];

    [JsonPropertyName("files")]
    public VideoFiles? Files { get; set; }

    [JsonPropertyName("likes")]
    public VideoLikes? Likes { get; set; }

    [JsonPropertyName("reposts")]
    public VideoReposts? Reposts { get; set; }

    [JsonPropertyName("player")]
    public string? Player { get; set; }

    [JsonPropertyName("direct_url")]
    public string? DirectUrl { get; set; }

    [JsonPropertyName("can_download")]
    public int CanDownload { get; set; }

    public string GetVideoKey()
    {
        return $"{OwnerId}_{Id}";
    }
}

public sealed class VideoImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public sealed class VideoFiles
{
    [JsonPropertyName("mp4_144")]
    public string? Mp4144 { get; set; }

    [JsonPropertyName("mp4_240")]
    public string? Mp4240 { get; set; }

    [JsonPropertyName("mp4_360")]
    public string? Mp4360 { get; set; }

    [JsonPropertyName("mp4_480")]
    public string? Mp4480 { get; set; }

    [JsonPropertyName("mp4_720")]
    public string? Mp4720 { get; set; }

    [JsonPropertyName("mp4_1080")]
    public string? Mp41080 { get; set; }

    [JsonPropertyName("hls")]
    public string? Hls { get; set; }

    [JsonPropertyName("failover_host")]
    public string? FailoverHost { get; set; }

    public string? GetBestQualityUrl()
    {
        return Mp41080 ?? Mp4720 ?? Mp4480 ?? Mp4360 ?? Mp4240 ?? Mp4144;
    }
}

public sealed class VideoLikes
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class VideoReposts
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class GroupItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("screen_name")]
    public string? ScreenName { get; set; }
}

public sealed class GroupsGetByIdResponse
{
    [JsonPropertyName("groups")]
    public List<GroupItem> Groups { get; set; } = [];
}

public sealed class VideoGetResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<VideoItem> Items { get; set; } = [];
}

public sealed class VideoGetByIdsResponse
{
    [JsonPropertyName("items")]
    public List<VideoItem> Items { get; set; } = [];
}

public sealed class VideoSaveResponse
{
    [JsonPropertyName("video_id")]
    public long VideoId { get; set; }

    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; set; } = string.Empty;

    [JsonPropertyName("thumb_upload_url")]
    public string? ThumbUploadUrl { get; set; }

    [JsonPropertyName("access_key")]
    public string? AccessKey { get; set; }
}

public sealed class FileUploadResponse
{
    [JsonPropertyName("video_hash")]
    public string VideoHash { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    [JsonPropertyName("video_id")]
    public long VideoId { get; set; }
}

public sealed class PublishVideoResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("direct_url")]
    public string? DirectUrl { get; set; }
}

public sealed class PublishResponse
{
    [JsonPropertyName("video")]
    public PublishVideoResponse? Video { get; set; }
}

public sealed class EditResponse
{
    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("access_key")]
    public string? AccessKey { get; set; }
}

public sealed class ThumbUploadResponse
{
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("server")]
    public string? Server { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("album_id")]
    public int AlbumId { get; set; }

    [JsonPropertyName("app_id")]
    public long AppId { get; set; }

    [JsonPropertyName("meta")]
    public string? Meta { get; set; }
}

public sealed class SaveThumbResponse
{
    [JsonPropertyName("photo_id")]
    public long PhotoId { get; set; }

    [JsonPropertyName("photo_hash")]
    public string PhotoHash { get; set; } = string.Empty;

    [JsonPropertyName("photo_owner_id")]
    public long PhotoOwnerId { get; set; }
}

public sealed class ShortVideoThumbUploadUrlResponse
{
    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; set; } = string.Empty;
}

public sealed class ShortVideoEncodeProgressResponse
{
    [JsonPropertyName("percents")]
    public int Percents { get; set; }

    [JsonPropertyName("is_claimed")]
    public bool IsClaimed { get; set; }

    [JsonPropertyName("is_ready")]
    public bool IsReady { get; set; }
}

public sealed class VideoForEditResponse
{
    [JsonPropertyName("item")]
    public VideoForEditItem Item { get; set; } = new();
}

public sealed class VideoForEditItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("thumb_upload_url")]
    public string? ThumbUploadUrl { get; set; }
}

public sealed class VkCommentsResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<VkCommentItem> Items { get; set; } = [];

    [JsonPropertyName("profiles")]
    public List<VkCommentProfile> Profiles { get; set; } = [];

    [JsonPropertyName("groups")]
    public List<VkCommentGroup> Groups { get; set; } = [];
}

public sealed class VkCommentItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("from_id")]
    public long FromId { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("parents_stack")]
    public List<long> ParentsStack { get; set; } = [];

    [JsonPropertyName("reply_to_comment")]
    public long? ReplyToComment { get; set; }

    [JsonPropertyName("reply_to_user")]
    public long? ReplyToUser { get; set; }

    [JsonPropertyName("likes")]
    public VkCommentLikes? Likes { get; set; }

    [JsonPropertyName("thread")]
    public VkCommentThread? Thread { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("can_edit")]
    public int CanEdit { get; set; }

    [JsonPropertyName("can_delete")]
    public int CanDelete { get; set; }
}

public sealed class VkCommentLikes
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("group_liked")]
    public bool GroupLiked { get; set; }

    [JsonPropertyName("user_likes")]
    public int UserLikes { get; set; }

    [JsonPropertyName("can_like")]
    public int CanLike { get; set; }
}

public sealed class VkLikesAddResponse
{
    [JsonPropertyName("likes")]
    public int Likes { get; set; }
}

public sealed class VkCommentThread
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class VkCommentProfile
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("photo_100")]
    public string? Photo100 { get; set; }
}

public sealed class VkCommentGroup
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("photo_100")]
    public string? Photo100 { get; set; }
}
