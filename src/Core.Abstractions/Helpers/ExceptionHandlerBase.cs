using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.CustomEventArgs;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Threading.Tasks;

namespace FEx.Core.Abstractions.Helpers;

public abstract class ExceptionHandlerBase : IExceptionHandler
{
    private bool? _consolePresent;

    /// <inheritdoc />
    public abstract event EventHandler<ExceptionEventArgs> ExceptionOccured;

    /// <inheritdoc />
    public Exception LastException { get; set; }

    /// <inheritdoc />
    public Func<string, bool, Task> Callback { get; set; }

    /// <inheritdoc />
    public bool ConsolePresent
    {
        get
        {
            if (_consolePresent is null)
            {
                _consolePresent = true;

                try
                {
                    _ = Console.WindowHeight;
                }
                catch
                {
                    _consolePresent = false;
                }
            }

            return _consolePresent.Value;
        }
    }

    public virtual void Handle(Exception exception, IExceptionHandlerOptions options = null) =>
        HandleException(exception, options);

    protected abstract void HandleException(Exception exception, IExceptionHandlerOptions options);
}