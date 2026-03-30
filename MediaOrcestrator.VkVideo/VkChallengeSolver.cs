using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaOrcestrator.VkVideo;

// TODO: Копипаста + нейросетевая дрисня. Дай бог будет работать.

/// <summary>
/// Решает JS-challenge VK ("Проверяем, что вы не робот") программно.
/// VK возвращает HTML-страницу с обфусцированным JS, который вычисляет salt,
/// затем key = md5(hash429 + ':' + salt) и редиректит на URL с key.
/// </summary>
internal static class VkChallengeSolver
{
    /// <summary>
    /// Проверяет, является ли HTML-ответ страницей challenge VK.
    /// </summary>
    public static bool IsChallengePage(string body)
    {
        return body.Contains("hash429") && body.Contains("var salt");
    }

    /// <summary>
    /// Пытается решить challenge: извлекает hash429 из URL, вычисляет salt из HTML,
    /// возвращает URL с параметром key для прохождения проверки.
    /// </summary>
    public static ChallengeResult TrySolve(Uri? challengeUri, string htmlBody)
    {
        if (challengeUri == null)
        {
            return ChallengeResult.Fail("challenge URI отсутствует");
        }

        var hash429 = ExtractQueryParam(challengeUri, "hash429");
        if (string.IsNullOrEmpty(hash429))
        {
            return ChallengeResult.Fail("параметр hash429 не найден в URL");
        }

        var salt = ComputeSalt(htmlBody);
        if (salt == null)
        {
            return ChallengeResult.Fail("не удалось вычислить salt из JS-кода");
        }

        var key = Md5Hash($"{hash429}:{salt}");

        var query = challengeUri.Query.TrimStart('?');
        var newQuery = AddOrReplaceQueryParam(query, "key", key);
        var uriBuilder = new UriBuilder(challengeUri) { Query = newQuery };
        return ChallengeResult.Ok(uriBuilder.Uri, hash429, salt, key);
    }

    private static string? ComputeSalt(string html)
    {
        var codesIdx = html.IndexOf("var codes = [", StringComparison.Ordinal);
        if (codesIdx < 0)
        {
            return null;
        }

        var arrayStart = html.IndexOf('[', codesIdx);
        var arrayEnd = FindMatchingBracket(html, arrayStart, '[', ']');
        if (arrayEnd < 0)
        {
            return null;
        }

        var subArrays = SplitNestedArrays(html[(arrayStart + 1)..arrayEnd]);

        var salt = new StringBuilder();
        foreach (var subArray in subArrays)
        {
            var charCode = EvaluateFunctionChain(subArray);
            if (charCode == null)
            {
                return null;
            }

            salt.Append((char)charCode.Value);
        }

        return salt.ToString();
    }

    private static int FindMatchingBracket(string text, int start, char open, char close)
    {
        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == open)
            {
                depth++;
            }
            else if (text[i] == close)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static List<string> SplitNestedArrays(string content)
    {
        var result = new List<string>();
        var depth = 0;
        var start = -1;
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '[')
            {
                if (depth == 0)
                {
                    start = i + 1;
                }

                depth++;
            }
            else if (content[i] == ']')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    result.Add(content[start..i]);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Вычисляет результат цепочки функций справа налево: последняя — константа, остальные — трансформации.
    /// </summary>
    private static int? EvaluateFunctionChain(string subArray)
    {
        var funcs = ExtractFunctions(subArray);
        if (funcs.Count == 0)
        {
            return null;
        }

        var code = ParseConstant(funcs[^1]) ?? 0;
        for (var j = funcs.Count - 2; j >= 0; j--)
        {
            var result = ApplyFunction(funcs[j], code);
            if (result == null)
            {
                return null;
            }

            code = result.Value;
        }

        return code;
    }

    /// <summary>
    /// Извлекает все функции (function(...){...}) из строки подмассива.
    /// </summary>
    private static List<string> ExtractFunctions(string content)
    {
        var functions = new List<string>();
        var i = 0;
        while (i < content.Length)
        {
            var idx = content.IndexOf("(function", i, StringComparison.Ordinal);
            if (idx < 0)
            {
                break;
            }

            // Ищем закрывающую скобку с учётом вложенности
            var depth = 0;
            var end = -1;
            for (var j = idx; j < content.Length; j++)
            {
                if (content[j] == '(')
                {
                    depth++;
                }
                else if (content[j] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end = j;
                        break;
                    }
                }
            }

            if (end < 0)
            {
                break;
            }

            functions.Add(content[idx..(end + 1)]);
            i = end + 1;
        }

        return functions;
    }

    /// <summary>
    /// Парсит константную функцию: (function(){return N;})
    /// </summary>
    private static int? ParseConstant(string func)
    {
        var match = Regex.Match(func, @"return\s+(-?\d+);");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// Применяет функцию-трансформацию к значению.
    /// Поддерживаемые паттерны: e+N, e-N, e^N, map[e].
    /// </summary>
    private static int? ApplyFunction(string func, int input)
    {
        // e + N
        var m = Regex.Match(func, @"return\s+e\s*\+\s*(-?\d+);");
        if (m.Success)
        {
            return input + int.Parse(m.Groups[1].Value);
        }

        // e - N
        m = Regex.Match(func, @"return\s+e\s*-\s*(-?\d+);");
        if (m.Success)
        {
            return input - int.Parse(m.Groups[1].Value);
        }

        // e ^ N (XOR)
        m = Regex.Match(func, @"return\s+e\s*\^\s*(-?\d+);");
        if (m.Success)
        {
            return input ^ int.Parse(m.Groups[1].Value);
        }

        m = Regex.Match(func, @"var\s+map\s*=\s*\{([^}]+)\}");
        if (!m.Success)
        {
            return null;
        }

        var mapEntry = Regex.Matches(m.Groups[1].Value, @"""(-?\d+)""\s*:\s*(-?\d+)")
            .FirstOrDefault(p => int.Parse(p.Groups[1].Value) == input);

        return mapEntry != null ? int.Parse(mapEntry.Groups[2].Value) : null;
    }

    private static string? ExtractQueryParam(Uri uri, string name)
    {
        var query = uri.Query.TrimStart('?');
        foreach (var segment in query.Split('&'))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length == 2 && parts[0] == name)
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }

    private static string AddOrReplaceQueryParam(string query, string name, string value)
    {
        var parts = string.IsNullOrEmpty(query) ? [] : query.Split('&').ToList();
        var found = false;
        for (var i = 0; i < parts.Count; i++)
        {
            if (parts[i].StartsWith($"{name}=", StringComparison.Ordinal))
            {
                parts[i] = $"{name}={Uri.EscapeDataString(value)}";
                found = true;
                break;
            }
        }

        if (!found)
        {
            parts.Add($"{name}={Uri.EscapeDataString(value)}");
        }

        return string.Join("&", parts);
    }

    private static string Md5Hash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public sealed class ChallengeResult
    {
        public Uri? SolveUri { get; private init; }
        public bool Success { get; private init; }
        public string? Error { get; private init; }
        public string? Hash429 { get; private init; }
        public string? Salt { get; private init; }
        public string? Key { get; private init; }

        public static ChallengeResult Ok(Uri solveUri, string hash429, string salt, string key)
        {
            return new() { Success = true, SolveUri = solveUri, Hash429 = hash429, Salt = salt, Key = key };
        }

        public static ChallengeResult Fail(string error)
        {
            return new() { Success = false, Error = error };
        }
    }
}
