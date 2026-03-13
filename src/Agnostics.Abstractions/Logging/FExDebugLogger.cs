using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using DebugConsole = System.Diagnostics.Debug;

namespace FEx.Agnostics.Abstractions.Logging;

/// <summary>
/// Simple debug console logger implementation (no structured logging support).
/// </summary>
public class FExDebugLogger : IFExLogger
{
    private const string DateTimeFormat = "s";
    private const string TraceLevel = "Trace";
    private const string DebugLevel = "Debug";
    private const string InformationLevel = "Information";
    private const string WarningLevel = "Warning";
    private const string ErrorLevel = "Error";
    private const string CriticalLevel = "Critical";

    public event EventHandler<FExErrorEventArgs> ErrorLogged;

    // Trace level
    public void Trace(string message) => WriteFormattedMessage(TraceLevel, message);
    public void Trace(Exception exception, string message = null) =>
        WriteFormattedMessage(TraceLevel, message, exception);

    // Debug level
    public void Debug(string message) => WriteFormattedMessage(DebugLevel, message);
    public void Debug(Exception exception, string message = null) =>
        WriteFormattedMessage(DebugLevel, message, exception);

    // Information level
    public void Information(string message) => WriteFormattedMessage(InformationLevel, message);
    public void Information(Exception exception, string message = null) =>
        WriteFormattedMessage(InformationLevel, message, exception);

    // Warning level
    public void Warning(string message) => WriteFormattedMessage(WarningLevel, message);
    public void Warning(Exception exception, string message = null) =>
        WriteFormattedMessage(WarningLevel, message, exception);

    // Error level
    public void Error(string message)
    {
        WriteFormattedMessage(ErrorLevel, message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Error(Exception exception, string message = null)
    {
        WriteFormattedMessage(ErrorLevel, message, exception);
        ErrorLogged?.Invoke(this, new(message, exception));
    }

    // Critical level
    public void Critical(string message)
    {
        WriteFormattedMessage(CriticalLevel, message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Critical(Exception exception, string message = null)
    {
        WriteFormattedMessage(CriticalLevel, message, exception);
        ErrorLogged?.Invoke(this, new(message, exception));
    }

    // Structured logging: Scopes (no-op for debug logger)
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public IDisposable BeginLabeledScope(params (string, object)[] state) => NullScope.Instance;
    public IDisposable BeginLabeledScope(IDictionary<string, object> argsCustom) => NullScope.Instance;
    public IDisposable BeginLabeledScope(ILoggerState state) => NullScope.Instance;
    public void EndScope() { }

    // Structured logging: Labels (no-op for debug logger)
    public void AddOrUpdateLabel(string key, object value) { }
    public void RemoveLabel(string key) { }

    private static void WriteFormattedMessage(string level, string message, Exception exception = null) =>
        DebugConsole.WriteLine(
            $"{DateTime.Now.ToString(DateTimeFormat)} [{level}] {message}{Environment.NewLine}{(exception is not null ? exception + Environment.NewLine : string.Empty)}");

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}