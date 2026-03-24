using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Web;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Collections.Concurrent;
using FEx.Downloader;
using FEx.Downloader.Clients;
using FEx.Logging;
using FEx.MVVM.Abstractions;
using FEx.MVVM.Rx.BaseObjects;
using FEx.Webx.Extensions;
using FEx.Downloader.Services;
using FEx.Imaging.Windows.Model;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FEx.Imaging.Windows;

#pragma warning disable IDISP025 // may be subclassed
public class CachedImage : ReactiveNotifyPropertyChanged, IDisposable
#pragma warning restore IDISP025
{
    private readonly bool _ownCTS;

    protected internal FileInfo Cache => ParentIndexEntry.Cache;

    protected CancellationTokenSource CancellationTokenSource { get; }
    protected CancellationToken CancellationToken => CancellationTokenSource.Token;
    protected ConcurrentObservableDictionary<WidthAndHeight, SizedImageCache> CachedImages { get; }
    protected Task<bool> CachingTask { get; set; }
    protected IndexEntry ParentIndexEntry { get; }
    protected string FileName => ParentIndexEntry.FileName;
    protected SemaphoreSlim Semaphore => ParentIndexEntry.Semaphore;
    protected Uri Url => ParentIndexEntry.Url;
    protected string FilesCacheDirPath => ParentIndexEntry.FilesCacheRootDirPath;
    protected TimeSpan? CacheValidTime => ParentIndexEntry.Config.CacheValidPeriod;

    protected string Extension
    {
        get => ParentIndexEntry.Extension;
        set => ParentIndexEntry.Extension = value;
    }

    protected long ResponseContentLength
    {
        get => ParentIndexEntry.ResponseContentLength;
        set => ParentIndexEntry.ResponseContentLength = value;
    }

    protected bool IsDownloading
    {
        get => ParentIndexEntry.IsDownloading;
        set => ParentIndexEntry.IsDownloading = value;
    }

    public CachedImage(IndexEntry indexEntry, CancellationTokenSource cancellationTokenSource = default)
    {
        ParentIndexEntry = indexEntry;
        CachedImages = [];

        if (cancellationTokenSource is not null)
        {
            CancellationTokenSource = cancellationTokenSource;
        }
        else
        {
            _ownCTS = true;
            CancellationTokenSource = new();
        }
    }

    public async Task<BitmapImage> GetImageAsync(WidthAndHeight size = null,
                                                 WebRequestParams pars = null,
                                                 bool refresh = false,
                                                 bool forceLoad = false,
                                                 bool forceMemoryStream = true,
                                                 bool useHttpClientService = false,
                                                 HttpWebResponse response = null)
    {
        SizedImageCache res = EnsureSize(size);

        try
        {
            if (!CacheIsInvalid(refresh))
                await res.ReloadImageFromCacheFileAsync(refresh, forceLoad, forceMemoryStream);
        }
        catch
        {
            //ignore
        }

        if (!refresh
            && res.CachedImage is not null)
            return res.CachedImage;

        if (CachingTask.IsRunning())
            await CachingTask;

        CachingTask = Task.Run(() => PrepareCacheAsync(pars, refresh, useHttpClientService, response),
            CancellationToken);

        if (await CachingTask)
            await CachedImages.Values.WithWhenAllTasksAsync(v =>
                v.ReloadImageFromCacheFileAsync(true, forceLoad, forceMemoryStream));
        else if (res.CachedImage is null)
            await res.ReloadImageFromCacheFileAsync(true, forceLoad, forceMemoryStream);

        return res.CachedImage;
    }

    public async Task<bool> PrepareCacheAsync(WebRequestParams pars = null,
                                              bool refresh = false,
                                              bool useHttpClientService = false,
                                              HttpWebResponse response = null,
                                              string checksum = null,
                                              Func<Uri, Uri> urlModifier = null)
    {
        try
        {
            if (IsDownloading)
                await AsyncStatics.DelayUntilAsync(() => IsDownloading, cancellationToken: CancellationToken);

            if (CacheIsInvalid(refresh))
            {
                await Semaphore.WaitAsync(CancellationToken);

                try
                {
                    IsDownloading = true;

                    if (useHttpClientService)
                    {
#pragma warning disable IDISP001 // cached singleton, disposed via RemoveInstanceAsync
                        HttpClientService httpClientService = await HttpClientService.GetInstanceAsync(Url.Host, pars);
#pragma warning restore IDISP001

                        return await httpClientService.DoHttpClientActionAsync(client =>
                                InternalPrepareCacheAsync(client, refresh),
                            pars);
                    }

                    return await InternalPrepareCacheAsync(pars, refresh, response, checksum, urlModifier);
                }
                finally
                {
                    IsDownloading = false;
                    Semaphore.Release();
                }
            }
        }
        finally
        {
            Cache?.Refresh();
        }

        return false;
    }

    public bool CacheIsInvalid(bool refresh = false)
    {
        Cache?.Refresh();

        return refresh
               || Cache?.Exists != true
               || ResponseContentLength != Cache.Length
               || Extension != Cache.Extension
               || CacheValidTime.HasValue && Cache.LastWriteTime.Add(CacheValidTime.Value).IsEarlierThan(DateTime.Now);
    }

    public void RemoveImageUpdate(WidthAndHeight size = null) => EnsureSize(size).RemoveImageUpdateAction();

    public void OnParentConfigurationChange() => CachedImages.Clear();

    /// <summary>
    ///     Internals the prepare cache asynchronous.
    /// </summary>
    /// <param name="pars">The pars.</param>
    /// <param name="refresh">if set to <c>true</c> [refresh].</param>
    /// <param name="response">The response.</param>
    /// <param name="checksum">The checksum.</param>
    /// <returns></returns>
    private async Task<bool> InternalPrepareCacheAsync(WebRequestParams pars,
                                                       bool refresh = false,
                                                       HttpWebResponse response = null,
                                                       string checksum = null,
                                                       Func<Uri, Uri> urlModifier = null)
    {
        try
        {
            do
            {
                try
                {
                    Uri calledUrl = urlModifier is not null
                        ? urlModifier(Url)
                        : Url;

                    response ??= await calledUrl.GetUriHttpResponseAsync(pars);

                    string filePath = ResetCacheFile(response);

                    if (!CacheIsInvalid(refresh))
                    {
                        response?.Dispose();

                        return true;
                    }

                    bool hasInvalidContentLength = response.ContentLength == -1;

                    using var file =
                        DownloadItem.CreateFromResponse(response, filePath, false, pars, 0, 50, checksum, default);

                    if (await file.DownloadFileAsync())
                    {
                        ParentIndexEntry.ResetCacheFile(file.FilePath);

                        if (hasInvalidContentLength)
                            ResponseContentLength = Cache.Length;

                        return true;
                    }

                    response?.Dispose();
                    response = null;
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.RequestCanceled)
                    {
                        await Task.Delay(100, CancellationToken);

                        if (CancellationToken.IsCancellationRequested)
                            return false;

                        response?.Dispose();
                        response = null;
                    }
                    else
                    {
                        response?.Dispose();

                        throw;
                    }
                }
            } while (CacheIsInvalid(refresh));

            return true;
        }
        catch (Exception ex)
        {
            FExLoggingModule.LoggingSrv.LogError(ex, ex.ToString());

            return false;
        }
    }

    private string ResetCacheFile(HttpWebResponse response) =>
        ResetCacheFile(response.GetDefaultExtension(), response.ContentLength);

    private string ResetCacheFile(HttpResponseMessage response) =>
        ResetCacheFile(response.GetDefaultExtension(), response.Content.Headers.ContentLength ?? -1L);

    private string ResetCacheFile(string extension, long contentLength)
    {
        Extension = extension;
        string filePath = Path.Combine(FilesCacheDirPath, FileName + Extension);
        ResponseContentLength = contentLength;
        ParentIndexEntry.ResetCacheFile(filePath);

        return filePath;
    }

    private async Task<bool> InternalPrepareCacheAsync(FlakHttpClient client, bool refresh = false)
    {
        var retry = true;

        while (retry)
        {
            using HttpResponseMessage res =
                await client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, CancellationToken);

            try
            {
                using HttpResponseMessage response = res.EnsureSuccessStatusCode();
                string filePath = ResetCacheFile(response);

                refresh = CacheIsInvalid(refresh);

                if (refresh)
                {
                    await client.DoDownloadAsync(filePath, response);
                    ParentIndexEntry.ResetCacheFile(filePath);

                    return true;
                }

                retry = false;
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException
                    && (int)res.StatusCode == 429)
                {
                    await Task.Delay(100, CancellationToken);
                    retry = !CancellationToken.IsCancellationRequested;
                }
                else
                {
                    throw;
                }
            }
        }

        return false;
    }

    private SizedImageCache EnsureSize(WidthAndHeight size = null, BitmapImage image = null)
    {
        size ??= WidthAndHeight.Default;

        return CachedImages.GetOrAddValue(size, () => new(this, size, image, CancellationTokenSource));
    }

    #region IDisposable
    public void Dispose()
    {
        Parallel.ForEach(CachedImages.Values, ci => ci?.Dispose());

        if (_ownCTS)
#pragma warning disable IDISP007 // conditional on _ownCTS ownership flag
            CancellationTokenSource.Dispose();
#pragma warning restore IDISP007
    }
    #endregion
}
