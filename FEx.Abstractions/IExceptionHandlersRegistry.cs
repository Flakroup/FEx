using System;

namespace FEx.Abstractions
{
    public interface IExceptionHandlersRegistry
    {
        void Handle(Exception exception);
        void Register(IExceptionHandler handler);
    }
}