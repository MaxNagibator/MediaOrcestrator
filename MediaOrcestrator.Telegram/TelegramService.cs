using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using TL;
using WTelegram;

namespace MediaOrcestrator.Telegram;

public sealed class TelegramService : IDisposable
{
    private readonly Client _client;
    private readonly ILogger _logger;
    private readonly string _phoneNumber;

    public TelegramService(int apiId, string apiHash, string phoneNumber, string sessionPath, ILogger logger)
    {
        _logger = logger;
        _phoneNumber = phoneNumber;
        _client = new(apiId, apiHash, sessionPath);
    }

    public async Task ConnectAsync()
    {
        _logger.LogInformation("Подключение к Telegram...");
        await _client.Login(_phoneNumber);

        if (_client.User == null)
        {
            throw new InvalidOperationException("Не удалось подключиться к Telegram. Выполните авторизацию через настройки источника.");
        }

        _logger.LogInformation("Подключено как {Name} (ID: {Id})", _client.User.first_name, _client.User.id);
    }

    public async Task<InputPeer> ResolveChannelAsync(string channel)
    {
        var trimmed = channel.Trim().TrimStart('@');

        if (long.TryParse(trimmed, out var numericId))
        {
            _logger.LogDebug("Резолв канала по числовому ID: {Id}", numericId);
            var chats = await _client.Messages_GetAllChats();

            var chat = chats.chats.Values.FirstOrDefault(c => c.ID == numericId)
                       ?? throw new InvalidOperationException($"Канал с ID {numericId} не найден. Убедитесь, что аккаунт подписан на канал.");

            return chat.ToInputPeer();
        }

        _logger.LogDebug("Резолв канала по username: @{Username}", trimmed);
        var resolved = await _client.Contacts_ResolveUsername(trimmed);

        return resolved.peer switch
        {
            PeerChannel pc => resolved.chats[pc.channel_id].ToInputPeer(),
            _ => throw new InvalidOperationException($"@{trimmed} не является каналом"),
        };
    }

    public async IAsyncEnumerable<Message> GetVideosAsync(InputPeer peer, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var offsetId = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var history = await _client.Messages_GetHistory(peer, offsetId, limit: 100);
            var messages = history.Messages;

            if (messages.Length == 0)
            {
                break;
            }

            foreach (var msgBase in messages)
            {
                if (msgBase is not Message { media: MessageMediaDocument { document: Document doc } } message)
                {
                    continue;
                }

                if (!doc.mime_type.StartsWith("video/"))
                {
                    continue;
                }

                yield return message;
            }

            if (messages.Length < 100)
            {
                break;
            }

            offsetId = messages[^1].ID;
        }
    }

    public async Task<Message?> GetVideoByIdAsync(InputPeer peer, int messageId)
    {
        _logger.LogDebug("Запрос сообщения {MessageId}", messageId);

        var result = await _client.GetMessages(peer, new InputMessageID { id = messageId });

        if (result.Messages.Length == 0 || result.Messages[0] is not Message message)
        {
            return null;
        }

        if (message.media is not MessageMediaDocument { document: Document doc } || !doc.mime_type.StartsWith("video/"))
        {
            _logger.LogWarning("Сообщение {MessageId} не содержит видео", messageId);
            return null;
        }

        return message;
    }

    public async Task DownloadFileAsync(Document document, string outputPath, long? bytesPerSecond = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Скачивание файла {Id} ({Size} байт)", document.id, document.size);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        await using var fileStream = File.Create(outputPath);
        await using var throttled = new ThrottledStream(fileStream, bytesPerSecond);
        await _client.DownloadFileAsync(document, throttled);

        _logger.LogInformation("Файл сохранён: {Path}", outputPath);
    }

    public async Task DownloadFileAsync(Document document, Stream outputStream, PhotoSizeBase? thumbSize = null)
    {
        await _client.DownloadFileAsync(document, outputStream, thumbSize);
    }

    public async Task<Message> UploadVideoAsync(InputPeer peer, string filePath, string caption, VideoInfo videoInfo, long? bytesPerSecond = null, IProgress<double>? uploadProgress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Загрузка видео в Telegram: {Path} ({W}x{H}, {Duration:F1}с)", filePath, videoInfo.Width, videoInfo.Height, videoInfo.Duration);

        await using var fileStream = File.OpenRead(filePath);
        var fileSize = fileStream.Length;

        Stream sourceStream = bytesPerSecond.HasValue
            ? new ThrottledStream(fileStream, bytesPerSecond)
            : fileStream;

        var byteProgress = uploadProgress != null && fileSize > 0
            ? new Progress<long>(bytes => uploadProgress.Report(Math.Min(1.0, (double)bytes / fileSize)))
            : null;

        await using var stream = new ProgressStream(sourceStream, byteProgress);
        var inputFile = await _client.UploadFileAsync(stream, Path.GetFileName(filePath));

        var mimeType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            _ => "video/mp4",
        };

        var videoAttr = new DocumentAttributeVideo
        {
            flags = DocumentAttributeVideo.Flags.supports_streaming,
            duration = videoInfo.Duration,
            w = videoInfo.Width,
            h = videoInfo.Height,
        };

        if (videoInfo.Codec != null)
        {
            videoAttr.video_codec = videoInfo.Codec;
        }

        var media = new InputMediaUploadedDocument(inputFile, mimeType,
            videoAttr,
            new DocumentAttributeFilename
            {
                file_name = Path.GetFileName(filePath),
            });

        var message = await _client.SendMessageAsync(peer, caption, media);

        _logger.LogInformation("Видео загружено. Message ID: {MessageId}", message.id);
        return message;
    }

    public async Task EditMessageAsync(InputPeer peer, int messageId, string caption)
    {
        _logger.LogInformation("Редактирование сообщения {MessageId}", messageId);
        await _client.Messages_EditMessage(peer, messageId, caption);
        _logger.LogInformation("Сообщение {MessageId} отредактировано", messageId);
    }

    public async Task DeleteMessageAsync(InputPeer peer, int messageId)
    {
        _logger.LogInformation("Удаление сообщения {MessageId}", messageId);
        await _client.DeleteMessages(peer, messageId);
        _logger.LogInformation("Сообщение {MessageId} удалено", messageId);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
