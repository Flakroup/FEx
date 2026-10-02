using FEx.WebScraping.Extensions;
using HtmlAgilityPack;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace FEx.WebScraping.Tests;

public sealed class HtmlDocumentExtensionsTests
{
    [Fact]
    public void SaveToFile_WithoutPath_WritesHtmlAndLeavesNoOrphanTempFile()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<html><body>hi</body></html>");

        var path = doc.SaveToFile();

        try
        {
            File.ReadAllText(path).ShouldContain("hi");
            Path.GetExtension(path).ShouldBe(".html");

            // Path.GetTempFileName() would have created this extension-less zero-byte sibling.
            File.Exists(Path.ChangeExtension(path, null)).ShouldBeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveToFile_WithoutPath_OnUnix_IsReadableOnlyByOwner()
    {
        if (OperatingSystem.IsWindows())
            return; // %TEMP% is per-user on Windows

        var doc = new HtmlDocument();
        doc.LoadHtml("<html><body>secret</body></html>");

        var path = doc.SaveToFile();

        try
        {
            File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
