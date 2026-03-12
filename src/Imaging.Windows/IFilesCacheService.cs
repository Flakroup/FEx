using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Models;
using FEx.Core.Abstractions.Interfaces;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using FEx.MVVM.Abstractions;
using FEx.Imaging.Windows.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FEx.Imaging.Windows;

public interface IFilesCacheService : ICachedImageStorage, IAsyncInitializable
{
    IFilesCacheServiceConfig Config { get; }
    DirectoryInfo FilesCacheDir { get; }
    IndexEntriesCache FilesCacheIndex { get; }
    bool UseHttpClientService { get; }

    bool EntryCacheShouldBePrepared(IndexEntry entry, bool refresh);
    Task<IndexEntry> GetEntryAsync(Uri fileUrl, bool addNew = true, string fileName = null);

    Task<Uri> PrepareAndGetFileLocalUriAsync(Uri fileUrl,
                                             WebRequestParams pars = null,
                                             bool refresh = false,
                                             HttpWebResponse response = null);

    Task<BitmapImage> GetImageAsync(Uri fileUrl,
                                    WidthAndHeight size = null,
                                    WebRequestParams pars = null,
                                    bool refresh = false,
                                    bool forceLoad = true,
                                    bool forceMemoryStream = false,
                                    HttpWebResponse response = null);

    LazyImagesHandler GetLazyImagesHandler();
    Task<bool> IsFileBeingDownloadedAsync(Uri fileUrl);
    Task<bool> IsFilePresentAsync(Uri fileUrl);
    Task<Result<AggregatedError>> OptimizeCacheAsync();

    Task<bool> PrepareCacheAsync(Uri fileUrl,
                                 WebRequestParams pars = null,
                                 bool refresh = false,
                                 HttpWebResponse response = null,
                                 Func<Uri, Uri> urlModifier = null);

    Task<IndexEntry> PrepareCacheAndGetEntryAsync(Uri fileUrl,
                                                  WebRequestParams pars = null,
                                                  bool refresh = false,
                                                  HttpWebResponse response = null,
                                                  Func<Uri, Uri> urlModifier = null);

    Task<bool> PrepareCacheEntryAsync(WebRequestParams pars,
                                      bool refresh,
                                      HttpWebResponse response,
                                      IndexEntry entry,
                                      string checksum = null,
                                      Func<Uri, Uri> urlModifier = null);

    Task RemoveImageUpdateAsync(Uri fileUrl, WidthAndHeight size = null);

    Task<bool> SetDefaultImageAsync(Uri fileUrl,
                                    Bitmap defaultImage,
                                    ImageFormat format,
                                    string extension,
                                    bool force = false);

    Task RemoveNotPresentFileUrlsAsync(HashSet<string> hashSet);
}
