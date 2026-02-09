using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.Rutube;

public sealed class RutubeService
{
    private readonly HttpClient _httpClient;
    private readonly string? _userId;
    private readonly bool _debug;

    public RutubeService(string cookieString, string csrfToken, bool debug = false)
    {
        _debug = debug;
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
        }
    }

    public async Task<string> UploadVideoAsync(string filePath, string title, string description, string categoryId)
    {
        // todo логгер сюда по братски
        Console.WriteLine("Initiating upload session...");
        var session = await InitUploadSessionAsync();
        Console.WriteLine($"Session ID: {session.Sid}, Video ID: {session.VideoId}");

        Console.WriteLine("Creating TUS upload resource...");
        var uploadUrl = await CreateTusResourceAsync(session.Sid, session.VideoId, filePath);
        Console.WriteLine($"Upload URL: {uploadUrl}");

        Console.WriteLine("Uploading video data...");
        await PerformTusUploadAsync(uploadUrl, filePath);
        Console.WriteLine("Data upload complete.");

        Console.WriteLine("Waiting 5 seconds for server-side processing...");
        await Task.Delay(5000);

        Console.WriteLine("Updating metadata...");
        await UpdateMetadataAsync(session.VideoId, title, description, categoryId);
        Console.WriteLine("Metadata updated.");

        Console.WriteLine("Publishing video...");
        await PublishVideoAsync(session.VideoId);
        Console.WriteLine("Video published successfully.");

        return session.Sid;
    }

    public async Task<List<CategoryInfo>> GetCategoriesAsync()
    {
        var url = "https://studio.rutube.ru/api/video/category/";
        LogRequest("GET", url);

        var response = await _httpClient.GetAsync(url);
        await LogResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get categories failed: {response.StatusCode}. Body: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<CategoryInfo>>(body);

        return result ?? [];
    }

    private void LogRequest(string method, string url, HttpRequestHeaders? headers = null, string? body = null)
    {
        if (!_debug)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n>>> [{method}] {url}");
        Console.ResetColor();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
            }
        }

        if (!string.IsNullOrEmpty(body))
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    Body: {body}");
        }

        Console.ResetColor();
    }

    private async Task LogResponse(HttpResponseMessage response)
    {
        if (!_debug)
        {
            return;
        }

        var statusColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = statusColor;
        Console.WriteLine($"<<< [{(int)response.StatusCode} {response.StatusCode}]");
        Console.ResetColor();

        foreach (var header in response.Headers)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    {header.Key}: {string.Join(", ", header.Value)}");
        }

        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(body);
                var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("    Body (JSON):");
                Console.WriteLine(prettyJson);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"    Body: {body}");
            }
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    private async Task<UploadSessionResponse> InitUploadSessionAsync()
    {
        var url = "https://studio.rutube.ru/api/uploader/upload_session/";
        var requestBody = new UploadSessionRequest();
        var jsonPayload = JsonSerializer.Serialize(requestBody);
        LogRequest("POST", url, body: jsonPayload);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        await LogResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Init session failed: {response.StatusCode}. Body: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UploadSessionResponse>(body);

        if (result == null || string.IsNullOrEmpty(result.Sid) || string.IsNullOrEmpty(result.VideoId))
        {
            throw new("Failed to deserialize upload session response.");
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

        LogRequest("POST", url, request.Headers, $"Metadata: {metadata}");
        var response = await _httpClient.SendAsync(request);
        await LogResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"TUS Create failed: {response.StatusCode}. Body: {err}");
        }

        if (response.Headers.Location != null)
        {
            return response.Headers.Location.ToString();
        }

        return url;
    }

    private async Task PerformTusUploadAsync(string uploadUrl, string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        var fileSize = fileStream.Length;
        var buffer = new byte[4 * 1024 * 1024];
        int bytesRead;
        long offset = 0;

        while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
        {
            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);

            var request = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
            request.Headers.Add("Tus-Resumable", "1.0.0");
            request.Headers.Add("Upload-Offset", offset.ToString());
            request.Content = new ByteArrayContent(chunk);
            request.Content.Headers.ContentType = new("application/offset+octet-stream");

            LogRequest("PATCH", uploadUrl, request.Headers, $"Chunk: {bytesRead} bytes");
            var response = await _httpClient.SendAsync(request);
            await LogResponse(response);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"TUS Patch failed at offset {offset}: {response.StatusCode}. Body: {err}");
            }

            offset += bytesRead;
            Console.WriteLine($"Progress: {offset}/{fileSize} bytes ({(double)offset / fileSize:P1})");
        }
    }

    private async Task UpdateMetadataAsync(string videoId, string title, string description, string categoryId)
    {
        var url = $"https://studio.rutube.ru/api/v2/video/{videoId}/?client=vulp";
        var payload = new MetadataUpdateRequest
        {
            Title = title,
            Description = description,
            IsHidden = false,
            IsAdult = false,
            Category = categoryId,
            Properties = new()
            {
                HideComments = false,
            },
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        LogRequest("PATCH", url, body: jsonPayload);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };

        var response = await _httpClient.SendAsync(request);
        await LogResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Update metadata failed: {response.StatusCode}. Body: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var videoDetails = JsonSerializer.Deserialize<VideoDetailsResponse>(body);
        if (videoDetails != null)
        {
            Console.WriteLine($"Confirmed video title: {videoDetails.Title}, Category: {videoDetails.Category.Name}");
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
        LogRequest("POST", url, body: jsonPayload);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        await LogResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Publish failed: {response.StatusCode}. Body: {err}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PublicationResponse>(body);
        if (result != null)
        {
            Console.WriteLine($"Publication confirmed. Video: {result.VideoId}, Scheduled: {result.Timestamp}");
        }
    }
}
