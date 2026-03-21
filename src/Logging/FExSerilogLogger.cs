using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Models;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;

namespace FEx.Logging;

/// <summary>
/// Serilog-backed implementation of IFExLogger.
/// Maps Critical → Serilog.Fatal, implements structured logging via LogContext.
/// </summary>
public class FExSerilogLogger : IFExLogger
{
    public event EventHandler<FExErrorEventArgs> ErrorLogged;

    private object _state;
    private IDisposable _scope;

    // Trace level
    public void Trace(string message) => Log.Verbose(message);
    public void Trace(Exception exception, string message) => Log.Verbose(exception, message ?? exception.Message);

    // Debug level
    public void Debug(string message) => Log.Debug(message);
    public void Debug(Exception exception, string message) => Log.Debug(exception, message ?? exception.Message);

    // Information level
    public void Information(string message) => Log.Information(message);
    public void Information(Exception exception, string message) =>
        Log.Information(exception, message ?? exception.Message);

    // Warning level
    public void Warning(string message) => Log.Warning(message);
    public void Warning(Exception exception, string message) =>
        Log.Warning(exception, message ?? exception.Message);

    // Error level
    public void Error(string message)
    {
        Log.Error(message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Error(Exception exception, string message)
    {
        Log.Error(exception, message ?? exception.Message);
        ErrorLogged?.Invoke(this, new(message));
    }

    // Critical level (maps to Serilog.Fatal)
    public void Critical(string message)
    {
        Log.Fatal(message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Critical(Exception exception, string message)
    {
        Log.Fatal(exception, message ?? exception.Message);
        ErrorLogged?.Invoke(this, new(message));
    }

    // Structured logging: Scopes
    public IDisposable BeginScope<TState>(TState state)
    {
        _state = state;
        _scope?.Dispose();
        _scope = LogContext.PushProperty("Scope", state);
        return _scope;
    }

    public IDisposable BeginLabeledScope(params (string, object)[] state) => 
        BeginLabeledScope(new LoggerState(state));

    public IDisposable BeginLabeledScope(IDictionary<string, object> argsCustom) =>
        BeginLabeledScope(new LoggerState(argsCustom));

    public IDisposable BeginLabeledScope(ILoggerState state) => BeginScope(state);

    public void EndScope()
    {
        _state = null;
        _scope.TryDispose();
        _scope = null;
    }

    // Structured logging: Labels
    public void AddOrUpdateLabel(string key, object value)
    {
        if (_state is not ILoggerState loggerState)
            return;

        loggerState.AddOrUpdateLabel(key, value);
    }

    public void RemoveLabel(string key)
    {
        if (_state is not ILoggerState loggerState)
            return;

        loggerState.RemoveLabel(key);
    }
}