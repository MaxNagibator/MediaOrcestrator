namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class ParallelSyncExecutorTests
{
    [TestCase(SyncRunOutcome.Transferred, 2)]
    [TestCase(SyncRunOutcome.Skipped, 1)]
    public async Task Цепочка_продолжается_только_после_передачи(SyncRunOutcome rootOutcome, int expectedRuns)
    {
        var youtube = CreateSource("youtube");
        var disk = CreateSource("disk");
        var rutube = CreateSource("rutube");
        var toDisk = CreateRelation(youtube, disk);
        var toRutube = CreateRelation(disk, rutube);
        var media = new Media { Id = "media", Title = "media" };
        var next = new SyncIntent { Media = media, From = disk, To = rutube, Relation = toRutube };
        var root = new SyncIntent { Media = media, From = youtube, To = disk, Relation = toDisk, NextIntents = [next] };
        var executed = new List<SyncIntent>();
        var executor = new ParallelSyncExecutor([root], [toDisk, toRutube]);

        await executor.ExecuteAsync(intent =>
            {
                lock (executed)
                {
                    executed.Add(intent);
                }

                return Task.FromResult(intent == root ? rootOutcome : SyncRunOutcome.Transferred);
            },
            CancellationToken.None);

        Assert.That(executed, Has.Count.EqualTo(expectedRuns));
    }

    private static Source CreateSource(string id)
    {
        return new() { Id = id, TypeId = id, Settings = [] };
    }

    private static SourceSyncRelation CreateRelation(Source from, Source to)
    {
        return new() { FromId = from.Id, ToId = to.Id, From = from, To = to };
    }
}
