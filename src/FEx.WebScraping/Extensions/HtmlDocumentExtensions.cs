using HtmlAgilityPack;
using System;
using System.IO;

namespace FEx.WebScraping.Extensions;

public static class HtmlDocumentExtensions
{
    public static string SaveToFile(this HtmlDocument doc, string? path)
    {
        // GetTempFileName() creates the file on disk; appending an extension would orphan it.
        path ??= Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.html");

        File.WriteAllText(path, doc.DocumentNode.OuterHtml);

        return path;
    }

    public static string SaveToFile(this HtmlDocument doc) => doc.SaveToFile(null);
}