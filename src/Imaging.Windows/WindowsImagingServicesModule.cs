using FEx.Agnostics.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Extensions;
using FEx.EFCore.Interfaces;
using FEx.Imaging.Windows.Model;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
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
[Register(typeof(FilesCacheServiceConfigurator), Scope.SingleInstance, typeof(IFilesCacheServiceConfigurator))]
[Register(typeof(IndexEntriesCache), Scope.SingleInstance, typeof(IndexEntriesCache))]
[Register(typeof(WindowsImagingServicesModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class WindowsImagingServicesModule : InitializeModule<IWindowsImagingServicesContainer, IServiceCollection>
{
    protected override void RegisterServices(IWindowsImagingServicesContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
#pragma warning disable IDISP004 // DI container manages lifetime
        var config = container.Resolve<IFilesCacheServiceConfig>().Value;
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