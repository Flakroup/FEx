using HtmlAgilityPack;
using System.IO;

namespace FEx.WebScraping.Extensions;

public static class HtmlDocumentExtensions
{
    public static string SaveToFile(this HtmlDocument doc, string? path)
    {
        path ??= $"{Path.GetTempFileName()}.html";

        File.WriteAllText(path, doc.DocumentNode.OuterHtml);

        return path;
    }

    public static string SaveToFile(this HtmlDocument doc) => doc.SaveToFile(null);
}