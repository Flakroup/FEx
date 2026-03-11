using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using FEx.Telemetry.Sentry.Services;
using Microsoft.Extensions.DependencyInjection;
using StrongInject;
using StrongInject.Extensions.DependencyInjection;

namespace FEx.Telemetry.Sentry;

[Register(typeof(SentryService), Scope.SingleInstance, typeof(ISentryService))]
[Register(typeof(FExSentryx), Scope.SingleInstance, typeof(FExSentryx), typeof(IInitializeModule<IServiceCollection>))]
public class FExSentryModule : FExTelemetryModule
{
    public static void AddServices(ISentryModule container, IServiceCollection services)
    {
        services.AddSingletonServiceUsingContainer<ISentryService>(container);
        services.AddSingletonServiceUsingContainer<FExSentryx>(container);
    }
}
