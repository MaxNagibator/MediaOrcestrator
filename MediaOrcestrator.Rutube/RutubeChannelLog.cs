using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Rutube;

internal static partial class RutubeChannelLog
{
    [LoggerMessage(EventId = 2100, Level = LogLevel.Error, Message = "Ошибка при получении категорий RuTube")]
    public static partial void GetCategoriesFailed(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Information, Message = "Получение списка медиа для хранилища {Name}")]
    public static partial void ListingMedia(
        this ILogger logger,
        string name);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Debug, Message = "Обработка видео: '{VideoTitle}' (ID: {VideoId})")]
    public static partial void ProcessingVideo(
        this ILogger logger,
        string videoTitle,
        string videoId);

    [LoggerMessage(EventId = 2103, Level = LogLevel.Information, Message = "Получение информации о видео RuTube. ID: {VideoId}")]
    public static partial void RequestingChannelMedia(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2104, Level = LogLevel.Information, Message = "Начало скачивания видео с RuTube. ID: {VideoId}")]
    public static partial void DownloadStarting(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2105, Level = LogLevel.Debug, Message = "Получена информация о видео. Название: '{Title}'")]
    public static partial void ChannelVideoInfoReceived(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 2106, Level = LogLevel.Debug, Message = "Создана временная директория: {TempPath}")]
    public static partial void TempDirectoryCreated(
        this ILogger logger,
        string? tempPath);

    [LoggerMessage(EventId = 2107, Level = LogLevel.Debug, Message = "Cookies конвертированы в Netscape формат: {Path}")]
    public static partial void CookiesConvertedToNetscape(
        this ILogger logger,
        string path);

    [LoggerMessage(EventId = 2108, Level = LogLevel.Information, Message = "Скачивание части #{PartNumber}")]
    public static partial void DownloadPartStarted(
        this ILogger logger,
        int partNumber);

    [LoggerMessage(EventId = 2109, Level = LogLevel.Information, Message = "Прогресс скачивания [Часть {PartNumber}]: {Percent:P0}")]
    public static partial void DownloadProgress(
        this ILogger logger,
        int partNumber,
        double percent);

    [LoggerMessage(EventId = 2110, Level = LogLevel.Information, Message = "Запуск скачивания через yt-dlp. URL: https://rutube.ru/video/{VideoId}/")]
    public static partial void StartingYtDlpDownload(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2111, Level = LogLevel.Information, Message = "Видео успешно скачано. ID: {VideoId}, Путь: {FilePath}")]
    public static partial void DownloadCompleted(
        this ILogger logger,
        string videoId,
        string filePath);

    [LoggerMessage(EventId = 2112, Level = LogLevel.Error, Message = "Ошибка при скачивании видео через yt-dlp. ID: {VideoId}")]
    public static partial void DownloadFailed(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 2113, Level = LogLevel.Information, Message = "Начало загрузки видео на RuTube. Название: '{Title}'")]
    public static partial void UploadStarting(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 2114, Level = LogLevel.Error, Message = "Файл видео не найден: {FilePath}")]
    public static partial void VideoFileNotFound(
        this ILogger logger,
        string filePath);

    [LoggerMessage(EventId = 2115, Level = LogLevel.Debug, Message = "Файл найден. Размер: {FileSize} байт")]
    public static partial void FileFound(
        this ILogger logger,
        long fileSize);

    [LoggerMessage(EventId = 2116, Level = LogLevel.Information, Message = "Кодек не найден в метаданных, определяем через ffprobe: {FilePath}")]
    public static partial void DetectingCodec(
        this ILogger logger,
        string filePath);

    [LoggerMessage(EventId = 2117, Level = LogLevel.Information, Message = "Обнаружен кодек {Codec}, запускаем транскодирование в H.264: {FilePath}")]
    public static partial void TranscodingToH264(
        this ILogger logger,
        string codec,
        string filePath);

    [LoggerMessage(EventId = 2118, Level = LogLevel.Information, Message = "Транскодирование завершено. Размер: {Size} байт. Новый путь: {Path}")]
    public static partial void TranscodingCompleted(
        this ILogger logger,
        long size,
        string path);

    [LoggerMessage(EventId = 2119, Level = LogLevel.Information, Message = "Кодек видео: {Codec}")]
    public static partial void VideoCodec(
        this ILogger logger,
        string codec);

    [LoggerMessage(EventId = 2120, Level = LogLevel.Information, Message = "Отложенная публикация запланирована через {Hours} ч. - на {PublishAt}")]
    public static partial void RelativePublicationScheduled(
        this ILogger logger,
        double hours,
        DateTime publishAt);

    [LoggerMessage(EventId = 2121, Level = LogLevel.Information, Message = "Отложенная публикация запланирована на {PublishAt}")]
    public static partial void PublicationScheduled(
        this ILogger logger,
        DateTime publishAt);

    [LoggerMessage(EventId = 2122, Level = LogLevel.Warning, Message = "Не удалось разобрать время публикации '{PublishAtRaw}'. Ожидается формат ЧЧ:ММ или +N (часов). Видео будет опубликовано немедленно")]
    public static partial void PublishAtParseFailed(
        this ILogger logger,
        string publishAtRaw);

    [LoggerMessage(EventId = 2123, Level = LogLevel.Information, Message = "Видео загружено на RuTube. Status: {Status}. Video ID: {SessionId}, Название: '{Title}'")]
    public static partial void VideoUploaded(
        this ILogger logger,
        string status,
        string? sessionId,
        string title);

    [LoggerMessage(EventId = 2124, Level = LogLevel.Error, Message = "Ошибка при загрузке видео на RuTube. Название: '{Title}'")]
    public static partial void UploadFailed(
        this ILogger logger,
        string title,
        Exception exception);

    [LoggerMessage(EventId = 2125, Level = LogLevel.Information, Message = "Начало обновление данных видео на RuTube. Название: '{Title}'")]
    public static partial void UpdateStarting(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 2126, Level = LogLevel.Information, Message = "Удаление медиа из RuTube. ID: {ExternalId}")]
    public static partial void DeletingMedia(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 2127, Level = LogLevel.Information, Message = "Медиа {ExternalId} успешно удалено из источника RuTube")]
    public static partial void MediaDeleted(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 2128, Level = LogLevel.Error, Message = "Ошибка HTTP при удалении из RuTube: {ExternalId}")]
    public static partial void DeleteHttpError(
        this ILogger logger,
        string externalId,
        Exception exception);

    [LoggerMessage(EventId = 2129, Level = LogLevel.Information, Message = "RuTube: авторизация сохранена в {Path}")]
    public static partial void AuthSaved(
        this ILogger logger,
        string path);

    [LoggerMessage(EventId = 2130, Level = LogLevel.Debug, Message = "Чтение данных аутентификации из: {AuthStatePath}")]
    public static partial void ReadingAuthState(
        this ILogger logger,
        string authStatePath);

    [LoggerMessage(EventId = 2131, Level = LogLevel.Debug, Message = "CSRF токен успешно получен")]
    public static partial void CsrfTokenReceived(this ILogger logger);
}
