using FEx.Utilities.Interfaces;
using System;
using System.Text;

namespace FEx.Logging;

public class FExLogger : IFExLogger
{
    public void LogInformation(string message)
    {
        Console.WriteLine($"[{DateTime.Now}] {message}");
    }

    public void LogError(string message, Exception exception = null)
    {
        message = message.TrimEnd();
        StringBuilder sb = new StringBuilder($"[{DateTime.Now}] [ERROR]\t").AppendLine(message);
        if (exception is not null)
            sb.AppendLine(exception.ToString());
        Console.Write(sb.ToString());
    }

    public void LogError(Exception exception)
    {
        LogError(null, exception);
    }
}