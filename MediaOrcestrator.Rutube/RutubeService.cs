using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Rutube;

public sealed class RutubeService
{
    private readonly HttpClient _httpClient;
    private readonly string? _userId;
    private readonly ILogger _logger;

    public RutubeService(string cookieString, string csrfToken, ILogger<RutubeService> logger)
    {
        _logger = logger;

        var handler = new HttpClientHandler { UseCookies = false };
        _httpClient = new(handler);

        _httpClient.DefaultRequestHeaders.Add("Cookie", cookieString);
        _httpClient.DefaultRequestHeaders.Add("x-csrftoken", csrfToken);
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://studio.rutube.ru/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://studio.rutube.ru");

        var visitorIdMatch = Regex.Match(cookieString, @"visitorID=([^;]+)");
        if (visitorIdMatch.Success)
        {
            _userId = visitorIdMatch.Groups[1].Value;
            //_logger.LogDebug("Получен ID посетителя: {UserId}", _userId);
        }
    }

    public async Task<string> UploadVideoAsync(string filePath, string title, string description, string categoryId)
    {
        _logger.LogInformation("Инициализация сессии загрузки на RuTube");
        var session = await InitUploadSessionAsync();
        _logger.LogInformation("Сессия создана. Session ID: {SessionId}, Video ID: {VideoId}", session.Sid, session.VideoId);

        _logger.LogDebug("Создание TUS ресурса для загрузки");
        var uploadUrl = await CreateTusResourceAsync(session.Sid, session.VideoId, filePath);
        _logger.LogDebug("URL загрузки получен: {UploadUrl}", uploadUrl);

        _logger.LogInformation("Начало загрузки видео данных");
        await PerformTusUploadAsync(uploadUrl, filePath);
        _logger.LogInformation("Загрузка данных завершена");

        _logger.LogDebug("Ожидание обработки на сервере (5 секунд)");
        await Task.Delay(5000);

        _logger.LogInformation("Обновление метаданных видео");
        await UpdateMetadataAsync(session.VideoId, title, description, categoryId);
        _logger.LogInformation("Метаданные обновлены");

        _logger.LogInformation("Публикация видео");
        await PublishVideoAsync(session.VideoId);
        _logger.LogInformation("Видео успешно опубликовано");

        return session.VideoId;
    }

    public async Task<List<CategoryInfo>> GetCategoriesAsync()
    {
        var url = "https://studio.rutube.ru/api/video/category/";
        _logger.LogDebug("Запрос списка категорий RuTube");

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка получения категорий. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
            throw new HttpRequestException($"Не удалось получить категории: {response.StatusCode}. Ответ: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<CategoryInfo>>(body);

        _logger.LogInformation("Получено категорий: {Count}", result?.Count ?? 0);
        return result ?? [];
    }

    public async Task DeleteVideoAsync(string videoId)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        _logger.LogInformation("Отправка DELETE запроса в RuTube API для видео {VideoId}", videoId);

        var response = await _httpClient.DeleteAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // TODO: Возможно не верный мув, но позволяет громоздить логику принудительного удаления
            _logger.LogWarning("Видео {VideoId} не найдено на RuTube, считаем уже удаленным", videoId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Не удалось удалить видео {VideoId}. Статус: {StatusCode}, Ответ: {Response}", videoId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось удалить видео: {response.StatusCode}. Ответ: {err}");
        }

        _logger.LogInformation("Видео {VideoId} успешно удалено через RuTube API", videoId);
    }

    private async Task<UploadSessionResponse> InitUploadSessionAsync()
    {
        var url = "https://studio.rutube.ru/api/uploader/upload_session/";
        var requestBody = new UploadSessionRequest();
        var jsonPayload = JsonSerializer.Serialize(requestBody);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка инициализации сессии. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
            throw new HttpRequestException($"Не удалось инициализировать сессию: {response.StatusCode}. Ответ: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UploadSessionResponse>(body);

        if (result == null || string.IsNullOrEmpty(result.Sid) || string.IsNullOrEmpty(result.VideoId))
        {
            _logger.LogError("Не удалось десериализовать ответ сессии загрузки");
            throw new InvalidOperationException("Не удалось десериализовать ответ сессии загрузки");
        }

        return result;
    }

    private async Task<string> CreateTusResourceAsync(string sessionId, string videoId, string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var url = $"https://u.rutube.ru/upload/{sessionId}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
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
        request.Content = new ByteArrayContent(Array.Empty<byte>());
        request.Content.Headers.ContentType = new("application/offset+octet-stream");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка создания TUS ресурса. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
            throw new HttpRequestException($"Не удалось создать TUS ресурс: {response.StatusCode}. Ответ: {err}");
        }

        if (response.Headers.Location != null)
        {
            return response.Headers.Location.ToString();
        }

        return url;
    }

    // TODO: Костыль
    private async Task PerformTusUploadAsync(string uploadUrl, string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        var fileSize = fileStream.Length;

        _logger.LogDebug("Начало загрузки файла. Размер: {FileSize} байт", fileSize);

        var request = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Offset", "0");
        request.Content = new StreamContent(fileStream);
        request.Content.Headers.ContentType = new("application/offset+octet-stream");
        request.Content.Headers.ContentLength = fileSize;

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка TUS загрузки. Статус: {StatusCode}, Ответ: {Response}",
                response.StatusCode, err);

            throw new HttpRequestException($"Ошибка TUS загрузки: {response.StatusCode}. Ответ: {err}");
        }

        _logger.LogInformation("Загрузка файла завершена. Всего загружено: {TotalBytes} байт", fileSize);
    }

    // private async Task PerformTusUploadAsync(string uploadUrl, string filePath)
    // {
    //     await using var fileStream = File.OpenRead(filePath);
    //     var fileSize = fileStream.Length;
    //     var buffer = new byte[4 * 1024 * 1024];
    //     int bytesRead;
    //     long offset = 0;
    //
    //     _logger.LogDebug("Начало загрузки файла. Размер: {FileSize} байт", fileSize);
    //
    //     while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
    //     {
    //         var chunk = new byte[bytesRead];
    //         Array.Copy(buffer, chunk, bytesRead);
    //
    //         var request = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
    //         request.Headers.Add("Tus-Resumable", "1.0.0");
    //         request.Headers.Add("Upload-Offset", offset.ToString());
    //         request.Content = new ByteArrayContent(chunk);
    //         request.Content.Headers.ContentType = new("application/offset+octet-stream");
    //
    //         var response = await _httpClient.SendAsync(request);
    //
    //         if (!response.IsSuccessStatusCode)
    //         {
    //             var err = await response.Content.ReadAsStringAsync();
    //             _logger.LogError("Ошибка TUS загрузки на смещении {Offset}. Статус: {StatusCode}, Ответ: {Response}",
    //                 offset, response.StatusCode, err);
    //
    //             throw new HttpRequestException($"Ошибка TUS загрузки на смещении {offset}: {response.StatusCode}. Ответ: {err}");
    //         }
    //
    //         offset += bytesRead;
    //         var progress = (double)offset / fileSize;
    //
    //         if (offset % (10 * 1024 * 1024) < bytesRead || offset == fileSize)
    //         {
    //             _logger.LogInformation("Прогресс загрузки: {Offset}/{FileSize} байт ({Progress:P1})", offset, fileSize, progress);
    //         }
    //     }
    //
    //     _logger.LogInformation("Загрузка файла завершена. Всего загружено: {TotalBytes} байт", offset);
    // }

    private async Task UpdateMetadataAsync(string videoId, string title, string description, string categoryId)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        var payload = new MetadataUpdateRequest
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? " " : description,
            IsHidden = false,
            IsAdult = false,
            Category = categoryId,
            Properties = new()
            {
                HideComments = false,
            },
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка обновления метаданных. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
            throw new HttpRequestException($"Не удалось обновить метаданные: {response.StatusCode}. Ответ: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var videoDetails = JsonSerializer.Deserialize<VideoDetailsResponse>(body);
        if (videoDetails != null)
        {
            _logger.LogDebug("Метаданные подтверждены. Название: '{Title}', Категория: '{Category}'",
                videoDetails.Title, videoDetails.Category.Name);
        }
    }

    private async Task PublishVideoAsync(string videoId)
    {
        var url = "https://studio.rutube.ru/api/video/publication/?client=vulp";
        var payload = new PublicationRequest
        {
            VideoId = videoId,
            Timestamp = DateTime.Now.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss"), // TODO
            HideVideo = true,
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка публикации видео. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
            throw new HttpRequestException($"Не удалось опубликовать видео: {response.StatusCode}. Ответ: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PublicationResponse>(body);
        if (result != null)
        {
            _logger.LogDebug("Публикация подтверждена. Video ID: {VideoId}, Запланировано: {Timestamp}",
                result.VideoId, result.Timestamp);
        }
    }
}
