using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using System;
using DebugConsole = System.Diagnostics.Debug;

namespace FEx.Agnostics.Abstractions.Logging;

public class FExDebugLogger : IFExLogger
{
    private const string DateTimeFormat = "s";
    private const string DebugLevel = "Debug";
    private const string InformationLevel = "Information";
    private const string WarningLevel = "Warning";
    private const string ErrorLevel = "Error";
    private const string FatalLevel = "Fatal";

    public event EventHandler<FExErrorEventArgs> ErrorLogged;

    public void Debug(string message) => WriteFormattedMessage(DebugLevel, message);

    public void Debug(Exception exception, string message = null) =>
        WriteFormattedMessage(DebugLevel, message, exception);

    public void Information(string message) => WriteFormattedMessage(InformationLevel, message);

    public void Information(Exception exception, string message = null) =>
        WriteFormattedMessage(InformationLevel, message);

    public void Warning(string message) => WriteFormattedMessage(WarningLevel, message);

    public void Warning(Exception exception, string message = null) =>
        WriteFormattedMessage(WarningLevel, message, exception);

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

    public void Fatal(string message)
    {
        WriteFormattedMessage(FatalLevel, message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Fatal(Exception exception, string message = null)
    {
        WriteFormattedMessage(FatalLevel, message, exception);
        ErrorLogged?.Invoke(this, new(message, exception));
    }

    private static void WriteFormattedMessage(string level, string message, Exception exception = null) =>
        DebugConsole.WriteLine(
            $"{DateTime.Now.ToString(DateTimeFormat)} [{level}] {message}{Environment.NewLine}{(exception is not null ? exception + Environment.NewLine : string.Empty)}");
}