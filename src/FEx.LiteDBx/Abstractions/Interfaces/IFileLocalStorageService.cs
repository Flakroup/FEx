using System;

namespace FEx.LiteDBx.Abstractions.Interfaces;

public interface IFileLocalStorageService
{
    ICachedFile CacheFile(IDownloadResult downloadResult);
    ICachedFile GetCachedFile(Uri fileUrl);
}