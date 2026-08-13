using System;
using System.Threading.Tasks;

namespace FEx.Telemetry.Sentry.Abstractions.Interfaces;

public interface ISentryService
{
    bool IsInitialized { get; }
    void Initialize(string dsn, string environment, string release);
    void CaptureException(Exception exception);
    Task FlushAsync(TimeSpan? timeout = null);
}
