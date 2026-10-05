using FEx.Telemetry.Sentry;
using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using FEx.Telemetry.Subjects;
using StrongInject;

namespace FEx.Telemetry.Tests;

[RegisterModule(typeof(FExSentryModule))]
internal sealed partial class SentryContainer : IFExSentryModule
{
    [Factory(Scope.SingleInstance, typeof(FExSentryConfig), typeof(IFExTelemetryConfig))]
    public static FExSentryConfig CreateConfig() => new(string.Empty, null!);

    public ISentryService GetSentryService() => Get<ISentryService>(this);

    public FExSentryx GetSentryx() => Get<FExSentryx>(this);

    public TelemetryAccessTokenSubject GetAccessTokenSubject() => Get<TelemetryAccessTokenSubject>(this);

    private static T Get<T>(IContainer<T> container) where T : notnull
    {
        using var owned = container.Resolve();
        return owned.Value;
    }
}
