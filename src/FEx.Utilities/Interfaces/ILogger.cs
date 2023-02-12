using System;

namespace FEx.Utilities.Interfaces;

public interface ILogger
{
    void Log(Exception ex);
    void LogInfo(string message);
}