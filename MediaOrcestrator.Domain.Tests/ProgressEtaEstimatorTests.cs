namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
public class ProgressEtaEstimatorTests
{
    [TestCase(0, 10)]
    [TestCase(1, 10)]
    [TestCase(100, 10)]
    [TestCase(50, 0)]
    public void Неоцениваемый_вход_даёт_null(double percent, double elapsedSeconds)
    {
        var estimator = new ProgressEtaEstimator();

        var result = estimator.Update(percent, TimeSpan.FromSeconds(elapsedSeconds));

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Первые_замеры_ещё_не_стабилизировались()
    {
        var estimator = new ProgressEtaEstimator();

        var first = estimator.Update(10, TimeSpan.FromSeconds(10));
        var second = estimator.Update(20, TimeSpan.FromSeconds(20));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.Null);
            Assert.That(second, Is.Null);
        }
    }

    [Test]
    public void После_трёх_замеров_возвращается_оценка_остатка()
    {
        var estimator = new ProgressEtaEstimator();

        estimator.Update(10, TimeSpan.FromSeconds(10));
        estimator.Update(20, TimeSpan.FromSeconds(20));
        var third = estimator.Update(30, TimeSpan.FromSeconds(30));

        Assert.That(third, Is.Not.Null);
        Assert.That(third!.Value.TotalSeconds, Is.EqualTo(70).Within(15));
    }

    [Test]
    public void Сглаживание_гасит_скачок_одиночного_замера()
    {
        var estimator = new ProgressEtaEstimator();

        estimator.Update(10, TimeSpan.FromSeconds(10));
        estimator.Update(20, TimeSpan.FromSeconds(20));
        var stable = estimator.Update(30, TimeSpan.FromSeconds(30));

        var afterSpike = estimator.Update(31, TimeSpan.FromSeconds(120));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stable, Is.Not.Null);
            Assert.That(afterSpike, Is.Not.Null);

            const double RawSpike = 120.0 / 0.31 - 120.0;
            Assert.That(afterSpike!.Value.TotalSeconds, Is.LessThan(RawSpike));
        }
    }
}
