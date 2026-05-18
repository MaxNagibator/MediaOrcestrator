using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using TL;
using WTelegram;

namespace MediaOrcestrator.Telegram;

public sealed class TelegramService : IDisposable
{
    private readonly Client _client;
    private readonly ILogger<TelegramService> _logger;
    private readonly TelegramOptions _options;
    private readonly string _phoneNumber;

    public TelegramService(
        int apiId,
        string apiHash,
        string phoneNumber,
        string sessionPath,
        TelegramOptions options,
        ILogger<TelegramService> logger)
    {
        _logger = logger;
        _options = options;
        _phoneNumber = phoneNumber;
        _client = new(apiId, apiHash, sessionPath);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.ConnectingToTelegram();

        await _client.Login(_phoneNumber).WaitAsync(_options.ConnectTimeout, cancellationToken);

        if (_client.User == null)
        {
            throw new InvalidOperationException("Не удалось подключиться к Telegram. Выполните авторизацию через настройки источника.");
        }

        _logger.ConnectedAs(_client.User.first_name, _client.User.id);
    }

    public async Task<InputPeer> ResolveChannelAsync(
        string channel,
        CancellationToken cancellationToken = default)
    {
        var trimmed = channel.Trim().TrimStart('@');

        if (long.TryParse(trimmed, out var numericId))
        {
            _logger.ResolvingChannelById(numericId);
            var chats = await _client.Messages_GetAllChats().WaitAsync(cancellationToken);

            var chat = chats.chats.Values.FirstOrDefault(c => c.ID == numericId)
                       ?? throw new InvalidOperationException($"Канал с ID {numericId} не найден. Убедитесь, что аккаунт подписан на канал.");

            return chat.ToInputPeer();
        }

        _logger.ResolvingChannelByUsername(trimmed);
        var resolved = await _client.Contacts_ResolveUsername(trimmed).WaitAsync(cancellationToken);

        return resolved.peer switch
        {
            PeerChannel pc => resolved.chats[pc.channel_id].ToInputPeer(),
            _ => throw new InvalidOperationException($"@{trimmed} не является каналом"),
        };
    }

    public async IAsyncEnumerable<Message> GetVideosAsync(
        InputPeer peer,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var offsetId = 0;
        var pageSize = _options.HistoryPageSize;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var history = await _client.Messages_GetHistory(peer, offsetId, limit: pageSize).WaitAsync(cancellationToken);
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

            if (messages.Length < pageSize)
            {
                break;
            }

            offsetId = messages[^1].ID;
        }
    }

    public async Task<Message?> GetVideoByIdAsync(
        InputPeer peer,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.RequestingMessage(messageId);

        var result = await _client.GetMessages(peer, new InputMessageID { id = messageId }).WaitAsync(cancellationToken);

        if (result.Messages.Length == 0 || result.Messages[0] is not Message message)
        {
            return null;
        }

        if (message.media is not MessageMediaDocument { document: Document doc } || !doc.mime_type.StartsWith("video/"))
        {
            _logger.MessageHasNoVideo(messageId);
            return null;
        }

        return message;
    }

    public async Task DownloadFileAsync(
        Document document,
        string outputPath,
        long? bytesPerSecond = null,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.DownloadingFile(document.id, document.size);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        await using var fileStream = File.Create(outputPath);
        await using var throttled = new ThrottledStream(fileStream, bytesPerSecond);

        var byteProgress = progress != null && document.size > 0
            ? new Progress<long>(bytes => progress.Report(new(Math.Min(100.0, bytes * 100.0 / document.size))))
            : null;

        await using var tracked = new ProgressStream(throttled, byteProgress);
        await _client.DownloadFileAsync(document, tracked).WaitAsync(cancellationToken);

        _logger.FileSaved(outputPath);
    }

    public async Task DownloadFileAsync(
        Document document,
        Stream outputStream,
        PhotoSizeBase? thumbSize = null,
        CancellationToken cancellationToken = default)
    {
        await _client.DownloadFileAsync(document, outputStream, thumbSize).WaitAsync(cancellationToken);
    }

    public async Task<Message> UploadVideoAsync(
        InputPeer peer,
        string filePath,
        string caption,
        VideoInfo videoInfo,
        long? bytesPerSecond = null,
        IProgress<double>? uploadProgress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.UploadingVideo(filePath, videoInfo.Width, videoInfo.Height, videoInfo.Duration);

        await using var fileStream = File.OpenRead(filePath);
        var fileSize = fileStream.Length;

        Stream sourceStream = bytesPerSecond.HasValue
            ? new ThrottledStream(fileStream, bytesPerSecond)
            : fileStream;

        var byteProgress = uploadProgress != null && fileSize > 0
            ? new Progress<long>(bytes => uploadProgress.Report(Math.Min(1.0, (double)bytes / fileSize)))
            : null;

        await using var stream = new ProgressStream(sourceStream, byteProgress);
        var inputFile = await _client.UploadFileAsync(stream, Path.GetFileName(filePath)).WaitAsync(cancellationToken);

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

        var message = await _client.SendMessageAsync(peer, caption, media).WaitAsync(cancellationToken);

        _logger.VideoUploaded(message.id);
        return message;
    }

    public async Task EditMessageAsync(
        InputPeer peer,
        int messageId,
        string caption,
        CancellationToken cancellationToken = default)
    {
        _logger.EditingMessage(messageId);
        await _client.Messages_EditMessage(peer, messageId, caption).WaitAsync(cancellationToken);
        _logger.MessageEdited(messageId);
    }

    public async Task DeleteMessageAsync(
        InputPeer peer,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        _logger.DeletingMessage(messageId);
        await _client.DeleteMessages(peer, messageId).WaitAsync(cancellationToken);
        _logger.MessageDeleted(messageId);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
