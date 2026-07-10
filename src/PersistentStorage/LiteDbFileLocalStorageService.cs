using FEx.Agnostics.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions;
using FEx.PersistentStorage.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions.Models;
using LiteDB;
using System;
using System.IO;
using System.Linq;

namespace FEx.PersistentStorage;

public class LiteDbFileLocalStorageService : FileLocalStorageService
{
    private readonly ILiteRepository _context;

    public LiteDbFileLocalStorageService(IDatabaseProvider databaseProvider)
    {
        _context = databaseProvider.Repository;
    }

    /// <inheritdoc />
    public override IFExCachedFile CacheFile(IFExDownloadResult downloadResult) =>
        downloadResult.UseDataStream(stream => Upload(downloadResult.Url, downloadResult.FileName, stream));

    /// <inheritdoc />
    public override IFExCachedFile? GetCachedFile(Uri fileUrl) =>
        FindById(fileUrl) is not { } file
            ? null
            : new FExCachedFile(file);

    /// <inheritdoc />
    public override void UpdateFile(IFExCachedFile cachedFile)
    {
        _context.Database.FileStorage.Delete(cachedFile.Id);
        Upload(cachedFile.Url, cachedFile.Filename, cachedFile.GetDataStream());
    }

    /// <inheritdoc />
    public override void DeleteExpiredFiles()
    {
        var isExpiredPredicate = CachedFileExtensions.GetIsExpiredPredicate();
        var filesToDelete = _context.Database.FileStorage.Find(isExpiredPredicate).ToList();

        if (filesToDelete.IsNullOrEmpty())
            return;

        foreach (var file in filesToDelete)
            _context.Database.FileStorage.Delete(file.Id);
    }

    private FExCachedFile Upload(Uri fileUrl, string fileName, MemoryStream stream)
    {
        var fileId = GetFileId(fileUrl);

        var fileMetadata = new BsonDocument
        {
            [FExCachedFile.UrlMetadataName] = fileUrl.ToString()
        };

        var result = _context.Database.FileStorage.Upload(fileId, fileName, stream, fileMetadata);

        return new(result);
    }

    private LiteFileInfo<string> FindById(Uri fileUrl) => FindById(GetFileId(fileUrl));

    private LiteFileInfo<string> FindById(string fileId) => _context.Database.FileStorage.FindById(fileId);
}