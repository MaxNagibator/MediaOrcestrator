using System.Diagnostics.CodeAnalysis;

[assembly: Retry(3)]
[assembly: ExcludeFromCodeCoverage]

namespace MediaOrcestrator.Core.Tests;

public class GlobalHooks
{
    [Before(TestSession)]
    public static void SetUp()
    {
        Console.WriteLine(@"Or you can define methods that do stuff before...");
    }

    [After(TestSession)]
    public static void CleanUp()
    {
        Console.WriteLine(@"...and after!");
    }
}
