using System;

namespace FEx.Utilities.Interfaces;

public interface ILogger
{
    void LogInformation(string message);
    void LogError(string message, Exception exception = null);
    void LogError(Exception exception);
}