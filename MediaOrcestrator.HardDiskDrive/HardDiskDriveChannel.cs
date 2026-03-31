using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.HardDiskDrive;

// TODO: Костыль Connection=shared
// TODO: Что-то класс совсем разросся
// todo разделить ответственность между ISourceDefinition and ISourceManager (а у SourceManager внутри будет Definition св-во)
public partial class HardDiskDriveChannel(ILogger<HardDiskDriveChannel> logger, IToolPathProvider toolPathProvider) : ISourceType, IToolConsumer
{
    private string? _h264Encoder;

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

    public Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
    {
        if (!settings.TryGetValue("path", out var basePath))
        {
            return null;
        }

        var folderPath = Path.Combine(basePath, externalId);
        return new(folderPath);
    }

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        using var db = new LiteDatabase($"Filename={dbPath};Connection=shared");
        var files = db.GetCollection<DriveMedia>("files").FindAll().ToList();

        logger.LogInformation("Найдено файлов в базе данных: {Count}", files.Count);

        foreach (var file in files)
        {
            logger.LogDebug("Обработка файла: ID={FileId}, Название='{Title}'", file.Id, file.Title);
            yield return await CreateMediaDtoAsync(file, basePath, cancellationToken);
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

        using var db = new LiteDatabase($"Filename={dbPath};Connection=shared");
        var file = db.GetCollection<DriveMedia>("files").FindById(externalId);

        if (file == null)
        {
            return null;
        }

        return await CreateMediaDtoAsync(file, basePath, cancellationToken);
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

        using var db = new LiteDatabase($"Filename={dbPath};Connection=shared");
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

    public async Task<UploadResult> Upload(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
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
            using var db = new LiteDatabase($"Filename={dbPath};Connection=shared");
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

        return new()
        {
            Status = MediaStatusHelper.Ok(),
            Id = hddId,
        };
    }

    public Task<UploadResult> Update(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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

        using var db = new LiteDatabase($"Filename={dbPath};Connection=shared");
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

    public ConvertType[] GetAvailabelConvertTypes()
    {
        return
        [
            new()
            {
                Id = 1,
                Name = "vp9 to h264",
            },
            new()
            {
                Id = 2,
                Name = "h264 to vp9",
            },
        ];
    }

    public ConvertAvailability CheckConvertAvailability(int typeId, MediaDto media)
    {
        var codec = media.Metadata?.FirstOrDefault(x => x.Key == "VideoCodec")?.Value;

        if (string.IsNullOrEmpty(codec))
        {
            return new(false, "Кодек не определён. Убедитесь, что ffprobe доступен.");
        }

        return typeId switch
        {
            1 => codec == "vp9"
                ? new ConvertAvailability(true, null)
                : new ConvertAvailability(false, $"Кодек '{codec}', конвертация VP9→H264 неприменима"),
            2 => codec == "h264"
                ? new ConvertAvailability(true, null)
                : new ConvertAvailability(false, $"Кодек '{codec}', конвертация H264→VP9 неприменима"),
            _ => new(false, $"Неизвестный тип конвертации: {typeId}"),
        };
    }

    public Task ConvertAsync(int typeId, string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        return typeId switch
        {
            1 or 2 => ConvertVideoCodec(externalId, settings, typeId, cancellationToken: cancellationToken),
            _ => throw new NotImplementedException("type not implemented " + typeId),
        };
    }

    public Task ConvertAsync(
        int typeId,
        string externalId,
        Dictionary<string, string> settings,
        IProgress<ConvertProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        return typeId switch
        {
            1 or 2 => ConvertVideoCodec(externalId, settings, typeId, progress, cancellationToken),
            _ => throw new NotImplementedException("type not implemented " + typeId),
        };
    }

    private static bool TryParseFraction(string value, out double result)
    {
        result = 0;
        var parts = value.Split('/');

        if (parts.Length == 2
            && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], CultureInfo.InvariantCulture, out var denominator)
            && denominator != 0)
        {
            result = numerator / denominator;
            return true;
        }

        return double.TryParse(value, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseFFmpegTime(string line, out double seconds)
    {
        seconds = 0;
        var match = FFmpegTimeRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        seconds = int.Parse(match.Groups[1].Value) * 3600
                  + int.Parse(match.Groups[2].Value) * 60
                  + int.Parse(match.Groups[3].Value)
                  + int.Parse(match.Groups[4].Value) / 100.0;

        return true;
    }

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})")]
    private static partial Regex FFmpegTimeRegex();

    private async Task<MediaDto> CreateMediaDtoAsync(DriveMedia file, string basePath, CancellationToken cancellationToken)
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

        if (fileExists)
        {
            var videoInfo = await GetVideoInfoAsync(fullPath, cancellationToken);

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

    private async Task<List<MetadataItem>?> GetVideoInfoAsync(string filePath, CancellationToken cancellationToken)
    {
        var ffprobePath = toolPathProvider.GetCompanionPath(WellKnownTools.FFmpeg, "ffprobe");

        if (ffprobePath is null)
        {
            return null;
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
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                return ParseFfprobeOutput(output);
            }

            logger.LogWarning("ffprobe завершился с кодом {ExitCode} для файла: {FilePath}", process.ExitCode, filePath);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить информацию о видео через ffprobe: {FilePath}", filePath);
            return null;
        }
    }

    private List<MetadataItem>? ParseFfprobeOutput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new List<MetadataItem>();

        if (root.TryGetProperty("format", out var format))
        {
            if (format.TryGetProperty("duration", out var duration)
                && double.TryParse(duration.GetString(), CultureInfo.InvariantCulture, out var seconds))
            {
                result.Add(new()
                {
                    Key = "Duration",
                    DisplayName = "Длительность",
                    Value = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"),
                    DisplayType = "System.TimeSpan",
                });
            }

            if (format.TryGetProperty("bit_rate", out var bitRate))
            {
                result.Add(new()
                {
                    Key = "Bitrate",
                    DisplayName = "Битрейт",
                    Value = bitRate.GetString() ?? "",
                    DisplayType = "Bitrate",
                });
            }
        }

        if (!root.TryGetProperty("streams", out var streams))
        {
            return result.Count > 0 ? result : null;
        }

        foreach (var stream in streams.EnumerateArray())
        {
            var codecType = stream.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;

            if (codecType != "video")
            {
                continue;
            }

            if (stream.TryGetProperty("width", out var w) && stream.TryGetProperty("height", out var h))
            {
                result.Add(new()
                {
                    Key = "Resolution",
                    DisplayName = "Разрешение",
                    Value = $"{w.GetInt32()}x{h.GetInt32()}",
                });
            }

            if (stream.TryGetProperty("codec_name", out var videoCodec))
            {
                result.Add(new()
                {
                    Key = "VideoCodec",
                    DisplayName = "Видеокодек",
                    Value = videoCodec.GetString() ?? "",
                });
            }

            if (stream.TryGetProperty("r_frame_rate", out var frameRate))
            {
                var fpsStr = frameRate.GetString();
                if (fpsStr is not null && TryParseFraction(fpsStr, out var fps))
                {
                    result.Add(new()
                    {
                        Key = "FrameRate",
                        DisplayName = "Частота кадров",
                        Value = fps.ToString("F2", CultureInfo.InvariantCulture),
                        DisplayType = "FPS",
                    });
                }
            }

            break;
        }

        foreach (var stream in streams.EnumerateArray())
        {
            var codecType = stream.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;

            if (codecType == "audio")
            {
                if (stream.TryGetProperty("codec_name", out var audioCodec))
                {
                    result.Add(new()
                    {
                        Key = "AudioCodec",
                        DisplayName = "Аудиокодек",
                        Value = audioCodec.GetString() ?? "",
                    });
                }

                if (stream.TryGetProperty("sample_rate", out var sampleRate))
                {
                    result.Add(new()
                    {
                        Key = "SampleRate",
                        DisplayName = "Частота дискретизации",
                        Value = sampleRate.GetString() ?? "",
                        DisplayType = "Hz",
                    });
                }

                break;
            }
        }

        return result.Count > 0 ? result : null;
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

    private async Task ConvertVideoCodec(
        string externalId,
        Dictionary<string, string> settings,
        int typeId,
        IProgress<ConvertProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var label = typeId == 1 ? "VP9→H264" : "H264→VP9";
        logger.LogInformation("Конвертация {Label}. ID: {ExternalId}", label, externalId);

        var m = await GetMediaByIdAsync(externalId, settings, cancellationToken);
        if (m == null)
        {
            logger.LogWarning("Медиа {ExternalId} не найдено, пропускаем конвертацию", externalId);
            return;
        }

        var availability = CheckConvertAvailability(typeId, m);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason ?? "Конвертация недоступна");
        }

        var durationStr = m.Metadata?.FirstOrDefault(x => x.Key == "Duration")?.Value;
        var totalDuration = TimeSpan.Zero;
        if (durationStr != null && TimeSpan.TryParse(durationStr, out var parsed))
        {
            totalDuration = parsed;
        }

        // TODO: Дублирование
        var basePath = settings["path"];
        var dbFileName = settings.GetValueOrDefault("dbFileName", "data.db");
        var dbPath = Path.Combine(basePath, dbFileName);

        if (!File.Exists(dbPath))
        {
            throw new FileNotFoundException("База данных не найдена", dbPath);
        }

        string fullPath;
        using (var db = new LiteDatabase($"Filename={dbPath};Connection=shared"))
        {
            var collection = db.GetCollection<DriveMedia>("files");
            var file = collection.FindById(externalId);

            if (file == null)
            {
                throw new InvalidOperationException($"Медиа {externalId} не найдено в базе данных");
            }

            fullPath = Path.Combine(basePath, file.Id, file.Path);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Исходный файл не найден", fullPath);
        }

        var outputExt = typeId == 2 ? ".webm" : ".mp4";
        var convertPath = fullPath + "_convert" + outputExt;
        var backupPath = fullPath + ".bak";
        var conversionSucceeded = false;

        try
        {
            var fileName = Path.GetFileName(fullPath);
            var success = await RunFfmpegConvertAsync(fullPath, convertPath, typeId, totalDuration,
                progress == null ? null : new Progress<double>(p => progress.Report(new(p, fileName))),
                cancellationToken);

            if (!success)
            {
                logger.LogWarning("Конвертация не удалась для {ExternalId}", externalId);
                return;
            }

            var convertedFileInfo = new FileInfo(convertPath);
            if (!convertedFileInfo.Exists || convertedFileInfo.Length == 0)
            {
                logger.LogWarning("Сконвертированный файл пуст или не существует: {Path}", convertPath);
                return;
            }

            File.Move(fullPath, backupPath, true);

            try
            {
                File.Move(convertPath, fullPath, true);
            }
            catch
            {
                File.Move(backupPath, fullPath, true);
                throw;
            }

            File.Delete(backupPath);
            conversionSucceeded = true;

            logger.LogInformation("Конвертация завершена, файл заменён: {ExternalId}", externalId);
        }
        finally
        {
            if (!conversionSucceeded && File.Exists(convertPath))
            {
                try
                {
                    File.Delete(convertPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Не удалось удалить временный файл: {Path}", convertPath);
                }
            }

            if (File.Exists(backupPath) && File.Exists(fullPath))
            {
                try
                {
                    File.Delete(backupPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Не удалось удалить файл резервной копии: {Path}", backupPath);
                }
            }
        }
    }

    private async Task<bool> RunFfmpegConvertAsync(
        string inputPath,
        string outputPath,
        int typeId,
        TimeSpan totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg);
        if (ffmpegPath is null)
        {
            logger.LogError("ffmpeg не найден, конвертация невозможна");
            return false;
        }

        string arguments;

        if (typeId == 2)
        {
            arguments = $"-i \"{inputPath}\" -c:v libvpx-vp9 -b:v 0 -crf 30 -deadline good -cpu-used 2 -c:a libopus \"{outputPath}\"";
        }
        else
        {
            var h264Encoder = await GetH264EncoderAsync();
            var preset = h264Encoder == "h264_nvenc" ? "slow" : "medium";
            arguments = $"-i \"{inputPath}\" -c:v {h264Encoder} -preset {preset} -c:a copy \"{outputPath}\"";
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);

            if (process is null)
            {
                logger.LogError("Не удалось запустить ffmpeg процесс");
                return false;
            }

            await using (cancellationToken.Register(ForceStop))
            {
                var totalSeconds = totalDuration.TotalSeconds;
                var stderrBuilder = new StringBuilder();

                while (await process.StandardError.ReadLineAsync(cancellationToken) is { } line)
                {
                    stderrBuilder.AppendLine(line);

                    if (!(totalSeconds > 0) || !TryParseFFmpegTime(line, out var currentSeconds))
                    {
                        continue;
                    }

                    var percent = Math.Min(currentSeconds / totalSeconds * 100, 100);
                    progress?.Report(percent);
                }

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    logger.LogInformation("ffmpeg конвертация завершена успешно: {OutputPath}", outputPath);
                    return true;
                }

                logger.LogWarning("ffmpeg завершился с кодом {ExitCode} для файла: {FilePath}. Stderr: {Stderr}",
                    process.ExitCode, inputPath, stderrBuilder.ToString());

                return false;
            }

            // TODO: Костыль
            void ForceStop()
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сконвертировать видео через ffmpeg: {FilePath}", inputPath);
            return false;
        }
    }

    private async Task<string> GetH264EncoderAsync()
    {
        if (_h264Encoder != null)
        {
            return _h264Encoder;
        }

        var ffmpegPath = toolPathProvider.GetToolPath(WellKnownTools.FFmpeg);
        if (ffmpegPath == null)
        {
            _h264Encoder = "libx264";
            return _h264Encoder;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-encoders",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _h264Encoder = "libx264";
                return _h264Encoder;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            _h264Encoder = output.Contains("h264_nvenc") ? "h264_nvenc" : "libx264";
            logger.LogInformation("Выбран H264 кодек: {Encoder}", _h264Encoder);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить доступные кодеки, используем libx264");
            _h264Encoder = "libx264";
        }

        return _h264Encoder;
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
