using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Rutube;

public sealed partial class RutubeService
{
    private readonly HttpClient _httpClient;
    private readonly string? _userId;
    private readonly ILogger _logger;

    public RutubeService(string cookieString, string csrfToken, ILogger<RutubeService> logger)
    {
        _logger = logger;

        var handler = new HttpClientHandler { UseCookies = false };
        _httpClient = new(handler);
        _httpClient.Timeout = TimeSpan.FromHours(8);
        _httpClient.DefaultRequestHeaders.Add("Cookie", cookieString);
        _httpClient.DefaultRequestHeaders.Add("x-csrftoken", csrfToken);
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://studio.rutube.ru/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://studio.rutube.ru");

        var visitorIdMatch = VisitorIdRegex().Match(cookieString);
        if (visitorIdMatch.Success)
        {
            _userId = visitorIdMatch.Groups[1].Value;
        }
    }

    public async Task<UploadResult> UploadVideoAsync(
        string? videoId,
        string filePath,
        string title,
        string description,
        string categoryId,
        string? thumbnailPath = null,
        DateTime? publishAt = null,
        long? uploadBytesPerSecond = null,
        IProgress<double>? uploadProgress = null)
    {
        var isNewVideo = false;
        if (videoId == null)
        {
            isNewVideo = true;
            _logger.LogInformation("Инициализация сессии загрузки на RuTube");
            var session = await InitUploadSessionAsync();
            _logger.LogInformation("Сессия создана. Session ID: {SessionId}, Video ID: {VideoId}", session.Sid, session.VideoId);

            _logger.LogDebug("Создание TUS ресурса для загрузки");
            var uploadUrl = await CreateTusResourceAsync(session.Sid, session.VideoId, filePath);
            _logger.LogDebug("URL загрузки получен: {UploadUrl}", uploadUrl);

            _logger.LogInformation("Начало загрузки видео данных");
            await PerformTusUploadAsync(uploadUrl, filePath, uploadBytesPerSecond, uploadProgress);
            _logger.LogInformation("Загрузка данных завершена");

            _logger.LogDebug("Ожидание обработки на сервере (5 секунд)");
            await Task.Delay(5000);

            _logger.LogInformation("Обновление метаданных видео (черновик)");
            await UpdateMetadataAsync(session.VideoId, title, description, categoryId, true);
            _logger.LogInformation("Метаданные обновлены");
            videoId = session.VideoId;
        }

        string? errorMessage = null;
        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
        {
            try
            {
                _logger.LogInformation("Загрузка превью");

                //throw new Exception("basdasd");
                string thumbnailUrl;
                if (Path.GetExtension(thumbnailPath).Equals(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _logger.LogInformation("Обнаружен WebP формат, конвертируем в JPG...");

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

                            using (var fileStream = new FileStream(tempJpgPath, FileMode.Create))
                            {
                                await image.SaveAsync(fileStream, encoder);
                            }
                        }

                        _logger.LogInformation("Конвертация завершена, загружаем JPG");
                        thumbnailUrl = await UploadThumbnailAsync(videoId, tempJpgPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при конвертации WebP в JPG, пробуем загрузить оригинал");
                        thumbnailUrl = await UploadThumbnailAsync(videoId, thumbnailPath);
                    }
                }
                else
                {
                    thumbnailUrl = await UploadThumbnailAsync(videoId, thumbnailPath);
                }

                _logger.LogInformation("Превью загружено: {ThumbnailUrl}", thumbnailUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки превьюшки");
                errorMessage += "Ошибка загрузки превьюшки";
            }
        }

        if (isNewVideo)
        {
            // todo код с душком
            try
            {
                if (publishAt.HasValue && publishAt.Value > DateTime.Now)
                {
                    _logger.LogInformation("Планирование публикации на {PublishAt}", publishAt.Value);
                    await PublishVideoAsync(videoId, publishAt.Value);
                    _logger.LogInformation("Видео успешно запланировано");
                }
                else
                {
                    _logger.LogInformation("Немедленная публикация видео");
                    await UpdateMetadataAsync(videoId, title, description, categoryId, false);
                    _logger.LogInformation("Видео успешно опубликовано");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка публикации");
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

    public async Task<VideoDetailsResponse> GetVideoByIdAsync(string videoId)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        _logger.LogDebug("Запрос информации о видео {VideoId}", videoId);

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка получения видео {VideoId}. Статус: {StatusCode}, Ответ: {Response}", videoId, response.StatusCode, err);
            throw new HttpRequestException($"Не удалось получить видео {videoId}: {response.StatusCode}. Ответ: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<VideoDetailsResponse>(body)
                     ?? throw new InvalidOperationException($"Не удалось десериализовать ответ для видео {videoId}");

        _logger.LogInformation("Получена информация о видео. Название: '{Title}'", result.Title);
        return result;
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

        if (response.StatusCode == HttpStatusCode.NotFound)
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

    public async Task<string?> UploadThumbnailAsync(string videoId, string thumbnailPath)
    {
        if (!File.Exists(thumbnailPath))
        {
            throw new FileNotFoundException($"Файл превью не найден: {thumbnailPath}");
        }

        var url = $"https://studio.rutube.ru/api/video/{videoId}/thumbnail/?client=vulp";
        _logger.LogDebug("Загрузка превью для видео {VideoId} из {ThumbnailPath}", videoId, thumbnailPath);

        using var form = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(thumbnailPath);
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

        var response = await _httpClient.PostAsync(url, form);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка загрузки превью. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
            throw new HttpRequestException($"Не удалось загрузить превью: {response.StatusCode}. Ответ: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ThumbnailResponse>(body);

        return result?.ThumbnailUrl;
    }

    public async IAsyncEnumerable<GetVideoApiItem> GetVideoAsync()
    {
        var url = "https://studio.rutube.ru/api/v2/video/person/?ordering=-calculated_date&limit=400&page=1";
        while (true)
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ошибка получения видео. Статус: {StatusCode}, Ответ: {Response}", response.StatusCode, err);
                throw new HttpRequestException($"Не удалось опубликовать видео: {response.StatusCode}. Ответ: {err}");
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetVideoApiResponse>(body);
            foreach (var v in result.Results)
            {
                yield return v;
            }

            if (result.HasNext)
            {
                url = result.Next;
            }
            else
            {
                yield break;
            }
        }
    }

    [GeneratedRegex(@"visitorID=([^;]+)")]
    private static partial Regex VisitorIdRegex();

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
    private async Task PerformTusUploadAsync(string uploadUrl, string filePath, long? uploadBytesPerSecond = null, IProgress<double>? progress = null)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using var throttled = new ThrottledStream(fileStream, uploadBytesPerSecond);
        var fileSize = fileStream.Length;

        var byteProgress = progress != null && fileSize > 0
            ? new Progress<long>(bytes => progress.Report(Math.Min(1.0, (double)bytes / fileSize)))
            : null;

        await using var stream = new ProgressStream(throttled, byteProgress);

        _logger.LogDebug("Начало загрузки файла. Размер: {FileSize} байт", fileSize);

        var request = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Offset", "0");
        request.Content = new StreamContent(stream);
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

    private async Task UpdateMetadataAsync(string videoId, string title, string description, string categoryId, bool isHidden)
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
            _logger.LogDebug("Метаданные подтверждены. Название: '{Title}', Категория: '{Category}', Скрыто: {IsHidden}",
                videoDetails.Title, videoDetails.Category.Name, videoDetails.IsHidden);
        }
    }

    private async Task PublishVideoAsync(string videoId, DateTime publishAt)
    {
        var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        var moscowTime = TimeZoneInfo.ConvertTime(publishAt, moscowTimeZone);

        var url = "https://studio.rutube.ru/api/video/publication/?client=vulp";
        var payload = new PublicationRequest
        {
            VideoId = videoId,
            Timestamp = moscowTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            HideVideo = false,
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
