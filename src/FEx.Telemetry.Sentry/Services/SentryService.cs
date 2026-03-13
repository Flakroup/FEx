using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using Sentry;
using System;
using System.Threading.Tasks;

namespace FEx.Telemetry.Sentry.Services;

public class SentryService : ISentryService
{
    public bool IsInitialized { get; private set; }

    public void Initialize(string dsn, string environment, string release)
    {
        SentrySdk.Init(o =>
        {
            o.Dsn = dsn;
            o.Environment = environment;
            o.AutoSessionTracking = true;
            o.AttachStacktrace = true;
            o.Release = release;
        });

        IsInitialized = true;
    }

    public void CaptureException(Exception exception) => SentrySdk.CaptureException(exception);

    public async Task FlushAsync(TimeSpan? timeout = null) =>
        await SentrySdk.FlushAsync(timeout ?? TimeSpan.FromSeconds(5));
}
