using FEx.Imaging.Windows.Model;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

public sealed class IndexEntryTests : ImagingTestBase
{
    private const string Url = "https://images.test/photos/cat.png";

    [Fact]
    public void GetFileName_IsTheMd5OfTheUri_AndNullForNothing()
    {
        IndexEntry.GetFileName(new Uri(Url)).ShouldBe(IndexEntry.GetFileName(Url));
        IndexEntry.GetFileName(Url).ShouldNotBeNullOrWhiteSpace();
        IndexEntry.GetFileName(Url).ShouldNotBe(IndexEntry.GetFileName("https://images.test/photos/dog.png"));
        IndexEntry.GetFileName((Uri?)null).ShouldBeNull();
        IndexEntry.GetFileName((string?)null).ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithConfig_DerivesUrlFileNameAndCachedImage()
    {
        var config = CreateConfig();

        using var sut = new IndexEntry(Url, config);

        sut.Config.ShouldBeSameAs(config);
        sut.FilesCacheRootDirPath.ShouldBe(config.FilesCacheDirPath);
        sut.AbsoluteUri.ShouldBe(Url);
        sut.Url.ShouldBe(new Uri(Url));
        sut.FileName.ShouldBe(IndexEntry.GetFileName(Url));
        sut.CachedImage.ShouldNotBeNull();
        sut.FilePath.ShouldBeNull();
        sut.DoesCacheExists().ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithoutConfig_HasNoCachedImage()
    {
        using var sut = new IndexEntry
        {
            AbsoluteUri = Url
        };

        sut.Url.ShouldBe(new Uri(Url));
        sut.CachedImage.ShouldBeNull();
    }

    [Fact]
    public void AbsoluteUri_Cleared_ResetsTheDerivedState()
    {
        using var sut = new IndexEntry(Url, CreateConfig());

        sut.AbsoluteUri = string.Empty;

        sut.Url.ShouldBeNull();
        sut.FileName.ShouldBeNull();
        sut.CachedImage.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithAnExistingFile_ReadsItsMetadata()
    {
        var png = Png(4, 6);
        var path = WriteFile("cat.png", png);

        using var sut = new IndexEntry(Url, CreateConfig(), "cat.png");

        sut.FilePath.ShouldBe(path);
        sut.Cache.ShouldNotBeNull().FullName.ShouldBe(path);
        sut.Extension.ShouldBe(".png");
        sut.FileName.ShouldBe("cat");
        sut.PixelWidth.ShouldBe(4);
        sut.PixelHeight.ShouldBe(6);
        sut.CheckSum.ShouldNotBeNullOrWhiteSpace();
        sut.LocalUri.ShouldBe(new Uri($"file:///{path}", UriKind.Absolute));
        sut.DoesCacheExists().ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithAMissingFile_DropsThePath()
    {
        using var sut = new IndexEntry(Url, CreateConfig(), "missing.png");

        sut.FilePath.ShouldBeNull();
        sut.Cache.ShouldBeNull();
        sut.Extension.ShouldBeNull();
        sut.LocalUri.ShouldBeNull();
        sut.DoesCacheExists().ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithAFileThatIsNotAnImage_KeepsTheEntryWithoutPixelSize()
    {
        WriteFile("notes.txt", [1, 2, 3, 4, 5]);

        using var sut = new IndexEntry(Url, CreateConfig(), "notes.txt");

        sut.DoesCacheExists().ShouldBeTrue();
        sut.PixelWidth.ShouldBe(0);
        sut.PixelHeight.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithASvgFile_DoesNotDecodeItsSize()
    {
        WriteFile("vector.svg", System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"));

        using var sut = new IndexEntry(Url, CreateConfig(), "vector.svg");

        sut.Extension.ShouldBe(".svg");
        sut.PixelWidth.ShouldBe(0);
    }

    [Fact]
    public void Config_MovedToAnotherFolder_RebasesTheFilePath()
    {
        var folderA = Path.Combine(Dir, "a");
        var folderB = Path.Combine(Dir, "b");
        WriteFile("cat.png", Png(2, 2), folderA);
        var pathB = WriteFile("cat.png", Png(2, 2), folderB);
        using var sut = new IndexEntry(Url, CreateConfig(folderA), "cat.png");
        var configB = CreateConfig(folderB);

        sut.Config = configB;

        sut.FilesCacheRootDirPath.ShouldBe(configB.FilesCacheDirPath);
        sut.FilePath.ShouldBe(pathB);
        sut.Cache.ShouldNotBeNull().FullName.ShouldBe(pathB);
    }

    [Fact]
    public void Config_Cleared_ClearsTheRootFolderAndTheCachedImage()
    {
        using var sut = new IndexEntry(Url, CreateConfig());

        sut.Config = null;

        sut.FilesCacheRootDirPath.ShouldBeNull();
        sut.CachedImage.ShouldBeNull();
    }

    [Fact]
    public void PropertyChanged_IsRaisedForChangedValuesOnly()
    {
        using var sut = new IndexEntry();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.PixelWidth = 5;
        sut.PixelWidth = 5;

        raised.Count(x => x == nameof(IndexEntry.PixelWidth)).ShouldBe(1);
    }

    [Fact]
    public void IsEqual_ComparesUriChecksumPathAndLength()
    {
        var first = new IndexEntryBase
        {
            AbsoluteUri = Url,
            CheckSum = "abc",
            FilePath = "x",
            ResponseContentLength = 10
        };
        var same = new IndexEntryBase
        {
            AbsoluteUri = Url,
            CheckSum = "abc",
            FilePath = "x",
            ResponseContentLength = 10
        };
        var otherLength = new IndexEntryBase
        {
            AbsoluteUri = Url,
            CheckSum = "abc",
            FilePath = "x",
            ResponseContentLength = 11
        };
        var otherChecksum = new IndexEntryBase
        {
            AbsoluteUri = Url,
            CheckSum = "def",
            FilePath = "x",
            ResponseContentLength = 10
        };

        first.IsEqual(same).ShouldBeTrue();
        first.Equals(same).ShouldBeTrue();
        first.Equals(otherLength).ShouldBeFalse();
        first.Equals(otherChecksum).ShouldBeFalse();
        first.Equals((IndexEntryBase?)null).ShouldBeFalse();
    }

    [Fact]
    public void Dispose_ReleasesTheEntry_AndCanBeRepeated()
    {
        var sut = new IndexEntry(Url, CreateConfig());

        Should.NotThrow(() =>
        {
            sut.Dispose();
            sut.Dispose();
        });
    }
}
