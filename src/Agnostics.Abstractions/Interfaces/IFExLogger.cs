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
    event EventHandler<FExErrorEventArgs>? ErrorLogged;

    // Trace level (most verbose)
    /// <summary>Writes a trace-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Trace(string message);
    /// <summary>Writes a trace-level message with an exception.</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">An optional message describing the context.</param>
    void Trace(Exception exception, string? message);

    // Debug level
    /// <summary>Writes a debug-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);
    /// <summary>Writes a debug-level message with an exception.</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">An optional message describing the context.</param>
    void Debug(Exception exception, string? message);

    // Information level
    /// <summary>Writes a information-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Information(string message);
    /// <summary>Writes a information-level message with an exception.</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">An optional message describing the context.</param>
    void Information(Exception exception, string? message);

    // Warning level
    /// <summary>Writes a warning-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Warning(string message);
    /// <summary>Writes a warning-level message with an exception.</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">An optional message describing the context.</param>
    void Warning(Exception exception, string? message);

    // Error level
    /// <summary>Writes a error-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Error(string message);
    /// <summary>Writes a error-level message with an exception.</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">An optional message describing the context.</param>
    void Error(Exception exception, string? message);

    // Critical level (most severe; maps to Serilog.Fatal)
    /// <summary>Writes a critical-level message.</summary>
    /// <param name="message">The message to log.</param>
    void Critical(string message);
    /// <summary>Writes a critical-level message with an exception.</summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">An optional message describing the context.</param>
    void Critical(Exception exception, string? message);

    // Structured logging: Scopes
    /// <summary>Begins a logical operation scope that is attached to subsequent log entries.</summary>
    /// <typeparam name="TState">The type of the scope state.</typeparam>
    /// <param name="state">The state to attach to the scope.</param>
    /// <returns>A disposable that ends the scope when disposed.</returns>
    IDisposable BeginScope<TState>(TState state);
    /// <summary>Begins a scope carrying the given key/value labels.</summary>
    /// <param name="state">The label names and values to attach to the scope.</param>
    /// <returns>A disposable that ends the scope when disposed.</returns>
    IDisposable BeginLabeledScope(params (string, object)[] state);
    /// <summary>Begins a scope carrying the labels of a dictionary.</summary>
    /// <param name="argsCustom">The label names and values to attach to the scope.</param>
    /// <returns>A disposable that ends the scope when disposed.</returns>
    IDisposable BeginLabeledScope(IDictionary<string, object> argsCustom);
    /// <summary>Begins a scope carrying the labels of a logger state.</summary>
    /// <param name="state">The label state to attach to the scope.</param>
    /// <returns>A disposable that ends the scope when disposed.</returns>
    IDisposable BeginLabeledScope(ILoggerState state);
    /// <summary>Ends the most recently begun scope.</summary>
    void EndScope();

    // Structured logging: Labels (contextual key-value pairs)
    /// <summary>Adds a contextual label to subsequent log entries, replacing an existing label with the same key.</summary>
    /// <param name="key">The label name.</param>
    /// <param name="value">The label value.</param>
    void AddOrUpdateLabel(string key, object value);
    /// <summary>Removes a contextual label.</summary>
    /// <param name="key">The label name.</param>
    void RemoveLabel(string key);
}