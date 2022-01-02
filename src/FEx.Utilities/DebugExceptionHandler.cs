using FEx.Abstractions;
using System.Diagnostics;

namespace FEx.Utilities;

public class DebugExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception)
    {
        return true;
    }

    public void Handle(Exception exception)
    {
        Debug.WriteLine(exception.ToString());
    }
}