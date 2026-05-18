using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TL;
using WTelegram;

namespace MediaOrcestrator.Telegram;

public sealed class TelegramChannel(
    ILogger<TelegramChannel> logger,
    ITelegramServiceFactory telegramServiceFactory,
    IToolPathProvider toolPathProvider)
    : ISourceType, IAuthenticatable, IToolConsumer
{
    private readonly ConcurrentDictionary<string, CachedService> _serviceCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public string Name => "Telegram";

    public SyncDirection ChannelType => SyncDirection.Full;

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "api_id",
            IsRequired = true,
            Title = "API ID",
            Description = "API ID приложения с my.telegram.org",
        },
        new()
        {
            Key = "api_hash",
            IsRequired = true,
            Title = "API Hash",
            Description = "API Hash приложения с my.telegram.org",
        },
        new()
        {
            Key = "phone_number",
            IsRequired = true,
            Title = "номер телефона",
            Description = "Номер телефона аккаунта Telegram (формат: +7XXXXXXXXXX)",
        },
        new()
        {
            Key = "channel",
            IsRequired = true,
            Title = "канал",
            Description = "Username канала (@channel) или числовой ID",
        },
        new()
        {
            Key = "speed_limit",
            IsRequired = false,
            Title = "ограничение скорости скачивания (Мбит/с)",
            Description = "Максимальная скорость скачивания видео. Пустое значение — без ограничений",
        },
        new()
        {
            Key = "upload_speed_limit",
            IsRequired = false,
            Title = "ограничение скорости выгрузки (Мбит/с)",
            Description = "Максимальная скорость выгрузки видео. Пустое значение — без ограничений",
        },
    ];

    public IReadOnlyList<ToolDescriptor> RequiredTools { get; } =
    [
        WellKnownTools.FFmpegWithProbeDescriptor,
    ];

    public Task<List<SettingOption>> GetSettingOptionsAsync(
        string settingKey,
        Dictionary<string, string> currentSettings)
    {
        return Task.FromResult<List<SettingOption>>([]);
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(
        Dictionary<string, string> settings,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.ListingMedia();
        var service = await CreateServiceAsync(settings, cancellationToken);
        var peer = await service.ResolveChannelAsync(settings["channel"], cancellationToken);

        await foreach (var message in service.GetVideosAsync(peer, cancellationToken))
        {
            yield return CreateMediaDto(message);
        }
    }

    public async Task<MediaDto?> GetMediaByIdAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.GettingMedia(externalId);
        var service = await CreateServiceAsync(settings, cancellationToken);
        var peer = await service.ResolveChannelAsync(settings["channel"], cancellationToken);
        var messageId = int.Parse(externalId);

        var message = await service.GetVideoByIdAsync(peer, messageId, cancellationToken);
        return message != null ? CreateMediaDto(message) : null;
    }

    public async Task<MediaDto> DownloadAsync(
        string videoId,
        Dictionary<string, string> settings,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.StartingDownload(videoId);
        var service = await CreateServiceAsync(settings, cancellationToken);
        var peer = await service.ResolveChannelAsync(settings["channel"], cancellationToken);
        var messageId = int.Parse(videoId);

        var message = await service.GetVideoByIdAsync(peer, messageId, cancellationToken)
                      ?? throw new InvalidOperationException($"Видео {videoId} не найдено");

        var doc = (Document)((MessageMediaDocument)message.media!).document!;
        var tempPath = settings["_system_temp_path"];
        var guid = Guid.NewGuid().ToString("N");
        var tempVideoPath = Path.Combine(tempPath, guid, "media.mp4");
        var tempPreviewPath = Path.Combine(tempPath, guid, "preview.jpg");

        var downloadBytesPerSecond = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);

        try
        {
            await service.DownloadFileAsync(doc, tempVideoPath, downloadBytesPerSecond, progress, cancellationToken);
        }
        catch
        {
            TryDeleteFile(tempVideoPath);
            throw;
        }

        logger.MediaDownloaded(tempVideoPath, new FileInfo(tempVideoPath).Length);

        if (doc.thumbs?.Length > 0)
        {
            var thumb = doc.thumbs
                .OfType<PhotoSize>()
                .OrderByDescending(t => t.w * t.h)
                .FirstOrDefault();

            if (thumb != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempPreviewPath)!);

                try
                {
                    await using var thumbStream = File.Create(tempPreviewPath);
                    await service.DownloadFileAsync(doc, thumbStream, thumb, cancellationToken);
                }
                catch
                {
                    TryDeleteFile(tempPreviewPath);
                    throw;
                }
            }
        }

        var dto = CreateMediaDto(message);
        dto.TempDataPath = tempVideoPath;
        dto.TempPreviewPath = File.Exists(tempPreviewPath) ? tempPreviewPath : string.Empty;

        return dto;
    }

    public async Task<UploadResult> UploadAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.StartingUpload(media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        var service = await CreateServiceAsync(settings, cancellationToken);
        var peer = await service.ResolveChannelAsync(settings["channel"], cancellationToken);

        var caption = string.IsNullOrWhiteSpace(media.Description)
            ? media.Title
            : $"{media.Title}\n{media.Description}";

        try
        {
            var videoInfo = await ProbeVideoInfoAsync(filePath, cancellationToken);
            var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);
            var uploadProgress = UploadProgressLogger.CreateBucketed(logger, media.Id, external: progress);
            var message = await service.UploadVideoAsync(peer, filePath, caption, videoInfo, uploadBytesPerSecond, uploadProgress, cancellationToken);

            var externalId = message.id.ToString();
            logger.MediaUploaded(externalId);

            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.UploadFailed(ex);
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = ex.Message,
            };
        }
    }

    public async Task<UploadResult> UpdateAsync(
        string externalId,
        MediaDto tempMedia,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        logger.StartingUpdate(externalId);

        var service = await CreateServiceAsync(settings, cancellationToken);
        var peer = await service.ResolveChannelAsync(settings["channel"], cancellationToken);
        var messageId = int.Parse(externalId);

        var caption = string.IsNullOrWhiteSpace(tempMedia.Description)
            ? tempMedia.Title
            : $"{tempMedia.Title}\n{tempMedia.Description}";

        try
        {
            await service.EditMessageAsync(peer, messageId, caption, cancellationToken);

            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.UpdateFailed(externalId, ex);
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = ex.Message,
            };
        }
    }

    public async Task DeleteAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.StartingDelete(externalId);

        var service = await CreateServiceAsync(settings, cancellationToken);
        var peer = await service.ResolveChannelAsync(settings["channel"], cancellationToken);
        var messageId = int.Parse(externalId);

        await service.DeleteMessageAsync(peer, messageId, cancellationToken);

        logger.MediaDeleted(externalId);
    }

    public Uri? GetExternalUri(
        string externalId,
        Dictionary<string, string> settings)
    {
        var channel = settings.GetValueOrDefault("channel", "").Trim().TrimStart('@');

        if (string.IsNullOrEmpty(channel) || long.TryParse(channel, out _))
        {
            return null;
        }

        return new($"https://t.me/{channel}/{externalId}");
    }

    // TODO: Придумать более умный механизм
    public bool IsAuthenticated(Dictionary<string, string> settings)
    {
        return File.Exists(GetSessionPath(settings));
    }

    public async Task AuthenticateAsync(
        Dictionary<string, string> settings,
        IAuthUI ui,
        CancellationToken ct)
    {
        var apiId = int.Parse(settings["api_id"]);
        var apiHash = settings["api_hash"];
        var phoneNumber = settings["phone_number"];
        var sessionPath = GetSessionPath(settings);

        await using (var client = new Client(apiId, apiHash, sessionPath))
        {
            while (client.User == null)
            {
                ct.ThrowIfCancellationRequested();

                var what = await client.Login(phoneNumber);
                if (what == null)
                {
                    break;
                }

                var prompt = what switch
                {
                    "verification_code" => "Введите код подтверждения из Telegram:",
                    "password" => "Введите пароль двухфакторной аутентификации:",
                    _ => $"Telegram запрашивает: {what}",
                };

                var value = await ui.PromptInputAsync(prompt, what == "password");
                if (value == null)
                {
                    throw new OperationCanceledException();
                }

                await client.Login(value);
            }

            logger.AuthSucceeded(client.User!.first_name, client.User.id);
            await ui.ShowMessageAsync($"Авторизация успешна!\nПользователь: {client.User.first_name} (ID: {client.User.id})");
        }

        InvalidateCachedService(sessionPath);
    }

    public ConvertType[] GetAvailableConvertTypes()
    {
        return [];
    }

    public Task ConvertAsync(
        int typeId,
        string externalId,
        Dictionary<string, string> settings,
        IProgress<ConvertProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private static MediaDto CreateMediaDto(Message message)
    {
        var doc = ((MessageMediaDocument)message.media!).document as Document;
        var videoAttr = doc?.attributes?.OfType<DocumentAttributeVideo>().FirstOrDefault();

        var caption = message.message ?? "";
        var lines = caption.Split('\n', 2);
        var title = string.IsNullOrWhiteSpace(lines[0]) ? "Без названия" : lines[0];
        var description = lines.Length > 1 ? lines[1] : "";

        var metadata = new List<MetadataItem>();

        if (videoAttr != null)
        {
            metadata.Add(new()
            {
                Key = "Duration",
                DisplayName = "Длительность",
                Value = TimeSpan.FromSeconds(videoAttr.duration).ToString(),
                DisplayType = "System.TimeSpan",
            });
        }

        metadata.Add(new()
        {
            Key = "CreationDate",
            DisplayName = "Дата создания",
            Value = message.date.ToString("O"),
            DisplayType = "System.DateTime",
        });

        return new()
        {
            Id = message.id.ToString(),
            Title = title,
            Description = description,
            DataPath = string.Empty,
            PreviewPath = string.Empty,
            Metadata = metadata,
        };
    }

    // TODO: Дублирование с HDD
    private static VideoInfo ParseFfprobeOutput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var info = new VideoInfo();

        if (root.TryGetProperty("format", out var format)
            && format.TryGetProperty("duration", out var duration)
            && double.TryParse(duration.GetString(), CultureInfo.InvariantCulture, out var seconds))
        {
            info.Duration = seconds;
        }

        if (!root.TryGetProperty("streams", out var streams))
        {
            return info;
        }

        foreach (var stream in streams.EnumerateArray())
        {
            var codecType = stream.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;

            if (codecType != "video")
            {
                continue;
            }

            if (stream.TryGetProperty("width", out var w))
            {
                info.Width = w.GetInt32();
            }

            if (stream.TryGetProperty("height", out var h))
            {
                info.Height = h.GetInt32();
            }

            if (stream.TryGetProperty("codec_name", out var codec))
            {
                info.Codec = codec.GetString();
            }

            break;
        }

        return info;
    }

    private static string GetSessionPath(Dictionary<string, string> settings)
    {
        return Path.Combine(settings["_system_state_path"], "telegram.session");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception)
        {
            // Частично записанный файл нельзя удалить - оставляем GC/юзеру.
        }
    }

    private async Task<TelegramService> CreateServiceAsync(
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        var sessionPath = GetSessionPath(settings);
        var apiId = int.Parse(settings["api_id"]);
        var apiHash = settings["api_hash"];
        var phoneNumber = settings["phone_number"];

        if (TryGetMatchingCached(sessionPath, apiId, apiHash, phoneNumber, out var cached))
        {
            return cached.Service;
        }

        await _cacheLock.WaitAsync(cancellationToken);

        try
        {
            if (TryGetMatchingCached(sessionPath, apiId, apiHash, phoneNumber, out cached))
            {
                return cached.Service;
            }

            if (_serviceCache.TryRemove(sessionPath, out var stale))
            {
                stale.Service.Dispose();
            }

            var service = telegramServiceFactory.Create(apiId, apiHash, phoneNumber, sessionPath);

            try
            {
                await service.ConnectAsync(cancellationToken);
            }
            catch
            {
                service.Dispose();
                throw;
            }

            _serviceCache[sessionPath] = new(service, apiId, apiHash, phoneNumber);
            return service;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private bool TryGetMatchingCached(
        string sessionPath,
        int apiId,
        string apiHash,
        string phoneNumber,
        [NotNullWhen(true)] out CachedService? cached)
    {
        if (_serviceCache.TryGetValue(sessionPath, out var existing)
            && existing.ApiId == apiId
            && existing.ApiHash == apiHash
            && existing.PhoneNumber == phoneNumber)
        {
            cached = existing;
            return true;
        }

        cached = null;
        return false;
    }

    private void InvalidateCachedService(string sessionPath)
    {
        if (_serviceCache.TryRemove(sessionPath, out var cached))
        {
            cached.Service.Dispose();
        }
    }

    private async Task<VideoInfo> ProbeVideoInfoAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            logger.FfprobeNotFound();
            return new();
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                return new();
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                return ParseFfprobeOutput(output);
            }

            logger.FfprobeExited(process.ExitCode, filePath);
            return new();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.FfprobeFailed(filePath, ex);
            return new();
        }
    }

    private sealed record CachedService(TelegramService Service, int ApiId, string ApiHash, string PhoneNumber);
}

public record VideoInfo
{
    public double Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Codec { get; set; }
}
