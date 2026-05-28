using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MediaOrcestrator.Domain.Tests.TestTools;

public sealed class SyncEnvironment : IDisposable
{
    private readonly List<TestObject> _objects = [];
    private readonly Orcestrator _orcestrator;
    private readonly SourceSyncRelation _relation;

    private SyncEnvironment()
    {
        Database = new(":memory:");
        Actions = new(NullLogger<ActionHolder>.Instance);

        FromType = Substitute.For<ISourceType>();
        ToType = Substitute.For<ISourceType>();

        FromType.Name.Returns("from");
        ToType.Name.Returns("to");

        FromType
            .DownloadAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new MediaDto { Id = MediaId, Title = "T", Description = "D", TempDataPath = string.Empty });

        ToType
            .UploadAsync(Arg.Any<MediaDto>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<UploadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new UploadResult { Status = MediaStatusHelper.Ok(), Id = TestRandom.GetString("to-ext") });

        FromType
            .GetMedia(Arg.Any<Dictionary<string, string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => Stream());

        From = new() { Id = "src-from", TypeId = "from", Settings = new(), Type = FromType };
        To = new() { Id = "src-to", TypeId = "to", Settings = new(), Type = ToType };

        _relation = new()
        {
            FromId = From.Id,
            ToId = To.Id,
            From = From,
            To = To,
        };

        var pluginManager = new PluginManager([FromType], null!, NullLogger<PluginManager>.Instance);
        pluginManager.Init();

        var tempManager = new TempManager(Path.GetTempPath(), Database, NullLogger<TempManager>.Instance);

        _orcestrator = new(pluginManager,
            Database,
            tempManager,
            null!,
            Actions,
            NullLogger<Orcestrator>.Instance);

        Database.GetCollection<Source>("sources").Insert(From);
    }

    public string MediaId { get; } = TestRandom.GetString("media");

    public LiteDatabase Database { get; }

    public ActionHolder Actions { get; }

    public ISourceType FromType { get; }
    public ISourceType ToType { get; }

    public Source From { get; }
    public Source To { get; }

    public static SyncEnvironment Create()
    {
        return new();
    }

    public TestMedia WithMedia()
    {
        var media = new TestMedia();
        media.Attach(this);
        return media;
    }

    public TestMedia SnapshotMedia()
    {
        var media = new TestMedia();

        var saved = _objects.OfType<TestMedia>().LastOrDefault();
        if (saved != null)
        {
            media.AsSnapshotOf(saved);
        }

        media.Bind(this);
        return media;
    }

    public void AddObject(TestObject testObject)
    {
        _objects.Add(testObject);
    }

    public SyncEnvironment Save()
    {
        foreach (var testObject in _objects)
        {
            testObject.SaveObject();
        }

        return this;
    }

    public SyncEnvironment WhenDownloadFails(string message)
    {
        FromType
            .DownloadAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(message));

        return this;
    }

    public SyncEnvironment WhenUploadFails(string message)
    {
        ToType
            .UploadAsync(Arg.Any<MediaDto>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<UploadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new UploadResult { Status = MediaStatusHelper.GetById(MediaStatus.Error), Message = message });

        return this;
    }

    public SyncEnvironment SourcePublishes(Source source, params string[] externalIds)
    {
        source.Type
            .GetMedia(Arg.Any<Dictionary<string, string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => Stream(externalIds));

        return this;
    }

    public Task Sync(bool isFull = false, bool onlyNew = false)
    {
        return _orcestrator.GetStorageFullInfo(isFull, onlyNew: onlyNew);
    }

    public async Task<TransferResult> Transfer(Media media)
    {
        Exception? error = null;

        try
        {
            await _orcestrator.TransferByRelation(media, _relation);
        }
        catch (Exception exception)
        {
            error = exception;
        }

        return new(FromType, ToType, error);
    }

    public void Dispose()
    {
        Database.Dispose();
    }

    private static async IAsyncEnumerable<MediaDto> Stream(params string[] externalIds)
    {
        foreach (var id in externalIds)
        {
            yield return new()
                { Id = id, Title = id, Description = string.Empty };
        }

        await Task.CompletedTask;
    }
}
