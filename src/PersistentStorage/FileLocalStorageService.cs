using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Extensions;
using System;

namespace FEx.PersistentStorage;

public abstract class FileLocalStorageService : IFileLocalStorageService
{
    public abstract IFExCachedFile CacheFile(IFExDownloadResult downloadResult);

    public abstract IFExCachedFile GetCachedFile(Uri fileUrl);

    public abstract void UpdateFile(IFExCachedFile cachedFile);

    public abstract void DeleteExpiredFiles();

    protected static string GetFileId(Uri fileUrl) => CachedFileExtensions.GetFileId(fileUrl);
}