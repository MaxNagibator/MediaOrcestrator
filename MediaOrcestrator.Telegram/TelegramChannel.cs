using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TL;
using WTelegram;

namespace MediaOrcestrator.Telegram;

public sealed class TelegramChannel(ILogger<TelegramChannel> logger, ILogger<TelegramService> serviceLogger, IToolPathProvider toolPathProvider) : ISourceType, IAuthenticatable, IToolConsumer
{
    private readonly SemaphoreSlim _serviceLock = new(1, 1);
    private TelegramService? _cachedService;
    private string? _cachedApiId;
    private string? _cachedApiHash;

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
            Key = "session_path",
            IsRequired = true,
            Title = "путь до файла сессии",
            Description = @"Файл сессии Telegram (например, C:\path\to\telegram.session)",
        },
        new()
        {
            Key = "temp_path",
            IsRequired = true,
            Title = "путь для временных файлов",
            Description = "Директория для скачанных видео и превью",
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

    public IReadOnlyList<ToolDescriptor> RequiredTools =>
    [
        new()
        {
            Name = WellKnownTools.FFmpeg,
            GitHubRepo = "BtbN/FFmpeg-Builds",
            AssetPattern = "ffmpeg-N-*-win64-gpl.zip",
            VersionCommand = "-version",
            VersionPattern = @"ffmpeg version N-\d+-\w+-(\d{8})",
            VersionTagPattern = @"autobuild-(\d{4}-\d{2}-\d{2})",
            ArchiveType = ArchiveType.Zip,
            ArchiveExecutablePath = "ffmpeg-*/bin/ffmpeg.exe",
            CompanionExecutables = ["ffprobe"],
        },
    ];

    public Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        return Task.FromResult<List<SettingOption>>([]);
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Получение списка видео из Telegram-канала");
        var service = await CreateServiceAsync(settings);
        var peer = await service.ResolveChannelAsync(settings["channel"]);

        await foreach (var message in service.GetVideosAsync(peer, cancellationToken))
        {
            yield return CreateMediaDto(message);
        }
    }

    public async Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Получение видео {ExternalId} из Telegram", externalId);
        var service = await CreateServiceAsync(settings);
        var peer = await service.ResolveChannelAsync(settings["channel"]);
        var messageId = int.Parse(externalId);

        var message = await service.GetVideoByIdAsync(peer, messageId);
        return message != null ? CreateMediaDto(message) : null;
    }

    public async Task<MediaDto> Download(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Скачивание видео {VideoId} из Telegram", videoId);
        var service = await CreateServiceAsync(settings);
        var peer = await service.ResolveChannelAsync(settings["channel"]);
        var messageId = int.Parse(videoId);

        var message = await service.GetVideoByIdAsync(peer, messageId)
                      ?? throw new InvalidOperationException($"Видео {videoId} не найдено");

        var doc = (Document)((MessageMediaDocument)message.media!).document!;
        var tempPath = settings["temp_path"];
        var guid = Guid.NewGuid().ToString("N");
        var tempVideoPath = Path.Combine(tempPath, guid, "media.mp4");
        var tempPreviewPath = Path.Combine(tempPath, guid, "preview.jpg");

        var downloadBytesPerSecond = SpeedLimitHelper.ParseDownloadBytesPerSecond(settings);
        await service.DownloadFileAsync(doc, tempVideoPath, downloadBytesPerSecond, cancellationToken);
        logger.LogInformation("Видео сохранено: {Path} ({Size} байт)", tempVideoPath, new FileInfo(tempVideoPath).Length);

        if (doc.thumbs?.Length > 0)
        {
            var thumb = doc.thumbs
                .OfType<PhotoSize>()
                .OrderByDescending(t => t.w * t.h)
                .FirstOrDefault();

            if (thumb != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempPreviewPath)!);
                await using var thumbStream = File.Create(tempPreviewPath);
                await service.DownloadFileAsync(doc, thumbStream, thumb);
            }
        }

        var dto = CreateMediaDto(message);
        dto.TempDataPath = tempVideoPath;
        dto.TempPreviewPath = File.Exists(tempPreviewPath) ? tempPreviewPath : string.Empty;

        return dto;
    }

    public async Task<UploadResult> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Загрузка видео в Telegram-канал. Название: '{Title}'", media.Title);

        var filePath = media.TempDataPath;
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл видео не найден", filePath);
        }

        var service = await CreateServiceAsync(settings);
        var peer = await service.ResolveChannelAsync(settings["channel"]);

        var caption = string.IsNullOrWhiteSpace(media.Description)
            ? media.Title
            : $"{media.Title}\n{media.Description}";

        try
        {
            var videoInfo = await ProbeVideoInfoAsync(filePath, cancellationToken);
            var uploadBytesPerSecond = SpeedLimitHelper.ParseUploadBytesPerSecond(settings);
            var message = await service.UploadVideoAsync(peer, filePath, caption, videoInfo, uploadBytesPerSecond, cancellationToken);

            var externalId = message.id.ToString();
            logger.LogInformation("Видео загружено в Telegram. Message ID: {ExternalId}", externalId);

            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки видео в Telegram");
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = ex.Message,
            };
        }
    }

    public async Task<UploadResult> Update(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        logger.LogInformation("Обновление видео {ExternalId} в Telegram", externalId);

        var service = await CreateServiceAsync(settings);
        var peer = await service.ResolveChannelAsync(settings["channel"]);
        var messageId = int.Parse(externalId);

        var caption = string.IsNullOrWhiteSpace(tempMedia.Description)
            ? tempMedia.Title
            : $"{tempMedia.Title}\n{tempMedia.Description}";

        try
        {
            await service.EditMessageAsync(peer, messageId, caption);

            return new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = externalId,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обновления видео {ExternalId} в Telegram", externalId);
            return new()
            {
                Status = MediaStatusHelper.GetById(MediaStatus.Error),
                Message = ex.Message,
            };
        }
    }

    public async Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Удаление видео {ExternalId} из Telegram", externalId);

        var service = await CreateServiceAsync(settings);
        var peer = await service.ResolveChannelAsync(settings["channel"]);
        var messageId = int.Parse(externalId);

        await service.DeleteMessageAsync(peer, messageId);

        logger.LogInformation("Видео {ExternalId} удалено из Telegram", externalId);
    }

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
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
        var sessionPath = settings.GetValueOrDefault("session_path");
        if (string.IsNullOrEmpty(sessionPath))
        {
            return false;
        }

        return File.Exists(sessionPath);
    }

    public async Task AuthenticateAsync(Dictionary<string, string> settings, IAuthUI ui, CancellationToken ct)
    {
        var apiId = int.Parse(settings["api_id"]);
        var apiHash = settings["api_hash"];
        var phoneNumber = settings["phone_number"];
        var sessionPath = settings["session_path"];

        Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);

        await using var client = new Client(apiId, apiHash, sessionPath);

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

        logger.LogInformation("Telegram: авторизация успешна. Пользователь: {Name} (ID: {Id})", client.User!.first_name, client.User.id);
        await ui.ShowMessageAsync($"Авторизация успешна!\nПользователь: {client.User.first_name} (ID: {client.User.id})");
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

    private async Task<TelegramService> CreateServiceAsync(Dictionary<string, string> settings)
    {
        var apiId = int.Parse(settings["api_id"]);
        var apiHash = settings["api_hash"];
        var sessionPath = settings["session_path"];

        await _serviceLock.WaitAsync();
        try
        {
            if (_cachedService != null && _cachedApiId == settings["api_id"] && _cachedApiHash == apiHash)
            {
                return _cachedService;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);

            var oldService = _cachedService;
            var phoneNumber = settings["phone_number"];
            _cachedService = new(apiId, apiHash, phoneNumber, sessionPath, serviceLogger);
            _cachedApiId = settings["api_id"];
            _cachedApiHash = apiHash;
            oldService?.Dispose();

            await _cachedService.ConnectAsync();
            return _cachedService;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    private async Task<VideoInfo> ProbeVideoInfoAsync(string filePath, CancellationToken cancellationToken)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            logger.LogWarning("ffprobe не найден, видео будет загружено без метаданных");
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

            logger.LogWarning("ffprobe завершился с кодом {ExitCode} для файла: {FilePath}", process.ExitCode, filePath);
            return new();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить информацию о видео через ffprobe: {FilePath}", filePath);
            return new();
        }
    }

    public ConvertType[] GetAvailabelConvertTypes()
    {
        return [];
    }

    public Task ConvertAsync(int typeId, string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public record VideoInfo
{
    public double Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Codec { get; set; }
}
