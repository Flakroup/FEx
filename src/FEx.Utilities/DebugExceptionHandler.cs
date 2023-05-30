using System;
using System.Diagnostics;

namespace FEx.Utilities;

public class DebugExceptionHandler : ExceptionHandlerBase
{
    protected override void HandleException(Exception exception)
    {
        Debug.WriteLine(exception.ToString());
    }
}