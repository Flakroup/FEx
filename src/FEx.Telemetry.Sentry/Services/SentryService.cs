using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using Sentry;
using System;
using System.Threading.Tasks;

namespace FEx.Telemetry.Sentry.Services;

public sealed class SentryService : ISentryService, IDisposable
{
    private IDisposable? _sdkHandle;

    public bool IsInitialized { get; private set; }

    public void Initialize(string dsn, string environment, string release)
    {
        _sdkHandle?.Dispose();
        _sdkHandle = SentrySdk.Init(o =>
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

    // IDISP004/IDISP006: SentrySdk.Init returns the SDK lifecycle handle; it is kept for the
    // service lifetime and disposed here so the Sentry SDK flushes and shuts down gracefully when
    // the DI container disposes this (singleton) service.
    public void Dispose()
    {
        _sdkHandle?.Dispose();
        _sdkHandle = null;
    }
}
