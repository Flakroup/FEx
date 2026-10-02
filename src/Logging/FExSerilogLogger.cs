using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Models;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Threading;

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

    // Scopes form a linked stack per async flow, like Serilog's LogContext: concurrent flows never share
    // state, and a disposed scope leaves nothing behind.
    private readonly AsyncLocal<Scope?> _currentScope = new();
    public event EventHandler<FExErrorEventArgs>? ErrorLogged;

    private object? CurrentState => _currentScope.Value?.State;

    private static bool IsSerilogSilent => Log.Logger.GetType().FullName == SilentLoggerTypeName;

    // Trace level
    public void Trace(string message) => Write(LogEventLevel.Verbose, null, message);
    public void Trace(Exception exception, string? message) => Write(LogEventLevel.Verbose, exception, message);

    // Debug level
    public void Debug(string message) => Write(LogEventLevel.Debug, null, message);
    public void Debug(Exception exception, string? message) => Write(LogEventLevel.Debug, exception, message);

    // Information level
    public void Information(string message) => Write(LogEventLevel.Information, null, message);

    public void Information(Exception exception, string? message) =>
        Write(LogEventLevel.Information, exception, message);

    // Warning level
    public void Warning(string message) => Write(LogEventLevel.Warning, null, message);

    public void Warning(Exception exception, string? message) => Write(LogEventLevel.Warning, exception, message);

    // Error level
    public void Error(string message)
    {
        Write(LogEventLevel.Error, null, message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Error(Exception exception, string? message)
    {
        Write(LogEventLevel.Error, exception, message);
        ErrorLogged?.Invoke(this, new(message, exception));
    }

    // Critical level (maps to Serilog.Fatal)
    public void Critical(string message)
    {
        Write(LogEventLevel.Fatal, null, message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Critical(Exception exception, string? message)
    {
        Write(LogEventLevel.Fatal, exception, message);
        ErrorLogged?.Invoke(this, new(message, exception));
    }

    // While Serilog is silent, write to stderr rather than drop the message. Console.Error (unlike
    // Debug.WriteLine) is not compiled out of Release builds.
    private static void Write(LogEventLevel level, Exception? exception, string? message)
    {
        message ??= exception?.Message ?? string.Empty;

        if (IsSerilogSilent)
        {
            Console.Error.WriteLine(
                $"{DateTime.Now:s} [{level}] {message}{(exception is null ? string.Empty : Environment.NewLine + exception)}");

            return;
        }

        if (exception is null)
            Log.Write(level, message);
        else
            Log.Write(level, exception, message);
    }

    // Structured logging: Scopes
    public IDisposable BeginScope<TState>(TState state)
    {
        // LogContext.PushProperty restores the previous context on Dispose, so a nested scope shadows the
        // outer one and hands it back when disposed - the outer scope must stay alive meanwhile.
#pragma warning disable IDISP004 // the scope is stored in _currentScope and returned to the caller, who disposes it
        Scope scope = new(this, _currentScope.Value, state, LogContext.PushProperty("Scope", state));
#pragma warning restore IDISP004
        _currentScope.Value = scope;

        return scope;
    }

    public IDisposable BeginLabeledScope(params (string, object)[] state) => BeginLabeledScope(new LoggerState(state));

    public IDisposable BeginLabeledScope(IDictionary<string, object> argsCustom) =>
        BeginLabeledScope(new LoggerState(argsCustom));

    public IDisposable BeginLabeledScope(ILoggerState state) => BeginScope(state);

    public void EndScope() => _currentScope.Value?.Dispose();

    // Structured logging: Labels
    public void AddOrUpdateLabel(string key, object value)
    {
        if (CurrentState is not ILoggerState loggerState)
            return;

        loggerState.AddOrUpdateLabel(key, value);
    }

    public void RemoveLabel(string key)
    {
        if (CurrentState is not ILoggerState loggerState)
            return;

        loggerState.RemoveLabel(key);
    }

    #region IDisposable
    public void Dispose()
    {
#pragma warning disable IDISP007 // the logger owns the scopes it hands out until they are disposed
        for (var scope = _currentScope.Value; scope is not null; scope = scope.Parent)
            scope.Dispose();
#pragma warning restore IDISP007

        _currentScope.Value = null;
    }
    #endregion

    private sealed class Scope(FExSerilogLogger owner, Scope? parent, object? state, IDisposable context) : IDisposable
    {
        private IDisposable? _context = context;

        public object? State { get; } = state;

        public Scope? Parent { get; } = parent;

        #region IDisposable
        public void Dispose()
        {
            // Only unwind this flow's stack when the scope is on it; a scope disposed from an unrelated flow
            // must not touch that flow's current scope. This also runs for a scope already disposed from
            // another flow, which is still current in its own flow.
            for (var current = owner._currentScope.Value; current is not null; current = current.Parent)
            {
                if (current != this)
                    continue;

                owner._currentScope.Value = Parent;

                break;
            }

            if (_context is null)
                return;

#pragma warning disable IDISP007 // Scope takes ownership of the LogContext restore handle it is constructed with
            _context.Dispose();
#pragma warning restore IDISP007
            _context = null;
        }
        #endregion
    }
}