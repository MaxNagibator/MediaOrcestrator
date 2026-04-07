using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Youtube;

internal static class MediaDtoFactory
{
    public static MediaDto CreateFull(
        string id,
        string title,
        string url,
        string previewUrl,
        string? description = null,
        TimeSpan? duration = null,
        string? author = null,
        string? creationDate = null,
        long? viewCount = null,
        string? tempDataPath = null)
    {
        var metadata = new List<MetadataItem>
        {
            new()
            {
                Key = "Duration",
                DisplayName = "Длительность",
                Value = duration?.ToString() ?? "",
                DisplayType = "System.TimeSpan",
            },
            new()
            {
                Key = "Author",
                DisplayName = "Автор",
                Value = author ?? "",
                DisplayType = "System.String",
            },
            new()
            {
                Key = "CreationDate",
                DisplayName = "Дата создания",
                Value = creationDate ?? "",
                DisplayType = "System.DateTime",
            },
            new()
            {
                Key = "Views",
                DisplayName = "Просмотры",
                Value = viewCount?.ToString() ?? "0",
                DisplayType = "System.Int64",
            },
            new()
            {
                Key = "PreviewUrl",
                Value = previewUrl,
            },
        };

        return new()
        {
            Id = id,
            Title = title,
            Description = description,
            DataPath = url,
            PreviewPath = previewUrl,
            Metadata = metadata,
            TempDataPath = tempDataPath,
        };
    }

    public static MediaDto CreateBasic(
        string id,
        string title,
        string url,
        string previewUrl,
        TimeSpan? duration = null,
        string? author = null)
    {
        var metadata = new List<MetadataItem>
        {
            new()
            {
                Key = "Duration",
                DisplayName = "Длительность",
                Value = duration?.ToString() ?? "",
                DisplayType = "System.TimeSpan",
            },
            new()
            {
                Key = "Author",
                DisplayName = "Автор",
                Value = author ?? "",
                DisplayType = "System.String",
            },
            new()
            {
                Key = "PreviewUrl",
                Value = previewUrl,
            },
        };

        return new()
        {
            Id = id,
            Title = title,
            DataPath = url,
            PreviewPath = previewUrl,
            Metadata = metadata,
        };
    }
}
