using System.Text.Json.Serialization;

namespace MediaOrcestrator.Rutube;

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(UploadSessionRequest))]
[JsonSerializable(typeof(UploadSessionResponse))]
[JsonSerializable(typeof(MetadataUpdateRequest))]
[JsonSerializable(typeof(VideoDetailsResponse))]
[JsonSerializable(typeof(ThumbnailResponse))]
[JsonSerializable(typeof(GetVideoApiResponse))]
[JsonSerializable(typeof(GetVideoApiItem))]
[JsonSerializable(typeof(PublicationRequest))]
[JsonSerializable(typeof(PublicationResponse))]
[JsonSerializable(typeof(List<CategoryInfo>))]
[JsonSerializable(typeof(RutubeCommentsResponse))]
[JsonSerializable(typeof(RutubeCommentItem))]
[JsonSerializable(typeof(RutubeCreateCommentRequest))]
[JsonSerializable(typeof(RutubeEditCommentRequest))]
[JsonSerializable(typeof(RutubeReactionRequest))]
[JsonSerializable(typeof(RutubeReactionResponse))]
internal sealed partial class RutubeJsonContext : JsonSerializerContext
{
}
