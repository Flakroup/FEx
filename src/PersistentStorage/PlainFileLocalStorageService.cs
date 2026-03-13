using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions.Models;
using System;
using System.IO;
using System.Linq;

namespace FEx.PersistentStorage;

public class PlainFileLocalStorageService : FileLocalStorageService
{
    private readonly DirectoryInfo _cacheRoot;

    public PlainFileLocalStorageService(IDatabaseFilePathResolver databaseFilePathResolver)
    {
        _cacheRoot = new(Path.Combine(databaseFilePathResolver.GetDatabasesFolderPath(), "_cache"));
        _cacheRoot.Create();
    }

    /// <inheritdoc />
    public override IFExCachedFile CacheFile(IFExDownloadResult downloadResult)
    {
        downloadResult.UseDataStream(stream => CopyData(downloadResult.Url, stream));

        return new FExCachedFile(downloadResult);
    }

    /// <inheritdoc />
    public override IFExCachedFile GetCachedFile(Uri fileUrl)
    {
        var file = GetFile(fileUrl);

        return file.Exists
            ? new FExCachedFile(file, fileUrl)
            : null;
    }

    /// <inheritdoc />
    public override void UpdateFile(IFExCachedFile cachedFile)
    {
        var file = GetFile(cachedFile.Url);

        using var stream = cachedFile.GetDataStream();
        CopyData(file, stream);

        file.LastWriteTimeUtc = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public override void DeleteExpiredFiles()
    {
        var expiredFiles = _cacheRoot.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
            .Where(static file => CachedFileExtensions.IsExpired(file.LastWriteTimeUtc))
            .ToList();

        foreach (var file in expiredFiles)
            file.Delete();
    }

    private static void CopyData(FileInfo file, Stream stream)
    {
        using var fileStream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);
        fileStream.SetLength(0);
        stream.CopyTo(fileStream);
    }

    private void CopyData(Uri fileUrl, Stream stream)
    {
        var file = GetFile(fileUrl);
        CopyData(file, stream);
    }

    private string GetFilePath(Uri fileUrl)
    {
        var fileName = GetFileId(fileUrl);

        return Path.Combine(_cacheRoot.FullName, fileName);
    }

    private FileInfo GetFile(Uri fileUrl)
    {
        var filePath = GetFilePath(fileUrl);

        return new(filePath);
    }
}