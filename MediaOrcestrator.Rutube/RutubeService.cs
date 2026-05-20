using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using Polly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Rutube;

public sealed partial class RutubeService
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36";
    private const string Origin = "https://studio.rutube.ru";
    private const string Referer = "https://studio.rutube.ru/";
    private const string PlayerOrigin = "https://rutube.ru";

    private readonly HttpClient _apiClient;
    private readonly HttpClient _uploadClient;
    private readonly string _cookieString;
    private readonly string _csrfToken;
    private readonly string _playerCsrfToken;
    private readonly string? _userId;
    private readonly ILogger<RutubeService> _logger;

    public RutubeService(
        HttpClient apiClient,
        HttpClient uploadClient,
        string cookieString,
        string csrfToken,
        string playerCsrfToken,
        ILogger<RutubeService> logger)
    {
        _apiClient = apiClient;
        _uploadClient = uploadClient;
        _cookieString = cookieString;
        _csrfToken = csrfToken;
        _playerCsrfToken = playerCsrfToken;
        _logger = logger;

        var visitorIdMatch = VisitorIdRegex().Match(cookieString);
        if (visitorIdMatch.Success)
        {
            _userId = visitorIdMatch.Groups[1].Value;
        }
    }

    public async Task<UploadResult> UploadVideoAsync(
        string? videoId,
        string? filePath,
        string title,
        string description,
        string categoryId,
        string? thumbnailPath = null,
        DateTime? publishAt = null,
        long? uploadBytesPerSecond = null,
        IProgress<double>? uploadProgress = null,
        CancellationToken cancellationToken = default)
    {
        var isNewVideo = false;
        if (videoId == null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Для загрузки нового видео требуется путь к файлу", nameof(filePath));
            }

            isNewVideo = true;
            _logger.InitUploadSession();
            var session = await InitUploadSessionAsync(cancellationToken);
            _logger.UploadSessionCreated(session.Sid, session.VideoId);

            _logger.CreatingTusResource();
            var uploadUrl = await CreateTusResourceAsync(session.Sid, session.VideoId, filePath, cancellationToken);
            _logger.TusResourceCreated(uploadUrl);

            _logger.StartingDataUpload();
            await PerformTusUploadAsync(uploadUrl, filePath, uploadBytesPerSecond, uploadProgress, cancellationToken);
            _logger.DataUploadCompleted();

            _logger.WaitingServerProcessing();
            await Task.Delay(5000, cancellationToken);

            _logger.UpdatingDraftMetadata();
            await UpdateMetadataAsync(session.VideoId, title, description, categoryId, true, cancellationToken);
            _logger.MetadataUpdated();
            videoId = session.VideoId;
        }

        string? errorMessage = null;
        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
        {
            try
            {
                _logger.UploadingThumbnail();

                string? thumbnailUrl;
                if (Path.GetExtension(thumbnailPath).Equals(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _logger.ConvertingWebPToJpg();

                        var tempJpgPath = thumbnailPath + ".jpg";

                        using (var image = Image.Load(thumbnailPath))
                        {
                            var encoder = new JpegEncoder
                            {
                                Quality = 90,
                            };

                            if (File.Exists(tempJpgPath))
                            {
                                File.Delete(tempJpgPath);
                            }

                            await using var fileStream = new FileStream(tempJpgPath, FileMode.Create);
                            await image.SaveAsync(fileStream, encoder, cancellationToken);
                        }

                        _logger.WebPConverted();
                        thumbnailUrl = await UploadThumbnailAsync(videoId, tempJpgPath, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.WebPConversionFailed(ex);
                        thumbnailUrl = await UploadThumbnailAsync(videoId, thumbnailPath, cancellationToken);
                    }
                }
                else
                {
                    thumbnailUrl = await UploadThumbnailAsync(videoId, thumbnailPath, cancellationToken);
                }

                _logger.ThumbnailUploaded(thumbnailUrl);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ThumbnailUploadFailed(ex);
                errorMessage += "Ошибка загрузки превьюшки";
            }
        }

        if (isNewVideo)
        {
            try
            {
                if (publishAt.HasValue && publishAt.Value > DateTime.Now)
                {
                    _logger.SchedulingPublication(publishAt.Value);
                    await PublishVideoAsync(videoId, publishAt.Value, cancellationToken);
                    _logger.PublicationScheduled();
                }
                else
                {
                    _logger.PublishingNow();
                    await UpdateMetadataAsync(videoId, title, description, categoryId, false, cancellationToken);
                    _logger.Published();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.PublicationFailed(ex);
                if (errorMessage != null)
                {
                    errorMessage += "\r\n";
                }

                errorMessage += "Ошибка публикации";
            }
        }

        if (string.IsNullOrEmpty(errorMessage))
        {
            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = videoId,
            };
        }

        return new()
        {
            Status = MediaStatusHelper.GetById(MediaStatus.PartialOk),
            Id = videoId,
            Message = errorMessage,
        };
    }

    public async Task<VideoDetailsResponse> GetVideoByIdAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        _logger.RequestingVideoInfo(videoId);

        using var request = CreateRequest(HttpMethod.Get, url);
        using var response = await SendApiAsync(Operations.GetVideo, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.GetVideoFailed(videoId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось получить видео {videoId}: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.VideoDetailsResponse, cancellationToken)
                     ?? throw new InvalidOperationException($"Не удалось десериализовать ответ для видео {videoId}");

        _logger.VideoInfoReceived(result.Title);
        return result;
    }

    public async Task<List<CategoryInfo>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string Url = "https://studio.rutube.ru/api/video/category/";
        _logger.RequestingCategories();

        using var request = CreateRequest(HttpMethod.Get, Url);
        using var response = await SendApiAsync(Operations.ListCategories, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.GetCategoriesFailed(response.StatusCode, err);
            throw new HttpRequestException($"Не удалось получить категории: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.ListCategoryInfo, cancellationToken);

        _logger.CategoriesReceived(result?.Count ?? 0);
        return result ?? [];
    }

    public async Task DeleteVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        _logger.SendingDeleteRequest(videoId);

        using var request = CreateRequest(HttpMethod.Delete, url);
        using var response = await SendApiAsync(Operations.DeleteVideo, request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.VideoNotFoundTreatedAsDeleted(videoId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.DeleteVideoFailed(videoId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось удалить видео: {response.StatusCode}. Ответ: {err}");
        }

        _logger.VideoDeleted(videoId);
    }

    public async Task<string?> UploadThumbnailAsync(
        string videoId,
        string thumbnailPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(thumbnailPath))
        {
            throw new FileNotFoundException($"Файл превью не найден: {thumbnailPath}");
        }

        var url = $"https://studio.rutube.ru/api/video/{videoId}/thumbnail/?client=vulp";
        _logger.UploadingThumbnailFile(videoId, thumbnailPath);

        using var form = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(thumbnailPath, cancellationToken);
        var fileContent = new ByteArrayContent(fileBytes);

        var extension = Path.GetExtension(thumbnailPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };

        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "file", Path.GetFileName(thumbnailPath));

        using var request = CreateRequest(HttpMethod.Post, url, form);
        using var response = await SendApiAsync(Operations.UploadThumbnail, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.ThumbnailUploadStatusFailed(response.StatusCode, err);
            throw new HttpRequestException($"Не удалось загрузить превью: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.ThumbnailResponse, cancellationToken);

        return result?.ThumbnailUrl;
    }

    public async IAsyncEnumerable<GetVideoApiItem> GetVideoAsync(
        string? userId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var foreignChannel = !string.IsNullOrWhiteSpace(userId);
        var url = foreignChannel
            ? $"https://rutube.ru/api/video/person/{userId}/?client=wdp&origin__type=rtb,rst,ifrm,rspa,vppl&page=1"
            : "https://studio.rutube.ru/api/v2/video/person/?ordering=-calculated_date&limit=400&page=1";

        while (true)
        {
            using var request = foreignChannel
                ? CreateForeignChannelRequest(HttpMethod.Get, url, userId!)
                : CreateRequest(HttpMethod.Get, url);

            using var response = await SendApiAsync(Operations.ListVideos, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.GetVideoListFailed(response.StatusCode, err);
                throw new HttpRequestException($"Не удалось получить список видео: {response.StatusCode}. Ответ: {err}");
            }

            GetVideoApiResponse? page;

            await using (var body = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                page = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.GetVideoApiResponse, cancellationToken);
            }

            if (page is null)
            {
                yield break;
            }

            foreach (var item in page.Results)
            {
                yield return item;
            }

            if (!page.HasNext || string.IsNullOrEmpty(page.Next))
            {
                yield break;
            }

            url = page.Next;
        }
    }

    public async IAsyncEnumerable<RutubeCommentItem> GetCommentRootsAsync(
        string videoId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.IsNullOrEmpty(cursor)
                ? $"https://rutube.ru/api/v2/comments/video/{videoId}/?client=wdp&direction=earliest&sort_by=date_added_desc"
                : $"https://rutube.ru/api/v2/comments/video/{videoId}/?client=wdp&direction=earliest&comment_id={cursor}&sort_by=date_added_desc";

            var page = await GetCommentsPageAsync(videoId, url, Operations.ListComments, cancellationToken);
            if (page.Results.Count == 0)
            {
                yield break;
            }

            foreach (var item in page.Results)
            {
                yield return item;
            }

            if (!page.HasNext)
            {
                yield break;
            }

            cursor = page.Results[^1].Id;
        }
    }

    public async IAsyncEnumerable<RutubeCommentItem> GetCommentRepliesAsync(
        string videoId,
        string parentId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.IsNullOrEmpty(cursor)
                ? $"https://rutube.ru/api/v2/comments/video/{videoId}/?client=wdp&direction=earliest&parent_id={parentId}&sort_by=date_added_desc"
                : $"https://rutube.ru/api/v2/comments/video/{videoId}/?client=wdp&direction=earliest&parent_id={parentId}&comment_id={cursor}&sort_by=date_added_desc";

            var page = await GetCommentsPageAsync(videoId, url, Operations.ListReplies, cancellationToken);
            if (page.Results.Count == 0)
            {
                yield break;
            }

            foreach (var item in page.Results)
            {
                yield return item;
            }

            if (!page.HasNext)
            {
                yield break;
            }

            cursor = page.Results[^1].Id;
        }
    }

    public async Task<RutubeCommentItem> CreateCommentAsync(
        string videoId,
        string? parentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var isReply = !string.IsNullOrEmpty(parentId);
        var url = $"https://rutube.ru/api/comments/video/{videoId}/?mention={(isReply ? "true" : "false")}&client=wdp";

        var payload = new RutubeCreateCommentRequest
        {
            Text = text,
            ParentId = parentId,
        };

        var jsonPayload = JsonSerializer.Serialize(payload, RutubeJsonContext.Default.RutubeCreateCommentRequest);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var request = CreatePlayerRequest(HttpMethod.Post, url, videoId, content);
        using var response = await SendApiAsync(Operations.CreateComment, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.CreateCommentFailed(videoId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось создать комментарий к {videoId}: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.RutubeCommentItem, cancellationToken)
                     ?? throw new InvalidOperationException("RuTube вернул пустой ответ на создание комментария");

        _logger.CommentCreated(result.Id, videoId, isReply);
        return result;
    }

    public async Task<RutubeCommentItem> EditCommentAsync(
        string videoId,
        string commentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://rutube.ru/api/comments/{commentId}/?mention=false&client=wdp";

        var payload = new RutubeEditCommentRequest
        {
            Text = text,
        };

        var jsonPayload = JsonSerializer.Serialize(payload, RutubeJsonContext.Default.RutubeEditCommentRequest);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var request = CreatePlayerRequest(HttpMethod.Patch, url, videoId, content);
        using var response = await SendApiAsync(Operations.EditComment, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.EditCommentFailed(commentId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось отредактировать комментарий {commentId}: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.RutubeCommentItem, cancellationToken)
                     ?? throw new InvalidOperationException("RuTube вернул пустой ответ на редактирование комментария");

        _logger.CommentEdited(commentId, videoId);
        return result;
    }

    public async Task DeleteCommentAsync(
        string videoId,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://rutube.ru/api/comments/{commentId}/?client=wdp";
        using var request = CreatePlayerRequest(HttpMethod.Delete, url, videoId);
        using var response = await SendApiAsync(Operations.DeleteComment, request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.CommentNotFoundTreatedAsDeleted(commentId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.DeleteCommentFailed(commentId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось удалить комментарий {commentId}: {response.StatusCode}. Ответ: {err}");
        }

        _logger.CommentDeleted(commentId, videoId);
    }

    public async Task LikeCommentAsync(
        string videoId,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://rutube.ru/api/comments/{commentId}/reactions/?client=wdp";

        var payload = new RutubeReactionRequest
        {
            Reaction = "like",
        };

        var jsonPayload = JsonSerializer.Serialize(payload, RutubeJsonContext.Default.RutubeReactionRequest);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var request = CreatePlayerRequest(HttpMethod.Put, url, videoId, content);
        using var response = await SendApiAsync(Operations.LikeComment, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LikeCommentFailed(commentId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось поставить лайк на {commentId}: {response.StatusCode}. Ответ: {err}");
        }

        _logger.CommentLiked(commentId);
    }

    public async Task UnlikeCommentAsync(
        string videoId,
        string commentId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://rutube.ru/api/comments/{commentId}/reactions/?client=wdp";
        using var request = CreatePlayerRequest(HttpMethod.Delete, url, videoId);
        using var response = await SendApiAsync(Operations.UnlikeComment, request, cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.UnlikeCommentFailed(commentId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось снять лайк с {commentId}: {response.StatusCode}. Ответ: {err}");
        }

        _logger.CommentUnliked(commentId);
    }

    [GeneratedRegex(@"visitorID=([^;]+)")]
    private static partial Regex VisitorIdRegex();

    private HttpRequestMessage CreateForeignChannelRequest(
        HttpMethod method,
        string url,
        string userId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Cookie", _cookieString);
        request.Headers.TryAddWithoutValidation("Referer", $"{PlayerOrigin}/channel/{userId}/videos/");
        request.Headers.TryAddWithoutValidation("Origin", PlayerOrigin);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        return request;
    }

    private async Task<RutubeCommentsResponse> GetCommentsPageAsync(
        string videoId,
        string url,
        string operationKey,
        CancellationToken cancellationToken)
    {
        using var request = CreatePlayerRequest(HttpMethod.Get, url, videoId);
        using var response = await SendApiAsync(operationKey, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.ListCommentsFailed(videoId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось получить комментарии {videoId}: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.RutubeCommentsResponse, cancellationToken)
               ?? throw new InvalidOperationException($"RuTube вернул пустой ответ для комментариев {videoId}");
    }

    private HttpRequestMessage CreatePlayerRequest(
        HttpMethod method,
        string url,
        string videoId,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Cookie", _cookieString);
        request.Headers.TryAddWithoutValidation("x-csrftoken", _playerCsrfToken);
        request.Headers.TryAddWithoutValidation("Referer", $"{PlayerOrigin}/video/{videoId}/");
        request.Headers.TryAddWithoutValidation("Origin", PlayerOrigin);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("x-ally", "1");

        if (content != null)
        {
            request.Content = content;
        }

        return request;
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Cookie", _cookieString);
        request.Headers.TryAddWithoutValidation("x-csrftoken", _csrfToken);
        request.Headers.TryAddWithoutValidation("Referer", Referer);
        request.Headers.TryAddWithoutValidation("Origin", Origin);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        if (content != null)
        {
            request.Content = content;
        }

        return request;
    }

    private async Task<HttpResponseMessage> SendApiAsync(
        string operationKey,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = ResilienceContextPool.Shared.Get(operationKey, cancellationToken);
        request.SetResilienceContext(context);

        try
        {
            return await _apiClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private async Task<UploadSessionResponse> InitUploadSessionAsync(CancellationToken cancellationToken)
    {
        const string Url = "https://studio.rutube.ru/api/uploader/upload_session/";
        var requestBody = new UploadSessionRequest();
        var jsonPayload = JsonSerializer.Serialize(requestBody, RutubeJsonContext.Default.UploadSessionRequest);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var request = CreateRequest(HttpMethod.Post, Url, content);
        using var response = await SendApiAsync(Operations.InitUploadSession, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.InitSessionFailed(response.StatusCode, err);
            throw new HttpRequestException($"Не удалось инициализировать сессию: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.UploadSessionResponse, cancellationToken);

        if (result == null || string.IsNullOrEmpty(result.Sid) || string.IsNullOrEmpty(result.VideoId))
        {
            _logger.UploadSessionDeserializationFailed();
            throw new InvalidOperationException("Не удалось десериализовать ответ сессии загрузки");
        }

        return result;
    }

    private async Task<string> CreateTusResourceAsync(
        string sessionId,
        string videoId,
        string filePath,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        var url = $"https://u.rutube.ru/upload/{sessionId}";
        using var request = CreateRequest(HttpMethod.Post, url);
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Length", fileInfo.Length.ToString());

        var metadataParts = new List<string>
        {
            $"sessionId {Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionId))}",
            $"videoId {Convert.ToBase64String(Encoding.UTF8.GetBytes(videoId))}",
        };

        if (!string.IsNullOrEmpty(_userId))
        {
            metadataParts.Add($"userId {Convert.ToBase64String(Encoding.UTF8.GetBytes(_userId))}");

            var uploadSessionIdString = $"{fileInfo.Name}::user-{_userId}::{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}";
            metadataParts.Add($"uploadSessionId {Convert.ToBase64String(Encoding.UTF8.GetBytes(uploadSessionIdString))}");
        }

        var metadata = string.Join(",", metadataParts);
        request.Headers.Add("Upload-Metadata", metadata);
        request.Content = new ByteArrayContent([]);
        request.Content.Headers.ContentType = new("application/offset+octet-stream");

        using var response = await SendApiAsync(Operations.CreateTusResource, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.TusResourceFailed(response.StatusCode, err);
            throw new HttpRequestException($"Не удалось создать TUS ресурс: {response.StatusCode}. Ответ: {err}");
        }

        if (response.Headers.Location != null)
        {
            return response.Headers.Location.ToString();
        }

        return url;
    }

    private async Task PerformTusUploadAsync(
        string uploadUrl,
        string filePath,
        long? uploadBytesPerSecond,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using var throttled = new ThrottledStream(fileStream, uploadBytesPerSecond);
        var fileSize = fileStream.Length;

        var byteProgress = progress != null && fileSize > 0
            ? new Progress<long>(bytes => progress.Report(Math.Min(1.0, (double)bytes / fileSize)))
            : null;

        await using var stream = new ProgressStream(throttled, byteProgress);

        _logger.StartingFileUpload(fileSize);

        using var request = CreateRequest(HttpMethod.Patch, uploadUrl);
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Offset", "0");
        request.Content = new StreamContent(stream);
        request.Content.Headers.ContentType = new("application/offset+octet-stream");
        request.Content.Headers.ContentLength = fileSize;

        using var response = await _uploadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.TusUploadFailed(response.StatusCode, err);
            throw new HttpRequestException($"Ошибка TUS загрузки: {response.StatusCode}. Ответ: {err}");
        }

        _logger.FileUploadCompleted(fileSize);
    }

    private async Task UpdateMetadataAsync(
        string videoId,
        string title,
        string description,
        string categoryId,
        bool isHidden,
        CancellationToken cancellationToken)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        var payload = new MetadataUpdateRequest
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? " " : description,
            IsHidden = isHidden,
            IsAdult = false,
            Category = categoryId,
            Properties = new()
            {
                HideComments = false,
            },
        };

        var jsonPayload = JsonSerializer.Serialize(payload, RutubeJsonContext.Default.MetadataUpdateRequest);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var request = CreateRequest(HttpMethod.Patch, url, content);
        using var response = await SendApiAsync(Operations.UpdateMetadata, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.MetadataUpdateFailed(response.StatusCode, err);
            throw new HttpRequestException($"Не удалось обновить метаданные: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var videoDetails = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.VideoDetailsResponse, cancellationToken);
        if (videoDetails != null)
        {
            _logger.MetadataConfirmed(videoDetails.Title, videoDetails.Category.Name, videoDetails.IsHidden);
        }
    }

    private async Task PublishVideoAsync(
        string videoId,
        DateTime publishAt,
        CancellationToken cancellationToken)
    {
        var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        var moscowTime = TimeZoneInfo.ConvertTime(publishAt, moscowTimeZone);

        const string Url = "https://studio.rutube.ru/api/video/publication/?client=vulp";
        var payload = new PublicationRequest
        {
            VideoId = videoId,
            Timestamp = moscowTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            HideVideo = false,
        };

        var jsonPayload = JsonSerializer.Serialize(payload, RutubeJsonContext.Default.PublicationRequest);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        using var request = CreateRequest(HttpMethod.Post, Url, content);
        using var response = await SendApiAsync(Operations.PublishVideo, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.PublishVideoFailed(response.StatusCode, err);
            throw new HttpRequestException($"Не удалось опубликовать видео: {response.StatusCode}. Ответ: {err}");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync(body, RutubeJsonContext.Default.PublicationResponse, cancellationToken);
        if (result != null)
        {
            _logger.PublicationConfirmed(result.VideoId, result.Timestamp);
        }
    }

    private static class Operations
    {
        public const string GetVideo = "rutube.video.get";
        public const string ListVideos = "rutube.video.list";
        public const string DeleteVideo = "rutube.video.delete";
        public const string ListCategories = "rutube.categories.list";
        public const string UploadThumbnail = "rutube.thumbnail.upload";
        public const string InitUploadSession = "rutube.upload-session.init";
        public const string CreateTusResource = "rutube.tus.create";
        public const string UpdateMetadata = "rutube.metadata.update";
        public const string PublishVideo = "rutube.publication.create";
        public const string ListComments = "rutube.comments.list";
        public const string ListReplies = "rutube.comments.replies";
        public const string CreateComment = "rutube.comments.create";
        public const string EditComment = "rutube.comments.edit";
        public const string DeleteComment = "rutube.comments.delete";
        public const string LikeComment = "rutube.comments.like";
        public const string UnlikeComment = "rutube.comments.unlike";
    }
}
