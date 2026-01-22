using MediaOrcestrator.Core.Extensions;
using System.Text.Json.Serialization;

namespace MediaOrcestrator.Core.Models;

public class VideoInfo
{
    /// <summary>
    /// ID видео на YouTube.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Заголовок видео.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Текущее состояние процесса загрузки видео.
    /// </summary>
    public VideoState State { get; set; }

    /// <summary>
    /// Имя файла, под которым будет сохранено видео.
    /// </summary>
    [JsonIgnore]
    public string FileName => Title.GetFileName();

    /// <summary>
    /// URL видео на YouTube.
    /// </summary>
    [JsonIgnore]
    public string Url => $"https://www.youtube.com/watch?v={Id}";

    /// <summary>
    /// URL миниатюры видео.
    /// </summary>
    [JsonIgnore]
    public string ThumbnailUrl => $"https://img.youtube.com/vi/{Id}/hqdefault.jpg";
}
