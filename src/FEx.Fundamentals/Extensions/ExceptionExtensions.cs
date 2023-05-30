using System;

namespace FEx.Fundamentals.Extensions;

public static class ExceptionExtensions
{
    public static void HandleException(this Exception exception)
    {
        Foundation.ExceptionHandler.Handle(exception);
    }
}