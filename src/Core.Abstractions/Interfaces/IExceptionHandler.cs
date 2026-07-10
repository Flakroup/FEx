using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.CustomEventArgs;
using System;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Interfaces;

public interface IExceptionHandler
{
    event EventHandler<ExceptionEventArgs>? ExceptionOccured;

    /// <summary>
    /// The last exception
    /// </summary>
    Exception? LastException { get; set; }

    Func<string, bool, Task>? Callback { get; set; }
    bool ConsolePresent { get; }

    void Handle(Exception exception, IExceptionHandlerOptions? options = null);
}