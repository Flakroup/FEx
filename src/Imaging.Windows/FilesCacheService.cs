using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Lists;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Models;
using FEx.Asyncx.Abstractions;
using FEx.Core.Abstractions.Extensions;
using FEx.FileSystem;
using FEx.Imaging.Windows.Model;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using FEx.MVVM.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FEx.Imaging.Windows;

public class FilesCacheService : AsyncInitializable, IFilesCacheService
{
    private bool _isDisposed;
    public bool UseHttpClientService => Config.UseHttpClientService;
    public IFilesCacheServiceConfig Config { get; }
    public DirectoryInfo FilesCacheDir => Config.FilesCacheDir;
    public IndexEntriesCache FilesCacheIndex { get; }

#pragma warning disable IDISP002 // semaphore from LockSrv, lifetime managed by lock service
    protected SemaphoreSlim InitializationSemaphore { get; }
#pragma warning restore IDISP002
    protected ISynchronizedAccessService LockSrv { get; }

    public FilesCacheService(IFilesCacheServiceConfig config,
                             IndexEntriesCache cache,
                             ISynchronizedAccessService lockService)
        : base(cache)
    {
        LockSrv = lockService;
        Config = config;
        FilesCacheDir.Create();
        FilesCacheIndex = cache;

        InitializationSemaphore = LockSrv.EnsureLock(GetType().FullName.Guard(nameof(FilesCacheService)));
        BeginInitialization();
    }

    public bool EntryCacheShouldBePrepared(IIndexEntryBase entry, bool refresh) =>
        EntryCacheShouldBePrepared((IndexEntry)entry, refresh);

    // ICachedImageStorage declares a non-nullable return; behavior preserved (null when file absent), trailing ! only satisfies that contract
    public string GetFileChecksum(Uri imageLink) => GetFile(imageLink)?.GenerateMd5OfFile()!;

    public long GetFileSize(Uri imageLink) => GetFile(imageLink)?.Length ?? 0;

    // ICachedImageStorage declares a non-nullable return; behavior preserved (null when file absent), trailing ! only satisfies that contract
    public string GetFileName(Uri imageLink) => GetFile(imageLink)?.Name!;

#pragma warning disable IDISP005 // caller receives cached entry, does not own it
    public async Task<IIndexEntryBase> GetEntryBaseAsync(Uri fileUrl, bool addNew = true, string? fileName = null) =>
        await GetEntryAsync(fileUrl, addNew, fileName);
#pragma warning restore IDISP005

#pragma warning disable IDISP005 // caller receives cached entry, does not own it
    public async Task<IIndexEntryBase> PrepareCacheAndGetEntryBaseAsync(
        Uri fileUrl,
        WebRequestParams? pars = null,
        bool refresh = false) =>
        await PrepareCacheAndGetEntryAsync(fileUrl, pars, refresh);
#pragma warning restore IDISP005

    public async Task RemoveIndexEntriesAsync(params string[] ids)
    {
        var entries = ids.Select(x => (id: x, file: GetFile(x)))
            .Where(x => x.file is not null)
            .ToDictionary(x => x.id, x => x.file!);

        await FilesCacheIndex.RemoveIndexEntriesAsync(entries);
    }

    public async Task CacheAllAsync(HashSet<string>? keys = null) => await FilesCacheIndex.CacheAllAsync(keys);

    public async Task<bool> ContainsEntryAsync(Uri fileUrl) =>
        await FilesCacheIndex.ContainsKeyAsync(fileUrl.AbsoluteUri);

    public async Task<bool> PrepareCacheEntryAsync(IIndexEntryBase entry,
                                                   WebRequestParams? pars = null,
                                                   bool refresh = false,
                                                   HttpWebResponse? response = null,
                                                   string? checksum = null,
                                                   Func<Uri, Uri>? urlModifier = null) =>
        await PrepareCacheEntryAsync(pars, refresh, response, (IndexEntry)entry, checksum, urlModifier);

    public async Task<BitmapImage?> GetImageAsync(Uri fileUrl,
                                                 WidthAndHeight? size = null,
                                                 WebRequestParams? pars = null,
                                                 bool refresh = false,
                                                 bool forceLoad = true,
                                                 bool forceMemoryStream = false,
                                                 HttpWebResponse? response = null)
    {
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await GetEntryAsync(fileUrl);
#pragma warning restore IDISP001

        // CachedImage is created for every entry with a non-empty URL key (see IndexEntry.RefreshCachedImage)
        return await entry.CachedImage!.GetImageAsync(size,
            pars,
            refresh,
            forceLoad,
            forceMemoryStream,
            UseHttpClientService,
            response);
    }

    public bool EntryCacheShouldBePrepared(IndexEntry entry, bool refresh) =>
        entry.CachedImage!.CacheIsInvalid(refresh) || entry.IsDownloading;

    public async Task RemoveImageUpdateAsync(Uri fileUrl, WidthAndHeight? size = null)
    {
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await GetEntryAsync(fileUrl);
#pragma warning restore IDISP001
        entry.CachedImage!.RemoveImageUpdate(size);
    }

    public async Task<bool> SetDefaultImageAsync(Uri fileUrl,
                                                 Bitmap defaultImage,
                                                 ImageFormat format,
                                                 string extension,
                                                 bool force = false)
    {
        var fileName = $"{fileUrl.AbsoluteUri.GenerateMd5OfString()}.{extension}";
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await GetEntryAsync(fileUrl, fileName: fileName);
#pragma warning restore IDISP001

        if (entry.FilePath is not null
            && File.Exists(entry.FilePath)
            && !force)
            return false;

        var file = FilesCacheDir.GetDescendantFile(fileName);
        defaultImage.Save(file.FullName, format);

        entry.FilePath ??= file.FullName;

        return entry.DoesCacheExists();
    }

    public async Task RemoveNotPresentFileUrlsAsync(HashSet<string> urls)
    {
        if (urls.IsNotNullOrEmptyCollection())
            await FilesCacheIndex.RemoveWhereAsync((k, _) => !urls.Contains(k));
    }

    public async Task<bool> IsFileBeingDownloadedAsync(Uri fileUrl)
    {
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await GetEntryAsync(fileUrl, false);
#pragma warning restore IDISP001

        return entry?.IsDownloading ?? false;
    }

    public async Task<Uri?> PrepareAndGetFileLocalUriAsync(Uri fileUrl,
                                                          WebRequestParams? pars = null,
                                                          bool refresh = false,
                                                          HttpWebResponse? response = null)
    {
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await PrepareCacheAndGetEntryAsync(fileUrl, pars, refresh, response);
#pragma warning restore IDISP001

        return entry.DoesCacheExists()
            ? entry.LocalUri
            : null;
    }

    public async Task<bool> IsFilePresentAsync(Uri fileUrl)
    {
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await GetEntryAsync(fileUrl, false);
#pragma warning restore IDISP001

        return entry.DoesCacheExists();
    }

    public async Task<IndexEntry> GetEntryAsync(Uri fileUrl, bool addNew = true, string? fileName = null)
    {
        await InitializeAsync();

        return await FilesCacheIndex.GetOrAddValueAsync(fileUrl.AbsoluteUri, addNew, fileName);
    }

    public LazyImagesHandler GetLazyImagesHandler() => new(this);

    public async Task<Result<AggregatedError>> OptimizeCacheAsync()
    {
        var files = GetExistingImageFiles().ToDictionary(x => Path.GetFileName(x.Name), x => x);

        var results = new ConcurrentDictionary<string, Result<ExceptionError>>();

        //add missing paths to files in index
        await FilesCacheIndex.ForAllAsync((_, entry) =>
        {
            var temp = new Result<ExceptionError>();

            try
            {
                if (entry.Cache?.Name is not null)
                {
                    var (isPresent, fileInfo) = files.GetValue(entry.Cache.Name);

                    if (isPresent && fileInfo is not null)
                    {
                        entry.FilePath ??= fileInfo.FullName;

                        if (entry.ResponseContentLength != fileInfo.Length)
                            temp = FileSystemUtilities.SafeDeleteFile(fileInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                temp = new ExceptionError(ex);
            }
            finally
            {
                if (temp.IsFailure)
                    results.AddOrUpdateValue(entry.AbsoluteUri, temp);
            }
        });

        var errors = results.Values.Where(x => x.IsFailure).Select(x => x.Error!).ToList();

        //remove not present in index files from disk
        var except = await FilesCacheIndex.ForAllFuncAsync((_, v) => v.FilePath);
        var filesToRemove = files.Values.Select(x => x.FullName).Except(except).ToArray();

        Parallel.ForEach(filesToRemove,
            file =>
            {
                var res = FileSystemUtilities.SafeDeleteFile(file);

                if (res.IsFailure)
                    errors.Add(res.Error!);
            });

        //remove not existing files from index
        var toRemove = (await FilesCacheIndex.ForAllFuncAsync((_, entry) =>
            {
                entry.Cache?.Refresh();

                return entry.Cache?.Exists != true
                    ? entry
                    : null;
            })).OfType<IndexEntry>()
            .ToArray();

        if (toRemove.IsNotNullOrEmptyList())
            await RemoveIndexEntriesAsync(toRemove);

        return errors.Count > 0
            ? new AggregatedError(errors)
            : Result<AggregatedError>.Success;
    }

    public async Task<bool> PrepareCacheAsync(Uri fileUrl,
                                              WebRequestParams? pars = null,
                                              bool refresh = false,
                                              HttpWebResponse? response = null,
                                              Func<Uri, Uri>? urlModifier = null)
    {
#pragma warning disable IDISP001 // cached entry, lifetime managed by FilesCacheIndex
        var entry = await PrepareCacheAndGetEntryAsync(fileUrl, pars, refresh, response, urlModifier);
#pragma warning restore IDISP001

        return entry.DoesCacheExists();
    }

    public async Task<IndexEntry> PrepareCacheAndGetEntryAsync(Uri fileUrl,
                                                               WebRequestParams? pars = null,
                                                               bool refresh = false,
                                                               HttpWebResponse? response = null,
                                                               Func<Uri, Uri>? urlModifier = null)
    {
        var entry = await GetEntryAsync(fileUrl);

        await PrepareCacheEntryAsync(pars, refresh, response, entry, urlModifier: urlModifier);

        return entry;
    }

    public async Task<bool> PrepareCacheEntryAsync(WebRequestParams? pars,
                                                   bool refresh,
                                                   HttpWebResponse? response,
                                                   IndexEntry entry,
                                                   string? checksum = null,
                                                   Func<Uri, Uri>? urlModifier = null)
    {
        if (EntryCacheShouldBePrepared(entry, refresh))
            _ = await entry.CachedImage!.PrepareCacheAsync(pars,
                refresh,
                UseHttpClientService,
                response,
                checksum,
                urlModifier);

        return entry.DoesCacheExists();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        if (await InitializationSemaphore.WaitAsync(TimeSpan.Zero))
            try
            {
                if (!IsInitialized)
                {
                    _ = await FilesCacheIndex.RemoveIndexEntriesOfMissingFilesAsync([
                        ..GetExistingImageFiles().Select(x => x.FullName)
                    ]);

                    if (Config.CacheAll)
                    {
                        await FilesCacheIndex.CacheAllAsync();

                        var result = await OptimizeCacheAsync();

                        if (!result.IsSuccess)
                            throw result.Error!.ToAggregateException();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.HandleException();

                throw;
            }
            finally
            {
                InitializationSemaphore.Release();
            }
    }

    private async Task RemoveIndexEntriesAsync(params IndexEntry[] entries)
    {
        if (entries.IsNotNullOrEmptyList())
        {
            if (entries.Length > 1)
            {
                var keys = new HashSet<string>(entries.Select(x => x.AbsoluteUri));
                await FilesCacheIndex.RemoveWhereAsync((k, _) => keys.Contains(k));
            }
            else
            {
                var entry = entries[0];
                FilesCacheIndex.Remove(entry.AbsoluteUri);
            }
        }
    }

    private IList<FileInfo> GetExistingImageFiles()
    {
        const string indexDbFileName = "IndexEF.db";
        const string indexDbShmFileName = "IndexEF.db-shm";
        const string indexDbWalFileName = "IndexEF.db-wal";

        var excludedPaths = new HashSet<string>
        {
            FilesCacheDir.GetDescendantPath(indexDbFileName),
            FilesCacheDir.GetDescendantPath(indexDbShmFileName),
            FilesCacheDir.GetDescendantPath(indexDbWalFileName)
        };

        return [.. FilesCacheDir.GetFiles("*.*").Where(x => !excludedPaths.Contains(x.FullName))];
    }

    private FileInfo? GetFile(Uri imageLink) => GetFile(imageLink?.AbsoluteUri);

    private FileInfo? GetFile(string? imageLink)
    {
        if (imageLink is null)
            return null;

        var entry = FilesCacheIndex[imageLink];

        if (entry is not null)
        {
            entry.Cache?.Refresh();

            return entry.Cache?.Exists == true
                ? entry.Cache
                : null;
        }

        var fileName = IndexEntry.GetFileName(imageLink);

        return FilesCacheDir.GetFiles($"{fileName}.*").SingleOrDefault();
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
#pragma warning disable IDISP007 // injected via DI, but this service takes ownership of the cache
            FilesCacheIndex?.Dispose();
#pragma warning restore IDISP007

        _isDisposed = true;
        base.Dispose(disposing);
    }
    #endregion
}