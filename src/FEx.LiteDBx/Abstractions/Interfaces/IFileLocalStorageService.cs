using System;

namespace FEx.LiteDbx.Abstractions.Interfaces;

public interface IFileLocalStorageService
{
    ICachedFile CacheFile(IDownloadResult downloadResult);
    ICachedFile GetCachedFile(Uri fileUrl);
}