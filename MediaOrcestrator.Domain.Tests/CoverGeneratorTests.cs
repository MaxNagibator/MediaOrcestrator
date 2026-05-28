using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
file sealed class CoverGeneratorTests
{
    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CoverGenTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
    }

    private string _tempDir = string.Empty;

    [Test]
    public void Render_возвращает_независимые_битмапы_а_не_общий_кеш()
    {
        var templatePath = CreateSolidTemplate(Path.Combine(_tempDir, "tpl.png"), 320, 180, SKColors.White);
        using var generator = new CoverGenerator(NullLogger<CoverGenerator>.Instance);
        var template = MakeTemplate(templatePath);

        using var first = generator.Render(template, 1);
        using var second = generator.Render(template, 2);

        Assert.That(first, Is.Not.SameAs(second), "Render должен отдавать свежий битмап на каждый вызов");
    }

    [Test]
    public void Изменение_файла_шаблона_инвалидирует_кеш_и_размеры_перечитываются()
    {
        var templatePath = Path.Combine(_tempDir, "tpl.png");
        CreateSolidTemplate(templatePath, 200, 100, SKColors.Black);
        using var generator = new CoverGenerator(NullLogger<CoverGenerator>.Instance);
        var template = MakeTemplate(templatePath);

        using (var beforeBitmap = generator.Render(template, 1))
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeBitmap.Width, Is.EqualTo(200));
                Assert.That(beforeBitmap.Height, Is.EqualTo(100));
            }
        }

        File.Delete(templatePath);
        CreateSolidTemplate(templatePath, 400, 240, SKColors.Black);
        File.SetLastWriteTimeUtc(templatePath, DateTime.UtcNow.AddSeconds(5));

        using var afterBitmap = generator.Render(template, 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterBitmap.Width, Is.EqualTo(400));
            Assert.That(afterBitmap.Height, Is.EqualTo(240));
        }
    }

    [Test]
    public void Render_бросает_FileNotFoundException_если_шаблона_нет()
    {
        using var generator = new CoverGenerator(NullLogger<CoverGenerator>.Instance);
        var template = MakeTemplate(Path.Combine(_tempDir, "missing.png"));

        Exception? caught = null;

        try
        {
            using var _ = generator.Render(template, 1);
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.That(caught, Is.InstanceOf<FileNotFoundException>());
    }

    [Test]
    public void Generate_создаёт_файл_с_номером_в_имени()
    {
        var templatePath = CreateSolidTemplate(Path.Combine(_tempDir, "tpl.png"), 320, 180, SKColors.White);
        using var generator = new CoverGenerator(NullLogger<CoverGenerator>.Instance);
        var outDir = Path.Combine(_tempDir, "out");
        var template = MakeTemplate(templatePath);

        var produced = generator.Generate(template, 42, outDir);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(produced, Does.EndWith("cover_42.png"));
            Assert.That(File.Exists(produced), Is.True);
        }
    }

    private static string CreateSolidTemplate(string path, int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(color);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
        return path;
    }

    private static CoverTemplate MakeTemplate(string templatePath)
    {
        return new(templatePath,
            1,
            CoverNumberMode.Sequential,
            string.Empty,
            [
                new("{number}", 0.5f, 0.5f, 0.25f, "Arial", CoverFontStyle.Bold, new(255, 0, 0), new(0, 0, 0),
                    0.0f),
            ]);
    }
}
