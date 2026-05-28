namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class CoverNumberResolverTests
{
    [Test]
    public void В_режиме_Sequential_всегда_возвращает_StartNumber_плюс_index_не_трогая_Title()
    {
        var template = MakeTemplate(CoverNumberMode.Sequential, 10);

        var number = CoverNumberResolver.Resolve(template, "#42 — Финал", 3);

        Assert.That(number, Is.EqualTo(13));
    }

    [Test]
    public void В_режиме_TitleRegex_тянет_число_из_первой_группы_захвата()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 1, @"#(\d+)");

        var number = CoverNumberResolver.Resolve(template, "Серия #07", 0);

        Assert.That(number, Is.EqualTo(7));
    }

    [Test]
    public void В_режиме_TitleRegex_без_групп_использует_всё_совпадение()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 1, @"\d+");

        var number = CoverNumberResolver.Resolve(template, "Эпизод 99", 0);

        Assert.That(number, Is.EqualTo(99));
    }

    [Test]
    public void Пустой_TitleRegexPattern_подменяется_дефолтным_паттерном()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 100, "   ");

        var number = CoverNumberResolver.Resolve(template, "Бэкпекинг #5", 0);

        Assert.That(number, Is.EqualTo(5));
    }

    [Test]
    public void Когда_совпадения_нет_фолбэк_на_StartNumber_плюс_index()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 50, @"EP(\d+)");

        var number = CoverNumberResolver.Resolve(template, "без номера", 4);

        Assert.That(number, Is.EqualTo(54));
    }

    [Test]
    public void Пустой_Title_тоже_даёт_фолбэк_а_не_исключение()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 1, @"(\d+)");

        var number = CoverNumberResolver.Resolve(template, null, 2);

        Assert.That(number, Is.EqualTo(3));
    }

    [Test]
    public void Невалидная_регулярка_не_роняет_резолвер()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 7, @"[unclosed");

        var number = CoverNumberResolver.Resolve(template, "Серия 5", 1);

        Assert.That(number, Is.EqualTo(8));
    }

    [Test]
    public void Совпадение_не_числом_даёт_фолбэк()
    {
        var template = MakeTemplate(CoverNumberMode.TitleRegex, 20, @"(\w+)");

        var number = CoverNumberResolver.Resolve(template, "Финал", 0);

        Assert.That(number, Is.EqualTo(20));
    }

    private static CoverTemplate MakeTemplate(CoverNumberMode mode, int startNumber, string regex = "")
    {
        return new(string.Empty,
            startNumber,
            mode,
            regex,
            [
                new("{number}", 0.5f, 0.5f, 0.25f, "Arial", CoverFontStyle.Bold, new(255, 255, 255), new(0, 0, 0),
                    0.01f),
            ]);
    }
}
