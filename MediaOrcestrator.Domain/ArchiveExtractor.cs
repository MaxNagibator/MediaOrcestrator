using SharpCompress.Archives;
using System.IO.Compression;
using ArchiveType = MediaOrcestrator.Modules.ArchiveType;

namespace MediaOrcestrator.Domain;

public static class ArchiveExtractor
{
    public static void Extract(string archivePath, string targetDir, ArchiveType type)
    {
        Directory.CreateDirectory(targetDir);

        switch (type)
        {
            case ArchiveType.None:
                var destPath = Path.Combine(targetDir, Path.GetFileName(archivePath));
                File.Copy(archivePath, destPath, true);
                break;

            case ArchiveType.Zip:
                ZipFile.ExtractToDirectory(archivePath, targetDir, true);
                break;

            case ArchiveType.SevenZip:
                using (var archive = ArchiveFactory.OpenArchive(archivePath))
                {
                    foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                    {
                        entry.WriteToDirectory(targetDir, new()
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                        });
                    }
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported archive type");
        }
    }

    public static string? FindExecutable(string extractedDir, string globPattern)
    {
        var allFiles = Directory.GetFiles(extractedDir, "*", SearchOption.AllDirectories);

        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(extractedDir, file).Replace('\\', '/');

            if (GlobMatcher.IsMatch(relativePath, globPattern))
            {
                return file;
            }
        }

        return null;
    }
}
