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

        _orcestrator = new(null!,
            Database,
            null!,
            null!,
            new(NullLogger<ActionHolder>.Instance),
            NullLogger<Orcestrator>.Instance);

        FromType = Substitute.For<ISourceType>();
        ToType = Substitute.For<ISourceType>();

        FromType
            .DownloadAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new MediaDto { Id = MediaId, Title = "T", Description = "D", TempDataPath = string.Empty });

        ToType
            .UploadAsync(Arg.Any<MediaDto>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<UploadProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new UploadResult { Status = MediaStatusHelper.Ok(), Id = TestRandom.GetString("to-ext") });

        From = new() { Id = "src-from", TypeId = "from", Settings = new(), Type = FromType };
        To = new() { Id = "src-to", TypeId = "to", Settings = new(), Type = ToType };

        _relation = new()
        {
            FromId = From.Id,
            ToId = To.Id,
            From = From,
            To = To,
        };
    }

    public string MediaId { get; } = TestRandom.GetString("media");

    public LiteDatabase Database { get; }

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
}
