using FEx.Agnostics.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Telemetry.Sentry;

public class FExSentryx : InitializeModule<IFExSentryModule, IServiceCollection>
{
    private static ISentryService _sentrySrv;

    public static ISentryService SentrySrv
    {
        get => _sentrySrv.GuardProperty();
        private set => _sentrySrv = value.Guard(nameof(value));
    }

    public FExSentryx(ISentryService sentrySrv)
    {
        SentrySrv = sentrySrv;
    }

    protected override void RegisterServices(IFExSentryModule container, IServiceCollection services) =>
        FExSentryModule.AddServices(container, services);
}
