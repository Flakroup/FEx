using FEx.DI.Abstractions.Interfaces;
using FEx.Legacy.Asyncx;
using FEx.Legacy.Asyncx.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Legacy;

[Register(typeof(TasksHandler), typeof(ITasksHandler))]
[Register(typeof(FExLegacy), Scope.SingleInstance, typeof(IInitializeModule))]
public class FExLegacyModule
{
    public static void AddServices(IFExLegacyContainer container, IServiceCollection services)
    {
        // ReSharper disable RedundantTypeArgumentsOfMethod
        services.AddTransientServiceUsingContainer<ITasksHandler>(container);
        // ReSharper restore RedundantTypeArgumentsOfMethod
    }
}