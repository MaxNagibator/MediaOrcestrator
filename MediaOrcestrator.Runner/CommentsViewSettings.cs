using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaOrcestrator.Runner;

public enum CommentsLayoutMode
{
    Grouped = 0,
    Flat = 1,
}

public enum CommentsReplyStatusFilter
{
    All = 0,
    WithoutReply = 1,
    WithReply = 2,
    NewReplies = 3,
    WithoutReplyAndLike = 4,
}

public sealed class CommentsViewSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string? SelectedSourceId { get; set; }
    public int Limit { get; set; } = 1000;
    public string Search { get; set; } = "";
    public int FetchSinceDays { get; set; }
    public int FetchOnlyRecent { get; set; }
    public CommentsLayoutMode LayoutMode { get; set; } = CommentsLayoutMode.Grouped;
    public CommentsReplyStatusFilter ReplyStatus { get; set; } = CommentsReplyStatusFilter.WithoutReplyAndLike;

    public static CommentsViewSettings Load()
    {
        var path = GetPath();

        if (!File.Exists(path))
        {
            return new();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CommentsViewSettings>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public void Save()
    {
        var path = GetPath();

        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
        }
    }

    private static string GetPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "comments-view-settings.json");
    }
}
