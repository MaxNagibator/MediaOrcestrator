using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Youtube;

internal static partial class YoutubeServiceLog
{
    [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "Получение списка медиа для канала: {ChannelUrl}")]
    public static partial void StartingChannelFetch(
        this ILogger logger,
        string channelUrl);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Завершено получение медиа для канала: {ChannelUrl}")]
    public static partial void ChannelFetchCompleted(
        this ILogger logger,
        string channelUrl);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Debug, Message = "Обработка видео: '{Title}' (ID: {VideoId})")]
    public static partial void ProcessingVideo(
        this ILogger logger,
        string title,
        string videoId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Начало загрузки видео с YouTube. ID: {VideoId}")]
    public static partial void DownloadStart(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Видео успешно загружено. ID: {VideoId}, Путь: {FilePath}")]
    public static partial void DownloadCompleted(
        this ILogger logger,
        string videoId,
        string filePath);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Error, Message = "Ошибка при загрузке видео через yt-dlp. ID: {VideoId}")]
    public static partial void DownloadFailed(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Information, Message = "Загрузка части #{PartNumber}")]
    public static partial void DownloadPartStarted(
        this ILogger logger,
        int partNumber);

    [LoggerMessage(EventId = 5007, Level = LogLevel.Information, Message = "Прогресс загрузки [Часть {PartNumber}]: {Percent:P0}")]
    public static partial void DownloadProgress(
        this ILogger logger,
        int partNumber,
        double percent);

    [LoggerMessage(EventId = 5008, Level = LogLevel.Information, Message = "Загрузка видео на YouTube. Название: '{Title}', Статус: {Privacy}")]
    public static partial void UploadStart(
        this ILogger logger,
        string title,
        string? privacy);

    [LoggerMessage(EventId = 5009, Level = LogLevel.Information, Message = "Видео загружено на YouTube. ID: {VideoId}")]
    public static partial void UploadCompleted(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5010, Level = LogLevel.Error, Message = "Загрузка на YouTube не удалась: {Error}")]
    public static partial void UploadFailed(
        this ILogger logger,
        string error);

    [LoggerMessage(EventId = 5011, Level = LogLevel.Error, Message = "Превышена квота YouTube API (лимит ~6 видео/день)")]
    public static partial void QuotaExceeded(this ILogger logger);

    [LoggerMessage(EventId = 5012, Level = LogLevel.Information, Message = "Обновление метаданных YouTube видео. ID: {VideoId}")]
    public static partial void UpdateStart(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5013, Level = LogLevel.Information, Message = "Метаданные обновлены для видео: {VideoId}")]
    public static partial void UpdateCompleted(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5014, Level = LogLevel.Information, Message = "Удаление видео с YouTube. ID: {VideoId}")]
    public static partial void DeleteStart(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5015, Level = LogLevel.Information, Message = "Видео {VideoId} удалено с YouTube")]
    public static partial void DeleteCompleted(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5016, Level = LogLevel.Information, Message = "Загрузка превью для видео: {VideoId}")]
    public static partial void ThumbnailUploadStart(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5017, Level = LogLevel.Information, Message = "Превью загружено для видео: {VideoId}")]
    public static partial void ThumbnailUploaded(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5018, Level = LogLevel.Warning, Message = "Не удалось загрузить превью для видео {VideoId}: {Error}")]
    public static partial void ThumbnailUploadFailed(
        this ILogger logger,
        string videoId,
        string? error);

    [LoggerMessage(EventId = 5019, Level = LogLevel.Debug, Message = "Авторизация YouTube API. Токен: {TokenDir}")]
    public static partial void OAuthAuthorizing(
        this ILogger logger,
        string tokenDir);

    [LoggerMessage(EventId = 5020, Level = LogLevel.Information, Message = "OAuth-токен устарел, обновляю...")]
    public static partial void OAuthTokenStale(this ILogger logger);

    [LoggerMessage(EventId = 5021, Level = LogLevel.Information, Message = "OAuth-токен успешно обновлён")]
    public static partial void OAuthTokenRefreshed(this ILogger logger);

    [LoggerMessage(EventId = 5022, Level = LogLevel.Information, Message = "YouTube API: получение видео из плейлиста загрузок {PlaylistId}")]
    public static partial void FetchingPlaylist(
        this ILogger logger,
        string playlistId);

    [LoggerMessage(EventId = 5023, Level = LogLevel.Information, Message = "YouTube API: завершено получение видео для канала {ChannelId}")]
    public static partial void PlaylistFetchCompleted(
        this ILogger logger,
        string channelId);

    [LoggerMessage(EventId = 5024, Level = LogLevel.Debug, Message = "YoutubeExplode: попытка определить канал по URL: {ChannelUrl}")]
    public static partial void ResolvingChannel(
        this ILogger logger,
        string channelUrl);

    [LoggerMessage(EventId = 5025, Level = LogLevel.Debug, Message = "Канал найден: '{Title}' (ID: {ChannelId})")]
    public static partial void ChannelResolved(
        this ILogger logger,
        string title,
        string channelId);

    [LoggerMessage(EventId = 5026, Level = LogLevel.Warning, Message = "Не удалось определить канал по URL: {ChannelUrl}")]
    public static partial void ChannelResolveFailed(
        this ILogger logger,
        string channelUrl);

    [LoggerMessage(EventId = 5027, Level = LogLevel.Warning, Message = "YouTube API: видео не найдено: {VideoId}")]
    public static partial void ApiVideoNotFound(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5028, Level = LogLevel.Information, Message = "YouTube комментарии: видео {VideoId}, верхних {Threads}, ответов {Replies}")]
    public static partial void CommentsCompleted(
        this ILogger logger,
        string videoId,
        int threads,
        int replies);

    [LoggerMessage(EventId = 5029, Level = LogLevel.Warning, Message = "Не удалось определить владельца видео {VideoId} для разметки авторских комментариев")]
    public static partial void OwnerLookupFailed(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 5052, Level = LogLevel.Information, Message = "YouTube: у видео {VideoId} отключены комментарии — пропуск")]
    public static partial void CommentsDisabled(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5030, Level = LogLevel.Information, Message = "Превью сохранено через yt-dlp: {ThumbnailPath}")]
    public static partial void YtDlpThumbnailSaved(
        this ILogger logger,
        string thumbnailPath);

    [LoggerMessage(EventId = 5031, Level = LogLevel.Debug, Message = "yt-dlp не сохранил превью рядом с видео: {ThumbnailPath}")]
    public static partial void YtDlpThumbnailMissing(
        this ILogger logger,
        string thumbnailPath);

    [LoggerMessage(EventId = 5032, Level = LogLevel.Debug, Message = "yt-dlp: получение метаданных видео {VideoId}")]
    public static partial void YtDlpFetchingInfo(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5033, Level = LogLevel.Warning, Message = "yt-dlp: пустой ответ для видео {VideoId}")]
    public static partial void YtDlpEmptyResponse(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5034, Level = LogLevel.Debug, Message = "yt-dlp: метаданные '{Title}' (ID: {VideoId})")]
    public static partial void YtDlpInfoReceived(
        this ILogger logger,
        string title,
        string videoId);

    [LoggerMessage(EventId = 5035, Level = LogLevel.Warning, Message = "Не удалось получить категории через API, используются встроенные")]
    public static partial void CategoriesFallback(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(EventId = 5036, Level = LogLevel.Warning, Message = "YoutubeExplode не смог получить канал, пробую YouTube API fallback")]
    public static partial void ExplodeChannelFallback(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(EventId = 5037, Level = LogLevel.Error, Message = "YoutubeExplode не работает, а OAuth не настроен. Укажите client_id и client_secret для fallback через YouTube API")]
    public static partial void OAuthNotConfigured(this ILogger logger);

    [LoggerMessage(EventId = 5038, Level = LogLevel.Error, Message = "YouTube API: канал не найден: {ChannelUrl}")]
    public static partial void ApiChannelNotFound(
        this ILogger logger,
        string channelUrl);

    [LoggerMessage(EventId = 5039, Level = LogLevel.Warning, Message = "YoutubeExplode не смог получить видео {VideoId}, пробую YouTube API fallback")]
    public static partial void ExplodeVideoFallback(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 5040, Level = LogLevel.Warning, Message = "YouTube API не смог получить видео {VideoId}, пробую yt-dlp fallback")]
    public static partial void ApiVideoFallback(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 5041, Level = LogLevel.Information, Message = "YouTube API не настроен, пробую yt-dlp fallback для видео {VideoId}")]
    public static partial void ApiNotConfiguredFallback(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5042, Level = LogLevel.Error, Message = "yt-dlp не смог получить метаданные видео {VideoId}")]
    public static partial void YtDlpMetadataFailed(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 5043, Level = LogLevel.Debug, Message = "Создана временная директория: {TempPath}")]
    public static partial void TempDirCreated(
        this ILogger logger,
        string? tempPath);

    [LoggerMessage(EventId = 5044, Level = LogLevel.Information, Message = "Запуск загрузки через yt-dlp. URL: https://www.youtube.com/watch?v={VideoId}")]
    public static partial void StartingYtDlpDownload(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5045, Level = LogLevel.Debug, Message = "Метаданные видео получены из info.json yt-dlp. Название: '{Title}'")]
    public static partial void InfoJsonMetadataParsed(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 5046, Level = LogLevel.Warning, Message = "yt-dlp не сохранил info.json, запрашиваю метаданные отдельным вызовом")]
    public static partial void InfoJsonMissing(this ILogger logger);

    [LoggerMessage(EventId = 5047, Level = LogLevel.Information, Message = "YouTube: авторизация сохранена в {Path}")]
    public static partial void AuthSaved(
        this ILogger logger,
        string path);

    [LoggerMessage(EventId = 5048, Level = LogLevel.Warning, Message = "YouTube API: не удалось определить канал по URL: {ChannelUrl}")]
    public static partial void ApiChannelResolveFailed(
        this ILogger logger,
        string channelUrl);

    [LoggerMessage(EventId = 5049, Level = LogLevel.Debug, Message = "YouTube API: канал найден по ID. Название: '{Title}', ID: {ChannelId}")]
    public static partial void ApiChannelByIdHit(
        this ILogger logger,
        string title,
        string channelId);

    [LoggerMessage(EventId = 5050, Level = LogLevel.Debug, Message = "YouTube API: канал найден по handle '@{Handle}'. Название: '{Title}', ID: {ChannelId}")]
    public static partial void ApiChannelByHandleHit(
        this ILogger logger,
        string handle,
        string title,
        string channelId);

    [LoggerMessage(EventId = 5051, Level = LogLevel.Debug, Message = "YouTube API: канал найден по username '{Username}'. Название: '{Title}', ID: {ChannelId}")]
    public static partial void ApiChannelByUsernameHit(
        this ILogger logger,
        string username,
        string title,
        string channelId);

    [LoggerMessage(EventId = 5053, Level = LogLevel.Information, Message = "yt-dlp: получение комментариев для видео {VideoId}")]
    public static partial void YtDlpCommentsFetching(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5054, Level = LogLevel.Information, Message = "yt-dlp: комментарии для видео {VideoId} не получены (пусто или отключены)")]
    public static partial void YtDlpCommentsEmpty(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 5055, Level = LogLevel.Information, Message = "yt-dlp комментарии: видео {VideoId}, верхних {TopLevel}, ответов {Replies}, макс. глубина {MaxDepth}")]
    public static partial void YtDlpCommentsCompleted(
        this ILogger logger,
        string videoId,
        int topLevel,
        int replies,
        int maxDepth);

    [LoggerMessage(EventId = 5056, Level = LogLevel.Warning, Message = "yt-dlp не смог получить комментарии для видео {VideoId}, пробую YouTube API fallback")]
    public static partial void YtDlpCommentsFailed(
        this ILogger logger,
        string videoId,
        Exception exception);

    [LoggerMessage(EventId = 5057, Level = LogLevel.Information, Message = "yt-dlp для комментариев недоступен (видео {VideoId}), пробую YouTube API fallback")]
    public static partial void YtDlpCommentsUnavailable(
        this ILogger logger,
        string videoId,
        Exception exception);
}
