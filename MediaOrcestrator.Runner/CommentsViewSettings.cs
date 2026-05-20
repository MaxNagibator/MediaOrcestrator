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

public sealed class CommentsAppearance
{
    public const int MinFontSize = 8;
    public const int MaxFontSize = 32;
    public const double MinLineHeight = 1.0;
    public const double MaxLineHeight = 2.5;

    public int BaseFontSize { get; set; } = 13;
    public int CommentFontSize { get; set; } = 13;
    public int AuthorFontSize { get; set; } = 12;
    public int MetaFontSize { get; set; } = 12;
    public int MediaTitleFontSize { get; set; } = 14;
    public int BadgeFontSize { get; set; } = 10;
    public double LineHeight { get; set; } = 1.45;

    public void Normalize()
    {
        BaseFontSize = Math.Clamp(BaseFontSize, MinFontSize, MaxFontSize);
        CommentFontSize = Math.Clamp(CommentFontSize, MinFontSize, MaxFontSize);
        AuthorFontSize = Math.Clamp(AuthorFontSize, MinFontSize, MaxFontSize);
        MetaFontSize = Math.Clamp(MetaFontSize, MinFontSize, MaxFontSize);
        MediaTitleFontSize = Math.Clamp(MediaTitleFontSize, MinFontSize, MaxFontSize);
        BadgeFontSize = Math.Clamp(BadgeFontSize, MinFontSize, MaxFontSize);
        LineHeight = Math.Clamp(LineHeight, MinLineHeight, MaxLineHeight);
    }
}

public sealed class CommentsViewSettings
{
    private const int CurrentSettingsVersion = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public int? SettingsVersion { get; set; }
    public string? SelectedSourceId { get; set; }
    public List<string> SelectedSourceIds { get; set; } = [];
    public int Limit { get; set; } = 1000;
    public string Search { get; set; } = "";
    public int FetchSinceDays { get; set; }
    public int FetchOnlyRecent { get; set; }
    public CommentsLayoutMode LayoutMode { get; set; } = CommentsLayoutMode.Flat;
    public CommentsReplyStatusFilter ReplyStatus { get; set; } = CommentsReplyStatusFilter.WithoutReplyAndLike;
    public CommentsAppearance Appearance { get; set; } = new();
    public string ReplyPrefixTemplate { get; set; } = "{name}, ";

    public static CommentsViewSettings Load()
    {
        var path = GetPath();

        if (!File.Exists(path))
        {
            return CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<CommentsViewSettings>(json, JsonOptions) ?? new();
            loaded.ApplyDefaultsAndMigrations();
            return loaded;
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save()
    {
        var path = GetPath();

        try
        {
            SettingsVersion = CurrentSettingsVersion;
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

    private static CommentsViewSettings CreateDefault()
    {
        return new()
        {
            SettingsVersion = CurrentSettingsVersion,
            LayoutMode = CommentsLayoutMode.Flat,
        };
    }

    private void ApplyDefaultsAndMigrations()
    {
        var previousVersion = SettingsVersion.GetValueOrDefault();

        if (previousVersion < 2)
        {
            LayoutMode = CommentsLayoutMode.Flat;
        }

        if (previousVersion < 3
            && SelectedSourceIds.Count == 0
            && !string.IsNullOrEmpty(SelectedSourceId))
        {
            SelectedSourceIds.Add(SelectedSourceId);
        }

        SelectedSourceId = null;

        SettingsVersion = CurrentSettingsVersion;
        Appearance ??= new();
        Appearance.Normalize();
        ReplyPrefixTemplate ??= "{name}, ";
    }
}
