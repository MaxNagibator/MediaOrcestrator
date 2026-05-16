namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
public class ProgressTextFormatterTests
{
    private static IEnumerable<TestCaseData> ОстаткиEta()
    {
        yield return new TestCaseData((TimeSpan?)null).Returns("оценка...");
        yield return new TestCaseData((TimeSpan?)TimeSpan.FromSeconds(-5)).Returns("оценка...");
        yield return new TestCaseData((TimeSpan?)TimeSpan.FromSeconds(45)).Returns("осталось ~45 с");
    }

    [TestCaseSource(nameof(ОстаткиEta))]
    public string FormatEta_отдаёт_заглушку_или_остаток_с_префиксом(TimeSpan? remaining)
    {
        return ProgressTextFormatter.FormatEta(remaining);
    }

    [TestCase(0, ExpectedResult = "0 с")]
    [TestCase(59, ExpectedResult = "59 с")]
    [TestCase(61, ExpectedResult = "2 мин")]
    [TestCase(4320, ExpectedResult = "1 ч 12 мин")]
    [TestCase(7200, ExpectedResult = "2 ч")]
    public string Грубая_длительность_считается_по_ветке(int totalSeconds)
    {
        return ProgressTextFormatter.FormatDurationApprox(TimeSpan.FromSeconds(totalSeconds));
    }
}
