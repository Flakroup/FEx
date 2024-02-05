using HtmlAgilityPack;
using System.IO;

namespace FEx.WebScraping.Extensions;

public static class HtmlDocumentExtensions
{
    public static string SaveToFile(this HtmlDocument doc, string path = null)
    {
        path ??= $"{Path.GetTempFileName()}.html";

        File.WriteAllText(path, doc.DocumentNode.OuterHtml);

        return path;
    }
}