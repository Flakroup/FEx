using FEx.Imaging.Windows.Model;
using FEx.MVVM.Abstractions;
using FEx.WPFx.Natives;
using Shouldly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

public sealed class SizedImageCacheTests : ImagingTestBase
{
    private IndexEntry EntryWithFile(byte[]? content = null)
    {
        var name = FileNameOf(ImageUrl) + ".png";
        WriteFile(name, content ?? Png(4, 6));

        return new(ImageUrl.AbsoluteUri, CreateConfig(), name);
    }

    private static SizedImageCache NewSut(IndexEntry entry,
                                          WidthAndHeight? size = null,
                                          CancellationTokenSource? cts = null) =>
        new(entry.CachedImage.ShouldNotBeNull(), size, null, cts);

    [Fact]
    public void Constructor_Defaults_UseTheDefaultSizeAndAnOwnedTokenSource()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);

        sut.ImageSize.ShouldBeSameAs(WidthAndHeight.Default);
        sut.CachedImage.ShouldBeNull();
        sut.IsLoadingImage.ShouldBeFalse();
        sut.ImageUpdateAction.ShouldBeNull();
        sut.LoadingSemaphore.CurrentCount.ShouldBe(1);
        sut.CancellationTokenSource.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public async Task Constructor_WithAnImage_StartsLoaded()
    {
        using var entry = EntryWithFile();
        var image = await CommonWindowsImaging.GetBitmapImageFromFileAsync(entry.FilePath);
        using var sut = new SizedImageCache(entry.CachedImage.ShouldNotBeNull(), new(3, 3), image);

        sut.CachedImage.ShouldBeSameAs(image);
        sut.ImageSize.Width.ShouldBe(3);
    }

    [Fact]
    public async Task ReloadImageFromCacheFileAsync_ExistingFile_LoadsTheImage()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);

        await sut.ReloadImageFromCacheFileAsync();

        var image = sut.CachedImage.ShouldNotBeNull();
        image.PixelWidth.ShouldBe(4);
        image.PixelHeight.ShouldBe(6);
        sut.IsLoadingImage.ShouldBeFalse();
        sut.LoadingSemaphore.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReloadImageFromCacheFileAsync_AlreadyLoaded_KeepsTheImageUnlessRefreshed()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);
        await sut.ReloadImageFromCacheFileAsync();
        var loaded = sut.CachedImage;

        await sut.ReloadImageFromCacheFileAsync();
        var kept = sut.CachedImage;
        await sut.ReloadImageFromCacheFileAsync(true);

        kept.ShouldBeSameAs(loaded);
        sut.CachedImage.ShouldNotBeNull().ShouldNotBeSameAs(loaded);
    }

    [Fact]
    public async Task ReloadImageFromCacheFileAsync_MissingFile_LeavesTheImageEmpty()
    {
        using var entry = new IndexEntry(ImageUrl.AbsoluteUri, CreateConfig());
        using var sut = NewSut(entry);

        await sut.ReloadImageFromCacheFileAsync();

        sut.CachedImage.ShouldBeNull();
        sut.IsLoadingImage.ShouldBeFalse();
        sut.LoadingSemaphore.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReloadImageFromCacheFileAsync_CorruptFile_DeletesItAndRethrows()
    {
        using var entry = EntryWithFile([1, 2, 3, 4, 5, 6, 7, 8]);
        var path = entry.FilePath;
        using var sut = NewSut(entry);

        var error = await Record.ExceptionAsync(() => sut.ReloadImageFromCacheFileAsync());

        error.ShouldNotBeNull();
        File.Exists(path).ShouldBeFalse();
        sut.CachedImage.ShouldBeNull();
        sut.IsLoadingImage.ShouldBeFalse();
        sut.LoadingSemaphore.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReloadImageFromCacheFileAsync_CancelledToken_Throws()
    {
        using var cts = CancelledSource();
        using var entry = EntryWithFile();
        using var sut = NewSut(entry, null, cts);

        var error = await Record.ExceptionAsync(() => sut.ReloadImageFromCacheFileAsync());

        error.ShouldBeAssignableTo<OperationCanceledException>();
        sut.IsLoadingImage.ShouldBeFalse();
    }

    [Fact(Timeout = 30_000)]
    public async Task ImageUpdateAction_AddedAfterTheImageLoaded_ReceivesTheImage()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);
        await sut.ReloadImageFromCacheFileAsync();
        var received = new TaskCompletionSource<BitmapImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = TestContext.Current.CancellationToken.Register(() => received.TrySetCanceled());

        sut.AddImageUpdateAction(received.SetResult);

        sut.ImageUpdateAction.ShouldNotBeNull();
        var image = await received.Task;
        image.ShouldBeSameAs(sut.CachedImage);
        await sut.ImageUpdateActionTask.ShouldNotBeNull();
    }

    [Fact(Timeout = 30_000)]
    public async Task ImageUpdateAction_AddedBeforeTheImageLoaded_ReceivesTheImageOnceItLoads()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);
        var received = new TaskCompletionSource<BitmapImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = TestContext.Current.CancellationToken.Register(() => received.TrySetCanceled());
        sut.AddImageUpdateAction(received.SetResult);

        await sut.ReloadImageFromCacheFileAsync();

        var image = await received.Task;
        image.ShouldBeSameAs(sut.CachedImage);
    }

    [Fact]
    public void ImageUpdateAction_WithoutAnImage_IsNotInvoked()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);
        var calls = 0;

        sut.AddImageUpdateAction(_ => calls++);

        sut.ImageUpdateActionTask.ShouldBeNull();
        calls.ShouldBe(0);
    }

    [Fact]
    public void RemoveImageUpdateAction_ClearsTheAction_AndIsHarmlessWhenNoneIsSet()
    {
        using var entry = EntryWithFile();
        using var sut = NewSut(entry);
        sut.AddImageUpdateAction(_ => { });

        sut.RemoveImageUpdateAction();
        sut.ImageUpdateAction.ShouldBeNull();

        Should.NotThrow(sut.RemoveImageUpdateAction);
    }

    [Fact]
    public void Dispose_OwnTokenSource_IsDisposedWithTheSemaphore()
    {
        using var entry = EntryWithFile();
        var sut = NewSut(entry);

        Should.NotThrow(sut.Dispose);

        Should.Throw<ObjectDisposedException>(() => _ = sut.CancellationTokenSource.Token);
        Should.Throw<ObjectDisposedException>(() => _ = sut.LoadingSemaphore.Wait(0));
    }

    [Fact]
    public void Dispose_SuppliedTokenSource_StaysUsable()
    {
        using var cts = new CancellationTokenSource();
        using var entry = EntryWithFile();
        var sut = NewSut(entry, null, cts);

        sut.Dispose();

        Should.NotThrow(() => _ = cts.Token);
    }
}
