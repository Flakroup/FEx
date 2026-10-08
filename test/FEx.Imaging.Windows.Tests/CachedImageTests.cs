using FEx.Imaging.Windows.Model;
using FEx.MVVM.Abstractions;
using Shouldly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

public sealed class CachedImageTests : ImagingTestBase
{
    private static readonly string Url = ImageUrl.AbsoluteUri;

    private static CachedImage ImageOf(IndexEntry entry) => entry.CachedImage.ShouldNotBeNull();

    /// <summary>An entry whose cache file already exists and whose recorded length matches it.</summary>
    private IndexEntry ValidEntry(int width = 4, int height = 6, TimeSpan? validPeriod = null)
    {
        var path = WriteFile(FileNameOf(ImageUrl) + ".png", Png(width, height));
        return new(Url, CreateConfig(validPeriod: validPeriod), Path.GetFileName(path))
        {
            ResponseContentLength = new FileInfo(path).Length
        };
    }

    [Fact]
    public void CacheIsInvalid_NoFile_IsTrue()
    {
        using var entry = new IndexEntry(Url, CreateConfig());

        ImageOf(entry).CacheIsInvalid().ShouldBeTrue();
    }

    [Fact]
    public void CacheIsInvalid_FileMatchingTheRecordedLength_IsFalse()
    {
        using var entry = ValidEntry();

        ImageOf(entry).CacheIsInvalid().ShouldBeFalse();
    }

    [Fact]
    public void CacheIsInvalid_RefreshRequested_IsTrueEvenForAValidFile()
    {
        using var entry = ValidEntry();

        ImageOf(entry).CacheIsInvalid(true).ShouldBeTrue();
    }

    [Fact]
    public void CacheIsInvalid_RecordedLengthDiffers_IsTrue()
    {
        using var entry = ValidEntry();
        entry.ResponseContentLength += 1;

        ImageOf(entry).CacheIsInvalid().ShouldBeTrue();
    }

    [Fact]
    public void CacheIsInvalid_FileOlderThanTheValidPeriod_IsTrue()
    {
        using var entry = ValidEntry(validPeriod: TimeSpan.FromHours(1));
        File.SetLastWriteTime(entry.FilePath, DateTime.Now.AddDays(-1));

        ImageOf(entry).CacheIsInvalid().ShouldBeTrue();
    }

    [Fact]
    public void CacheIsInvalid_FileYoungerThanTheValidPeriod_IsFalse()
    {
        using var entry = ValidEntry(validPeriod: TimeSpan.FromDays(1));

        ImageOf(entry).CacheIsInvalid().ShouldBeFalse();
    }

    [Fact]
    public async Task GetImageAsync_ValidCacheFile_LoadsTheImageOnceAndReusesIt()
    {
        using var entry = ValidEntry();
        var sut = ImageOf(entry);

        var first = await sut.GetImageAsync();
        var second = await sut.GetImageAsync();

        first.ShouldNotBeNull();
        first.PixelWidth.ShouldBe(4);
        first.PixelHeight.ShouldBe(6);
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task GetImageAsync_DifferentSizes_AreCachedSeparately()
    {
        using var entry = ValidEntry(8, 8);
        var sut = ImageOf(entry);

        var original = await sut.GetImageAsync();
        var small = await sut.GetImageAsync(new(2, 2));
        var smallAgain = await sut.GetImageAsync(new(2, 2));

        small.ShouldNotBeNull();
        small.ShouldNotBeSameAs(original);
        small.PixelWidth.ShouldBeLessThanOrEqualTo(2);
        smallAgain.ShouldBeSameAs(small);
    }

    [Fact]
    public async Task OnParentConfigurationChange_DropsTheLoadedImages()
    {
        using var entry = ValidEntry();
        var sut = ImageOf(entry);
        var before = await sut.GetImageAsync();

        sut.OnParentConfigurationChange();
        var after = await sut.GetImageAsync();

        after.ShouldNotBeNull();
        after.ShouldNotBeSameAs(before);
    }

    [Fact]
    public async Task GetImageAsync_NothingCached_DownloadsFromTheSuppliedResponse()
    {
        var png = Png(3, 5);
        using var entry = new IndexEntry(Url, CreateConfig());
        var sut = ImageOf(entry);
        using var response = PngResponse(ImageUrl, png);

        var image = await sut.GetImageAsync(response: response);

        image.ShouldNotBeNull();
        image.PixelWidth.ShouldBe(3);
        image.PixelHeight.ShouldBe(5);
        ReadFile(Path.Combine(Dir, FileNameOf(ImageUrl) + ".png")).ShouldBe(png);
        entry.DoesCacheExists().ShouldBeTrue();
        entry.ResponseContentLength.ShouldBe(png.Length);
    }

    [Fact]
    public async Task PrepareCacheAsync_NothingCached_DownloadsAndRecordsTheFile()
    {
        var png = Png(2, 2);
        using var entry = new IndexEntry(Url, CreateConfig());
        using var response = PngResponse(ImageUrl, png);

        var downloaded = await ImageOf(entry).PrepareCacheAsync(response: response);

        downloaded.ShouldBeTrue();
        entry.Extension.ShouldBe(".png");
        entry.IsDownloading.ShouldBeFalse();
        entry.PixelWidth.ShouldBe(2);
        entry.CheckSum.ShouldNotBeNullOrWhiteSpace();
        ImageOf(entry).CacheIsInvalid().ShouldBeFalse();
    }

    [Fact]
    public async Task PrepareCacheAsync_UrlModifier_IsAskedOnceEvenWithASuppliedResponse()
    {
        using var entry = new IndexEntry(Url, CreateConfig());
        using var response = PngResponse(ImageUrl, Png(2, 2));
        var modifierCalls = 0;

        var downloaded = await ImageOf(entry).PrepareCacheAsync(response: response,
            urlModifier: u =>
            {
                modifierCalls++;

                return u;
            });

        downloaded.ShouldBeTrue();
        modifierCalls.ShouldBe(1);
    }

    [Fact]
    public async Task PrepareCacheAsync_CacheStillValid_DoesNothing()
    {
        using var entry = ValidEntry();
        using var response = PngResponse(ImageUrl, Png(9, 9));

        var downloaded = await ImageOf(entry).PrepareCacheAsync(response: response);

        downloaded.ShouldBeFalse();
        entry.PixelWidth.ShouldBe(4);
    }

    [Fact]
    public async Task PrepareCacheAsync_RefreshRequested_ReplacesAValidFile()
    {
        using var entry = ValidEntry();
        var replacement = Png(7, 7);
        using var response = PngResponse(ImageUrl, replacement);

        var downloaded = await ImageOf(entry).PrepareCacheAsync(refresh: true, response: response);

        downloaded.ShouldBeTrue();
        ReadFile(entry.FilePath).ShouldBe(replacement);
        entry.PixelWidth.ShouldBe(7);
    }

    [Fact]
    public async Task PrepareCacheAsync_CancelledWhileWaiting_Throws()
    {
        using var entry = new IndexEntry(Url, CreateConfig());
        using var cts = CancelledSource();
        using var sut = new CachedImage(entry, cts);
        using var response = PngResponse(ImageUrl, Png(2, 2));

        var error = await Record.ExceptionAsync(() => sut.PrepareCacheAsync(response: response));

        error.ShouldBeAssignableTo<OperationCanceledException>();
        entry.IsDownloading.ShouldBeFalse();
    }

    [Fact]
    public void RemoveImageUpdate_ForAnUnknownSize_DoesNotThrow()
    {
        using var entry = ValidEntry();

        Should.NotThrow(() => ImageOf(entry).RemoveImageUpdate(new WidthAndHeight(1, 1)));
        Should.NotThrow(() => ImageOf(entry).RemoveImageUpdate());
    }

    [Fact]
    public void Dispose_SuppliedTokenSource_IsKept_AndDisposeCanBeRepeated()
    {
        using var entry = new IndexEntry(Url, CreateConfig());
        using var supplied = new CancellationTokenSource();
        var sut = new CachedImage(entry, supplied);

        Should.NotThrow(() =>
        {
            sut.Dispose();
            sut.Dispose();
        });
        Should.NotThrow(() => _ = supplied.Token);
    }
}
