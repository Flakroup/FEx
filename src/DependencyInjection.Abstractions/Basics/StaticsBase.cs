using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace FEx.DependencyInjection.Abstractions.Basics;

public abstract class StaticsBase : IFExInitializable
{
    private static IFExServiceProvider _serviceProvider;

    public static IFExServiceProvider ServiceProvider
    {
        get => _serviceProvider;
        set
        {
            if (_serviceProvider is not null)
            {
                if (!ReferenceEquals(_serviceProvider, value))
                    throw new InvalidOperationException($"{nameof(ServiceProvider)} is already set");

                return;
            }

            _serviceProvider = value;
        }
    }

    protected bool HasBeenInitialized { get; private set; }

    public virtual void Initialize() => HasBeenInitialized = true;

    protected static T Get<T>(Func<T> localFactory, Func<T> fallback = null, [CallerMemberName] string paramName = null)
        where T : class
    {
        try
        {
            if (fallback is not null
                && localFactory is null
                && ServiceProvider is null)
                return fallback().Guard(paramName);

            return localFactory is not null
                ? localFactory().Guard(paramName)
                : ServiceProvider.GetInstance<T>().Guard(paramName);
        }
        catch when (fallback is not null)
        {
            return fallback().Guard(paramName);
        }
    }
}