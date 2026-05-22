using LiteDB;
using MediaOrcestrator.Domain.Merging;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
public sealed class MediaMergeServiceTests
{
    [Test]
    public void Конфликт_источников_оставляет_рабочую_ссылку_а_не_пропавшую()
    {
        using var bed = TestBed.Create();

        var target = bed.AddMedia("Перезалитое видео",
            ("yt", "OLD", MediaStatus.Missing),
            ("hdd", "h1", MediaStatus.Ok));

        var donor = bed.AddMedia("Перезалитое видео",
            ("yt", "NEW", MediaStatus.Ok));

        var preview = bed.Service.BuildPreview([target, donor]);
        bed.Service.Apply(preview, true);

        var merged = bed.GetMedia(target.Id);
        var ytLink = merged.Sources.Single(s => s.SourceId == "yt");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(preview.HasConflicts, Is.True, "Дубль источника всё равно остаётся конфликтом");
            Assert.That(ytLink.Status, Is.EqualTo(MediaStatus.Ok));
            Assert.That(ytLink.ExternalId, Is.EqualTo("NEW"), "Победить должна ссылка на живое видео, а не мёртвый ExternalId");
            Assert.That(bed.GetMedia(donor.Id), Is.Null, "Присоединённое медиа удаляется");
        }
    }

    [Test]
    public void Из_нескольких_конфликтующих_ссылок_выбирается_самая_живая()
    {
        using var bed = TestBed.Create();

        var target = bed.AddMedia("Видео",
            ("yt", "err", MediaStatus.Error),
            ("hdd", "h1", MediaStatus.Ok));

        var missingDonor = bed.AddMedia("Видео", ("yt", "gone", MediaStatus.Missing));
        var okDonor = bed.AddMedia("Видео", ("yt", "live", MediaStatus.Ok));

        var preview = bed.Service.BuildPreview([target, missingDonor, okDonor], target);
        bed.Service.Apply(preview, true);

        var ytLink = bed.GetMedia(target.Id).Sources.Single(s => s.SourceId == "yt");

        Assert.That(ytLink.ExternalId, Is.EqualTo("live"));
    }

    [Test]
    public void Непересекающаяся_пропавшая_ссылка_переживает_объединение_как_есть()
    {
        using var bed = TestBed.Create();

        var target = bed.AddMedia("Видео", ("hdd", "h1", MediaStatus.Ok));
        var donor = bed.AddMedia("Видео", ("yt", "y1", MediaStatus.Missing));

        var preview = bed.Service.BuildPreview([target, donor], target);
        bed.Service.Apply(preview);

        var merged = bed.GetMedia(target.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(preview.HasConflicts, Is.False);
            Assert.That(merged.Sources.Single(s => s.SourceId == "yt").Status,
                Is.EqualTo(MediaStatus.Missing),
                "Пропавший источник без конфликта остаётся пропавшим — объединение его не трогает");
        }
    }

    private sealed class TestBed : IDisposable
    {
        private TestBed(LiteDatabase database, MediaMergeService service)
        {
            Database = database;
            Service = service;
        }

        public LiteDatabase Database { get; }

        public MediaMergeService Service { get; }

        public static TestBed Create()
        {
            var database = new LiteDatabase(":memory:");
            var pluginManager = new PluginManager([], null!, NullLogger<PluginManager>.Instance);
            var orcestrator = new Orcestrator(pluginManager, database, null!, null!, null!, NullLogger<Orcestrator>.Instance);
            var service = new MediaMergeService(orcestrator, NullLogger<MediaMergeService>.Instance);

            return new(database, service);
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

        public Media GetMedia(string id)
        {
            return Database.GetCollection<Media>("medias").FindById(id);
        }

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}
