using System;

namespace FEx.Abstractions.Interfaces;

public interface IExceptionHandler
{
    void Handle(Exception exception, IExceptionHandlerOptions options = null);
}