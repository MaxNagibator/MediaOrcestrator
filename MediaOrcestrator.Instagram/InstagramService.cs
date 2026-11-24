using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MediaOrcestrator.Instagram;

public sealed class InstagramService(HttpClient httpClient, IOptions<InstagramOptions> options)
{
    private readonly InstagramOptions _options = options.Value;

    public async Task<string> ValidateAccountAsync(
        string apiVersion,
        string instagramUserId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var uri = $"https://graph.facebook.com/{apiVersion}/{Uri.EscapeDataString(instagramUserId)}" +
                  $"?fields=id,username&access_token={Uri.EscapeDataString(accessToken)}";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        using var json = await ReadJsonAsync(response, cancellationToken);
        return json.RootElement.TryGetProperty("username", out var username)
            ? username.GetString() ?? instagramUserId
            : instagramUserId;
    }

    public async Task<string> PublishReelAsync(
        string apiVersion,
        string instagramUserId,
        string accessToken,
        string filePath,
        string caption,
        bool shareToFeed,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var containerId = await CreateContainerAsync(
            apiVersion, instagramUserId, accessToken, caption, shareToFeed, cancellationToken);

        await UploadFileAsync(apiVersion, containerId, accessToken, filePath, progress, cancellationToken);
        await WaitUntilReadyAsync(apiVersion, containerId, accessToken, cancellationToken);
        return await PublishContainerAsync(apiVersion, instagramUserId, containerId, accessToken, cancellationToken);
    }

    private async Task<string> CreateContainerAsync(
        string apiVersion,
        string instagramUserId,
        string accessToken,
        string caption,
        bool shareToFeed,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["media_type"] = "REELS",
            ["upload_type"] = "resumable",
            ["caption"] = caption,
            ["share_to_feed"] = shareToFeed ? "true" : "false",
            ["access_token"] = accessToken,
        });
        var uri = $"https://graph.facebook.com/{apiVersion}/{Uri.EscapeDataString(instagramUserId)}/media";
        using var response = await httpClient.PostAsync(uri, content, cancellationToken);
        using var json = await ReadJsonAsync(response, cancellationToken);
        return RequiredString(json.RootElement, "id");
    }

    private async Task UploadFileAsync(
        string apiVersion,
        string containerId,
        string accessToken,
        string filePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo(filePath);
        await using var stream = file.OpenRead();
        await using var progressStream = new ProgressReadStream(stream, file.Length, progress);
        using var content = new StreamContent(progressStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://rupload.facebook.com/ig-api-upload/{apiVersion}/{Uri.EscapeDataString(containerId)}")
        {
            Content = content,
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"OAuth {accessToken}");
        request.Headers.TryAddWithoutValidation("offset", "0");
        request.Headers.TryAddWithoutValidation("file_size", file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        using var _ = await ReadJsonAsync(response, cancellationToken);
        progress?.Report(1);
    }

    private async Task WaitUntilReadyAsync(
        string apiVersion,
        string containerId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + _options.ProcessingTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var uri = $"https://graph.facebook.com/{apiVersion}/{Uri.EscapeDataString(containerId)}" +
                      $"?fields=status_code,status&access_token={Uri.EscapeDataString(accessToken)}";
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            using var json = await ReadJsonAsync(response, cancellationToken);
            var status = RequiredString(json.RootElement, "status_code");

            if (status is "FINISHED" or "PUBLISHED")
            {
                return;
            }

            if (status is "ERROR" or "EXPIRED")
            {
                var details = json.RootElement.TryGetProperty("status", out var value) ? value.GetString() : null;
                throw new InvalidOperationException($"Instagram не обработал Reel: {status}. {details}".Trim());
            }

            await Task.Delay(_options.ProcessingPollInterval, cancellationToken);
        }

        throw new TimeoutException("Instagram не закончил обработку Reel за отведённое время.");
    }

    private async Task<string> PublishContainerAsync(
        string apiVersion,
        string instagramUserId,
        string containerId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["creation_id"] = containerId,
            ["access_token"] = accessToken,
        });
        var uri = $"https://graph.facebook.com/{apiVersion}/{Uri.EscapeDataString(instagramUserId)}/media_publish";
        using var response = await httpClient.PostAsync(uri, content, cancellationToken);
        using var json = await ReadJsonAsync(response, cancellationToken);
        return RequiredString(json.RootElement, "id");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = body;
            try
            {
                using var errorJson = JsonDocument.Parse(body);
                if (errorJson.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var errorMessage))
                {
                    message = errorMessage.GetString() ?? body;
                }
            }
            catch (JsonException)
            {
            }

            throw new InstagramApiException(response.StatusCode, $"Instagram API: {message}");
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Instagram API вернул некорректный JSON.", ex);
        }
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.GetString() is { Length: > 0 } value)
        {
            return value;
        }

        throw new InvalidOperationException($"Instagram API не вернул поле {propertyName}.");
    }

    private sealed class ProgressReadStream(Stream inner, long length, IProgress<double>? progress) : Stream
    {
        private long _read;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            Report(read);
            return read;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            Report(read);
            return read;
        }
        private void Report(int count)
        {
            _read += count;
            if (length > 0) progress?.Report(Math.Min(1, (double)_read / length));
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
