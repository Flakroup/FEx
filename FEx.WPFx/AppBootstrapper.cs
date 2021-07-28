using FEx.Abstractions;
using FEx.DependencyInjection;
using FEx.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Threading;

namespace FEx.WPFx
{
    public class AppBootstrapper : Application
    {
        protected AppBootstrapper()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
            DispatcherUnhandledException += OnAppDispatcherUnhandledException;
        }

        protected virtual void ConfigureServiceProvider()
        {
            FExIoCProvider.IoCProvider.ConfigureServiceProvider(ConfigureServices);
        }

        protected virtual IServiceCollection ConfigureServices(IServiceCollection services) => services;

        private void OnAppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                HandleAppException(e.Exception);
            }

            e.Handled = true;
        }

        private void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleAppException(exception);
            }
        }

        protected virtual void HandleAppException(Exception exception)
        {
            IExceptionHandlersRegistry registry = GetExceptionsHandlerRegistry();
            registry.Handle(exception);
        }

        protected virtual IExceptionHandlersRegistry GetExceptionsHandlerRegistry()
        {
            var registry = FExIoCProvider.IoCProvider.GetService<IExceptionHandlersRegistry>();

            if (registry is null)
            {
                throw new FExException($"There is no implementation of {nameof(IExceptionHandlersRegistry)} registered in IoC provider");
            }

            return registry;
        }
    }
}
