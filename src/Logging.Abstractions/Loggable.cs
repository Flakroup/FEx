using FEx.Common.Extensions;
using FEx.Extensions;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FEx.Logging;

public class Loggable : ILoggable
{
    private readonly ILogger _logger;

#pragma warning disable IDISP006
    protected IDisposable Scope { get; private set; }
#pragma warning restore IDISP006
    protected object State { get; private set; }

    public Loggable(ILogger logger)
    {
        _logger = logger.Guard(nameof(logger));
    }

    public void LogCritical(string message, Exception exception = null) =>
        _logger.LogCritical(exception, Combine(message));

    public void LogDebug(string message, Exception exception = null) => _logger.LogDebug(exception, Combine(message));

    public void LogError(string message, Exception exception = null) => _logger.LogError(exception, Combine(message));

    public void LogInformation(string message, Exception exception = null) =>
        _logger.LogInformation(exception, Combine(message));

    public void LogTrace(string message, Exception exception = null) => _logger.LogTrace(exception, Combine(message));

    public void LogWarning(string message, Exception exception = null) =>
        _logger.LogWarning(exception, Combine(message));

    public void BeginLabeledScope(params (string, object)[] state) => BeginLabeledScope(new LoggerState(state));

    public void BeginLabeledScope(IDictionary<string, object> argsCustom) =>
        BeginLabeledScope(new LoggerState(argsCustom));

    public void BeginScope<TState>(TState state)
    {
        State = state;
        Scope?.Dispose();
        Scope = _logger.BeginScope(state);
    }

    public void BeginLabeledScope(ILoggerState state) => BeginScope(state);

    public void AddOrUpdateLabel(string key, object value)
    {
        if (State is not LoggerState lS)
            return;

        lS.AddOrUpdateLabel(key, value);
    }

    public void RemoveLabel(string key)
    {
        if (State is not LoggerState lS)
            return;

        lS.RemoveLabel(key);
    }

    public void EndScope()
    {
        if (State is not null)
            State = null;

        Scope.TryDispose();
    }

    private string Combine(string message) =>
        State is null
            ? message
            : $"[State:{State}]   {message}";
}