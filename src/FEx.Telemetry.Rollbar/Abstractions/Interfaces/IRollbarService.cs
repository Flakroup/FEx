using Rollbar;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Telemetry.Rollbar.Abstractions.Interfaces;

public interface IRollbarService
{
    ILogger Logger { get; }
    bool IsConfigured { get; }

    void Error(Exception error, IDictionary<string, object> custom = null, bool force = false);
    void SendMessage(string message, ErrorLevel level = ErrorLevel.Info, bool force = false);
    Task WaitForLoggingEndAsync();
    void WaitForLoggingEnd();

    Task OnExceptionOccuredAsync(Exception ex, params (string, object)[] custom); //todo is it necessary (to extension?)
    Task OnExceptionOccuredAsync(Exception ex, IDictionary<string, object> custom);
}