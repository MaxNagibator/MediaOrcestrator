using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.HardDiskDrive;

internal static partial class HardDiskDriveLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Получение списка медиа с жёсткого диска. Путь: {BasePath}")]
    public static partial void ListingMedia(
        this ILogger logger,
        string basePath);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Директория не существует: {BasePath}")]
    public static partial void DirectoryNotFound(
        this ILogger logger,
        string basePath);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "База данных не найдена: {DbPath}")]
    public static partial void DatabaseNotFoundWarning(
        this ILogger logger,
        string dbPath);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Открытие базы данных: {DbPath}")]
    public static partial void OpeningDatabase(
        this ILogger logger,
        string dbPath);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Найдено файлов в базе данных: {Count}")]
    public static partial void FilesFound(
        this ILogger logger,
        int count);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Debug, Message = "Обработка файла: ID={FileId}, Название='{Title}'")]
    public static partial void ProcessingFile(
        this ILogger logger,
        string fileId,
        string title);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Завершено получение медиа с жёсткого диска")]
    public static partial void MediaListingCompleted(this ILogger logger);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Получение информации о файле с жёсткого диска. ID: {VideoId}")]
    public static partial void RequestingFileInfo(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Error, Message = "Файл не найден в базе данных. ID: {VideoId}")]
    public static partial void FileNotFoundInDatabase(
        this ILogger logger,
        string videoId);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Error, Message = "Физический файл не найден: {FilePath}")]
    public static partial void PhysicalFileNotFound(
        this ILogger logger,
        string filePath);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "Файл найден. ID: {VideoId}, Путь: {FilePath}, Превью: {HasThumbnail}")]
    public static partial void FileFound(
        this ILogger logger,
        string videoId,
        string filePath,
        bool hasThumbnail);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Начало сохранения файла на жёсткий диск. Название: '{Title}'")]
    public static partial void UploadStarting(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Information, Message = "Создание базовой директории: {BasePath}")]
    public static partial void CreatingBaseDirectory(
        this ILogger logger,
        string basePath);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Debug, Message = "Создание директории для файла: {Path}")]
    public static partial void CreatingFileDirectory(
        this ILogger logger,
        string path);

    [LoggerMessage(EventId = 1015, Level = LogLevel.Error, Message = "Временный файл не найден: {TempPath}")]
    public static partial void TempFileNotFound(
        this ILogger logger,
        string tempPath);

    [LoggerMessage(EventId = 1016, Level = LogLevel.Debug, Message = "Перемещение файла. Из: {Source}, В: {Destination}")]
    public static partial void MovingFile(
        this ILogger logger,
        string source,
        string destination);

    [LoggerMessage(EventId = 1017, Level = LogLevel.Debug, Message = "Файл успешно перемещён: {FilePath}")]
    public static partial void FileMoved(
        this ILogger logger,
        string filePath);

    [LoggerMessage(EventId = 1018, Level = LogLevel.Error, Message = "Ошибка при перемещении файла. Из: {Source}, В: {Destination}")]
    public static partial void FileMoveFailed(
        this ILogger logger,
        string source,
        string destination,
        Exception exception);

    [LoggerMessage(EventId = 1019, Level = LogLevel.Debug, Message = "Сохранение информации в базу данных: {DbPath}")]
    public static partial void SavingToDatabase(
        this ILogger logger,
        string dbPath);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Warning, Message = "Файл с ID '{HddId}' уже существует в базе данных. Обновление записи")]
    public static partial void FileExistsUpdating(
        this ILogger logger,
        string hddId);

    [LoggerMessage(EventId = 1021, Level = LogLevel.Information, Message = "Файл успешно сохранён на жёсткий диск. ID: {HddId}, Название: '{Title}'")]
    public static partial void UploadCompleted(
        this ILogger logger,
        string hddId,
        string title);

    [LoggerMessage(EventId = 1022, Level = LogLevel.Error, Message = "Ошибка при сохранении в базу данных. ID: {HddId}")]
    public static partial void DatabaseSaveFailed(
        this ILogger logger,
        string hddId,
        Exception exception);

    [LoggerMessage(EventId = 1023, Level = LogLevel.Information, Message = "Удаление медиа из HDD. ID: {ExternalId}")]
    public static partial void DeletingMedia(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1024, Level = LogLevel.Warning, Message = "Медиа {ExternalId} не найдено в базе данных, считаем уже удаленным")]
    public static partial void MediaAlreadyDeleted(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1025, Level = LogLevel.Information, Message = "Удалена папка медиа: {FolderPath}")]
    public static partial void MediaFolderDeleted(
        this ILogger logger,
        string folderPath);

    [LoggerMessage(EventId = 1026, Level = LogLevel.Error, Message = "Отказано в доступе при удалении папки: {FolderPath}")]
    public static partial void FolderDeleteAccessDenied(
        this ILogger logger,
        string folderPath,
        Exception exception);

    [LoggerMessage(EventId = 1027, Level = LogLevel.Error, Message = "Ошибка ввода-вывода при удалении папки: {FolderPath}")]
    public static partial void FolderDeleteIoError(
        this ILogger logger,
        string folderPath,
        Exception exception);

    [LoggerMessage(EventId = 1028, Level = LogLevel.Warning, Message = "Папка медиа {FolderPath} не существует, пропускаем удаление файлов")]
    public static partial void MediaFolderMissing(
        this ILogger logger,
        string folderPath);

    [LoggerMessage(EventId = 1029, Level = LogLevel.Information, Message = "Медиа {ExternalId} удалено из базы данных HDD")]
    public static partial void MediaDeletedFromDatabase(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1030, Level = LogLevel.Warning, Message = "ffprobe завершился с кодом {ExitCode} для файла: {FilePath}")]
    public static partial void FfprobeNonZeroExit(
        this ILogger logger,
        int exitCode,
        string filePath);

    [LoggerMessage(EventId = 1031, Level = LogLevel.Warning, Message = "Не удалось получить информацию о видео через ffprobe: {FilePath}")]
    public static partial void FfprobeFailed(
        this ILogger logger,
        string filePath,
        Exception exception);

    [LoggerMessage(EventId = 1032, Level = LogLevel.Debug, Message = "Превью скопировано из временного файла: {DestPath}")]
    public static partial void ThumbnailCopiedFromTemp(
        this ILogger logger,
        string destPath);

    [LoggerMessage(EventId = 1033, Level = LogLevel.Debug, Message = "Превью загружено из URL: {ThumbnailUrl}, сохранено: {DestPath}")]
    public static partial void ThumbnailDownloaded(
        this ILogger logger,
        string thumbnailUrl,
        string destPath);

    [LoggerMessage(EventId = 1034, Level = LogLevel.Warning, Message = "Не удалось загрузить превью из URL: {ThumbnailUrl}")]
    public static partial void ThumbnailDownloadFailed(
        this ILogger logger,
        string thumbnailUrl,
        Exception exception);

    [LoggerMessage(EventId = 1035, Level = LogLevel.Debug, Message = "Превью для медиа '{Title}' не найдено, сохраняем без превью")]
    public static partial void NoThumbnailFound(
        this ILogger logger,
        string title);

    [LoggerMessage(EventId = 1036, Level = LogLevel.Information, Message = "Конвертация {Label}. ID: {ExternalId}")]
    public static partial void ConversionStarting(
        this ILogger logger,
        string label,
        string externalId);

    [LoggerMessage(EventId = 1037, Level = LogLevel.Warning, Message = "Медиа {ExternalId} не найдено, пропускаем конвертацию")]
    public static partial void MediaNotFoundSkippingConversion(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1038, Level = LogLevel.Warning, Message = "Конвертация не удалась для {ExternalId}")]
    public static partial void ConversionFailed(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1039, Level = LogLevel.Warning, Message = "Сконвертированный файл пуст или не существует: {Path}")]
    public static partial void ConvertedFileInvalid(
        this ILogger logger,
        string path);

    [LoggerMessage(EventId = 1040, Level = LogLevel.Information, Message = "Конвертация завершена, файл заменён: {ExternalId}")]
    public static partial void ConversionCompleted(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1041, Level = LogLevel.Warning, Message = "Не удалось удалить временный файл: {Path}")]
    public static partial void TempFileDeleteFailed(
        this ILogger logger,
        string path,
        Exception exception);

    [LoggerMessage(EventId = 1042, Level = LogLevel.Warning, Message = "Не удалось удалить файл резервной копии: {Path}")]
    public static partial void BackupFileDeleteFailed(
        this ILogger logger,
        string path,
        Exception exception);

    [LoggerMessage(EventId = 1043, Level = LogLevel.Error, Message = "Не удалось восстановить исходный файл из резервной копии: {BackupPath}")]
    public static partial void BackupRestoreFailed(
        this ILogger logger,
        string backupPath,
        Exception exception);

    [LoggerMessage(EventId = 1044, Level = LogLevel.Information, Message = "Обновление метаданных на HDD. ID: {ExternalId}, Название: '{Title}'")]
    public static partial void UpdateStartingHdd(
        this ILogger logger,
        string externalId,
        string title);

    [LoggerMessage(EventId = 1045, Level = LogLevel.Warning, Message = "Файл {ExternalId} не найден в БД при обновлении метаданных")]
    public static partial void UpdateFileMissing(
        this ILogger logger,
        string externalId);

    [LoggerMessage(EventId = 1046, Level = LogLevel.Information, Message = "Метаданные на HDD обновлены. ID: {ExternalId}, Название: '{Title}'")]
    public static partial void UpdateCompleted(
        this ILogger logger,
        string externalId,
        string title);
}
