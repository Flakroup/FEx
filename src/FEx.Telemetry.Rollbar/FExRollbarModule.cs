using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Telemetry.Rollbar.Abstractions.Interfaces;
using FEx.Telemetry.Rollbar.Services;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Telemetry.Rollbar;

[Register(typeof(RollbarService), Scope.SingleInstance, typeof(IRollbarService))]
[Register(typeof(FExRollbarx), Scope.SingleInstance, typeof(FExRollbarx), typeof(IInitializeModule<IServiceCollection>))]
public class FExRollbarModule : FExTelemetryModule
{
    public static void AddServices(IRollbarModule container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<IRollbarService>(container);
        services.AddSingletonServiceUsingContainer<IRollbarConfig>(container);
        services.AddSingletonServiceUsingContainer<FExRollbarx>(container);
    }
}