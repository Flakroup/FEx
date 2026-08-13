using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions;
using FEx.EFCore.Configuration;

namespace FEx.Imaging.Windows;

public class FExDefaultFilesCacheServiceConfig : FilesCacheServiceConfig
{
    public FExDefaultFilesCacheServiceConfig(IAppInfoProvider appInfoProvider,
                                             IFilesCacheServiceConfigurator filesCacheServiceConfigurator)
        : base(new DbServiceConfig(new FExDbConfig
        {
            SqliteDbFile =
                    (filesCacheServiceConfigurator.ImageCache
                     ?? appInfoProvider.AppData.GetDescendantDirectory("ImageCache")).GetDescendantFile(
                        filesCacheServiceConfigurator.SqliteDbFileName ?? "IndexEF.db"),
            SqlDbName = filesCacheServiceConfigurator.SqlDbName
                            ?? $"{FExCoreStatics.AppInfoProvider.Name}ImageCache",
            UseSqlite = filesCacheServiceConfigurator.UseSqlite,
            DropIfMigrationFailed = true
        }),
            filesCacheServiceConfigurator.ImageCache ?? appInfoProvider.AppData.GetDescendantDirectory("ImageCache"),
            filesCacheServiceConfigurator.UseHttpClientService,
            filesCacheServiceConfigurator.CacheValidPeriod,
            filesCacheServiceConfigurator.CacheAll)
    {
    }
}