using System.Collections.Frozen;
using System.Drawing;
using System.Reflection;

namespace MediaOrcestrator.Modules;

public abstract class MediaStatus
{
    public abstract string Id { get; }
    public abstract string Text { get; }
    public abstract string IconText { get; }
    public abstract Color IconColor { get; }

    public const string Ok = "OK";

    /// <summary>
    /// Фаил существует, но часть метаинформации не загрущена (превью или ещё что то).
    /// </summary>
    public const string PartialOk = "PartialOk";
    public const string None = "None";
    public const string Missing = "Missing";
    public const string Error = "Error";
}

public static class MediaStatusHelper
{
    private static readonly FrozenDictionary<string, MediaStatus> _statuses;

    static MediaStatusHelper()
    {
        var statuses = new Dictionary<string, MediaStatus>();

        var statusTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(MediaStatus)));

        foreach (var type in statusTypes)
        {
            if (Activator.CreateInstance(type) is MediaStatus status)
            {
                statuses[status.Id] = status;
            }
        }

        _statuses = statuses.ToFrozenDictionary();
    }

    /// <summary>
    /// Получает статус по ID
    /// </summary>
    public static MediaStatus GetById(string id)
    {
        return _statuses[id];
    }

    /// <summary>
    /// Получает все статусы
    /// </summary>
    public static MediaStatus[] GetAll()
    {
        return _statuses.Values.ToArray();
    }

    public static MediaStatus Ok()
    {
        return GetById(MediaStatus.Ok);
    }
}

public class MediaStatusError : MediaStatus
{
    public override string Id => Error;

    public override string Text => "Ошибка";

    public override string IconText => "✘";

    public override Color IconColor => Color.DarkRed;
}

public class MediaStatusOk : MediaStatus
{
    public override string Id => Ok;

    public override string Text => "OK";

    public override string IconText => "✔";

    public override Color IconColor => Color.DarkGreen;
}

public class MediaStatusMissing : MediaStatus
{
    public override string Id => Missing;

    public override string Text => "Пропал";

    public override string IconText => "⛒";

    public override Color IconColor => Color.DarkRed;
}

public class MediaStatusNone : MediaStatus
{
    public override string Id => None;

    public override string Text => "Отсутствует";

    public override string IconText => "○";

    public override Color IconColor => Color.Gray;
}

// todo это в плугин унесём и сделаем пропертю "успех не успех"
public class MediaStatusPartialOk : MediaStatus
{
    public override string Id => PartialOk;

    public override string Text => "Частичный успех";

    public override string IconText => "!";

    public override Color IconColor => Color.DarkOrange;
}
