using System;

namespace FEx.PersistentStorage.Abstractions;

public interface IFileLocalStorageService
{
    /// <summary>
    /// Cache file stream
    /// </summary>
    /// <param name="downloadResult">Result of stream download</param>
    /// <returns>Cached file</returns>
    IFExCachedFile CacheFile(IFExDownloadResult downloadResult);

    /// <summary>
    /// Find cached file for provided download URL
    /// </summary>
    /// <param name="fileUrl">URL of downloaded file</param>
    /// <returns>Cached file</returns>
    IFExCachedFile GetCachedFile(Uri fileUrl);

    /// <summary>
    /// Overwrite cached file with new value
    /// </summary>
    /// <param name="cachedFile">Cached file</param>
    void UpdateFile(IFExCachedFile cachedFile);

    /// <summary>
    /// Delete all outdated files from cache
    /// </summary>
    void DeleteExpiredFiles();
}