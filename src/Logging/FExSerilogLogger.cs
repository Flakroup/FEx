using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
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
#pragma warning disable IDISP025 // logger, may be extended
public class FExSerilogLogger : IFExLogger, IDisposable
#pragma warning restore IDISP025
{
    // Serilog's own SilentLogger (internal) is what Log.Logger holds before UseSerilog's host is built and after
    // it is disposed; anything written to it vanishes.
    private const string SilentLoggerTypeName = "Serilog.Core.Pipeline.SilentLogger";

    private readonly FExDebugLogger _fallback = new();
    private readonly List<Scope> _scopes = [];
    public event EventHandler<FExErrorEventArgs>? ErrorLogged;

    private object? _state => _scopes.Count > 0 ? _scopes[^1].State : null;

    private static bool IsSerilogSilent => Log.Logger.GetType().FullName == SilentLoggerTypeName;

    // Trace level
    public void Trace(string message)
    {
        if (IsSerilogSilent)
            _fallback.Trace(message);
        else
            Log.Verbose(message);
    }

    public void Trace(Exception exception, string? message)
    {
        if (IsSerilogSilent)
            _fallback.Trace(exception, message ?? exception.Message);
        else
            Log.Verbose(exception, message ?? exception.Message);
    }

    // Debug level
    public void Debug(string message)
    {
        if (IsSerilogSilent)
            _fallback.Debug(message);
        else
            Log.Debug(message);
    }

    public void Debug(Exception exception, string? message)
    {
        if (IsSerilogSilent)
            _fallback.Debug(exception, message ?? exception.Message);
        else
            Log.Debug(exception, message ?? exception.Message);
    }

    // Information level
    public void Information(string message)
    {
        if (IsSerilogSilent)
            _fallback.Information(message);
        else
            Log.Information(message);
    }

    public void Information(Exception exception, string? message)
    {
        if (IsSerilogSilent)
            _fallback.Information(exception, message ?? exception.Message);
        else
            Log.Information(exception, message ?? exception.Message);
    }

    // Warning level
    public void Warning(string message)
    {
        if (IsSerilogSilent)
            _fallback.Warning(message);
        else
            Log.Warning(message);
    }

    public void Warning(Exception exception, string? message)
    {
        if (IsSerilogSilent)
            _fallback.Warning(exception, message ?? exception.Message);
        else
            Log.Warning(exception, message ?? exception.Message);
    }

    // Error level
    public void Error(string message)
    {
        if (IsSerilogSilent)
            _fallback.Error(message);
        else
            Log.Error(message);

        ErrorLogged?.Invoke(this, new(message));
    }

    public void Error(Exception exception, string? message)
    {
        if (IsSerilogSilent)
            _fallback.Error(exception, message ?? exception.Message);
        else
            Log.Error(exception, message ?? exception.Message);

        ErrorLogged?.Invoke(this, new(message, exception));
    }

    // Critical level (maps to Serilog.Fatal)
    public void Critical(string message)
    {
        if (IsSerilogSilent)
            _fallback.Critical(message);
        else
            Log.Fatal(message);

        ErrorLogged?.Invoke(this, new(message));
    }

    public void Critical(Exception exception, string? message)
    {
        if (IsSerilogSilent)
            _fallback.Critical(exception, message ?? exception.Message);
        else
            Log.Fatal(exception, message ?? exception.Message);

        ErrorLogged?.Invoke(this, new(message, exception));
    }

    // Structured logging: Scopes
    public IDisposable BeginScope<TState>(TState state)
    {
        // LogContext.PushProperty restores the previous context on Dispose, so a nested scope shadows the
        // outer one and hands it back when disposed - the outer scope must stay alive meanwhile.
#pragma warning disable IDISP004 // the scope is tracked in _scopes and returned to the caller, who disposes it
        var scope = new Scope(this, state, LogContext.PushProperty("Scope", state));
#pragma warning restore IDISP004
        _scopes.Add(scope);

        return scope;
    }

    public IDisposable BeginLabeledScope(params (string, object)[] state) => BeginLabeledScope(new LoggerState(state));

    public IDisposable BeginLabeledScope(IDictionary<string, object> argsCustom) =>
        BeginLabeledScope(new LoggerState(argsCustom));

    public IDisposable BeginLabeledScope(ILoggerState state) => BeginScope(state);

    public void EndScope()
    {
        if (_scopes.Count > 0)
            _scopes[^1].Dispose();
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

    #region IDisposable
    public void Dispose()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
    }
    #endregion

    private sealed class Scope(FExSerilogLogger owner, object? state, IDisposable context) : IDisposable
    {
        private IDisposable? _context = context;

        public object? State { get; } = state;

        #region IDisposable
        public void Dispose()
        {
            if (_context is null)
                return;

            owner._scopes.Remove(this);
#pragma warning disable IDISP007 // Scope takes ownership of the LogContext restore handle it is constructed with
            _context.Dispose();
#pragma warning restore IDISP007
            _context = null;
        }
        #endregion
    }
}