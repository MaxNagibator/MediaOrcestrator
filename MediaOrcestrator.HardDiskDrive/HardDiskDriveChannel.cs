using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MediaOrcestrator.HardDiskDrive;

// todo разделить ответственность между ISourceDefinition and ISourceManager (а у SourceManager внутри будет Definition св-во)
public class HardDiskDriveChannel(ILogger<HardDiskDriveChannel> logger) : ISourceType
{
    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "HardDiskDrive";

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "path",
            IsRequired = true,
            Title = "путь к папке хранения",
            Description = "Папка для хранения видеофайлов и базы данных",
        },
        new()
        {
            Key = "dbFileName",
            IsRequired = false,
            Title = "имя файла базы данных",
            DefaultValue = "data.db",
            Description = "Имя файла LiteDB для хранения метаданных",
        },
        new()
        {
            Key = "mainFileName",
            IsRequired = false,
            Title = "имя основного видеофайла",
            DefaultValue = "main.mp4",
            Description = "Имя файла для сохранения видео в папке каждого медиа",
        },
    ];

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var basePath = settings["path"];
        var dbFileName = settings.GetValueOrDefault("dbFileName", "data.db");
        logger.LogInformation("Получение списка медиа с жёсткого диска. Путь: {BasePath}", basePath);

        if (!Directory.Exists(basePath))
        {
            logger.LogWarning("Директория не существует: {BasePath}", basePath);
            yield break;
        }

        var dbPath = Path.Combine(basePath, dbFileName);

        if (!File.Exists(dbPath))
        {
            logger.LogWarning("База данных не найдена: {DbPath}", dbPath);
            yield break;
        }

        logger.LogDebug("Открытие базы данных: {DbPath}", dbPath);

        using var db = new LiteDatabase(dbPath);
        var files = db.GetCollection<DriveMedia>("files").FindAll().ToList();

        logger.LogInformation("Найдено файлов в базе данных: {Count}", files.Count);

        await Task.CompletedTask;

        foreach (var file in files)
        {
            logger.LogDebug("Обработка файла: ID={FileId}, Название='{Title}'", file.Id, file.Title);
            yield return CreateMediaDto(file, basePath);
        }

        logger.LogInformation("Завершено получение медиа с жёсткого диска");
    }

    public async Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        var basePath = settings["path"];
        var dbFileName = settings.GetValueOrDefault("dbFileName", "data.db");

        if (!Directory.Exists(basePath))
        {
            return null;
        }

        var dbPath = Path.Combine(basePath, dbFileName);

        if (!File.Exists(dbPath))
        {
            return null;
        }

        using var db = new LiteDatabase(dbPath);
        var file = db.GetCollection<DriveMedia>("files").FindById(externalId);

        if (file == null)
        {
            return null;
        }

        await Task.CompletedTask;

        return CreateMediaDto(file, basePath);
    }

    public Task<MediaDto> Download(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Получение информации о файле с жёсткого диска. ID: {VideoId}", videoId);

        var basePath = settings["path"];
        var dbFileName = settings.GetValueOrDefault("dbFileName", "data.db");
        var dbPath = Path.Combine(basePath, dbFileName);

        if (!File.Exists(dbPath))
        {
            logger.LogError("База данных не найдена: {DbPath}", dbPath);
            throw new FileNotFoundException("База данных не найдена", dbPath);
        }

        logger.LogDebug("Открытие базы данных: {DbPath}", dbPath);

        using var db = new LiteDatabase(dbPath);
        var file = db.GetCollection<DriveMedia>("files").FindById(videoId);

        if (file == null)
        {
            logger.LogError("Файл не найден в базе данных. ID: {VideoId}", videoId);
            throw new InvalidOperationException($"Файл с ID '{videoId}' не найден в базе данных");
        }

        var fullPath = Path.Combine(basePath, file.Id, file.Path);

        if (!File.Exists(fullPath))
        {
            logger.LogError("Физический файл не найден: {FilePath}", fullPath);
            throw new FileNotFoundException("Физический файл не найден", fullPath);
        }

        var thumbnailFullPath = !string.IsNullOrEmpty(file.PreviewPath)
            ? Path.Combine(basePath, file.Id, file.PreviewPath)
            : null;

        var thumbnailExists = thumbnailFullPath != null && File.Exists(thumbnailFullPath);

        logger.LogInformation("Файл найден. ID: {VideoId}, Путь: {FilePath}, Превью: {HasThumbnail}",
            videoId, fullPath, thumbnailExists);

        return Task.FromResult(new MediaDto
        {
            Id = file.Id,
            Description = file.Description,
            Title = file.Title,
            TempDataPath = fullPath,
            TempPreviewPath = thumbnailExists ? thumbnailFullPath! : null,
        });
    }

    public async Task<string> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Начало сохранения файла на жёсткий диск. Название: '{Title}'", media.Title);

        var hddId = media.Id;
        var basePath = settings["path"];
        var dbFileName = settings.GetValueOrDefault("dbFileName", "data.db");
        var mainFileName = settings.GetValueOrDefault("mainFileName", "main.mp4");

        if (!Directory.Exists(basePath))
        {
            logger.LogInformation("Создание базовой директории: {BasePath}", basePath);
            Directory.CreateDirectory(basePath);
        }

        var path = Path.Combine(basePath, hddId);

        if (!Directory.Exists(path))
        {
            logger.LogDebug("Создание директории для файла: {Path}", path);
            Directory.CreateDirectory(path);
        }

        var mainFilePath = Path.Combine(path, mainFileName);

        if (!File.Exists(media.TempDataPath))
        {
            logger.LogError("Временный файл не найден: {TempPath}", media.TempDataPath);
            throw new FileNotFoundException("Временный файл не найден", media.TempDataPath);
        }

        logger.LogDebug("Перемещение файла. Из: {Source}, В: {Destination}", media.TempDataPath, mainFilePath);

        try
        {
            // todo идеалогически move не верный, но пока безразлично
            File.Move(media.TempDataPath, mainFilePath, true);
            logger.LogDebug("Файл успешно перемещён: {FilePath}", mainFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при перемещении файла. Из: {Source}, В: {Destination}",
                media.TempDataPath, mainFilePath);

            throw;
        }

        var thumbnailFileName = await SaveThumbnailAsync(media, path, cancellationToken);

        // todo дублирование
        var dbPath = Path.Combine(basePath, dbFileName);
        logger.LogDebug("Сохранение информации в базу данных: {DbPath}", dbPath);

        try
        {
            using var db = new LiteDatabase(dbPath);
            var collection = db.GetCollection<DriveMedia>("files");

            var existingFile = collection.FindById(hddId);
            if (existingFile != null)
            {
                logger.LogWarning("Файл с ID '{HddId}' уже существует в базе данных. Обновление записи", hddId);
                collection.Update(new DriveMedia
                {
                    Id = hddId,
                    Description = media.Description,
                    Title = media.Title,
                    Path = mainFileName,
                    PreviewPath = thumbnailFileName ?? string.Empty,
                });
            }
            else
            {
                collection.Insert(new DriveMedia
                {
                    Id = hddId,
                    Description = media.Description,
                    Title = media.Title,
                    Path = mainFileName,
                    PreviewPath = thumbnailFileName ?? string.Empty,
                });
            }

            logger.LogInformation("Файл успешно сохранён на жёсткий диск. ID: {HddId}, Название: '{Title}'",
                hddId, media.Title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при сохранении в базу данных. ID: {HddId}", hddId);
            throw;
        }

        return hddId;
    }

    public Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Удаление медиа из HDD. ID: {ExternalId}", externalId);

        // TODO: Дублирование
        var basePath = settings["path"];
        var dbFileName = settings.GetValueOrDefault("dbFileName", "data.db");
        var dbPath = Path.Combine(basePath, dbFileName);

        if (!File.Exists(dbPath))
        {
            logger.LogError("База данных не найдена: {DbPath}", dbPath);
            throw new FileNotFoundException("База данных не найдена", dbPath);
        }

        using var db = new LiteDatabase(dbPath);
        var collection = db.GetCollection<DriveMedia>("files");
        var file = collection.FindById(externalId);

        if (file == null)
        {
            // TODO: Возможно не верный мув, но позволяет громоздить логику принудительного удаления
            logger.LogWarning("Медиа {ExternalId} не найдено в базе данных, считаем уже удаленным", externalId);
            return Task.CompletedTask;
        }

        var mediaFolder = Path.Combine(basePath, externalId);
        if (Directory.Exists(mediaFolder))
        {
            try
            {
                Directory.Delete(mediaFolder, true);
                logger.LogInformation("Удалена папка медиа: {FolderPath}", mediaFolder);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Отказано в доступе при удалении папки: {FolderPath}", mediaFolder);
                throw new UnauthorizedAccessException($"Нет прав для удаления папки: {mediaFolder}", ex);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Ошибка ввода-вывода при удалении папки: {FolderPath}", mediaFolder);
                throw new IOException($"Ошибка при удалении папки: {mediaFolder}", ex);
            }
        }
        else
        {
            logger.LogWarning("Папка медиа {FolderPath} не существует, пропускаем удаление файлов", mediaFolder);
        }

        collection.Delete(externalId);
        logger.LogInformation("Медиа {ExternalId} удалено из базы данных HDD", externalId);

        return Task.CompletedTask;
    }

    private static MediaDto CreateMediaDto(DriveMedia file, string basePath)
    {
        var fullPath = Path.Combine(basePath, file.Id, file.Path);
        var fileInfo = new FileInfo(fullPath);
        var fileExists = fileInfo.Exists;

        var previewFullPath = !string.IsNullOrEmpty(file.PreviewPath)
            ? Path.Combine(basePath, file.Id, file.PreviewPath)
            : string.Empty;

        return new()
        {
            Id = file.Id,
            Description = file.Description,
            Title = file.Title,
            PreviewPath = previewFullPath,
            Metadata =
            [
                new()
                {
                    Key = "Size",
                    DisplayName = "Размер",
                    Value = fileExists ? fileInfo.Length.ToString() : "0",
                    DisplayType = "ByteSize",
                },
                new()
                {
                    Key = "CreationDate",
                    DisplayName = "Дата создания",
                    Value = fileExists ? fileInfo.CreationTime.ToString("O") : "",
                    DisplayType = "System.DateTime",
                },
            ],
        };
    }

    private async Task<string?> SaveThumbnailAsync(MediaDto media, string mediaFolder, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(media.TempPreviewPath) && File.Exists(media.TempPreviewPath))
        {
            var ext = Path.GetExtension(media.TempPreviewPath);
            var thumbnailFileName = $"thumbnail{(string.IsNullOrEmpty(ext) ? ".jpg" : ext)}";
            var destPath = Path.Combine(mediaFolder, thumbnailFileName);
            File.Copy(media.TempPreviewPath, destPath, true);
            logger.LogDebug("Превью скопировано из временного файла: {DestPath}", destPath);
            return thumbnailFileName;
        }

        if (!string.IsNullOrEmpty(media.PreviewPath) && Uri.TryCreate(media.PreviewPath, UriKind.Absolute, out var thumbnailUri))
        {
            try
            {
                var ext = Path.GetExtension(thumbnailUri.LocalPath);
                var thumbnailFileName = $"thumbnail{(string.IsNullOrEmpty(ext) ? ".jpg" : ext)}";
                var destPath = Path.Combine(mediaFolder, thumbnailFileName);

                using var httpClient = new HttpClient();
                var thumbnailBytes = await httpClient.GetByteArrayAsync(thumbnailUri, cancellationToken);
                await File.WriteAllBytesAsync(destPath, thumbnailBytes, cancellationToken);
                logger.LogDebug("Превью загружено из URL: {ThumbnailUrl}, сохранено: {DestPath}", media.PreviewPath, destPath);
                return thumbnailFileName;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось загрузить превью из URL: {ThumbnailUrl}", media.PreviewPath);
            }
        }

        logger.LogDebug("Превью для медиа '{Title}' не найдено, сохраняем без превью", media.Title);
        return null;
    }
}

public class DriveMedia
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string PreviewPath { get; set; } = string.Empty;
}
