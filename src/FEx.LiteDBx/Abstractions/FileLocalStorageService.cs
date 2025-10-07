using FEx.Extensions;
using FEx.LiteDBx.Abstractions.Interfaces;
using System;

namespace FEx.LiteDBx.Abstractions;

public abstract class FileLocalStorageService : IFileLocalStorageService
{
    public abstract ICachedFile CacheFile(IDownloadResult downloadResult);

    public abstract ICachedFile GetCachedFile(Uri fileUrl);

    protected virtual string GetFileId(Uri fileUrl) => fileUrl.ToString().GenerateMd5OfString();
}