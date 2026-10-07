using FEx.Agnostics.Abstractions.Helpers;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Services;
using FEx.Imaging.Windows.Model;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

/// <summary>The service and its index over a real (in-memory) SQLite database and a real temp folder.</summary>
public sealed class FilesCacheServiceTests : ImagingTestBase
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private readonly TestDbService _dbService;
    private readonly List<IDisposable> _disposables = [];

    public FilesCacheServiceTests()
    {
        _connection.Open();
        var builder = new DbContextOptionsBuilder<FilesCacheContext>().UseSqlite(_connection);
#if NET9_0_OR_GREATER
        // The shipped migrations (EF Core 2.2 era) lag the model snapshot; the tests run the schema as shipped.
        builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
#endif
        var options = builder.Options;

        using (var setup = new FilesCacheContext(options))
            setup.Database.Migrate();

        var services = new ServiceCollection();
        services.AddScoped(_ => new FilesCacheContext(options));
        _services = services.BuildServiceProvider();
        _dbService = new(new ScopeProvider(_services));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var disposable in Enumerable.Reverse(_disposables))
                disposable.Dispose();

            _dbService.Dispose();
            _services.Dispose();
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }

    private FilesCacheService CreateSut(FilesCacheServiceConfig? config = null)
    {
        config ??= CreateConfig();
        var sut = new FilesCacheService(config,
            new(_dbService, config),
            Locks);
        _disposables.Add(sut);

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
        await sut.InitializeAsync();

        await Should.NotThrowAsync(() => sut.RemoveIndexEntriesAsync(new Dictionary<string, FileInfo>()));
    }

    private sealed class ScopeProvider(IServiceProvider services) : IScopeProvider
    {
        public IServiceScope CreateScope() => services.CreateScope();
    }

    private sealed class TestDbService : DbServiceBase<FilesCacheContext>, IEFCoreDatabaseBackedService<FilesCacheContext>
    {
        public string? DbKey => null;

        public TestDbService(IScopeProvider scopeProvider)
            : base(scopeProvider, new(Substitute.For<IFExLogger>()), Substitute.For<IFExDbConfig>(), [])
        {
        }

        // Skips the SQL server probe of the production service; the cache only needs the mapping snapshot.
        protected override Task OnInitializeAsync() => EnsureMappingSnapshotAsync();
    }
}
