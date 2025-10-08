using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using Serilog;
using System;

namespace FEx.Logging;

public class FExSerilogLogger : IFExLogger
{
    public event EventHandler<FExErrorEventArgs> ErrorLogged;
    public void Debug(string message) => Log.Debug(message);

    public void Debug(Exception exception, string message = null) => Log.Debug(exception, message ?? exception.Message);

    public void Information(string message) => Log.Information(message);

    public void Information(Exception exception, string message = null) =>
        Log.Information(exception, message ?? exception.Message);

    public void Warning(string message) => Log.Warning(message);

    public void Warning(Exception exception, string message = null) =>
        Log.Warning(exception, message ?? exception.Message);

    public void Error(string message)
    {
        Log.Error(message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Error(Exception exception, string message = null)
    {
        Log.Error(exception, message ?? exception.Message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Fatal(string message)
    {
        Log.Fatal(message);
        ErrorLogged?.Invoke(this, new(message));
    }

    public void Fatal(Exception exception, string message = null)
    {
        Log.Fatal(exception, message ?? exception.Message);
        ErrorLogged?.Invoke(this, new(message));
    }
}