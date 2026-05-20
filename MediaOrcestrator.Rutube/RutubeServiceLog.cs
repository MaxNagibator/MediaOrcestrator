using Microsoft.Extensions.Logging;
using System.Net;

namespace MediaOrcestrator.Rutube;

internal static partial class RutubeServiceLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Инициализация сессии загрузки на RuTube")]
    public static partial void InitUploadSession(this ILogger logger);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Сессия создана. Session ID: {SessionId}, Video ID: {VideoId}")]
    public static partial void UploadSessionCreated(
        this ILogger logger,
        string sessionId,
        string videoId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug, Message = "Создание TUS ресурса для загрузки")]
    public static partial void CreatingTusResource(this ILogger logger);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug, Message = "URL загрузки получен: {UploadUrl}")]
    public static partial void TusResourceCreated(
        this ILogger logger,
        string uploadUrl);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Начало загрузки видео данных")]
    public static partial void StartingDataUpload(this ILogger logger);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Загрузка данных завершена")]
    public static partial void DataUploadCompleted(this ILogger logger);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Debug, Message = "Ожидание обработки на сервере (5 секунд)")]
    public static partial void WaitingServerProcessing(this ILogger logger);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information, Message = "Обновление метаданных видео (черновик)")]
    public static partial void UpdatingDraftMetadata(this ILogger logger);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Метаданные обновлены")]
    public static partial void MetadataUpdated(this ILogger logger);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Information, Message = "Загрузка превью")]
    public static partial void UploadingThumbnail(this ILogger logger);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Обнаружен WebP формат, конвертируем в JPG...")]
    public static partial void ConvertingWebPToJpg(this ILogger logger);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "Конвертация завершена, загружаем JPG")]
    public static partial void WebPConverted(this ILogger logger);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Error, Message = "Ошибка при конвертации WebP в JPG, пробуем загрузить оригинал")]
    public static partial void WebPConversionFailed(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Information, Message = "Превью загружено: {ThumbnailUrl}")]
    public static partial void ThumbnailUploaded(
        this ILogger logger,
        string? thumbnailUrl);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Error, Message = "Ошибка загрузки превьюшки")]
    public static partial void ThumbnailUploadFailed(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(EventId = 2015, Level = LogLevel.Information, Message = "Планирование публикации на {PublishAt}")]
    public static partial void SchedulingPublication(
        this ILogger logger,
        DateTime publishAt);

    [LoggerMessage(EventId = 2016, Level = LogLevel.Information, Message = "Видео успешно запланировано")]
    public static partial void PublicationScheduled(this ILogger logger);

    [LoggerMessage(EventId = 2017, Level = LogLevel.Information, Message = "Немедленная публикация видео")]
    public static partial void PublishingNow(this ILogger logger);

    [LoggerMessage(EventId = 2018, Level = LogLevel.Information, Message = "Видео успешно опубликовано")]
    public static partial void Published(this ILogger logger);

    [LoggerMessage(EventId = 2019, Level = LogLevel.Error, Message = "Ошибка публикации")]
    public static partial void PublicationFailed(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Debug, Message = "Запрос информации о видео {VideoId}")]
    public static partial void RequestingVideoInfo(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2021, Level = LogLevel.Error, Message = "Ошибка получения видео {VideoId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void GetVideoFailed(
        this ILogger logger,
        string videoId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2022, Level = LogLevel.Information, Message = "Получена информация о видео. Название: '{Title}'")]
    public static partial void VideoInfoReceived(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 2023, Level = LogLevel.Debug, Message = "Запрос списка категорий RuTube")]
    public static partial void RequestingCategories(this ILogger logger);

    [LoggerMessage(EventId = 2024, Level = LogLevel.Error, Message = "Ошибка получения категорий. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void GetCategoriesFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2025, Level = LogLevel.Information, Message = "Получено категорий: {Count}")]
    public static partial void CategoriesReceived(
        this ILogger logger,
        int count);

    [LoggerMessage(EventId = 2026, Level = LogLevel.Information, Message = "Отправка DELETE запроса в RuTube API для видео {VideoId}")]
    public static partial void SendingDeleteRequest(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2027, Level = LogLevel.Warning, Message = "Видео {VideoId} не найдено на RuTube, считаем уже удаленным")]
    public static partial void VideoNotFoundTreatedAsDeleted(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2028, Level = LogLevel.Error, Message = "Не удалось удалить видео {VideoId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void DeleteVideoFailed(
        this ILogger logger,
        string videoId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2029, Level = LogLevel.Information, Message = "Видео {VideoId} успешно удалено через RuTube API")]
    public static partial void VideoDeleted(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 2030, Level = LogLevel.Debug, Message = "Загрузка превью для видео {VideoId} из {ThumbnailPath}")]
    public static partial void UploadingThumbnailFile(
        this ILogger logger,
        string videoId,
        string thumbnailPath);

    [LoggerMessage(EventId = 2031, Level = LogLevel.Error, Message = "Ошибка загрузки превью. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void ThumbnailUploadStatusFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2032, Level = LogLevel.Error, Message = "Ошибка получения видео. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void GetVideoListFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2033, Level = LogLevel.Error, Message = "Ошибка инициализации сессии. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void InitSessionFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2034, Level = LogLevel.Error, Message = "Не удалось десериализовать ответ сессии загрузки")]
    public static partial void UploadSessionDeserializationFailed(this ILogger logger);

    [LoggerMessage(EventId = 2035, Level = LogLevel.Error, Message = "Ошибка создания TUS ресурса. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void TusResourceFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2036, Level = LogLevel.Debug, Message = "Начало загрузки файла. Размер: {FileSize} байт")]
    public static partial void StartingFileUpload(
        this ILogger logger,
        long fileSize);

    [LoggerMessage(EventId = 2037, Level = LogLevel.Error, Message = "Ошибка TUS загрузки. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void TusUploadFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2038, Level = LogLevel.Information, Message = "Загрузка файла завершена. Всего загружено: {TotalBytes} байт")]
    public static partial void FileUploadCompleted(
        this ILogger logger,
        long totalBytes);

    [LoggerMessage(EventId = 2039, Level = LogLevel.Error, Message = "Ошибка обновления метаданных. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void MetadataUpdateFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2040, Level = LogLevel.Debug, Message = "Метаданные подтверждены. Название: '{Title}', Категория: '{Category}', Скрыто: {IsHidden}")]
    public static partial void MetadataConfirmed(
        this ILogger logger,
        string title,
        string category,
        bool isHidden);

    [LoggerMessage(EventId = 2041, Level = LogLevel.Error, Message = "Ошибка публикации видео. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void PublishVideoFailed(
        this ILogger logger,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2042, Level = LogLevel.Debug, Message = "Публикация подтверждена. Video ID: {VideoId}, Запланировано: {Timestamp}")]
    public static partial void PublicationConfirmed(
        this ILogger logger,
        string videoId,
        string timestamp);

    [LoggerMessage(EventId = 2050, Level = LogLevel.Error, Message = "Не удалось получить комментарии видео {VideoId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void ListCommentsFailed(
        this ILogger logger,
        string videoId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2051, Level = LogLevel.Information, Message = "Комментарий {CommentId} создан под видео {VideoId} (ответ: {IsReply})")]
    public static partial void CommentCreated(
        this ILogger logger,
        string commentId,
        string videoId,
        bool isReply);

    [LoggerMessage(EventId = 2052, Level = LogLevel.Error, Message = "Не удалось создать комментарий к видео {VideoId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void CreateCommentFailed(
        this ILogger logger,
        string videoId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2053, Level = LogLevel.Information, Message = "Комментарий {CommentId} отредактирован в видео {VideoId}")]
    public static partial void CommentEdited(
        this ILogger logger,
        string commentId,
        string videoId);

    [LoggerMessage(EventId = 2054, Level = LogLevel.Error, Message = "Не удалось отредактировать комментарий {CommentId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void EditCommentFailed(
        this ILogger logger,
        string commentId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2055, Level = LogLevel.Information, Message = "Комментарий {CommentId} удалён в видео {VideoId}")]
    public static partial void CommentDeleted(
        this ILogger logger,
        string commentId,
        string videoId);

    [LoggerMessage(EventId = 2056, Level = LogLevel.Warning, Message = "Комментарий {CommentId} не найден на RuTube, считаем уже удалённым")]
    public static partial void CommentNotFoundTreatedAsDeleted(
        this ILogger logger,
        string commentId);

    [LoggerMessage(EventId = 2057, Level = LogLevel.Error, Message = "Не удалось удалить комментарий {CommentId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void DeleteCommentFailed(
        this ILogger logger,
        string commentId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2058, Level = LogLevel.Information, Message = "Лайк поставлен на комментарий {CommentId}")]
    public static partial void CommentLiked(
        this ILogger logger,
        string commentId);

    [LoggerMessage(EventId = 2059, Level = LogLevel.Error, Message = "Не удалось поставить лайк на {CommentId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void LikeCommentFailed(
        this ILogger logger,
        string commentId,
        HttpStatusCode statusCode,
        string response);

    [LoggerMessage(EventId = 2060, Level = LogLevel.Information, Message = "Лайк снят с комментария {CommentId}")]
    public static partial void CommentUnliked(
        this ILogger logger,
        string commentId);

    [LoggerMessage(EventId = 2061, Level = LogLevel.Error, Message = "Не удалось снять лайк с {CommentId}. Статус: {StatusCode}, Ответ: {Response}")]
    public static partial void UnlikeCommentFailed(
        this ILogger logger,
        string commentId,
        HttpStatusCode statusCode,
        string response);
}
