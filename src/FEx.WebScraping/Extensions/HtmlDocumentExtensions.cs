using HtmlAgilityPack;
using System;
using System.IO;
// ReSharper disable once RedundantUsingDirective - needed on the TFMs without implicit usings
using System.Text;

namespace FEx.WebScraping.Extensions;

public static class HtmlDocumentExtensions
{
    public static string SaveToFile(this HtmlDocument doc, string? path)
    {
        if (path is not null)
        {
            File.WriteAllText(path, doc.DocumentNode.OuterHtml);

            return path;
        }

        // GetTempFileName() creates the file on disk; appending an extension would orphan it.
        path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.html");

#if NET7_0_OR_GREATER
        if (!OperatingSystem.IsWindows())
        {
            // The shared temp directory is world-readable: keep scraped pages private to the current user.
            using var stream = new FileStream(path,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
                });
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(doc.DocumentNode.OuterHtml);

            return path;
        }
#endif
        File.WriteAllText(path, doc.DocumentNode.OuterHtml);

        return path;
    }

    public static string SaveToFile(this HtmlDocument doc) => doc.SaveToFile(null);
}
