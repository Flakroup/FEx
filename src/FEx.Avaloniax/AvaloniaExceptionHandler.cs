using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.CustomEventArgs;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Avaloniax;

public class AvaloniaExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public event EventHandler<ExceptionEventArgs> ExceptionOccured;

    /// <inheritdoc />
    public Exception LastException { get; set; }

    /// <inheritdoc />
    public Func<string, bool, Task> Callback { get; set; }

    /// <inheritdoc />
    public bool ConsolePresent { get; }

    public void Handle(Exception exception, IExceptionHandlerOptions options = null) =>
        Console.WriteLine(exception.ToString());
}