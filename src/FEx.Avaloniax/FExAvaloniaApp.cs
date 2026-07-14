using Avalonia;
using Avalonia.Data;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using FEx.DependencyInjection.Abstractions;
using System;

namespace FEx.Avaloniax;

public abstract class FExAvaloniaApp<TContainer> : Application
    where TContainer : class, IFExContainer, IDisposable, new()
{
    // Never assigned in this base type - the container lifetime is owned by FExServiceProvider; exposed here only as an
    // optional hook for derived apps, hence nullable.
    public TContainer? Container { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FExAvaloniaApp{TContainer}" /> class.
    /// </summary>
    protected FExAvaloniaApp()
    {
        try
        {
            // VSTHRD002: synchronous wait is required in this constructor bootstrap entry point;
            // exceptions are handled by the catch below.
#pragma warning disable VSTHRD002
            FExServiceProvider.InitializeAsync<TContainer>().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            OnActivation();
        }
        catch (Exception ex)
        {
            HandleCriticalException(ex);
        }
    }

    protected virtual void OnActivation() => AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;

    protected virtual void HandleCriticalException(Exception ex) => ex.HandleException(true, true);

    protected virtual void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            HandleAppException(exception);
    }

    protected virtual void HandleAppException(Exception exception)
    {
        var bindingException = exception is BindingChainException;
        exception.HandleException(!bindingException);
    }
}