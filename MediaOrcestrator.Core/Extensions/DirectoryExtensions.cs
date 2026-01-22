using MediaOrcestrator.Core.Models;

namespace MediaOrcestrator.Core.Extensions;

public static class DirectoryExtensions
{
    private const double Kb = 1024;
    private const double Mb = Kb * 1024;
    private const double Gb = Mb * 1024;

    /// <summary>
    /// Получает статистику директории по указанному пути.
    /// </summary>
    /// <param name="path">Путь к директории.</param>
    /// <returns>Статистика директории.</returns>
    public static DirectoryStats GetDirectoryInfo(this string path)
    {
        return new DirectoryInfo(path).GetFiles().GetDirectoryInfo();
    }

    /// <summary>
    /// Получает статистику директории на основе массива файлов.
    /// </summary>
    /// <param name="files">Массив файлов для анализа.</param>
    /// <returns>Статистика директории.</returns>
    public static DirectoryStats GetDirectoryInfo(this FileInfo[] files)
    {
        double totalSize = files.Sum(file => file.Length);
        var averageSize = files.Length > 0 ? totalSize / files.Count(info => info.Length >= Mb) : 0;

        return new(files.Length, FormatSize(totalSize), FormatSize(averageSize));
    }

    /// <summary>
    /// Форматирует размер в байтах в удобочитаемый вид.
    /// </summary>
    /// <param name="bytes">Размер в байтах.</param>
    /// <returns>Строка с отформатированным размером.</returns>
    private static string FormatSize(double bytes)
    {
        return bytes switch
        {
            >= Gb => $"{bytes / Gb:F2} ГБ",
            >= Mb => $"{bytes / Mb:F2} МБ",
            >= Kb => $"{bytes / Kb:F2} КБ",
            _ => $"{bytes} байт",
        };
    }
}
