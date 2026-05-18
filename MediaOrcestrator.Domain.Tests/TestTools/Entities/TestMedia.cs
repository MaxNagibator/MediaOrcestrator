namespace MediaOrcestrator.Domain.Tests.TestTools.Entities;

public class TestMedia : TestObject
{
    private readonly List<MediaSourceLink> _links = [];

    public string Title { get; private set; } = TestRandom.GetString("Title");
    public string Description { get; } = TestRandom.GetString("Desc");

    public override void LocalSave()
    {
        if (!IsNew)
        {
            return;
        }

        Environment.Database.GetCollection<Media>("medias").Insert(Build());
        IsNew = false;
    }

    public TestMedia SetTitle(string value)
    {
        Title = value;
        return this;
    }

    public TestMedia WithSourceLink(Source source, string status, string? externalId = null)
    {
        _links.Add(new()
        {
            SourceId = source.Id,
            Status = status,
            ExternalId = externalId ?? TestRandom.GetString("ext"),
            Title = Title,
            Description = Description,
            SortNumber = TestRandom.GetInt(1, 1000),
        });

        return this;
    }

    public Media Build()
    {
        var media = new Media
        {
            Id = Environment.MediaId,
            Title = Title,
            Description = Description,
            Sources = [],
        };

        foreach (var link in _links)
        {
            media.Sources.Add(new()
            {
                MediaId = media.Id,
                Media = media,
                SourceId = link.SourceId,
                Status = link.Status,
                ExternalId = link.ExternalId,
                Title = link.Title,
                Description = link.Description,
                SortNumber = link.SortNumber,
            });
        }

        return media;
    }
}
