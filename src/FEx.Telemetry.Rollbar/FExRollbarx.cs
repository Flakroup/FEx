using FEx.Agnostics.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using FEx.Telemetry.Subjects;
using FEx.Telemetry.Rollbar.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Telemetry.Rollbar;

public class FExRollbarx : InitializeModule<IRollbarModule, IServiceCollection>
{
    private static IRollbarService _rollbarSrv;
    private static TelemetryAccessTokenSubject _telemetryAccessTokenSubject;

    public static IRollbarService RollbarSrv
    {
        get => _rollbarSrv.GuardProperty();
        private set => _rollbarSrv = value.Guard(nameof(value));
    }

    public static TelemetryAccessTokenSubject TelemetryAccessTokenSubject
    {
        get => _telemetryAccessTokenSubject.GuardProperty();
        private set => _telemetryAccessTokenSubject = value.Guard(nameof(value));
    }

    public FExRollbarx(IRollbarService rollbarSrv, TelemetryAccessTokenSubject telemetryAccessTokenSubject)
    {
        RollbarSrv = rollbarSrv;
        TelemetryAccessTokenSubject = telemetryAccessTokenSubject;
    }

    protected override void RegisterServices(IRollbarModule container, IServiceCollection services) =>
        FExRollbarModule.AddServices(container, services);
}