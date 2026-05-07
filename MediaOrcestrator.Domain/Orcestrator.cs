using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public class Orcestrator(
    PluginManager pluginManager,
    LiteDatabase db,
    TempManager tempManager,
    StateManager stateManager,
    ActionHolder actionHolder,
    ILogger<Orcestrator> logger)
{
    public Dictionary<string, ISourceType> GetSourceTypes()
    {
        return pluginManager.MediaSources;
    }

    public void Init(string pluginPath)
    {
        pluginManager.Init(pluginPath);
        var sources = GetSourceTypes();
    }

    public async Task GetStorageFullInfo(bool isFull, Source? filterSource = null, bool onlyNew = false, IProgress<string>? progress = null)
    {
        logger.LogInformation("Запуск процесса синхронизации {Source}...", filterSource?.TitleFull);
        progress?.Report(filterSource != null ? $"Запуск синхронизации «{filterSource.TitleFull}»" : "Запуск полной синхронизации");

        var mediaCol = db.GetCollection<Media>("medias");
        //mediaCol.DeleteAll();
        var mediaAll = mediaCol.FindAll().ToList();

        var cache = new MediaSourceCache();

        foreach (var media in mediaAll)
        {
            foreach (var source in media.Sources)
            {
                if (filterSource != null)
                {
                    if (source.SourceId != filterSource.Id)
                    {
                        continue;
                    }
                }

                source.Media = media;
                cache.GetMedia(source.SourceId).Add(source);
            }
        }

        logger.LogInformation("Обнаружено {Count} элементов медиа в локальном кэше.", mediaAll.Count);
        progress?.Report($"Загружено {mediaAll.Count} медиа из кэша");

        var sourceTypes = GetSourceTypes();
        var sources = GetSources();

        if (filterSource != null)
        {
            sources = sources.Where(x => x.Id == filterSource.Id).ToList();
        }

        await Parallel.ForEachAsync(sources, async (mediaSource, cancellationToken) =>
        {
            var ctx = new CancellationTokenSource();
            var action = actionHolder.Register("Синхронизация " + mediaSource.TitleFull + " isFull=" + isFull + " onlyNew=" + onlyNew, "Запущена", 0, ctx);

            var plugin = sourceTypes.Values.FirstOrDefault(x => x.Name == mediaSource.TypeId);

            if (plugin == null)
            {
                logger.LogError("Плагин для типа {TypeId} не найден.", mediaSource.TypeId);
                progress?.Report($"Плагин для {mediaSource.TypeId} не найден");
                return;
            }

            progress?.Report($"Сбор медиа из «{mediaSource.TitleFull}»...");
            var syncMedia = plugin.GetMedia(mediaSource.Settings, isFull, ctx.Token);
            var mediaList = new List<MediaDto>();
            var foundIds = new List<string>();
            await foreach (var s in syncMedia)
            {
                foundIds.Add(s.Id);
                if (foundIds.Count % 25 == 0)
                {
                    progress?.Report($"«{mediaSource.TitleFull}»: получено {foundIds.Count}");
                }

                action.Status = $"получено {foundIds.Count}";

                var foundMediaSource = cache.GetMedia(mediaSource.Id).FirstOrDefault(x => x.ExternalId == s.Id);
                if (foundMediaSource == null)
                {
                    mediaList.Insert(0, s);
                    continue;
                }

                if (onlyNew)
                {
                    logger.LogInformation(s.Title + " уже существует. синхронизация остановлена");
                    // получили только свежие, обновлять ничего не будем
                    break;
                }

                var hasChange = false;
                if (s.Metadata is { Count: > 0 } && foundMediaSource.Media != null)
                {
                    var providedKeys = new HashSet<string>();
                    foreach (var item in s.Metadata)
                    {
                        providedKeys.Add(item.Key);
                        var existing = foundMediaSource.Media.Metadata
                            .FirstOrDefault(m => m.Key == item.Key && m.SourceId == mediaSource.Id);

                        if (existing != null)
                        {
                            existing.Value = item.Value;
                            existing.DisplayName = item.DisplayName;
                            existing.DisplayType = item.DisplayType;
                        }
                        else
                        {
                            item.SourceId = mediaSource.Id;
                            foundMediaSource.Media.Metadata.Add(item);
                        }
                    }

                    foundMediaSource.Media.Metadata
                        .RemoveAll(m => m.SourceId == mediaSource.Id && !providedKeys.Contains(m.Key));

                    hasChange = true;
                }

                if (foundMediaSource.Media.Sources.Count == 1)
                {
                    if (foundMediaSource.Media.Title != s.Title)
                    {
                        foundMediaSource.Media.Title = s.Title;
                        hasChange = true;
                    }

                    if (foundMediaSource.Media.Description != s.Description)
                    {
                        foundMediaSource.Media.Description = s.Description;
                        hasChange = true;
                    }
                }

                if (foundMediaSource.Title != s.Title)
                {
                    foundMediaSource.Title = s.Title;
                    hasChange = true;
                }

                if (foundMediaSource.Description != s.Description)
                {
                    foundMediaSource.Description = s.Description;
                    hasChange = true;
                }

                if (foundMediaSource.Status is MediaStatus.Missing or MediaStatus.Error)
                {
                    foundMediaSource.Status = MediaStatus.Ok;
                    hasChange = true;
                }

                if (hasChange)
                {
                    mediaCol.Update(foundMediaSource!.Media);
                }
            }

            var sortNumber = cache.GetMedia(mediaSource.Id).Select(x => x.SortNumber).DefaultIfEmpty(1).Max();
            foreach (var s in mediaList)
            {
                var mediaId = Guid.NewGuid().ToString();
                foreach (var item in s.Metadata ?? [])
                {
                    item.SourceId = mediaSource.Id;
                }

                var myMedia = new Media
                {
                    Title = s.Title,
                    Id = mediaId,
                    Description = s.Description,
                    Metadata = s.Metadata ?? [],
                    Sources = [],
                };

                var newMediaSource = new MediaSourceLink
                {
                    MediaId = mediaId,
                    Media = myMedia,
                    ExternalId = s.Id,
                    Description = s.Description,
                    Title = s.Title,
                    Status = MediaStatus.Ok,
                    SourceId = mediaSource.Id,
                    SortNumber = sortNumber,
                };

                sortNumber++;

                myMedia.Sources.Add(newMediaSource);
                // поправить циклический зависимость
                mediaCol.Insert(myMedia);
                mediaAll.Add(myMedia);
                cache.GetMedia(mediaSource.Id).Add(newMediaSource);
            }

            if (!onlyNew)
            {
                if (foundIds.Count > 0)
                {
                    var existsVideos = cache.GetMedia(mediaSource.Id);
                    foreach (var existsVideo in existsVideos)
                    {
                        if (!foundIds.Contains(existsVideo.ExternalId))
                        {
                            existsVideo.Status = MediaStatus.Missing;
                            mediaCol.Update(existsVideo.Media);
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Пропуск пометки «пропало» для источника {Source}: список полученных элементов пуст", mediaSource.TitleFull);
                }
            }

            var sourcesCol = db.GetCollection<Source>("sources");
            var dbSource = sourcesCol.FindById(mediaSource.Id);
            if (dbSource != null)
            {
                dbSource.LastSyncedAt = DateTime.UtcNow;
                sourcesCol.Update(dbSource);
            }

            action.Finish();
        });

        logger.LogInformation("Синхронизация успешно завершена.");
        progress?.Report("Синхронизация завершена");
    }

    public List<Media> GetMedias()
    {
        // TODO: System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
        var medias = db.GetCollection<Media>("medias").FindAll().ToList();
        //foreach (var media in medias) // времяночка для очистки
        //{
        //    var gr = media.Sources.GroupBy(x => x.SourceId).Where(x => x.Count() > 1).ToArray();
        //    foreach (var source in gr)
        //    {
        //        for (var i = 1; i < source.Count(); i++)
        //        {
        //            media.Sources.Remove(source.Skip(i).First());
        //            db.GetCollection<Media>("medias").Update(media);
        //        }
        //    }
        //    //if (media.Sources.Any(x => x.ExternalId == null))
        //    //{
        //    //    db.GetCollection<Media>("medias").Delete(media.Id);
        //    //}
        //}
        //medias = db.GetCollection<Media>("medias").FindAll().ToList();
        // foreach (var media in medias) // времяночка для очистки
        // {
        //     foreach (var source in media.Sources)
        //   {
        //     if (source.Status == MediaStatus.Missing)
        //   {
        //     source.Status = MediaStatus.Ok;
        // }
        //
        //              db.GetCollection<Media>("medias").Update(media);
        //        }
        //if (media.Sources.Any(x => x.ExternalId == null))
        //{
        //    db.GetCollection<Media>("medias").Delete(media.Id);
        //}
        // }

        foreach (var media in medias)
        {
            foreach (var mediaLink in media.Sources)
            {
                if (mediaLink.Description == null || mediaLink.Title == null)
                {
                    if (mediaLink.Description == null)
                    {
                        mediaLink.Description = media.Description;
                    }

                    if (mediaLink.Title == null)
                    {
                        mediaLink.Title = media.Title;
                    }

                    db.GetCollection<Media>("medias").Update(media);
                }
            }
        }

        return medias;
    }

    public void UpdateMedia(Media media)
    {
        db.GetCollection<Media>("medias").Update(media);
    }

    public async Task ForceUpdateMetadataAsync(Media media, CancellationToken ct = default)
    {
        logger.LogInformation("Начато принудительное обновление метаданных для {MediaTitle}", media.Title);

        media.Metadata.Clear();

        foreach (var sourceLink in media.Sources)
        {
            await UpdateSourceMetadataAsync(media, sourceLink.SourceId, ct);
        }

        UpdateMedia(media);
        logger.LogInformation("Завершено принудительное обновление метаданных для {MediaTitle}", media.Title);
    }

    public async Task ForceUpdateMetadataAsync(Media media, string sourceId, CancellationToken ct = default)
    {
        media.Metadata.RemoveAll(m => m.SourceId == sourceId);
        await UpdateSourceMetadataAsync(media, sourceId, ct);
        UpdateMedia(media);
        logger.LogInformation("Обновлены метаданные источника {SourceId} для {MediaTitle}", sourceId, media.Title);
    }

    public void ClearSourceMetadata(Media media, string sourceId)
    {
        var removedCount = media.Metadata.RemoveAll(m => m.SourceId == sourceId);
        if (removedCount <= 0)
        {
            return;
        }

        UpdateMedia(media);
        logger.LogInformation("Очищено {Count} метаданных для источника {SourceId} в медиа {MediaTitle}", removedCount, sourceId, media.Title);
    }

    public void ClearAllMetadata(Media media)
    {
        if (media.Metadata.Count <= 0)
        {
            return;
        }

        var count = media.Metadata.Count;
        media.Metadata.Clear();
        UpdateMedia(media);
        logger.LogInformation("Очищено {Count} метаданных для медиа {MediaTitle}", count, media.Title);
    }

    public void SetSourceSkipped(Media media, string sourceId)
    {
        var link = media.Sources.FirstOrDefault(x => x.SourceId == sourceId);

        if (link == null)
        {
            link = new()
            {
                MediaId = media.Id,
                Media = media,
                SourceId = sourceId,
                ExternalId = string.Empty,
                Title = media.Title,
                Description = media.Description ?? string.Empty,
                Status = MediaStatus.Skipped,
                SortNumber = 0,
            };

            media.Sources.Add(link);
        }
        else
        {
            link.Status = MediaStatus.Skipped;
            link.StatusMessage = null;
        }

        UpdateMedia(media);
        logger.LogInformation("Медиа '{MediaTitle}' помечено как пропускаемое для источника {SourceId}", media.Title, sourceId);
    }

    public void UnsetSourceSkipped(Media media, string sourceId)
    {
        var link = media.Sources.FirstOrDefault(x => x.SourceId == sourceId);

        if (link is not { Status: MediaStatus.Skipped })
        {
            return;
        }

        if (string.IsNullOrEmpty(link.ExternalId))
        {
            media.Sources.Remove(link);
        }
        else
        {
            link.Status = MediaStatus.Ok;
            link.StatusMessage = null;
        }

        UpdateMedia(media);
        logger.LogInformation("С медиа '{MediaTitle}' снята пометка пропуска для источника {SourceId}", media.Title, sourceId);
    }

    public void RemoveMedia(Media media)
    {
        db.GetCollection<Media>("medias").Delete(media.Id);
    }

    public void MergeMedia(Media target, IReadOnlyList<Media> toRemove)
    {
        var collection = db.GetCollection<Media>("medias");
        db.BeginTrans();

        try
        {
            collection.Update(target);

            foreach (var media in toRemove)
            {
                collection.Delete(media.Id);
            }

            db.Commit();
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }

    public List<Source> GetSources()
    {
        var sources = db.GetCollection<Source>("sources").FindAll().ToList();
        var sourceTypes = GetSourceTypes();
        foreach (var source in sources)
        {
            var sourceType = sourceTypes.Values.FirstOrDefault(x => x.Name == source.TypeId);
            if (sourceType == null)
            {
                source.IsDisable = true;
                continue;
            }

            source.Type = sourceType;

            source.Settings["_system_temp_path"] = tempManager.TempPath;

            if (sourceType is IAuthenticatable)
            {
                source.Settings["_system_state_path"] = stateManager.GetSourceStatePath(source.Id);
            }
        }

        return sources;
    }

    public void AddSource(string sourceId, string typeId, Dictionary<string, string> settings)
    {
        db.GetCollection<Source>("sources")
            .Insert(new Source
            {
                Id = sourceId,
                TypeId = typeId,
                Settings = settings,
            });
    }

    public void RemoveSource(string sourceId)
    {
        db.GetCollection<Source>("sources").Delete(sourceId);

        // TODO: По идее нужно, но для отладки неудобно
        //stateManager.CleanSource(sourceId);
    }

    public void UpdateSource(Source source)
    {
        db.GetCollection<Source>("sources").Update(source);
    }

    public List<SourceSyncRelation> GetRelations()
    {
        //db.GetCollection<SourceSyncRelation>("source_relations")
        //    .DeleteMany(x => true);

        var relations = db.GetCollection<SourceSyncRelation>("source_relations").FindAll().ToList();
        var sourceTypes = GetSourceTypes();
        var sources = GetSources();
        foreach (var item in relations)
        {
            //item.From.Type = sourceTypes.Values.First(x => x.Name == item.From.TypeId);
            var fromSource = sources.FirstOrDefault(x => x.Id == item.FromId);
            var toSource = sources.FirstOrDefault(x => x.Id == item.ToId);
            item.From = fromSource;
            item.To = toSource;
            if (fromSource == null || toSource == null || fromSource.IsDisable || toSource.IsDisable)
            {
                item.IsDisable = true;
            }
        }

        return relations;
    }

    public void AddRelation(Source from, Source to)
    {
        AddRelation(from.Id, to.Id);
    }

    public void AddRelation(string fromId, string toId)
    {
        db.GetCollection<SourceSyncRelation>("source_relations")
            .Insert(new SourceSyncRelation
            {
                FromId = fromId,
                ToId = toId,
            });
    }

    public void RemoveRelation(Source from, Source to)
    {
        RemoveRelation(from.Id, to.Id);
    }

    public void RemoveRelation(string fromId, string toId)
    {
        db.GetCollection<SourceSyncRelation>("source_relations")
            .DeleteMany(x => x.FromId == fromId && x.ToId == toId);
    }

    public void InvertRelation(string fromId, string toId)
    {
        db.BeginTrans();

        try
        {
            var collection = db.GetCollection<SourceSyncRelation>("source_relations");
            collection.DeleteMany(x => x.FromId == fromId && x.ToId == toId);
            collection.Insert(new SourceSyncRelation
            {
                FromId = toId,
                ToId = fromId,
            });

            db.Commit();
        }
        catch
        {
            db.Rollback();
            throw;
        }
    }

    public void ClearCollection(string collectionName)
    {
        logger.LogInformation("Очистка коллекции: {CollectionName}", collectionName);
        var collection = db.GetCollection(collectionName);
        var count = collection.Count();
        collection.DeleteAll();
        logger.LogInformation("[✓] Коллекция {CollectionName} очищена. Удалено записей: {Count}", collectionName, count);
    }

    public void ClearDatabase()
    {
        logger.LogInformation("========== Начало очистки базы данных ==========");

        var mediasCollection = db.GetCollection<Media>("medias");
        var mediaCount = mediasCollection.Count();
        mediasCollection.DeleteAll();
        logger.LogInformation("[✓] Коллекция медиа очищена. Удалено записей: {MediaCount}", mediaCount);

        var relationsCollection = db.GetCollection<SourceSyncRelation>("source_relations");
        var relationCount = relationsCollection.Count();
        relationsCollection.DeleteAll();
        logger.LogInformation("[✓] Коллекция связей очищена. Удалено записей: {RelationCount}", relationCount);

        var sourcesCollection = db.GetCollection<Source>("sources");
        var sourceCount = sourcesCollection.Count();
        sourcesCollection.DeleteAll();
        logger.LogInformation("[✓] Коллекция источников очищена. Удалено записей: {SourceCount}", sourceCount);

        logger.LogInformation("========== Очистка БД завершена успешно ==========");
        logger.LogInformation("Всего удалено записей: {TotalCount} (медиа: {MediaCount}, связи: {RelationCount}, источники: {SourceCount})",
            mediaCount + relationCount + sourceCount, mediaCount, relationCount, sourceCount);
    }

    // TODO: Сомнительно
    public async Task<Media> PublishMediaAsync(
        Source source,
        string title,
        string? description,
        string videoFilePath,
        string? coverFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoFilePath);

        if (!File.Exists(videoFilePath))
        {
            throw new FileNotFoundException("Файл видео не найден", videoFilePath);
        }

        if (!string.IsNullOrEmpty(coverFilePath) && !File.Exists(coverFilePath))
        {
            throw new FileNotFoundException("Файл обложки не найден", coverFilePath);
        }

        if (source.Type == null)
        {
            throw new InvalidOperationException($"Источник «{source.TitleFull}» не инициализирован (Type == null).");
        }

        var mediaId = Guid.NewGuid().ToString();
        var tempDir = Path.Combine(tempManager.TempPath, mediaId);
        Directory.CreateDirectory(tempDir);

        logger.LogInformation("Публикация «{Title}» в источник {Source}. MediaId: {MediaId}", title, source.TitleFull, mediaId);

        var videoExt = Path.GetExtension(videoFilePath);
        var tempVideoPath = Path.Combine(tempDir, "media" + (string.IsNullOrEmpty(videoExt) ? ".mp4" : videoExt));
        File.Copy(videoFilePath, tempVideoPath, true);

        string? tempCoverPath = null;
        if (!string.IsNullOrEmpty(coverFilePath))
        {
            var coverExt = Path.GetExtension(coverFilePath);
            tempCoverPath = Path.Combine(tempDir, "cover" + (string.IsNullOrEmpty(coverExt) ? ".jpg" : coverExt));
            File.Copy(coverFilePath, tempCoverPath, true);
        }

        var dto = new MediaDto
        {
            Id = mediaId,
            Title = title,
            Description = description ?? string.Empty,
            TempDataPath = tempVideoPath,
        };

        if (tempCoverPath != null)
        {
            dto.TempPreviewPath = tempCoverPath;
        }

        UploadResult uploadResult;
        try
        {
            uploadResult = await source.Type.UploadAsync(dto, source.Settings, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            tempManager.CleanMedia(mediaId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при публикации «{Title}» в {Source}", title, source.TitleFull);
            tempManager.CleanMedia(mediaId);
            throw;
        }

        if (string.IsNullOrEmpty(uploadResult.Id))
        {
            tempManager.CleanMedia(mediaId);
            throw new InvalidOperationException($"Публикация в {source.TitleFull} не вернула идентификатор: {uploadResult.Status.Text} {uploadResult.Message}");
        }

        var mediaCollection = db.GetCollection<Media>("medias");
        var existingLinks = mediaCollection.FindAll()
            .SelectMany(x => x.Sources ?? [])
            .Where(x => x.SourceId == source.Id)
            .ToList();

        var sortNumber = existingLinks.Select(x => x.SortNumber).DefaultIfEmpty(0).Max() + 1;

        var media = new Media
        {
            Id = mediaId,
            Title = title,
            Description = description ?? string.Empty,
            Sources = [],
        };

        var link = new MediaSourceLink
        {
            MediaId = mediaId,
            Media = media,
            SourceId = source.Id,
            ExternalId = uploadResult.Id!,
            Status = uploadResult.Status.Id,
            SortNumber = sortNumber,
        };

        media.Sources.Add(link);
        mediaCollection.Insert(media);

        tempManager.CleanMedia(mediaId);

        logger.LogInformation("Публикация «{Title}» завершена. ExternalId: {ExternalId}", title, uploadResult.Id);
        return media;
    }

    public async Task TransferByRelation(Media media, SourceSyncRelation rel, CancellationToken cancellationToken = default)
    {
        var fromMediaSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.From.Id);
        if (fromMediaSource == null)
        {
            throw new($"MediaSourceLink не найден для {rel.From.Id}");
        }

        if (fromMediaSource.Status != MediaStatus.Ok)
        {
            throw new($"MediaSourceLink для {rel.From.Id} в плохом статусе " + fromMediaSource.Status);
        }

        var toMediaSource = media.Sources.FirstOrDefault(x => x.SourceId == rel.To.Id);
        // todo частично успешные пока повторно не обрабатываем
        if (toMediaSource != null && toMediaSource.Status == MediaStatus.Ok)
        {
            // фаил уже загружен, значит ничего не делаем
            // в будущем возможно обновлять будем
            logger.LogInformation("Пропущено синхронизирование медиа {Media} в {ToSource} / уже имеется", media, rel.To);
            return;
        }

        UploadResult uploadResult;
        MediaDto? tempMedia = null;
        var debug = false;
        if (debug)
        {
            if (DateTime.Now.Second % 10 < 5)
            {
                throw new("ошибка");
            }

            uploadResult = new()
            {
                Status = MediaStatusHelper.Ok(),
                Id = Guid.NewGuid().ToString(),
            };
        }
        else
        {
            tempMedia = await rel.From.Type.DownloadAsync(fromMediaSource.ExternalId, rel.From.Settings, cancellationToken);
            tempMedia.Id = media.Id;
            try
            {
                if (toMediaSource?.Status == MediaStatus.PartialOk)
                {
                    var externalId = toMediaSource.ExternalId;
                    uploadResult = await rel.To.Type.UpdateAsync(externalId, tempMedia, rel.To.Settings, cancellationToken);
                }
                else
                {
                    uploadResult = await rel.To.Type.UploadAsync(tempMedia, rel.To.Settings, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // TODO: Слегка жидкая тема
                logger.LogError(ex, "Загрузка медиа {Media} в {ToSource} завершилась ошибкой, проставляется статус Error", media, rel.To);
                MarkUploadFailure(media, rel.To.Id, ex.Message);
                throw;
            }
        }

        // если айди есть, значит частично или полностью успех и связь устанавливаем
        if (uploadResult.Id != null)
        {
            if (toMediaSource == null)
            {
                toMediaSource = new()
                {
                    MediaId = media.Id,
                    Media = media,
                    SourceId = rel.To.Id,
                };

                media.Sources.Add(toMediaSource);
            }

            toMediaSource.Title = media.Title;
            toMediaSource.Description = media.Description;
            toMediaSource.Status = uploadResult.Status.Id;
            toMediaSource.ExternalId = uploadResult.Id!;
            UpdateMedia(media);
            logger.LogInformation("Успешно синхронизировано медиа {Media} в {ToSource}. ExternalId: {ExternalId}", media, rel.To, uploadResult.Id);

            if (tempMedia != null && !string.IsNullOrEmpty(tempMedia.TempDataPath))
            {
                var guid = Path.GetFileName(Path.GetDirectoryName(tempMedia.TempDataPath));

                if (guid != null)
                {
                    tempManager.CleanMedia(guid);
                }
            }
        }
        else
        {
            throw new("Провал синхронизации: " + uploadResult.Status.Text + " " + uploadResult.Message);
        }
    }

    public async Task DeleteMediaFromSourceAsync(Media media, Source source, CancellationToken cancellationToken = default)
    {
        var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == source.Id);
        if (sourceLink == null)
        {
            logger.LogWarning("Попытка удалить медиа {MediaId} из источника {SourceId}, но связь отсутствует", media.Id, source.Id);
            throw new InvalidOperationException($"Медиа {media.Title} отсутствует в источнике {source.TitleFull}");
        }

        logger.LogInformation("Начало удаления медиа {MediaId} ({MediaTitle}) из источника {SourceId} ({SourceTitle})",
            media.Id, media.Title, source.Id, source.TitleFull);

        try
        {
            await source.Type.DeleteAsync(sourceLink.ExternalId, source.Settings, cancellationToken);
            logger.LogInformation("Успешно удалено медиа {MediaId} из источника {SourceId}", media.Id, source.Id);
        }
        catch (NotSupportedException exception)
        {
            logger.LogError(exception, "Тип источника {SourceType} не поддерживает удаление", source.TypeId);
            throw new InvalidOperationException($"Источник {source.TitleFull} не поддерживает удаление", exception);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при удалении медиа {MediaId} из источника {SourceId}", media.Id, source.Id);
            throw;
        }

        CleanupMediaLinks(media, source.Id);
    }

    private async Task UpdateSourceMetadataAsync(Media media, string sourceId, CancellationToken ct)
    {
        var source = GetSources().FirstOrDefault(s => s.Id == sourceId);
        if (source == null || source.IsDisable)
        {
            return;
        }

        var plugin = GetSourceTypes().Values.FirstOrDefault(x => x.Name == source.TypeId);
        if (plugin == null)
        {
            return;
        }

        var sourceLink = media.Sources.FirstOrDefault(s => s.SourceId == sourceId);
        if (sourceLink == null)
        {
            return;
        }

        try
        {
            var dto = await plugin.GetMediaByIdAsync(sourceLink.ExternalId, source.Settings, ct);
            if (dto is null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(dto.Title))
            {
                sourceLink.Title = dto.Title;
            }

            if (dto.Description is not null)
            {
                sourceLink.Description = dto.Description;
            }

            if (dto.Metadata is { Count: > 0 })
            {
                foreach (var item in dto.Metadata)
                {
                    item.SourceId = source.Id;
                    media.Metadata.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении метаданных из источника {SourceTitle}", source.TitleFull);
        }
    }

    private void MarkUploadFailure(Media media, string toSourceId, string errorMessage)
    {
        var link = media.Sources.FirstOrDefault(x => x.SourceId == toSourceId);
        if (link == null)
        {
            link = new()
            {
                MediaId = media.Id,
                Media = media,
                SourceId = toSourceId,
                ExternalId = string.Empty,
                Title = string.Empty,
                Description = string.Empty,
            };

            media.Sources.Add(link);
        }

        link.Status = MediaStatus.Error;
        UpdateMedia(media);
        logger.LogInformation("Для медиа {MediaId}/{ToSourceId} проставлен статус Error: {Message}", media.Id, toSourceId, errorMessage);
    }

    private void CleanupMediaLinks(Media media, string sourceId)
    {
        logger.LogInformation("Очистка записей в базе данных для медиа {MediaId}, источник {SourceId}", media.Id, sourceId);

        var linkToRemove = media.Sources.FirstOrDefault(s => s.SourceId == sourceId);
        if (linkToRemove != null)
        {
            media.Sources.Remove(linkToRemove);
            logger.LogDebug("Удалена связь MediaSourceLink для источника {SourceId}", sourceId);
        }

        var removedMetadataCount = media.Metadata.RemoveAll(m => m.SourceId == sourceId);
        if (removedMetadataCount > 0)
        {
            logger.LogDebug("Удалено {RemovedCount} элементов метаданных для источника {SourceId}", removedMetadataCount, sourceId);
        }

        if (media.Sources.Count != 0)
        {
            UpdateMedia(media);
            logger.LogInformation("Медиа {MediaId} обновлено в базе данных (осталось источников: {RemainingCount})", media.Id, media.Sources.Count);
        }
        else
        {
            RemoveMedia(media);
            logger.LogInformation("Медиа {MediaId} удалено из базы данных (не осталось источников)", media.Id);
        }
    }

    public class MediaSourceCache
    {
        private readonly Dictionary<string, List<MediaSourceLink>> _holder = new();

        public List<MediaSourceLink> GetMedia(string sourceId)
        {
            if (_holder.TryGetValue(sourceId, out var value))
            {
                return value;
            }

            value = [];
            _holder[sourceId] = value;

            return value;
        }
    }
}
