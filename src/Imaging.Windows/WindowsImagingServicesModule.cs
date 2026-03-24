using FEx.DependencyInjection.Abstractions;
using FEx.EFCore.Extensions;
using FEx.EFCore.Interfaces;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using FEx.Imaging.Windows.Model;
using FEx.Sqlx.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Imaging.Windows;

[Register(typeof(FExDefaultFilesCacheServiceConfig),
    Scope.SingleInstance,
    typeof(IFilesCacheServiceConfig),
    typeof(IIndexEntryConfig))]
[Register(typeof(FilesCacheService), Scope.SingleInstance, typeof(IFilesCacheService), typeof(ICachedImageStorage))]
[Register(typeof(FilesCacheDbService), Scope.SingleInstance, typeof(IEFCoreDatabaseBackedService<FilesCacheContext>))]
[Register(typeof(IndexEntriesCache), Scope.SingleInstance, typeof(IndexEntriesCache))]
public class WindowsImagingServicesModule
{
    public static void AddServices(IWindowsImagingServicesModule container, IServiceCollection services)
    {
#pragma warning disable IDISP004 // DI container manages lifetime
        IFilesCacheServiceConfig config = container.Resolve<IFilesCacheServiceConfig>().Value;
#pragma warning restore IDISP004

        services.AddSingletonServiceUsingContainer<IFilesCacheServiceConfig>(container);
        services.AddSingletonServiceUsingContainer<IIndexEntryConfig>(container);
        services.AddSingletonServiceUsingContainer<IFilesCacheService>(container);
        services.AddSingletonServiceUsingContainer<IEFCoreDatabaseBackedService<FilesCacheContext>>(container);
        services.AddSingletonServiceUsingContainer<IndexEntriesCache>(container);
        services.AddSingletonServiceUsingContainer<ICachedImageStorage>(container);

        services.AddDbContextPool<FilesCacheContext>(
            (_, options) =>
                options.ConfigureDbContext(config.DbServiceConfig.DbConfig, FExServiceProvider.Get<ISqlDbHelper>()),
            config.DbServiceConfig.DbConfig.PoolSize);
    }
}
