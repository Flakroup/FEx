using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Services;
using FEx.Imaging.Windows.Model;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

/// <summary>
/// The service and its index over a real SQLite database file and a real temp folder. Like in production, every
/// context opens its own connection to the file: the index saves in the background while a test goes on, and
/// the two must not share a connection or a transaction.
/// </summary>
public sealed class FilesCacheServiceTests : ImagingTestBase, IAsyncDisposable
{
    // Never cached, so reloading it changes nothing.
    private const string FlushKey = "https://images.test/flush";

    // Beside the cache folder, not in it: the service deletes every file in its folder the index does not know.
    private readonly string _dbPath;
    private readonly ConcurrentQueue<string> _errors = new();
    private readonly TestDbService _dbService;
    private readonly List<IDisposable> _disposables = [];
    private readonly List<Func<Task>> _flushes = [];

    public FilesCacheServiceTests()
    {
        _dbPath = Dir + ".db";
        var options = CreateOptions();

        using (var setup = new FilesCacheContext(options))
            setup.Database.Migrate();

        var services = new ServiceCollection();
        services.AddScoped(_ => new FilesCacheContext(options));
        var provider = services.BuildServiceProvider();
        _disposables.Add(provider);
        _dbService = new(new ScopeProvider(provider));
        FExStaticLogger.ErrorLogged += OnErrorLogged;
    }

    /// <summary>
    /// Saves every change the indexes still hold or are saving, checks that nothing logged an error, then tears down.
    /// The background save outlives the test body: tearing the database down under it makes it fail, and a failure
    /// there is only logged.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var flush in _flushes)
        {
            try
            {
                await flush();
            }
            catch (ObjectDisposedException)
            {
                // the test disposed the index itself, which drops what it still held
            }
        }

        FExStaticLogger.ErrorLogged -= OnErrorLogged;

        foreach (var disposable in Enumerable.Reverse(_disposables))
            disposable.Dispose();

#if NET
        SqliteConnection.ClearAllPools();
#endif
        DeleteDatabase();

        _errors.ShouldBeEmpty();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _dbService.Dispose();

        base.Dispose(disposing);
    }

    private void OnErrorLogged(object? sender, FExErrorEventArgs e) =>
        _errors.Enqueue(e.Exception?.ToString() ?? e.Message ?? string.Empty);

    private DbContextOptions<FilesCacheContext> CreateOptions() =>
        new DbContextOptionsBuilder<FilesCacheContext>().UseSqlite($"Data Source={_dbPath}").Options;

    private void DeleteDatabase()
    {
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // still held for a moment; the file lives under the temp path anyway
        }
        catch (UnauthorizedAccessException)
        {
            // same as above
        }
    }

    // The index has to be saved before the database goes away, whoever disposes it: see DisposeAsync.
    private void FlushOnTearDown(IndexEntriesCache index) => _flushes.Add(() => index.ReloadAsync(FlushKey));

    private FilesCacheService CreateSut(FilesCacheServiceConfig? config = null)
    {
        config ??= CreateConfig();
        var sut = new FilesCacheService(config,
            new(_dbService, config),
            Locks);
        _disposables.Add(sut);
        FlushOnTearDown(sut.FilesCacheIndex);

        return sut;
    }

    private static async Task<FilesCacheService> InitializedAsync(FilesCacheService sut)
    {
        await sut.InitializeAsync();

        return sut;
    }

    [Fact]
    public async Task Constructor_ExposesTheConfigAndCreatesTheFolder()
    {
        var dir = Path.Combine(Dir, "created");
        var config = CreateConfig(dir);

        var sut = await InitializedAsync(CreateSut(config));

        sut.Config.ShouldBeSameAs(config);
        sut.FilesCacheDir.FullName.ShouldBe(Path.GetFullPath(dir));
        sut.UseHttpClientService.ShouldBeFalse();
        Directory.Exists(dir).ShouldBeTrue();
    }

    [Fact]
    public async Task Initialization_RemovesFilesTheIndexDoesNotKnow()
    {
        var orphan = WriteFile("orphan.png", Png(2, 2));

        await InitializedAsync(CreateSut());

        File.Exists(orphan).ShouldBeFalse();
    }

    [Fact]
    public async Task Initialization_CacheAll_LoadsTheIndexAndOptimizes()
    {
        var orphan = WriteFile("orphan.png", Png(2, 2));

        var sut = await InitializedAsync(CreateSut(CreateConfig(cacheAll: true)));

        File.Exists(orphan).ShouldBeFalse();
        sut.FilesCacheIndex.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetEntryAsync_NewUrl_AddsAnEntryOnce()
    {
        var sut = await InitializedAsync(CreateSut());

        var first = Borrow(await sut.GetEntryAsync(ImageUrl));
        var second = Borrow(await sut.GetEntryAsync(ImageUrl));

        first.AbsoluteUri.ShouldBe(ImageUrl.AbsoluteUri);
        first.Config.ShouldBeSameAs(sut.Config);
        second.ShouldBeSameAs(first);
        (await sut.ContainsEntryAsync(ImageUrl)).ShouldBeTrue();
        (await sut.GetEntryBaseAsync(ImageUrl)).ShouldBeSameAs(first);
    }

    [Fact]
    public async Task GetEntryAsync_UnknownUrlWithoutAddNew_IsNull()
    {
        var sut = await InitializedAsync(CreateSut());

        var entry = Borrow(await sut.GetEntryAsync(ImageUrl, false));

        entry.ShouldBeNull();
        sut.FilesCacheIndex.Count.ShouldBe(0);
    }

    [Fact]
    public async Task IsFilePresentAsync_And_IsFileBeingDownloadedAsync_UnknownUrl_AreFalse()
    {
        var sut = await InitializedAsync(CreateSut());

        (await sut.IsFilePresentAsync(ImageUrl)).ShouldBeFalse();
        (await sut.IsFileBeingDownloadedAsync(ImageUrl)).ShouldBeFalse();
    }

    [Fact]
    public async Task PrepareCacheAsync_DownloadsFromTheSuppliedResponse()
    {
        var sut = await InitializedAsync(CreateSut());
        var png = Png(3, 3);
        using var response = PngResponse(ImageUrl, png);

        var prepared = await sut.PrepareCacheAsync(ImageUrl, response: response);

        prepared.ShouldBeTrue();
        (await sut.IsFilePresentAsync(ImageUrl)).ShouldBeTrue();
        (await sut.IsFileBeingDownloadedAsync(ImageUrl)).ShouldBeFalse();
        ReadFile(Path.Combine(Dir, FileNameOf(ImageUrl) + ".png")).ShouldBe(png);
    }

    [Fact]
    public async Task PrepareAndGetFileLocalUriAsync_ReturnsTheFileUriOfTheDownloadedImage()
    {
        var sut = await InitializedAsync(CreateSut());
        using var response = PngResponse(ImageUrl, Png(3, 3));

        var uri = await sut.PrepareAndGetFileLocalUriAsync(ImageUrl, response: response);

        var expected = Path.Combine(Dir, FileNameOf(ImageUrl) + ".png");
        uri.ShouldNotBeNull().IsFile.ShouldBeTrue();
        uri.LocalPath.ShouldBe(expected);
    }

    [Fact]
    public async Task PrepareCacheAndGetEntryAsync_SecondCallWithAValidFile_DoesNotDownloadAgain()
    {
        var sut = await InitializedAsync(CreateSut());
        using var first = PngResponse(ImageUrl, Png(3, 3));
        await sut.PrepareCacheAndGetEntryAsync(ImageUrl, response: first);
        using var second = PngResponse(ImageUrl, Png(9, 9));

        var entry = Borrow(await sut.PrepareCacheAndGetEntryAsync(ImageUrl, response: second));

        entry.PixelWidth.ShouldBe(3);
        sut.EntryCacheShouldBePrepared(entry, false).ShouldBeFalse();
        sut.EntryCacheShouldBePrepared(entry, true).ShouldBeTrue();
        sut.EntryCacheShouldBePrepared((IIndexEntryBase)entry, true).ShouldBeTrue();
    }

    [Fact]
    public async Task PrepareCacheEntryAsync_ThroughTheBaseInterface_Downloads()
    {
        var sut = await InitializedAsync(CreateSut());
        IIndexEntryBase entry = Borrow(await sut.GetEntryAsync(ImageUrl));
        using var response = PngResponse(ImageUrl, Png(2, 2));

        var prepared = await sut.PrepareCacheEntryAsync(entry, response: response);

        prepared.ShouldBeTrue();
    }

    [Fact]
    public async Task GetImageAsync_DownloadsAndDecodesTheImage()
    {
        var sut = await InitializedAsync(CreateSut());
        using var response = PngResponse(ImageUrl, Png(5, 7));

        var image = await sut.GetImageAsync(ImageUrl, response: response);

        image.ShouldNotBeNull().PixelWidth.ShouldBe(5);
        image.PixelHeight.ShouldBe(7);
    }

    [Fact]
    public async Task RemoveImageUpdateAsync_ForAKnownUrl_DoesNotThrow()
    {
        var sut = await InitializedAsync(CreateSut());

        await Should.NotThrowAsync(() => sut.RemoveImageUpdateAsync(ImageUrl));
        await Should.NotThrowAsync(() => sut.RemoveImageUpdateAsync(ImageUrl, new(2, 2)));
    }

    [Fact]
    public async Task FileInfoAccessors_UnindexedFileOnDisk_AreReadFromTheFolder()
    {
        var sut = await InitializedAsync(CreateSut());
        var png = Png(2, 2);
        var path = WriteFile(FileNameOf(ImageUrl) + ".png", png);

        sut.GetFileSize(ImageUrl).ShouldBe(png.Length);
        sut.GetFileName(ImageUrl).ShouldBe(Path.GetFileName(path));
        sut.GetFileChecksum(ImageUrl).ShouldBe(FileSystemHelper.GenerateMd5OfFile(path));
    }

    [Fact]
    public async Task FileInfoAccessors_UnknownFile_AreEmpty()
    {
        var sut = await InitializedAsync(CreateSut());

        sut.GetFileSize(ImageUrl).ShouldBe(0);
        sut.GetFileName(ImageUrl).ShouldBeNull();
        sut.GetFileChecksum(ImageUrl).ShouldBeNull();
    }

    [Fact]
    public async Task FileInfoAccessors_IndexedEntryWithItsFile_AreReadThroughTheEntry()
    {
        var sut = await InitializedAsync(CreateSut());
        var png = Png(2, 2);
        using var response = PngResponse(ImageUrl, png);
        await sut.PrepareCacheAsync(ImageUrl, response: response);

        sut.GetFileSize(ImageUrl).ShouldBe(png.Length);
        sut.GetFileName(ImageUrl).ShouldBe(FileNameOf(ImageUrl) + ".png");
    }

    [Fact]
    public async Task FileInfoAccessors_IndexedEntryWhoseFileIsGone_FallBackToTheFolderScan()
    {
        var sut = await InitializedAsync(CreateSut());
        await sut.GetEntryAsync(ImageUrl);

        sut.GetFileSize(ImageUrl).ShouldBe(0);
        sut.GetFileName(ImageUrl).ShouldBeNull();
    }

    [Fact]
    public async Task SetDefaultImageAsync_WritesTheImageOnce_UnlessForced()
    {
        var sut = await InitializedAsync(CreateSut());
        using var first = new System.Drawing.Bitmap(4, 4);
        using var second = new System.Drawing.Bitmap(8, 8);
        var path = Path.Combine(Dir, FileNameOf(ImageUrl) + ".png");

        (await sut.SetDefaultImageAsync(ImageUrl, first, ImageFormat.Png, "png")).ShouldBeTrue();
        var firstBytes = ReadFile(path);
        (await sut.SetDefaultImageAsync(ImageUrl, second, ImageFormat.Png, "png")).ShouldBeFalse();
        ReadFile(path).ShouldBe(firstBytes);
        (await sut.SetDefaultImageAsync(ImageUrl, second, ImageFormat.Png, "png", true)).ShouldBeTrue();

        ReadFile(path).ShouldNotBe(firstBytes);
    }

    [Fact]
    public async Task RemoveNotPresentFileUrlsAsync_DropsEntriesOutsideTheGivenSet()
    {
        var sut = await InitializedAsync(CreateSut());
        var keep = new Uri("https://images.test/keep.png");
        var drop = new Uri("https://images.test/drop.png");
        await sut.GetEntryAsync(keep);
        await sut.GetEntryAsync(drop);

        await sut.RemoveNotPresentFileUrlsAsync([keep.AbsoluteUri]);

        sut.FilesCacheIndex.Select(x => x.AbsoluteUri).ShouldBe([keep.AbsoluteUri]);
    }

    [Fact]
    public async Task RemoveNotPresentFileUrlsAsync_EmptySet_KeepsEverything()
    {
        var sut = await InitializedAsync(CreateSut());
        await sut.GetEntryAsync(ImageUrl);

        await sut.RemoveNotPresentFileUrlsAsync([]);

        sut.FilesCacheIndex.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveIndexEntriesAsync_DeletesTheFilesOfTheGivenEntries()
    {
        var sut = await InitializedAsync(CreateSut());
        using var response = PngResponse(ImageUrl, Png(2, 2));
        await sut.PrepareCacheAsync(ImageUrl, response: response);
        var path = Path.Combine(Dir, FileNameOf(ImageUrl) + ".png");
        File.Exists(path).ShouldBeTrue();

        await sut.RemoveIndexEntriesAsync(ImageUrl.AbsoluteUri);

        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public async Task OptimizeCacheAsync_RemovesUnindexedFilesAndEntriesWithoutFiles()
    {
        var sut = await InitializedAsync(CreateSut());
        var orphan = WriteFile("orphan.png", Png(2, 2));
        await sut.GetEntryAsync(ImageUrl);

        var result = await sut.OptimizeCacheAsync();

        result.IsSuccess.ShouldBeTrue();
        File.Exists(orphan).ShouldBeFalse();
        sut.FilesCacheIndex.Count.ShouldBe(0);
    }

    [Fact]
    public async Task OptimizeCacheAsync_KeepsEntriesWhoseFileIsPresent()
    {
        var sut = await InitializedAsync(CreateSut());
        using var response = PngResponse(ImageUrl, Png(2, 2));
        await sut.PrepareCacheAsync(ImageUrl, response: response);

        var result = await sut.OptimizeCacheAsync();

        result.IsSuccess.ShouldBeTrue();
        (await sut.IsFilePresentAsync(ImageUrl)).ShouldBeTrue();
        sut.FilesCacheIndex.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetLazyImagesHandler_IsBoundToTheService()
    {
        var sut = await InitializedAsync(CreateSut());

        sut.GetLazyImagesHandler().Cache.ShouldBeSameAs(sut);
    }

    [Fact]
    public async Task Dispose_CanBeRepeated()
    {
        var sut = await InitializedAsync(CreateSut());

        Should.NotThrow(() =>
        {
            sut.Dispose();
            sut.Dispose();
        });
    }

    [Fact]
    public async Task IndexEntriesCache_GetOrAddValueAsync_UsesTheGivenFileName()
    {
        var config = CreateConfig();
        var png = WriteFile("given.png", Png(2, 2));
        var sut = new IndexEntriesCache(_dbService, config);
        _disposables.Add(sut);
        FlushOnTearDown(sut);
        await sut.InitializeAsync();

        var entry = Borrow(await sut.GetOrAddValueAsync(ImageUrl.AbsoluteUri, true, "given.png"));
        var missing = Borrow(await sut.GetOrAddValueAsync("https://images.test/other.png", false));

        sut.Config.ShouldBeSameAs(config);
        entry.FilePath.ShouldBe(png);
        missing.ShouldBeNull();
    }

    [Fact]
    public async Task IndexEntriesCache_RemoveIndexEntriesOfMissingFilesAsync_DeletesUnreferencedFiles()
    {
        var sut = new IndexEntriesCache(_dbService, CreateConfig());
        _disposables.Add(sut);
        FlushOnTearDown(sut);
        await sut.InitializeAsync();
        var stray = WriteFile("stray.png", Png(2, 2));

        var removed = await sut.RemoveIndexEntriesOfMissingFilesAsync([stray]);

        removed.ShouldBe(0);
        File.Exists(stray).ShouldBeFalse();
    }

    [Fact]
    public async Task IndexEntriesCache_RemoveIndexEntriesAsync_EmptyDictionary_IsANoOp()
    {
        var sut = new IndexEntriesCache(_dbService, CreateConfig());
        _disposables.Add(sut);
        FlushOnTearDown(sut);
        await sut.InitializeAsync();

        await Should.NotThrowAsync(() => sut.RemoveIndexEntriesAsync(new Dictionary<string, FileInfo>()));
    }

    [Fact]
    public async Task GetEntryAsync_NewUrl_IsSavedByTheBackgroundSave()
    {
        var sut = await InitializedAsync(CreateSut());

        var entry = Borrow(await sut.GetEntryAsync(ImageUrl));
        await sut.FilesCacheIndex.ReloadAsync(FlushKey, TestContext.Current.CancellationToken);

        using var context = new FilesCacheContext(CreateOptions());
        context.IndexEntries.AsNoTracking().Select(x => x.AbsoluteUri).ShouldBe([entry.AbsoluteUri]);
    }

    [Fact]
    public async Task DbService_OverlappingOperations_DoNotShareATransaction()
    {
        var holding = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = _dbService.RunTaskInDbContextAsync(async context =>
        {
            var count = await context.IndexEntries.CountAsync(TestContext.Current.CancellationToken);
            holding.SetResult(true);
#pragma warning disable VSTHRD003 // completed by the test
            await release.Task;
#pragma warning restore VSTHRD003

            return count;
        });

        try
        {
            await Task.WhenAny(holding.Task, first);

            // On a connection of its own the second operation waits for the first one's lock or just reads; on a shared
            // connection it would try to begin a nested transaction and fail. Off the test thread: SQLite waits for a
            // lock synchronously, and the lock is released below.
            var second = Task.Run(() => _dbService.RunTaskInDbContextAsync(context =>
                context.IndexEntries.CountAsync(TestContext.Current.CancellationToken)));
            await Task.Delay(500, TestContext.Current.CancellationToken);
            release.SetResult(true);

            (await second).ShouldBe(0);
        }
        finally
        {
            release.TrySetResult(true);
#pragma warning disable VSTHRD003 // the task is the operation this test started
            await first;
#pragma warning restore VSTHRD003
        }
    }

    private sealed class ScopeProvider(IServiceProvider services) : IScopeProvider
    {
        public IServiceScope CreateScope() => services.CreateScope();
    }

    private sealed class TestDbService : DbServiceBase<FilesCacheContext>, IEFCoreDatabaseBackedService<FilesCacheContext>
    {
        public string? DbKey => null;

        public TestDbService(IScopeProvider scopeProvider)
            : base(scopeProvider, new(FExStaticLogger.Instance), Substitute.For<IFExDbConfig>(), [])
        {
        }

        // Skips the SQL server probe of the production service; the cache only needs the mapping snapshot.
        protected override Task OnInitializeAsync() => EnsureMappingSnapshotAsync();
    }
}
