using System;

namespace FEx.Utilities.Interfaces;

public interface IFExLogger
{
    void LogInformation(string message);
    void LogError(string message, Exception exception = null);
    void LogError(Exception exception);
}