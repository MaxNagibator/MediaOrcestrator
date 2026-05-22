namespace MediaOrcestrator.Domain.Tests.TestTools.Extensions;

public static class LinkAssertions
{
    public static MediaSourceLink ShouldHaveStatus(this MediaSourceLink? link, string expectedStatus)
    {
        Assert.That(link, Is.Not.Null, "Связь с источником не найдена");
        Assert.That(link!.Status, Is.EqualTo(expectedStatus));

        return link;
    }
}
