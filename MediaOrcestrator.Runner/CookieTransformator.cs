using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

public class CookieTransformator
{
    private class CookieData
    {
        // todo регистр нормально сделать :)
        public string name { get; set; }
        public string value { get; set; }
        public string domain { get; set; }
        public string path { get; set; }
        public double expires { get; set; }
        public bool httpOnly { get; set; }
        public bool secure { get; set; }
        public string sameSite { get; set; }
    }

    private class OriginData
    {
        public string origin { get; set; }
        public List<StorageData> localStorage { get; set; }
    }

    private class StorageData
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    private class ChromeCookiesRoot
    {
        public List<CookieData> cookies { get; set; }
        public List<OriginData> origins { get; set; }
    }

    public static void Run(string playwrightCookiePath, string outputFile, ILogger _logger)
    {
        try
        {
            var playwrightCookieString = File.ReadAllText(playwrightCookiePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var root = JsonSerializer.Deserialize<ChromeCookiesRoot>(playwrightCookieString, options);

            // Собираем все куки из основного массива
            var allCookies = new List<CookieData>();

            if (root!.cookies != null)
            {
                allCookies.AddRange(root.cookies);
            }

            ////// В некоторых форматах куки могут быть в origins
            ////if (root.origins != null)
            ////{
            ////    foreach (var origin in root.origins)
            ////    {
            ////        // Извлекаем домен из origin URL
            ////        string domain = ExtractDomainFromOrigin(origin.origin);

            ////        // localStorage не нужен для yt-dlp, только куки
            ////        // но если вдруг там будут куки (редко), можно добавить
            ////    }
            ////}

            //// Фильтруем только YouTube и Google куки (остальные нахуй не нужны)
            ////var youtubeCookies = allCookies
            ////    .Where(c => c.domain != null &&
            ////          (c.domain.Contains("youtube.com") ||
            ////           c.domain.Contains("ytimg.com") ||
            ////           c.domain.Contains("google.com") ||
            ////           c.domain.Contains("accounts.google.com")))
            ////    .ToList();

            using (var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("# Netscape HTTP Cookie File");
                writer.WriteLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                writer.WriteLine($"# Total cookies: {allCookies.Count}");
                writer.WriteLine();

                foreach (var cookie in allCookies)
                {
                    // domain: должен начинаться с точки для поддоменов
                    string domain = cookie.domain;
                    if (!string.IsNullOrEmpty(domain))
                    {
                        if (!domain.StartsWith('.'))
                            domain = "." + domain;
                    }
                    else
                    {
                        continue; // без домена кука бесполезна
                    }

                    // flag: TRUE если кука для всех поддоменов
                    // В Chrome куки всегда для всех поддоменов если domain начинается с точки
                    string flag = domain.StartsWith('.') ? "TRUE" : "FALSE";

                    // path: если нет, ставим корень
                    string path = string.IsNullOrEmpty(cookie.path) ? "/" : cookie.path;

                    // secure
                    string secure = cookie.secure ? "TRUE" : "FALSE";

                    // expiration: Unix timestamp
                    long expiry = 0;
                    if (cookie.expires > 0)
                    {
                        // Chrome отдает в секундах
                        expiry = (long)cookie.expires;

                        // Если timestamp в прошлом или совсем маленький - похуй, оставляем
                        if (expiry < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            // Просроченные куки тоже могут быть нужны для yt-dlp?
                            // Оставляем как есть, yt-dlp сам разберется
                        }
                    }

                    // Экранируем спецсимволы
                    string name = EscapeField(cookie.name);
                    string value = EscapeField(cookie.value);

                    // Формируем строку: domain\tflag\tpath\tsecure\texpiry\tname\tvalue
                    string line = $"{domain}\t{flag}\t{path}\t{secure}\t{expiry}\t{name}\t{value}";
                    writer.WriteLine(line);
                }

                writer.WriteLine();
                writer.WriteLine("# Для использования:");
                writer.WriteLine($"# yt-dlp --cookies {outputFile} <URL>");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Ошибка: {ex.Message}");
            _logger.LogError($"Стек: {ex.StackTrace}");
        }
    }

    ////static string ExtractDomainFromOrigin(string origin)
    ////{
    ////    if (string.IsNullOrEmpty(origin)) return null;

    ////    try
    ////    {
    ////        var uri = new Uri(origin);
    ////        return uri.Host;
    ////    }
    ////    catch
    ////    {
    ////        return null;
    ////    }
    ////}

    static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Заменяем проблемные символы
        return value
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\0", string.Empty); // нулевые байты нахуй
    }
}
