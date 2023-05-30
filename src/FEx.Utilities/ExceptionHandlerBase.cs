using FEx.Abstractions;
using System;

namespace FEx.Utilities;

public abstract class ExceptionHandlerBase : IExceptionHandler
{
    public void Handle(Exception exception, object options)
    {
        HandleException(exception);
    }

    protected abstract void HandleException(Exception exception);
}