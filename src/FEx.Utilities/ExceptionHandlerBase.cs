using FEx.Abstractions;
using System;

namespace FEx.Utilities;

public abstract class ExceptionHandlerBase : IExceptionHandler
{
    public abstract bool CanHandle(Exception exception);

    public void Handle(Exception exception)
    {
        if (CanHandle(exception))
            HandleException(exception);
    }

    protected abstract void HandleException(Exception exception);
}