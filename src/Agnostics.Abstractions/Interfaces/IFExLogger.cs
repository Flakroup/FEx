using FEx.Agnostics.Abstractions.Flow;
using System;

namespace FEx.Agnostics.Abstractions.Interfaces;

public interface IFExLogger
{
    event EventHandler<FExErrorEventArgs> ErrorLogged;

    void Debug(string message);
    void Debug(Exception exception, string message = null);
    void Information(string message);
    void Information(Exception exception, string message = null);
    void Warning(string message);
    void Warning(Exception exception, string message = null);
    void Error(string message);
    void Error(Exception exception, string message = null);
    void Fatal(string message);
    void Fatal(Exception exception, string message = null);
}