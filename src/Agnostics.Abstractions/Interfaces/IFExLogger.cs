using FEx.Agnostics.Abstractions.Flow;
using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>
/// Framework-agnostic logger interface aligned with Microsoft.Extensions.Logging semantics.
/// Provides leveled logging (Trace/Debug/Info/Warning/Error/Critical), structured logging via scopes/labels, and low-level
/// error event hooks.
/// </summary>
public interface IFExLogger
{
    /// <summary>
    /// Event raised when Error or Critical is logged (fail-safe for low-level debugging).
    /// Only fires if subscribers are attached.
    /// </summary>
    event EventHandler<FExErrorEventArgs> ErrorLogged;

    // Trace level (most verbose)
    void Trace(string message);
    void Trace(Exception exception, string message);

    // Debug level
    void Debug(string message);
    void Debug(Exception exception, string message);

    // Information level
    void Information(string message);
    void Information(Exception exception, string message);

    // Warning level
    void Warning(string message);
    void Warning(Exception exception, string message);

    // Error level
    void Error(string message);
    void Error(Exception exception, string message);

    // Critical level (most severe; maps to Serilog.Fatal)
    void Critical(string message);
    void Critical(Exception exception, string message);

    // Structured logging: Scopes
    IDisposable BeginScope<TState>(TState state);
    IDisposable BeginLabeledScope(params (string, object)[] state);
    IDisposable BeginLabeledScope(IDictionary<string, object> argsCustom);
    IDisposable BeginLabeledScope(ILoggerState state);
    void EndScope();

    // Structured logging: Labels (contextual key-value pairs)
    void AddOrUpdateLabel(string key, object value);
    void RemoveLabel(string key);
}