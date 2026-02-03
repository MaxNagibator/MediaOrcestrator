namespace MediaOrcestrator.Core.Extensions;

public static class FilePathExtensions
{
    public static string AddPrefixToFileName(this string filePath, string prefix)
    {
        var fileName = $"{prefix}_{Path.GetFileName(filePath)}";
        var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException();
        return Path.Combine(directoryPath, fileName);
    }

    public static string AddSuffixToFileName(this string filePath, string suffix)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{suffix}{Path.GetExtension(filePath)}";
        var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException();
        return Path.Combine(directoryPath, fileName);
    }
}
