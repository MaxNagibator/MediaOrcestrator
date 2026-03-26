using System.Text.RegularExpressions;

namespace MediaOrcestrator.Domain;

public static class GlobMatcher
{
    public static bool IsMatch(string input, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", @"[^/\\]*")
            .Replace(@"\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}
