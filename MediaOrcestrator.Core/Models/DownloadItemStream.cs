using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MediaOrcestrator.Core.Models;

public class DownloadItemStream
{
    private const string FileNameFormat = "{0}__{1}.{2}";

    private double? _sizeMegaBytes;
    private string? _title;
    private string? _videoType;

    private DownloadItemStream(int id, string tempName, string tempPath, string fileName, string filePath)
    {
        Id = id;
        TempName = tempName;
        TempPath = tempPath;
        FileName = fileName;
        FilePath = filePath;
    }

    private DownloadItemStream(int id, string tempName, string tempPath, string fileName, string filePath, IStreamInfo stream)
        : this(id, tempName, tempPath, fileName, filePath)
    {
        Stream = stream;
    }

    private DownloadItemStream(int id, string tempName, string tempPath, string fileName, string filePath, IAudioStreamInfo audioStream, IVideoStreamInfo videoStream)
        : this(id, tempName, tempPath, fileName, filePath)
    {
        AudioStreamInfo = audioStream;
        VideoStreamInfo = videoStream;
    }

    public int Id { get; }
    public string TempName { get; }
    public string TempPath { get; }
    public string FileName { get; }
    public string FilePath { get; }
    public DownloadItemState State { get; set; } = DownloadItemState.Added;
    public IStreamInfo? Stream { get; }

    public bool IsCombineAfterDownload => AudioStreamInfo is not null && VideoStreamInfo is not null;
    public IAudioStreamInfo? AudioStreamInfo { get; }
    public IVideoStreamInfo? VideoStreamInfo { get; }

    public bool IsNeedDownload => State == DownloadItemState.Wait;

    public string Title => _title ??= IsCombineAfterDownload
        ? $"Muxed (custom) ({VideoStreamInfo!.VideoQuality.MaxHeight} | {VideoStreamInfo.Container.Name}) ~{SizeMegaBytes}МБ"
        : $"{Stream} {SizeMegaBytes}МБ";

    public double SizeMegaBytes => _sizeMegaBytes ??= IsCombineAfterDownload
        ? Math.Round(AudioStreamInfo!.Size.MegaBytes + VideoStreamInfo!.Size.MegaBytes, 2)
        : Math.Round(Stream!.Size.MegaBytes, 2);

    public string VideoType => _videoType ??= IsCombineAfterDownload
        ? VideoStreamInfo!.Container.Name
        : Stream!.Container.Name;

    public static DownloadItemStream Create(int id, string tempPath, string filePath, Video video, IStreamInfo stream)
    {
        var (tempName, fileName) = GetFileNames(id, video, stream);
        return new(id, tempName, Path.Combine(tempPath, tempName), fileName, Path.Combine(filePath, fileName), stream);
    }

    public static DownloadItemStream Create(int id, string tempPath, string filePath, Video video, IAudioStreamInfo audioStream, IVideoStreamInfo videoStream)
    {
        var (tempName, fileName) = GetFileNames(id, video, videoStream);
        return new(id, tempName, Path.Combine(tempPath, tempName), fileName, Path.Combine(filePath, fileName), audioStream, videoStream);
    }

    private static (string tempName, string fileName) GetFileNames(int id, Video video, IStreamInfo stream)
    {
        var tempName = string.Format(FileNameFormat, video.Id, id, stream.Container.Name);
        var fileName = $"{video.Id}.{stream.Container.Name}";
        return (tempName, fileName);
    }
}
