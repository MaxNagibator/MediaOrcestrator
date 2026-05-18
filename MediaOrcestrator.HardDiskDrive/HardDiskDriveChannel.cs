using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MediaOrcestrator.HardDiskDrive;

// todo разделить ответственность между ISourceDefinition and ISourceManager (а у SourceManager внутри будет Definition св-во)
public sealed class HardDiskDriveChannel(
    ILogger<HardDiskDriveChannel> logger,
    FfprobeMetadataReader metadataReader,
    HardDiskDriveCodecConverter codecConverter,
    IHttpClientFactory httpClientFactory) : ISourceType, IToolConsumer
{
    public const string ThumbnailClientName = "HardDiskDrive.Thumbnail";

    public SyncDirection ChannelType => SyncDirection.Full;

    public string Name => "HardDiskDrive";

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "path",
            IsRequired = true,
            Title = "путь к папке хранения",
            Description = "Папка для хранения видеофайлов и базы данных",
            Type = SettingType.FolderPath,
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

    public IReadOnlyList<ToolDescriptor> RequiredTools { get; } =
    [
        WellKnownTools.FFmpegWithProbeDescriptor,
    ];

    public Uri? GetExternalUri(
        string externalId,
        Dictionary<string, string> settings)
    {
        if (!settings.TryGetValue("path", out var basePath))
        {
            return null;
        }

        var folderPath = Path.Combine(basePath, externalId);
        return new(folderPath);
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(
        Dictionary<string, string> settings,
        bool isFull,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (basePath, dbPath) = HardDiskDriveStore.ResolveDbPath(settings);
        logger.ListingMedia(basePath);

        if (!Directory.Exists(basePath))
        {
            logger.DirectoryNotFound(basePath);
            yield break;
        }

        if (!File.Exists(dbPath))
        {
            logger.DatabaseNotFoundWarning(dbPath);
            yield break;
        }

        logger.OpeningDatabase(dbPath);

        using var db = HardDiskDriveStore.OpenDatabase(dbPath);
        var files = db.GetCollection<DriveMedia>("files").FindAll().ToList();

        logger.FilesFound(files.Count);

        foreach (var file in files)
        {
            logger.ProcessingFile(file.Id, file.Title);
            yield return await CreateMediaDtoAsync(file, basePath, cancellationToken);
        }

        logger.MediaListingCompleted();
    }

    public async Task<MediaDto?> GetMediaByIdAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        using var db = HardDiskDriveStore.TryOpen(settings, out var basePath);

        var file = db?.GetCollection<DriveMedia>("files").FindById(externalId);

        if (file == null)
        {
            return null;
        }

        return await CreateMediaDtoAsync(file, basePath, cancellationToken);
    }

    public Task<MediaDto> DownloadAsync(
        string videoId,
        Dictionary<string, string> settings,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.RequestingFileInfo(videoId);

        using var db = HardDiskDriveStore.OpenOrThrow(settings, out var basePath);
        var file = db.GetCollection<DriveMedia>("files").FindById(videoId);

        if (file == null)
        {
            logger.FileNotFoundInDatabase(videoId);
            throw new InvalidOperationException($"Файл с ID '{videoId}' не найден в базе данных");
        }

        var fullPath = Path.Combine(basePath, file.Id, file.Path);

        if (!File.Exists(fullPath))
        {
            logger.PhysicalFileNotFound(fullPath);
            throw new FileNotFoundException("Физический файл не найден", fullPath);
        }

        var thumbnailFullPath = !string.IsNullOrEmpty(file.PreviewPath)
            ? Path.Combine(basePath, file.Id, file.PreviewPath)
            : null;

        var thumbnailExists = thumbnailFullPath != null && File.Exists(thumbnailFullPath);

        logger.FileFound(videoId, fullPath, thumbnailExists);

        progress?.Report(new(100));

        return Task.FromResult(new MediaDto
        {
            Id = file.Id,
            Description = file.Description,
            Title = file.Title,
            TempDataPath = fullPath,
            TempPreviewPath = thumbnailExists ? thumbnailFullPath! : null,
        });
    }

    public async Task<UploadResult> UploadAsync(
        MediaDto media,
        Dictionary<string, string> settings,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        logger.UploadStarting(media.Title);

        var hddId = media.Id;
        var (basePath, dbPath) = HardDiskDriveStore.ResolveDbPath(settings);
        var mainFileName = settings.GetValueOrDefault("mainFileName", "main.mp4");

        if (!Directory.Exists(basePath))
        {
            logger.CreatingBaseDirectory(basePath);
            Directory.CreateDirectory(basePath);
        }

        var path = Path.Combine(basePath, hddId);

        if (!Directory.Exists(path))
        {
            logger.CreatingFileDirectory(path);
            Directory.CreateDirectory(path);
        }

        var mainFilePath = Path.Combine(path, mainFileName);

        if (!File.Exists(media.TempDataPath))
        {
            logger.TempFileNotFound(media.TempDataPath);
            throw new FileNotFoundException("Временный файл не найден", media.TempDataPath);
        }

        logger.MovingFile(media.TempDataPath, mainFilePath);

        try
        {
            // todo идеалогически move не верный, но пока безразлично
            File.Move(media.TempDataPath, mainFilePath, true);
            logger.FileMoved(mainFilePath);
        }
        catch (Exception ex)
        {
            logger.FileMoveFailed(media.TempDataPath, mainFilePath, ex);
            throw;
        }

        var thumbnailFileName = await SaveThumbnailAsync(media, path, cancellationToken);

        logger.SavingToDatabase(dbPath);

        try
        {
            using var db = HardDiskDriveStore.OpenDatabase(dbPath);
            var collection = db.GetCollection<DriveMedia>("files");

            var existingFile = collection.FindById(hddId);
            if (existingFile != null)
            {
                logger.FileExistsUpdating(hddId);
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

            logger.UploadCompleted(hddId, media.Title);
        }
        catch (Exception ex)
        {
            logger.DatabaseSaveFailed(hddId, ex);
            throw;
        }

        progress?.Report(new(100));

        return new()
        {
            Status = MediaStatusHelper.Ok(),
            Id = hddId,
        };
    }

    public Task<UploadResult> UpdateAsync(
        string externalId,
        MediaDto tempMedia,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(
        string externalId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        logger.DeletingMedia(externalId);

        using var db = HardDiskDriveStore.OpenOrThrow(settings, out var basePath);
        var collection = db.GetCollection<DriveMedia>("files");
        var file = collection.FindById(externalId);

        if (file == null)
        {
            // TODO: Возможно не верный мув, но позволяет громоздить логику принудительного удаления
            logger.MediaAlreadyDeleted(externalId);
            return Task.CompletedTask;
        }

        var mediaFolder = Path.Combine(basePath, externalId);
        if (Directory.Exists(mediaFolder))
        {
            try
            {
                Directory.Delete(mediaFolder, true);
                logger.MediaFolderDeleted(mediaFolder);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.FolderDeleteAccessDenied(mediaFolder, ex);
                throw new UnauthorizedAccessException($"Нет прав для удаления папки: {mediaFolder}", ex);
            }
            catch (IOException ex)
            {
                logger.FolderDeleteIoError(mediaFolder, ex);
                throw new IOException($"Ошибка при удалении папки: {mediaFolder}", ex);
            }
        }
        else
        {
            logger.MediaFolderMissing(mediaFolder);
        }

        collection.Delete(externalId);
        logger.MediaDeletedFromDatabase(externalId);

        return Task.CompletedTask;
    }

    public ConvertType[] GetAvailableConvertTypes()
    {
        return CodecConversions.All
            .Select(c => new ConvertType { Id = c.Id, Name = c.Name })
            .ToArray();
    }

    public ConvertAvailability CheckConvertAvailability(
        int typeId,
        MediaDto media)
    {
        if (CodecConversions.Find(typeId) is not { } conversion)
        {
            return new(false, $"Неизвестный тип конвертации: {typeId}");
        }

        var codec = media.Metadata?.FirstOrDefault(x => x.Key == "VideoCodec")?.Value;

        if (string.IsNullOrEmpty(codec))
        {
            return new(false, "Кодек не определён. Убедитесь, что ffprobe доступен.");
        }

        return codec == conversion.SourceCodec
            ? new(true, null)
            : new ConvertAvailability(false, $"Кодек '{codec}', конвертация {conversion.Label} неприменима");
    }

    public Task ConvertAsync(
        int typeId,
        string externalId,
        Dictionary<string, string> settings,
        IProgress<ConvertProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (CodecConversions.Find(typeId) is null)
        {
            throw new NotImplementedException("type not implemented " + typeId);
        }

        return RunConversion(typeId, externalId, settings, progress, cancellationToken);
    }

    private static TimeSpan TryParseDuration(MediaDto media)
    {
        var durationStr = media.Metadata?.FirstOrDefault(x => x.Key == "Duration")?.Value;
        return durationStr != null && TimeSpan.TryParse(durationStr, out var parsed)
            ? parsed
            : TimeSpan.Zero;
    }

    private async Task<MediaDto> CreateMediaDtoAsync(
        DriveMedia file,
        string basePath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(basePath, file.Id, file.Path);
        var fileInfo = new FileInfo(fullPath);
        var fileExists = fileInfo.Exists;

        var previewFullPath = !string.IsNullOrEmpty(file.PreviewPath)
            ? Path.Combine(basePath, file.Id, file.PreviewPath)
            : string.Empty;

        var metadata = new List<MetadataItem>
        {
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
        };

        if (!string.IsNullOrEmpty(previewFullPath))
        {
            metadata.Add(new() { Key = "PreviewUrl", Value = previewFullPath });
        }

        if (fileExists)
        {
            var videoInfo = await metadataReader.ReadAsync(fullPath, cancellationToken);

            if (videoInfo is not null)
            {
                metadata.AddRange(videoInfo);
            }
        }

        return new()
        {
            Id = file.Id,
            Description = file.Description,
            Title = file.Title,
            PreviewPath = previewFullPath,
            Metadata = metadata,
        };
    }

    private async Task<string?> SaveThumbnailAsync(
        MediaDto media,
        string mediaFolder,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(media.TempPreviewPath) && File.Exists(media.TempPreviewPath))
        {
            var ext = Path.GetExtension(media.TempPreviewPath);
            var thumbnailFileName = $"thumbnail{(string.IsNullOrEmpty(ext) ? ".jpg" : ext)}";
            var destPath = Path.Combine(mediaFolder, thumbnailFileName);
            File.Copy(media.TempPreviewPath, destPath, true);
            logger.ThumbnailCopiedFromTemp(destPath);
            return thumbnailFileName;
        }

        if (!string.IsNullOrEmpty(media.PreviewPath) && Uri.TryCreate(media.PreviewPath, UriKind.Absolute, out var thumbnailUri))
        {
            try
            {
                var ext = Path.GetExtension(thumbnailUri.LocalPath);
                var thumbnailFileName = $"thumbnail{(string.IsNullOrEmpty(ext) ? ".jpg" : ext)}";
                var destPath = Path.Combine(mediaFolder, thumbnailFileName);

                var httpClient = httpClientFactory.CreateClient(ThumbnailClientName);
                using var request = new HttpRequestMessage(HttpMethod.Get, thumbnailUri);
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = File.Create(destPath);
                await responseStream.CopyToAsync(fileStream, cancellationToken);

                logger.ThumbnailDownloaded(media.PreviewPath, destPath);
                return thumbnailFileName;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.ThumbnailDownloadFailed(media.PreviewPath, ex);
            }
        }

        logger.NoThumbnailFound(media.Title);
        return null;
    }

    private async Task RunConversion(
        int typeId,
        string externalId,
        Dictionary<string, string> settings,
        IProgress<ConvertProgress>? progress,
        CancellationToken cancellationToken)
    {
        var media = await GetMediaByIdAsync(externalId, settings, cancellationToken);
        if (media == null)
        {
            logger.MediaNotFoundSkippingConversion(externalId);
            return;
        }

        var availability = CheckConvertAvailability(typeId, media);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason ?? "Конвертация недоступна");
        }

        var totalDuration = TryParseDuration(media);
        var srcFilePath = HardDiskDriveStore.ResolveSourceFilePath(externalId, settings);

        await codecConverter.ConvertAsync(typeId, externalId, srcFilePath, totalDuration, progress, cancellationToken);
    }
}
