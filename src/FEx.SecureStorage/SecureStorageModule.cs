using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.SecureStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.SecureStorage;

[Register(typeof(SecureStorageModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
[Register(typeof(FileSecureStorageService), Scope.SingleInstance, typeof(ISecureStorageService))]
public class SecureStorageModule : InitializeModule<ISecureStorageContainer, IServiceCollection>
{
    protected override void RegisterServices(ISecureStorageContainer container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<ISecureStorageService>(container);
    }
}
