using MediaOrcestrator.Modules;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class OrcestratorDownloadPolicyTests
{
    [Test]
    public async Task Передача_при_запрете_скачивания_пропускается_без_заливки_и_изменения_связей()
    {
        using var environment = SyncEnvironment.Create();
        environment.WithMedia().WithSourceLink(environment.From, MediaStatus.Ok);
        environment.Save();
        var media = environment.SnapshotMedia().WithSourceLink(environment.From, MediaStatus.Ok).Build();
        const string Reason = "Скачивание трансляций отключено";
        environment.FromType
            .DownloadAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MediaDownloadSkippedException(Reason));

        var result = await environment.Transfer(media);

        await environment.ToType.DidNotReceiveWithAnyArgs().UploadAsync(null!, null!);
        await environment.ToType.DidNotReceiveWithAnyArgs().UpdateAsync(null!, null!, null!);
        var actions = environment.Actions.CompletedSnapshot().Where(x => x.Kind is ActionKind.Transfer or ActionKind.Download).ToList();
        var persisted = environment.Database.GetCollection<Media>("medias").FindById(media.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Error, Is.TypeOf<MediaDownloadSkippedException>());
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions.All(x => x is { State: ActionState.Succeeded, Status: "Пропущено: " + Reason }), Is.True);
            Assert.That(persisted.Sources, Has.Count.EqualTo(1));
            Assert.That(persisted.Sources.Single().Status, Is.EqualTo(MediaStatus.Ok));
            Assert.That(media.Sources.Any(x => x.SourceId == environment.To.Id), Is.False);
        }
    }

    [Test]
    public async Task Повторы_при_пропуске_скачивания_сообщают_пропуск_без_новых_попыток()
    {
        using var environment = SyncEnvironment.Create();
        environment.WithMedia().WithSourceLink(environment.From, MediaStatus.Ok);
        environment.Save();
        var media = environment.SnapshotMedia().WithSourceLink(environment.From, MediaStatus.Ok).Build();
        environment.FromType
            .DownloadAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<DownloadProgress>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MediaDownloadSkippedException("Скачивание трансляций отключено"));
        var progress = new AttemptRecorder();

        var outcome = await environment.TransferWithRetries(media, progress, maxAttempts: 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(SyncRunOutcome.Skipped));
            Assert.That(progress.Kinds, Is.EqualTo(new[] { SyncAttemptKind.Started, SyncAttemptKind.Skipped }));
        }
        await environment.FromType.ReceivedWithAnyArgs(1).DownloadAsync(null!, null!);
    }

    [Test]
    public async Task Передача_при_ошибке_скачивания_остаётся_ошибкой()
    {
        using var environment = SyncEnvironment.Create();
        environment.WithMedia().WithSourceLink(environment.From, MediaStatus.Ok);
        environment.Save();
        var media = environment.SnapshotMedia().WithSourceLink(environment.From, MediaStatus.Ok).Build();
        environment.WhenDownloadFails("network failed");

        var result = await environment.Transfer(media);

        var actions = environment.Actions.CompletedSnapshot().Where(x => x.Kind is ActionKind.Transfer or ActionKind.Download).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions.All(x => x.State == ActionState.Failed), Is.True);
        }
    }

    private sealed class AttemptRecorder : IProgress<SyncAttemptStatus>
    {
        public List<SyncAttemptKind> Kinds { get; } = [];

        public void Report(SyncAttemptStatus value)
        {
            Kinds.Add(value.Kind);
        }
    }
}
