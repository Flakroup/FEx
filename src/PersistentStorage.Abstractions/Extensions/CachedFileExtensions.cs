using FEx.Agnostics.Abstractions.Extensions;
using FEx.PersistentStorage.Abstractions.Configuration;
using LiteDB;
using System;
using System.IO;
using System.Linq.Expressions;

namespace FEx.PersistentStorage.Abstractions.Extensions;

public static class CachedFileExtensions
{
    public static bool IsExpired(this LiteFileInfo<string> file) => IsExpired(file.UploadDate.ToUniversalTime());

    public static bool IsExpired(DateTime timestamp) =>
        timestamp.Add(CacheServiceConfiguration.ExpirationTimeSpan) <= DateTime.UtcNow;

    public static string GetFileId(Uri fileUrl) =>
        fileUrl.ToString().ComputeMd5Hash() + Path.GetExtension(fileUrl.ToString());

    public static Expression<Func<LiteFileInfo<string>, bool>> GetIsExpiredPredicate()
    {
        var expirationThreshold = DateTime.UtcNow.AddTicks(-CacheServiceConfiguration.ExpirationTimeSpan.Ticks);

        return file => file.UploadDate.ToUniversalTime() < expirationThreshold;
    }
}