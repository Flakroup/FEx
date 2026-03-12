using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.Core.Abstractions.Interfaces;
using FEx.EFCore.Configuration;
using System;
using System.IO;

namespace FEx.Imaging.Windows;

public class FExDefaultFilesCacheServiceConfig : FilesCacheServiceConfig
{
    public FExDefaultFilesCacheServiceConfig(IAppInfoProvider appInfoProvider,
                                             DirectoryInfo imageCache = null,
                                             string sqlDbName = null,
                                             string sqliteDbFileName = null,
                                             bool useSqlite = false,
                                             bool useHttpClientService = false,
                                             TimeSpan? cacheValidPeriod = null,
                                             bool cacheAll = true)
        : base(new DbServiceConfig(new FExDbConfig
            {
                SqliteDbFile =
                    (imageCache ?? appInfoProvider.AppData.GetDescendantDirectory("ImageCache")).GetDescendantFile(
                        sqliteDbFileName ?? "IndexEF.db"),
                SqlDbName = sqlDbName ?? $"{FExCoreStatics.AppInfoProvider.Name}ImageCache",
                UseSqlite = useSqlite,
                DropIfMigrationFailed = true
            }),
            imageCache ?? appInfoProvider.AppData.GetDescendantDirectory("ImageCache"),
            useHttpClientService,
            cacheValidPeriod,
            cacheAll)
    {
    }
}
