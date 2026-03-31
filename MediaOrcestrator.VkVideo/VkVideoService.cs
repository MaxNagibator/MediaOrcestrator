using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace MediaOrcestrator.VkVideo;

public sealed class VkVideoService : IDisposable
{
    private const string ApiBase = "https://api.vkvideo.ru/method/";
    private const string ApiVkBase = "https://api.vk.com/method/";
    private const string ApiVersion = "5.274";
    private const string ClientId = "52461373";
    private const string OwnerIdKey = "owner_id";
    private const string VideoIdKey = "video_id";

    private readonly ILogger _logger;
    private readonly string _cookieString;

    private string? _accessToken;
    private DateTimeOffset _tokenExpires = DateTimeOffset.MinValue;

    public VkVideoService(string cookieString, ILogger logger)
    {
        _logger = logger;
        _cookieString = cookieString;

        var handler = new HttpClientHandler { UseCookies = false };
        HttpClient = new(handler);
        HttpClient.Timeout = TimeSpan.FromHours(8);
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
    }

    public HttpClient HttpClient { get; }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpires.AddMinutes(-1))
        {
            return _accessToken;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            _logger.LogInformation("Запрос нового access_token через web_token");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://cabinet.vkvideo.ru/al_video.php?act=web_token");
            request.Headers.Add("Cookie", _cookieString);
            request.Headers.Add("Origin", "https://cabinet.vkvideo.ru");
            request.Headers.Add("Referer", "https://cabinet.vkvideo.ru/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["version"] = "1",
                ["app_id"] = ClientId,
            });

            var response = await HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!IsHtmlResponse(body))
            {
                return ParseWebTokenResponse(body, response.StatusCode);
            }

            _logger.LogWarning("Ответ web_token — HTML (попытка {Attempt}/2). Статус: {StatusCode}", attempt + 1, response.StatusCode);

            if (attempt == 0 && await TrySolveChallengeAsync(response, body))
            {
                _logger.LogInformation("Повторяем запрос web_token после решения challenge...");
                continue;
            }

            ThrowHtmlResponseError(body, "web_token");
        }

        throw new InvalidOperationException("Не удалось получить access_token после решения challenge.");
    }

    private Task<T> CallApiAsync<T>(string method, Dictionary<string, string> parameters)
    {
        return CallApiInternalAsync<T>(ApiBase, method, parameters);
    }

    private Task<T> CallVkApiAsync<T>(string method, Dictionary<string, string> parameters)
    {
        return CallApiInternalAsync<T>(ApiVkBase, method, parameters);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }

    public async Task<(List<VideoItem> Videos, string? SectionId, string? NextFrom)> GetCatalogFirstPageAsync(long groupId)
    {
        var catalogUrl = $"https://vkvideo.ru/@club{groupId}/all";
        _logger.LogDebug("Запрос каталога: {Url}", catalogUrl);

        CatalogResponse response;

        try
        {
            response = await CallApiAsync<CatalogResponse>("catalog.getVideo", new()
            {
                ["need_blocks"] = "1",
                [OwnerIdKey] = "0",
                ["url"] = catalogUrl,
            });
        }
        catch (VkApiException ex) when (ex.ErrorCode == 104)
        {
            _logger.LogWarning(ex, "Каталог видео пуст или не найден для группы {GroupId}", groupId);
            return ([], null, null);
        }

        var (sectionId, nextFrom) = FindVideosSectionBlock(response.Catalog.Sections);

        return (response.Videos, sectionId, nextFrom);
    }

    public async Task<(List<VideoItem> Videos, string? NextFrom)> GetCatalogNextPageAsync(string sectionId, string startFrom)
    {
        _logger.LogDebug("Запрос следующей страницы каталога: section={SectionId}, from={StartFrom}", sectionId, startFrom);

        var response = await CallApiAsync<CatalogSectionResponse>("catalog.getSection", new()
        {
            ["section_id"] = sectionId,
            ["start_from"] = startFrom,
        });

        string? nextFrom = null;
        if (response.Section.Blocks == null)
        {
            return (response.Videos, nextFrom);
        }

        foreach (var block in response.Section.Blocks)
        {
            if (block.DataType != "videos")
            {
                continue;
            }

            nextFrom = block.NextFrom;
            break;
        }

        return (response.Videos, nextFrom);
    }

    public async Task<VideoItem?> GetVideoByIdAsync(long ownerId, long videoId)
    {
        var videoKey = $"{ownerId}_{videoId}";
        _logger.LogDebug("Запрос деталей видео: {VideoKey}", videoKey);

        var response = await CallVkApiAsync<VideoGetByIdsResponse>("video.getByIds", new()
        {
            ["videos"] = videoKey,
            ["video_fields"] = "files,image,subtitles",
        });

        return response.Items.Count > 0 ? response.Items[0] : null;
    }

    public async Task EditVideoAsync(long ownerId, long videoId, string title, string description)
    {
        _logger.LogInformation("Редактирование видео {OwnerId}_{VideoId}", ownerId, videoId);

        var result = await CallApiAsync<EditResponse>("video.edit", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
            ["name"] = title,
            ["desc"] = description,
        });

        if (result.Success != 1)
        {
            throw new InvalidOperationException($"Ошибка редактирования видео {ownerId}_{videoId}");
        }

        _logger.LogInformation("Видео {OwnerId}_{VideoId} успешно отредактировано", ownerId, videoId);
    }

    public async Task DeleteVideoAsync(long ownerId, long videoId)
    {
        _logger.LogInformation("Удаление видео {OwnerId}_{VideoId}", ownerId, videoId);

        await CallApiAsync<int>("video.delete", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
        });

        _logger.LogInformation("Видео {OwnerId}_{VideoId} удалено", ownerId, videoId);
    }

    public async Task<string> GetThumbUploadUrlAsync(long ownerId, long videoId)
    {
        var response = await CallApiAsync<VideoForEditResponse>("video.getVideoForEdit", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
        });

        return response.Item.ThumbUploadUrl
               ?? throw new InvalidOperationException("thumb_upload_url не получен");
    }

    public async Task UploadThumbnailAsync(long ownerId, long videoId, string thumbnailPath)
    {
        var thumbUploadUrl = await GetThumbUploadUrlAsync(ownerId, videoId);

        _logger.LogInformation("Загрузка превью для видео {OwnerId}_{VideoId}", ownerId, videoId);

        using var form = new MultipartFormDataContent();
        var fileStream = File.OpenRead(thumbnailPath);
        var fileContent = new StreamContent(fileStream);

        var extension = Path.GetExtension(thumbnailPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "image/jpeg",
        };

        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "photo", Path.GetFileName(thumbnailPath));

        var uploadUrl = thumbUploadUrl + "&ajx=1";
        var uploadResponse = await HttpClient.PostAsync(uploadUrl, form);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();

        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ошибка загрузки превью: {uploadResponse.StatusCode}");
        }

        var saveResult = await CallApiAsync<SaveThumbResponse>("video.saveUploadedThumb", new()
        {
            [OwnerIdKey] = ownerId.ToString(),
            [VideoIdKey] = videoId.ToString(),
            ["thumb_json"] = uploadBody,
            ["thumb_size"] = "l",
        });

        _logger.LogInformation("Превью загружено. PhotoId: {PhotoId}", saveResult.PhotoId);
    }

    public async Task<PublishVideoResponse> UploadVideoAsync(
        long groupId,
        string filePath,
        string title,
        string description,
        string fileExtension,
        long? publishAt = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        _logger.LogInformation("Шаг 1/3: Резервирование слота для '{Title}'", title);

        var saveResponse = await CallApiAsync<VideoSaveResponse>("video.save", new()
        {
            ["group_id"] = groupId.ToString(),
            ["source_file_name"] = fileInfo.Name,
            ["file_size"] = fileInfo.Length.ToString(),
            ["batch_id"] = $"-{groupId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            ["preview"] = "1",
            ["thumb_upload"] = "1",
        });

        _logger.LogInformation("Слот зарезервирован. VideoId: {VideoId}, OwnerId: {OwnerId}", saveResponse.VideoId, saveResponse.OwnerId);

        _logger.LogInformation("Шаг 2/3: Загрузка файла ({Size} байт)", fileInfo.Length);

        var uploadResponse = await UploadFileAsync(saveResponse.UploadUrl, filePath, fileInfo, cancellationToken);

        _logger.LogInformation("Файл загружен. Hash: {Hash}", uploadResponse.VideoHash);

        _logger.LogInformation("Шаг 3/3: Публикация");

        var publishParams = new Dictionary<string, string>
        {
            ["owner_id"] = saveResponse.OwnerId.ToString(),
            ["video_id"] = saveResponse.VideoId.ToString(),
            ["title"] = title,
            ["description"] = description,
            ["file_ext"] = fileExtension,
            ["repeat"] = "0",
            ["hide_auto_subs"] = "0",
            ["add_to_wall"] = "0",
            ["check_content_id"] = "0",
        };

        if (publishAt.HasValue)
        {
            publishParams["publish_at"] = publishAt.Value.ToString();
        }

        var publishResponse = await CallApiAsync<PublishResponse>("video.publish", publishParams);

        _logger.LogInformation("Видео опубликовано: {Url}", publishResponse.Video?.DirectUrl);

        return publishResponse.Video
               ?? throw new InvalidOperationException("Ответ video.publish не содержит объект video");
    }

    private static bool IsHtmlResponse(string body)
    {
        return string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith('<');
    }

    private static (string? SectionId, string? NextFrom) FindVideosSectionBlock(List<CatalogSection>? sections)
    {
        if (sections == null)
        {
            return (null, null);
        }

        foreach (var section in sections)
        {
            var nextFrom = section.NextFrom ?? FindVideosBlockNextFrom(section.Blocks);
            if (nextFrom != null)
            {
                return (section.Id, nextFrom);
            }
        }

        return (null, null);
    }

    private static string? FindVideosBlockNextFrom(List<CatalogBlock>? blocks)
    {
        if (blocks == null)
        {
            return null;
        }

        foreach (var block in blocks)
        {
            if (block.DataType == "videos")
            {
                return block.NextFrom;
            }
        }

        return null;
    }

    private string ParseWebTokenResponse(string body, HttpStatusCode statusCode)
    {
        if ((int)statusCode >= 400)
        {
            _logger.LogError("Ошибка получения web_token. Статус: {StatusCode}, Ответ: {Body}", statusCode, body);
            throw new HttpRequestException($"Не удалось получить access_token: {statusCode}");
        }

        var tokens = JsonSerializer.Deserialize<List<WebTokenResponse>>(body);
        if (tokens == null || tokens.Count == 0 || string.IsNullOrEmpty(tokens[0].AccessToken))
        {
            throw new InvalidOperationException("Не удалось получить access_token. Возможно, cookies устарели — переавторизуйтесь через Playwright.");
        }

        _accessToken = tokens[0].AccessToken;
        _tokenExpires = DateTimeOffset.FromUnixTimeSeconds(tokens[0].Expires);
        _logger.LogInformation("access_token получен, истекает в {Expires}", _tokenExpires);

        return _accessToken;
    }

    private async Task<T> CallApiInternalAsync<T>(string baseUrl, string method, Dictionary<string, string> parameters)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var token = await GetAccessTokenAsync();

            var requestParams = new Dictionary<string, string>(parameters)
            {
                ["access_token"] = token,
            };

            var url = $"{baseUrl}{method}?v={ApiVersion}&client_id={ClientId}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Origin", "https://cabinet.vkvideo.ru");
            request.Headers.Add("Referer", "https://cabinet.vkvideo.ru/");
            request.Content = new FormUrlEncodedContent(requestParams);

            var response = await HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (IsHtmlResponse(body))
            {
                _logger.LogWarning("Ответ API {Method} — HTML (попытка {Attempt}/2). Статус: {StatusCode}",
                    method, attempt + 1, response.StatusCode);

                if (attempt == 0 && await TrySolveChallengeAsync(response, body))
                {
                    _logger.LogInformation("Повторяем запрос API {Method} после решения challenge...", method);
                    continue;
                }

                ThrowHtmlResponseError(body, method);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ошибка API {Method}. Статус: {StatusCode}, Ответ: {Body}", method, response.StatusCode, body);
                throw new HttpRequestException($"Ошибка API {method}: {response.StatusCode}");
            }

            return ParseApiResponse<T>(body, method);
        }

        throw new InvalidOperationException($"Не удалось выполнить API запрос {method} после решения challenge.");
    }

    private T ParseApiResponse<T>(string body, string method)
    {
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("error", out var errorElement))
        {
            var errorCode = errorElement.TryGetProperty("error_code", out var code) ? code.GetInt32() : 0;
            var errorMsg = errorElement.TryGetProperty("error_msg", out var msg) ? msg.GetString() : body;
            _logger.LogError("Ошибка VK API {Method}. Код: {ErrorCode}, Сообщение: {ErrorMsg}", method, errorCode, errorMsg);
            throw new VkApiException(errorCode, errorMsg ?? body, method);
        }

        if (!doc.RootElement.TryGetProperty("response", out var responseElement))
        {
            throw new InvalidOperationException($"Ответ API {method} не содержит поле 'response'");
        }

        return JsonSerializer.Deserialize<T>(responseElement.GetRawText())
               ?? throw new InvalidOperationException($"Не удалось десериализовать ответ API {method}");
    }

    // TODO: Копипаста + нейросетевая дрисня. Дай бог будет работать.
    /// <summary>
    /// Пытается решить VK challenge (rate limit). Извлекает hash429 из redirect URL,
    /// вычисляет key и отправляет решение. Возвращает true если challenge решён.
    /// </summary>
    private async Task<bool> TrySolveChallengeAsync(HttpResponseMessage response, string body)
    {
        if (!VkChallengeSolver.IsChallengePage(body))
        {
            _logger.LogDebug("Ответ — HTML, но не challenge-страница");
            return false;
        }

        var challengeUri = response.RequestMessage?.RequestUri;
        _logger.LogWarning("VK challenge (rate limit) обнаружен. Redirect URI: {Uri}", challengeUri);

        var result = VkChallengeSolver.TrySolve(challengeUri, body);
        if (!result.Success)
        {
            _logger.LogError("Не удалось решить VK challenge: {Error}. URI: {Uri}", result.Error, challengeUri);
            return false;
        }

        _logger.LogInformation("VK challenge решён: hash429={Hash429}, salt={Salt}, key={Key}",
            result.Hash429, result.Salt, result.Key);

        _logger.LogDebug("Отправка решения на {SolveUri}", result.SolveUri);

        using var solveRequest = new HttpRequestMessage(HttpMethod.Get, result.SolveUri);
        solveRequest.Headers.Add("Cookie", _cookieString);
        var solveResponse = await HttpClient.SendAsync(solveRequest);
        var solveBody = await solveResponse.Content.ReadAsStringAsync();

        _logger.LogInformation("Ответ на решение challenge: статус {StatusCode}, длина тела {Length}",
            solveResponse.StatusCode, solveBody.Length);

        if (IsHtmlResponse(solveBody))
        {
            // TODO: Нужно отловить и посмотреть, что там
            _logger.LogWarning("Ответ на решение challenge снова HTML. Начало: {Body}", solveBody.Length > 200 ? solveBody[..200] : solveBody);
            return false;
        }

        return true;
    }

    private async Task<FileUploadResponse> UploadFileAsync(string uploadUrl, string filePath, FileInfo fileInfo, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        var content = new StreamContent(fileStream);

        var mimeType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "application/octet-stream",
        };

        content.Headers.ContentType = new(mimeType);
        content.Headers.ContentLength = fileInfo.Length;
        content.Headers.ContentRange = new(0, fileInfo.Length - 1, fileInfo.Length);
        content.Headers.ContentDisposition = new("attachment")
        {
            FileName = fileInfo.Name,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Content = content;
        request.Headers.Add("Session-ID", Guid.NewGuid().ToString("N")[..14]);
        request.Headers.Add("X-Uploading-Mode", "parallel");
        request.Headers.Add("Origin", "https://cabinet.vkvideo.ru");

        var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ошибка загрузки файла. Статус: {StatusCode}, Ответ: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Ошибка загрузки файла: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<FileUploadResponse>(body)
               ?? throw new InvalidOperationException("Не удалось десериализовать ответ upload");
    }

    [DoesNotReturn]
    private void ThrowHtmlResponseError(string body, string context)
    {
        var reason = VkChallengeSolver.IsChallengePage(body)
            ? "сервер требует прохождение капчи (rate limit), автоматическое решение не удалось"
            : "сервер вернул HTML вместо JSON. Cookies устарели — переавторизуйтесь через Playwright";

        _logger.LogError("Ответ {Context} — HTML вместо JSON. Начало: {Body}",
            context, body.Length > 200 ? body[..200] : body);

        throw new InvalidOperationException($"Не удалось выполнить {context}: {reason}.");
    }
}
