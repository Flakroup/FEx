using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Legacy.Asyncx;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Legacy;

[Register(typeof(TasksHandler), typeof(ITasksHandler))]
[Register(typeof(FExLegacy), Scope.SingleInstance, typeof(FExLegacy), typeof(IFExInitializable))]
[Register(typeof(FExLegacyModule), Scope.SingleInstance, typeof(IInitializeModule<IServiceCollection>))]
public class FExLegacyModule : InitializeModule<IFExLegacyContainer, IServiceCollection>
{
    protected override void RegisterServices(IFExLegacyContainer? container, IServiceCollection services)
    {
        container = container.Guard(nameof(container));
        services.AddTransientServiceUsingContainer<ITasksHandler>(container);
    }
}