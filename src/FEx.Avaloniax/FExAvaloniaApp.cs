using Avalonia;
using Avalonia.Data;
using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Basics.Extensions;
using FEx.DependencyInjection;
using FEx.DI.Abstractions;
using FEx.Fundamentals;
using FEx.Fundamentals.Models;
using System;

namespace FEx.Avaloniax;

public abstract class FExAvaloniaApp<TContainer> : Application
    where TContainer : class, IFExFundamentalsModule, IDisposable, new()
{
    public TContainer Container { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FExAvaloniaApp{TContainer}" /> class.
    /// </summary>
    protected FExAvaloniaApp(IAppInfo appInfo = null)
    {
        try
        {
            FExServiceProvider.Initialize<TContainer, FExStrongInjectServiceProvider>();
            appInfo ??= new AppInfo();
            appInfo.IsUIApp = true;
            FExFoundation.AppInfoProvider.Initialize(appInfo);
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
        bool bindingException = exception is BindingChainException;
        exception.HandleException(!bindingException);
    }
}