using FEx.Agnostics.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace FEx.Legacy.Imaging.Abstractions.Interfaces;

public interface ICachedImageStorage
{
    bool EntryCacheShouldBePrepared(IIndexEntryBase entry, bool refresh = false);
    Task<IIndexEntryBase> GetEntryBaseAsync(Uri fileUrl, bool addNew = true, string fileName = null);

    Task<IIndexEntryBase> PrepareCacheAndGetEntryBaseAsync(Uri fileUrl,
                                                           WebRequestParams pars = null,
                                                           bool refresh = false);

    Task<bool> PrepareCacheEntryAsync(IIndexEntryBase entry,
                                      WebRequestParams pars = null,
                                      bool refresh = false,
                                      HttpWebResponse response = null,
                                      string checksum = null,
                                      Func<Uri, Uri> urlModifier = null);

    long GetFileSize(Uri imageLink);
    string GetFileName(Uri imageLink);

    Task CacheAllAsync(HashSet<string> keys = null);
    Task<bool> ContainsEntryAsync(Uri fileUrl);
    string GetFileChecksum(Uri imageLink);
    Task RemoveIndexEntriesAsync(params string[] ids);
}