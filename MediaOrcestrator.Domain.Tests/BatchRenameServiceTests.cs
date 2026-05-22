using LiteDB;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
public sealed class BatchRenameServiceTests
{
    [Test]
    public async Task PartialOk_от_плагина_трактуется_как_провал_и_БД_не_меняется()
    {
        using var bed = TestBed.Create();
        var source = bed.RegisterSource<FakeSourceA>("src-a");
        source.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult
        {
            Status = MediaStatusHelper.GetById(MediaStatus.PartialOk),
            Id = "ext-1",
            Message = "Не удалось обновить превью",
        });

        var media = bed.AddMedia("Старое название", ("src-a", "ext-1", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое название", null)],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Не удалось обновить превью"));
            Assert.That(result.Sources, Has.One.Matches<BatchRenameSourceResult>(s => s.Outcome == BatchRenameSourceOutcome.Failed));
            Assert.That(persisted.Title, Is.EqualTo("Старое название"), "БД не должна меняться при провале");
        }
    }

    [Test]
    public async Task Провал_второго_источника_не_откатывает_успешно_обновлённый_первый()
    {
        using var bed = TestBed.Create();
        var first = bed.RegisterSource<FakeSourceA>("src-a");
        var second = bed.RegisterSource<FakeSourceB>("src-b");

        first.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult
        {
            Status = MediaStatusHelper.Ok(),
            Id = "ext-a",
        });

        second.UpdateHandler = (_, _, _, _) => throw new InvalidOperationException("API упало");

        var media = bed.AddMedia("Старое",
            ("src-a", "ext-a", MediaStatus.Ok),
            ("src-b", "ext-b", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое", null)],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var titlesSentToFirst = first.UpdateCalls.Select(c => c.Dto.Title).ToArray();
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(titlesSentToFirst, Is.EqualTo(["Новое"]),
                "Первый источник получает запрос ровно один раз — отката к старому названию больше нет");

            Assert.That(result.Sources.Single(s => s.SourceId == "src-a").Outcome, Is.EqualTo(BatchRenameSourceOutcome.Updated));
            Assert.That(result.Sources.Single(s => s.SourceId == "src-b").Outcome, Is.EqualTo(BatchRenameSourceOutcome.Failed));
            Assert.That(persisted.Sources.Single(s => s.SourceId == "src-a").Title, Is.EqualTo("Новое"),
                "Удавшийся источник фиксируется в БД, чтобы она не разъехалась с площадкой");

            Assert.That(persisted.Sources.Single(s => s.SourceId == "src-b").Title, Is.EqualTo("Старое"));
            Assert.That(persisted.Title, Is.EqualTo("Старое"),
                "media.Title остаётся прежним — переименование завершилось не на всех площадках");
        }
    }

    [Test]
    public async Task Описание_не_переданное_в_запросе_подтягивается_из_link_не_перезатирая_per_source()
    {
        using var bed = TestBed.Create();
        var source = bed.RegisterSource<FakeSourceA>("src-a");

        var media = bed.AddMedia("Заголовок", ("src-a", "ext", MediaStatus.Ok));
        media.Description = "Общее описание";
        media.Sources[0].Description = "Кастомное описание площадки";
        bed.SaveMedia(media);

        await bed.Service.ApplyAsync([new(media, "Новый заголовок", null)],
            null,
            null,
            CancellationToken.None);

        Assert.That(source.UpdateCalls.Single().Dto.Description,
            Is.EqualTo("Кастомное описание площадки"),
            "Описание не передавали в запросе — должно остаться кастомное per-source, а не общее");
    }

    [Test]
    public async Task Источник_с_невыгруженным_плагином_пропускается_остальные_обновляются()
    {
        using var bed = TestBed.Create();
        var working = bed.RegisterSource<FakeSourceA>("src-ok");
        working.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-ok" });

        bed.AddOrphanSource("src-broken");

        var media = bed.AddMedia("Старое",
            ("src-broken", "ext-broken", MediaStatus.Ok),
            ("src-ok", "ext-ok", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое", null)],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var brokenOutcome = result.Sources.Single(s => s.SourceId == "src-broken").Outcome;
        var okOutcome = result.Sources.Single(s => s.SourceId == "src-ok").Outcome;
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(brokenOutcome, Is.EqualTo(BatchRenameSourceOutcome.Skipped));
            Assert.That(okOutcome, Is.EqualTo(BatchRenameSourceOutcome.Updated));
            Assert.That(persisted.Title, Is.EqualTo("Новое"));
            Assert.That(persisted.Sources.Single(s => s.SourceId == "src-ok").Title, Is.EqualTo("Новое"),
                "sourceLink.Title должен обновиться после успешного апдейта");
        }
    }

    [Test]
    public async Task Источник_не_в_AllowedSourceIds_не_получает_запрос_и_помечается_Skipped()
    {
        using var bed = TestBed.Create();
        var allowed = bed.RegisterSource<FakeSourceA>("src-a");
        var excluded = bed.RegisterSource<FakeSourceB>("src-b");

        var media = bed.AddMedia("Старое",
            ("src-a", "ext-a", MediaStatus.Ok),
            ("src-b", "ext-b", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое", null, new HashSet<string> { "src-a" })],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var persisted = bed.GetMedia(media.Id);
        var excludedOutcome = result.Sources.Single(s => s.SourceId == "src-b");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(allowed.UpdateCalls, Has.Count.EqualTo(1));
            Assert.That(excluded.UpdateCalls, Is.Empty, "Снятый источник вообще не должен получать UpdateAsync");
            Assert.That(excludedOutcome.Outcome, Is.EqualTo(BatchRenameSourceOutcome.Skipped));
            Assert.That(excludedOutcome.Message, Does.Contain("Снят"));
            Assert.That(persisted.Title, Is.EqualTo("Новое"));
            Assert.That(persisted.Sources.Single(s => s.SourceId == "src-b").Title, Is.EqualTo("Старое"),
                "У снятого источника sourceLink.Title не должен меняться");
        }
    }

    [Test]
    public async Task Отмена_после_первого_успеха_не_откатывает_первый_но_фиксирует_прогресс()
    {
        using var bed = TestBed.Create();
        var first = bed.RegisterSource<FakeSourceA>("src-a");
        var second = bed.RegisterSource<FakeSourceB>("src-b");

        using var cts = new CancellationTokenSource();
        first.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-a" });
        second.UpdateHandler = (_, _, _, token) =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-b" });
        };

        var media = bed.AddMedia("Старое",
            ("src-a", "ext-a", MediaStatus.Ok),
            ("src-b", "ext-b", MediaStatus.Ok));

        OperationCanceledException? caught = null;
        try
        {
            await bed.Service.ApplyAsync([new(media, "Новое", null)],
                null,
                null,
                cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            caught = ex;
        }

        var titlesSentToFirst = first.UpdateCalls.Select(c => c.Dto.Title).ToArray();
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caught, Is.Not.Null);
            Assert.That(titlesSentToFirst, Is.EqualTo(["Новое"]),
                "Отмена не откатывает уже переименованную площадку");

            Assert.That(persisted.Sources.Single(s => s.SourceId == "src-a").Title, Is.EqualTo("Новое"),
                "Прогресс до отмены фиксируется в БД — иначе она отстанет от площадки");

            Assert.That(persisted.Title, Is.EqualTo("Старое"));
        }
    }

    [Test]
    public async Task Площадка_вернула_Ok_но_название_на_деле_не_изменилось_трактуется_как_провал()
    {
        using var bed = TestBed.Create();
        var source = bed.RegisterSource<FakeSourceA>("src-a");

        source.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-1" });
        source.GetMediaByIdHandler = (_, _, _) => Task.FromResult<MediaDto?>(new()
            { Title = "Старое название" });

        var media = bed.AddMedia("Старое название", ("src-a", "ext-1", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое название", null)],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Sources, Has.One.Matches<BatchRenameSourceResult>(s => s.Outcome == BatchRenameSourceOutcome.VerificationFailed));
            Assert.That(persisted.Title, Is.EqualTo("Старое название"), "БД не должна меняться, если сверка не прошла");
            Assert.That(persisted.Sources.Single().Title, Is.EqualTo("Старое название"));
        }
    }

    [Test]
    public async Task Провал_сверки_на_втором_источнике_не_откатывает_успешный_первый()
    {
        using var bed = TestBed.Create();
        var first = bed.RegisterSource<FakeSourceA>("src-a");
        var second = bed.RegisterSource<FakeSourceB>("src-b");

        first.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-a" });
        first.GetMediaByIdHandler = (_, _, _) => Task.FromResult<MediaDto?>(new()
            { Title = "Новое" });

        second.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-b" });
        second.GetMediaByIdHandler = (_, _, _) => Task.FromResult<MediaDto?>(new()
            { Title = "Старое" });

        var media = bed.AddMedia("Старое",
            ("src-a", "ext-a", MediaStatus.Ok),
            ("src-b", "ext-b", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое", null)],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var titlesSentToFirst = first.UpdateCalls.Select(c => c.Dto.Title).ToArray();
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(titlesSentToFirst, Is.EqualTo(["Новое"]),
                "Удавшийся первый источник не откатывается из-за провала сверки на втором");

            Assert.That(result.Sources.Single(s => s.SourceId == "src-a").Outcome, Is.EqualTo(BatchRenameSourceOutcome.Updated));
            Assert.That(result.Sources.Single(s => s.SourceId == "src-b").Outcome, Is.EqualTo(BatchRenameSourceOutcome.VerificationFailed));
            Assert.That(persisted.Sources.Single(s => s.SourceId == "src-a").Title, Is.EqualTo("Новое"),
                "Провал сверки на втором источнике фиксирует первый в БД, а не откатывает его");

            Assert.That(persisted.Title, Is.EqualTo("Старое"));
        }
    }

    [Test]
    public async Task Сверка_проходит_когда_площадка_вернула_нормализованное_название()
    {
        using var bed = TestBed.Create();
        var source = bed.RegisterSource<FakeSourceA>("src-a");

        source.UpdateHandler = (_, _, _, _) => Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = "ext-1" });

        source.GetMediaByIdHandler = (_, _, _) => Task.FromResult<MediaDto?>(new()
            { Title = "Новое название" });

        var media = bed.AddMedia("Старое", ("src-a", "ext-1", MediaStatus.Ok));

        var results = await bed.Service.ApplyAsync([new(media, "Новое: название", null)],
            null,
            null,
            CancellationToken.None);

        var result = results.Single();
        var persisted = bed.GetMedia(media.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Sources.Single().Outcome, Is.EqualTo(BatchRenameSourceOutcome.Updated));
            Assert.That(persisted.Title, Is.EqualTo("Новое: название"), "В БД сохраняем запрошенное название, а не нормализованное площадкой");
        }
    }

    private sealed class TestBed : IDisposable
    {
        private readonly List<FakeSourceTypeBase> _sourceTypes = [];

        private TestBed(LiteDatabase database, Orcestrator orcestrator, PluginManager pluginManager, BatchRenameService service)
        {
            Database = database;
            Orcestrator = orcestrator;
            PluginManager = pluginManager;
            Service = service;
        }

        public LiteDatabase Database { get; }
        public Orcestrator Orcestrator { get; }
        public PluginManager PluginManager { get; }
        public BatchRenameService Service { get; }

        public static TestBed Create()
        {
            var database = new LiteDatabase(":memory:");
            var pluginManager = new PluginManager([], null!, NullLogger<PluginManager>.Instance);
            var actionHolder = new ActionHolder(NullLogger<ActionHolder>.Instance);
            var tempManager = new TempManager(Path.Combine(Path.GetTempPath(), "MediaOrcestratorTests"), database, NullLogger<TempManager>.Instance);
            var orcestrator = new Orcestrator(pluginManager, database, tempManager, null!, actionHolder, NullLogger<Orcestrator>.Instance);
            var service = new BatchRenameService(orcestrator, NullLogger<BatchRenameService>.Instance);

            return new(database, orcestrator, pluginManager, service);
        }

        public T RegisterSource<T>(string sourceId) where T : FakeSourceTypeBase, new()
        {
            var instance = new T { Name = sourceId };
            _sourceTypes.Add(instance);

            var prop = typeof(PluginManager).GetProperty(nameof(PluginManager.MediaSources))!;
            var current = (Dictionary<string, ISourceType>)prop.GetValue(PluginManager)!;
            current[typeof(T).FullName!] = instance;

            Database.GetCollection<Source>("sources")
                .Insert(new Source
                {
                    Id = sourceId,
                    TypeId = sourceId,
                    Settings = new() { ["_system_name"] = sourceId },
                });

            return instance;
        }

        public void AddOrphanSource(string sourceId)
        {
            Database.GetCollection<Source>("sources")
                .Insert(new Source
                {
                    Id = sourceId,
                    TypeId = sourceId,
                    Settings = new() { ["_system_name"] = sourceId },
                });
        }

        public Media AddMedia(string title, params (string SourceId, string ExternalId, string Status)[] links)
        {
            var media = new Media
            {
                Id = "media-" + Guid.NewGuid().ToString("N"),
                Title = title,
                Description = "desc",
                Sources = [],
            };

            foreach (var (sourceId, externalId, status) in links)
            {
                media.Sources.Add(new()
                {
                    MediaId = media.Id,
                    SourceId = sourceId,
                    ExternalId = externalId,
                    Status = status,
                    Title = title,
                    Description = "desc",
                    SortNumber = 1,
                });
            }

            Database.GetCollection<Media>("medias").Insert(media);
            return media;
        }

        public void SaveMedia(Media media)
        {
            Database.GetCollection<Media>("medias").Update(media);
        }

        public Media GetMedia(string id)
        {
            return Database.GetCollection<Media>("medias").FindById(id);
        }

        public void Dispose()
        {
            Database.Dispose();
        }
    }

    private abstract class FakeSourceTypeBase : ISourceType
    {
        public string Name { get; set; } = "fake";
        public SyncDirection ChannelType => SyncDirection.Full;
        public IEnumerable<SourceSettings> SettingsKeys => [];

        public Func<string, MediaDto, Dictionary<string, string>, CancellationToken, Task<UploadResult>>? UpdateHandler { get; set; }
        public List<(string ExternalId, MediaDto Dto)> UpdateCalls { get; } = [];

        public Func<string, Dictionary<string, string>, CancellationToken, Task<MediaDto?>>? GetMediaByIdHandler { get; set; }

        public Task<UploadResult> UpdateAsync(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((externalId, new()
            {
                Title = tempMedia.Title,
                Description = tempMedia.Description,
            }));

            if (UpdateHandler != null)
            {
                return UpdateHandler(externalId, tempMedia, settings, cancellationToken);
            }

            return Task.FromResult(new UploadResult { Status = MediaStatusHelper.Ok(), Id = externalId });
        }

        public IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, CancellationToken cancellationToken = default)
        {
            return EmptyAsync(cancellationToken);

            static async IAsyncEnumerable<MediaDto> EmptyAsync([EnumeratorCancellation] CancellationToken ct)
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        public Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        {
            return GetMediaByIdHandler?.Invoke(externalId, settings, cancellationToken)
                   ?? Task.FromResult<MediaDto?>(null);
        }

        public Task<MediaDto> DownloadAsync(string videoId, Dictionary<string, string> settings, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UploadResult> UploadAsync(MediaDto media, Dictionary<string, string> settings, IProgress<UploadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSourceA : FakeSourceTypeBase;

    private sealed class FakeSourceB : FakeSourceTypeBase;
}
