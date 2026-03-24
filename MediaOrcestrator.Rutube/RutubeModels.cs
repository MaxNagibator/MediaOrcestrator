using System.Text.Json.Serialization;

namespace MediaOrcestrator.Rutube;

public sealed class UploadSessionRequest
{
    [JsonPropertyName("cancelToken")]
    public CancelToken CancelToken { get; set; } = new();
}

public sealed class CancelToken
{
    [JsonPropertyName("promise")]
    public object Promise { get; set; } = new();
}

public sealed class UploadSessionResponse
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = string.Empty;

    [JsonPropertyName("video")]
    public string VideoId { get; set; } = string.Empty;
}

public sealed class MetadataUpdateRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("is_adult")]
    public bool IsAdult { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "13"; // 13 - Разное

    [JsonPropertyName("properties")]
    public MetadataUpdateProperties Properties { get; set; } = new();
}

public sealed class MetadataUpdateProperties
{
    [JsonPropertyName("hide_comments")]
    public bool HideComments { get; set; }
}

public sealed class VideoDetailsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("is_audio")]
    public bool IsAudio { get; set; }

    [JsonPropertyName("created_ts")]
    public string? CreatedTs { get; set; }

    [JsonPropertyName("video_url")]
    public string? VideoUrl { get; set; }

    [JsonPropertyName("track_id")]
    public long TrackId { get; set; }

    [JsonPropertyName("hits")]
    public int Hits { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("is_livestream")]
    public bool IsLivestream { get; set; }

    [JsonPropertyName("is_on_air")]
    public bool IsOnAir { get; set; }

    [JsonPropertyName("last_update_ts")]
    public string? LastUpdateTs { get; set; }

    [JsonPropertyName("author")]
    public Author Author { get; set; } = new();

    [JsonPropertyName("pg_rating")]
    public PgRating PgRating { get; set; } = new();

    [JsonPropertyName("publication_ts")]
    public string? PublicationTs { get; set; }

    [JsonPropertyName("category")]
    public CategoryInfo Category { get; set; } = new();

    [JsonPropertyName("action_reason")]
    public ActionReason? ActionReason { get; set; }

    [JsonPropertyName("embed_url")]
    public string? EmbedUrl { get; set; }

    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("restrictions")]
    public Restrictions Restrictions { get; set; } = new();

    [JsonPropertyName("properties")]
    public VideoProperties Properties { get; set; } = new();
}

public sealed class Author
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("site_url")]
    public string? SiteUrl { get; set; }
}

public sealed class PgRating
{
    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}

public sealed class CategoryInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category_url")]
    public string? CategoryUrl { get; set; }
}

public class ActionReason
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class Restrictions
{
    [JsonPropertyName("country")]
    public CountryRestrictions Country { get; set; } = new();
}

public sealed class CountryRestrictions
{
    [JsonPropertyName("allowed")]
    public List<string> Allowed { get; set; } = new();

    [JsonPropertyName("restricted")]
    public List<string> Restricted { get; set; } = new();
}

public sealed class VideoProperties
{
    [JsonPropertyName("hide_comments")]
    public bool HideComments { get; set; }

    [JsonPropertyName("is_donate_allowed")]
    public bool IsDonateAllowed { get; set; }
}

public sealed class PublicationRequest
{
    [JsonPropertyName("video")]
    public string VideoId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("hideVideo")]
    public bool HideVideo { get; set; }
}

public sealed class PublicationResponse
{
    [JsonPropertyName("video")]
    public string VideoId { get; set; } = string.Empty;

    [JsonPropertyName("blocking_rule")]
    public long? BlockingRule { get; set; }

    [JsonPropertyName("pub_timestamp")]
    public string? PubTimestamp { get; set; }

    [JsonPropertyName("hide_video")]
    public bool HideVideo { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}

public sealed class ThumbnailResponse
{
    [JsonPropertyName("thumbnail_url")]
    public string ThumbnailUrl { get; set; } = string.Empty;
}


public class GetVideoApiResponse
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("results")]
    public List<GetVideoApiItem> Results { get; set; } = new();

    [JsonPropertyName("num_pages")]
    public int NumPages { get; set; }

    [JsonPropertyName("video_count")]
    public int VideoCount { get; set; }
}

public class GetVideoApiItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("video_url")]
    public string? VideoUrl { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("is_audio")]
    public bool IsAudio { get; set; }

    [JsonPropertyName("created_ts")]
    public DateTime CreatedTs { get; set; }

    [JsonPropertyName("track_id")]
    public long TrackId { get; set; }

    [JsonPropertyName("is_livestream")]
    public bool IsLivestream { get; set; }

    [JsonPropertyName("is_on_air")]
    public bool IsOnAir { get; set; }

    [JsonPropertyName("last_update_ts")]
    public DateTime LastUpdateTs { get; set; }

    [JsonPropertyName("stream_type")]
    public string? StreamType { get; set; }

    [JsonPropertyName("picture_url")]
    public string? PictureUrl { get; set; }

    [JsonPropertyName("author")]
    public Author? Author { get; set; }

    [JsonPropertyName("pg_rating")]
    public PgRating? PgRating { get; set; }

    [JsonPropertyName("origin_type")]
    public string? OriginType { get; set; }

    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; set; }

    [JsonPropertyName("is_adult")]
    public bool IsAdult { get; set; }

    [JsonPropertyName("is_club")]
    public bool IsClub { get; set; }

    [JsonPropertyName("is_classic")]
    public bool IsClassic { get; set; }

    [JsonPropertyName("is_paid")]
    public bool IsPaid { get; set; }

    [JsonPropertyName("product_id")]
    public string? ProductId { get; set; }

    [JsonPropertyName("common_subscription_product_codes")]
    public List<string> CommonSubscriptionProductCodes { get; set; } = new();

    [JsonPropertyName("publication_ts")]
    public DateTime PublicationTs { get; set; }

    [JsonPropertyName("pepper")]
    public string? Pepper { get; set; }

    [JsonPropertyName("delayed")]
    public bool Delayed { get; set; }

    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("hits")]
    public int Hits { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("has_advert")]
    public bool HasAdvert { get; set; }

    [JsonPropertyName("is_reborn_channel")]
    public bool IsRebornChannel { get; set; }

    [JsonPropertyName("action_reason")]
    public ActionReason? ActionReason { get; set; }

    [JsonPropertyName("future_publication")]
    public object? FuturePublication { get; set; }

    [JsonPropertyName("video_related")]
    public object? VideoRelated { get; set; }
}
