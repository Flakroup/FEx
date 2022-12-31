using System;
using System.Diagnostics;

namespace FEx.Utilities;

public class DebugExceptionHandler : ExceptionHandlerBase
{
    public override bool CanHandle(Exception exception)
    {
        return true;
    }

    protected override void HandleException(Exception exception)
    {
        Debug.WriteLine(exception.ToString());
    }
}