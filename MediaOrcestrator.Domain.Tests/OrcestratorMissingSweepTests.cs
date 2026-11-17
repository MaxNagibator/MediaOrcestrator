using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class OrcestratorMissingSweepTests
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
    public async Task Пропущенная_связь_не_становится_пропавшей_после_полной_синхронизации()
    {
        var skipped = _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Skipped, string.Empty);
        _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Ok, "live-1");
        _env.Save();

        _env.SourcePublishes(_env.From, "live-1");

        await _env.Sync();

        skipped.LinkTo(_env.From).ShouldHaveStatus(MediaStatus.Skipped);
    }

    [Test]
    public async Task Пропуск_поверх_ранее_существовавшей_связи_переживает_синхронизацию()
    {
        var skippedOverExisting = _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Skipped, "ext-removed");
        _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Ok, "live-1");
        _env.Save();

        _env.SourcePublishes(_env.From, "live-1");

        await _env.Sync();

        skippedOverExisting.LinkTo(_env.From).ShouldHaveStatus(MediaStatus.Skipped);
    }

    [Test]
    public async Task Исключённое_источником_медиа_не_пропадает_и_не_добавляется()
    {
        var excluded = _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Ok, "stream-old");
        _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Ok, "live-1");
        _env.Save();

        _env.SourcePublishesExcluded(_env.From, ["live-1"], ["stream-old", "stream-new"]);

        await _env.Sync();

        excluded.LinkTo(_env.From).ShouldHaveStatus(MediaStatus.Ok);
        var externalIds = _env.Database.GetCollection<Media>("medias").FindAll()
            .SelectMany(x => x.Sources)
            .Select(x => x.ExternalId);
        Assert.That(externalIds, Is.EquivalentTo(new[] { "stream-old", "live-1" }));
    }

    [Test]
    public async Task Реально_пропавшая_связь_по_прежнему_помечается_пропавшей()
    {
        var gone = _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Ok, "ext-gone");
        _env.WithMedia().WithSourceLink(_env.From, MediaStatus.Ok, "live-1");
        _env.Save();

        _env.SourcePublishes(_env.From, "live-1");

        await _env.Sync();

        gone.LinkTo(_env.From).ShouldHaveStatus(MediaStatus.Missing);
    }
}
