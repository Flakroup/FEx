using FEx.Agnostics.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace FEx.Legacy.Imaging.Abstractions.Interfaces;

public interface ICachedImageStorage
{
    bool EntryCacheShouldBePrepared(IIndexEntryBase entry, bool refresh);
    Task<IIndexEntryBase> GetEntryBaseAsync(Uri fileUrl, bool addNew, string? fileName);

    Task<IIndexEntryBase> PrepareCacheAndGetEntryBaseAsync(Uri fileUrl, WebRequestParams? pars, bool refresh);

    Task<bool> PrepareCacheEntryAsync(IIndexEntryBase entry,
                                      WebRequestParams? pars,
                                      bool refresh,
                                      HttpWebResponse? response,
                                      string? checksum,
                                      Func<Uri, Uri>? urlModifier);

    long GetFileSize(Uri imageLink);
    string GetFileName(Uri imageLink);

    Task CacheAllAsync(HashSet<string>? keys);
    Task<bool> ContainsEntryAsync(Uri fileUrl);
    string GetFileChecksum(Uri imageLink);
    Task RemoveIndexEntriesAsync(params string[] ids);
}