using MediaOrcestrator.Core.Extensions;
using MediaOrcestrator.Core.Models;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Core.Services;

public class DirectoryService(
    ILogger<DirectoryService> logger)
{
    /// <summary>
    /// Очищает временные директории и создает необходимые папки.
    /// </summary>
    /// <param name="path">Путь к основной директории.</param>
    public void CleanUpDirectories(string path)
    {
        var tempFolderPath = Path.Combine(path, ".temp");

        try
        {
            CreateDirectoryIfNotExists(path, "видео");
            CreateDirectoryIfNotExists(tempFolderPath, "временные");

            var tempFilesStats = CleanUpFiles(tempFolderPath);
            var mainFilesStats = path.GetDirectoryInfo();

            logger.LogInformation("Удалено временных файлов: {Count}, общий объём: {TotalSize}, средний размер: {AverageSize}",
                tempFilesStats.Count, tempFilesStats.TotalSize, tempFilesStats.AverageSize);

            logger.LogInformation("Всего файлов в директории: {Count}, общий объём: {TotalSize}, средний размер: {AverageSize}",
                mainFilesStats.Count, mainFilesStats.TotalSize, mainFilesStats.AverageSize);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при обновлении директорий: {Path}", path);
        }
    }

    /// <summary>
    /// Очищает временные файлы, связанные с указанным элементом загрузки.
    /// </summary>
    /// <param name="item">Элемент загрузки, для которого нужно очистить временные файлы.</param>
    /// <param name="stream">Поток загрузки, содержащий путь к временным файлам.</param>
    public void CleanUpTempFiles(DownloadItem item, DownloadItemStream stream)
    {
        logger.LogInformation("Начало очистки временных файлов для элемента: {VideoTitle}, временный путь: {TempPath}", item.Video.Title, stream.TempPath);

        var directoryName = Path.GetDirectoryName(stream.TempPath);

        if (string.IsNullOrWhiteSpace(directoryName))
        {
            logger.LogWarning("Не удалось получить имя директории из временного пути: {TempPath}", stream.TempPath);
            return;
        }

        DirectoryInfo directoryInfo = new(directoryName);
        var filesToDelete = directoryInfo.GetFiles().Where(file => file.Name.StartsWith(item.Id)).ToArray();

        if (filesToDelete.Length == 0)
        {
            logger.LogInformation("Не найдено временных файлов для удаления, соответствующих элементу: {ItemId}", item.Id);
            return;
        }

        foreach (var file in filesToDelete)
        {
            File.Delete(file.FullName);
            logger.LogDebug("Удалён временный файл: {File}", file.FullName);
        }
    }

    /// <summary>
    /// Создает директорию, если она не существует.
    /// </summary>
    /// <param name="path">Путь к директории.</param>
    /// <param name="directoryType">Тип директории (например, "видео" или "временные").</param>
    private void CreateDirectoryIfNotExists(string path, string directoryType)
    {
        if (Directory.Exists(path))
        {
            logger.LogDebug("Директория для {Type} уже существует: {FullPath}", directoryType, path);
        }
        else
        {
            Directory.CreateDirectory(path);
            logger.LogInformation("Создана директория для {Type}: {FullPath}", directoryType, path);
        }
    }

    /// <summary>
    /// Очищает файлы в указанной директории.
    /// </summary>
    /// <param name="folderPath">Путь к директории, которую нужно очистить.</param>
    /// <returns>Статистика директории после очистки.</returns>
    private DirectoryStats CleanUpFiles(string folderPath)
    {
        var files = new DirectoryInfo(folderPath).GetFiles();

        foreach (var file in files)
        {
            File.Delete(file.FullName);
            logger.LogDebug("Удалён файл: {File}", file.FullName);
        }

        return files.GetDirectoryInfo();
    }
}
