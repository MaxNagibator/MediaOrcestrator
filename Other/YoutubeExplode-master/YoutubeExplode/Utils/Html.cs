using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace YoutubeExplode.Utils;

internal static class Html
{
    public static IHtmlDocument Parse(string source) => new HtmlParser().ParseDocument(source);
}
