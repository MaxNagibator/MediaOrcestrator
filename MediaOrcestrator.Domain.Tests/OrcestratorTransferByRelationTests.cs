using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class OrcestratorTransferByRelationTests
{
    [SetUp]
    public void Setup()
    {
        _env = SyncEnvironment.Create();
    }

    [TearDown]
    public void TearDown()
    {
        _env.Dispose();
    }

    private SyncEnvironment _env = null!;

    [Test]
    public async Task Двойной_залив_предотвращён_когда_снимок_устарел_а_в_БД_уже_есть_успешная_связь()
    {
        _env.WithMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok)
            .WithSourceLink(_env.To, MediaStatus.Ok, "to-ext");

        _env.Save();

        var stale = _env.SnapshotMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok)
            .Build();

        await _env.Transfer(stale).ShouldNotReupload();

        var toLink = stale.LinkTo(_env.To);

        Assert.That(toLink, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toLink.Status, Is.EqualTo(MediaStatus.Ok));
            Assert.That(toLink.ExternalId, Is.EqualTo("to-ext"));
        }
    }

    [Test]
    public async Task Существующая_in_memory_проверка_статуса_Ok_по_прежнему_короткозамыкает()
    {
        var media = _env.SnapshotMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok)
            .WithSourceLink(_env.To, MediaStatus.Ok, "to-ext")
            .Build();

        await _env.Transfer(media).ShouldNotReupload();
    }

    [Test]
    public async Task Провал_заливки_помечает_действие_заливки_ошибкой_а_не_успехом()
    {
        _env.WithMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok);

        _env.Save();

        var media = _env.SnapshotMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok)
            .Build();

        _env.WhenUploadFails("Превышена квота YouTube API");

        var result = await _env.Transfer(media);

        var uploadAction = _env.Actions.CompletedSnapshot()
            .Single(action => action.Kind == ActionKind.Upload);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Error, Is.Not.Null, "Провальная заливка должна провалить трансфер");
            Assert.That(uploadAction.State, Is.EqualTo(ActionState.Failed));
        }
    }

    [Test]
    public async Task Трансфер_не_блокируется_когда_связи_с_целевым_источником_ещё_нет()
    {
        _env.WithMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok);

        _env.Save();

        var media = _env.SnapshotMedia()
            .WithSourceLink(_env.From, MediaStatus.Ok)
            .Build();

        _env.WhenDownloadFails("download-reached");

        await _env.Transfer(media).ShouldFailWith("download-reached");
    }
}
