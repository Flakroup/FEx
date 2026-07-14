using FEx.Agnostics.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.PersistentStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.PersistentStorage;

[Register(typeof(FExPersistentStorageModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(DatabaseProvider), Scope.SingleInstance, typeof(IDatabaseProvider))]
[Register(typeof(CacheService), typeof(ICacheService))]
[Register(typeof(LiteDbFileLocalStorageService), typeof(IFileLocalStorageService))]
[Register(typeof(LocalStorageService), typeof(ILocalStorageService))]
public class FExPersistentStorageModule : InitializeModule<IFExPersistentStorageContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExPersistentStorageContainer? container, IServiceCollection services)
    {
        container.Guard(nameof(container));

        services.AddSingletonServiceUsingContainer<IDatabaseProvider>(container);

        services.AddTransientServiceUsingContainer<ICacheService>(container);
        services.AddTransientServiceUsingContainer<IFileLocalStorageService>(container);
        services.AddTransientServiceUsingContainer<ILocalStorageService>(container);
    }
}