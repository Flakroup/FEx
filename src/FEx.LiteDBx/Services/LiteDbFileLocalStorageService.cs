using FEx.LiteDbx.Abstractions;
using FEx.LiteDbx.Abstractions.Interfaces;
using FEx.LiteDbx.Models;
using LiteDB;
using System;
using System.IO;

namespace FEx.LiteDbx.Services;

public class LiteDbFileLocalStorageService : FileLocalStorageService
{
    private readonly ILiteRepository _context;

    public LiteDbFileLocalStorageService(IDatabaseProvider databaseProvider)
    {
        _context = databaseProvider.Repository;
    }

    public override ICachedFile CacheFile(IDownloadResult downloadResult) =>
        downloadResult.UseDataStream(stream => Upload(downloadResult.Url, downloadResult.FileName, stream));

    public override ICachedFile GetCachedFile(Uri fileUrl)
    {
        string fileId = GetFileId(fileUrl);

        return _context.Database.FileStorage.FindById(fileId) is { } file
            ? new CachedFile(file, fileUrl)
            : null;
    }

    private CachedFile Upload(Uri fileUrl, string fileName, MemoryStream stream)
    {
        string fileId = GetFileId(fileUrl);
        LiteFileInfo<string> result = _context.Database.FileStorage.Upload(fileId, fileName, stream);

        return new CachedFile(result, fileUrl);
    }
}