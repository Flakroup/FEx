using System;
using System.Collections.Generic;

namespace FEx.Agnostics.Utilities;

/// <summary>Base class that tracks every singleton instance so they can all be disposed together.</summary>
public abstract class FExSingleton : IDisposable
{
    private static readonly List<FExSingleton> _singletons = [];

    /// <summary>Registers the new instance in the global singleton list.</summary>
    protected FExSingleton()
    {
        lock (_singletons)
            _singletons.Add(this);
    }

    /// <summary>Disposes every registered singleton and clears the registry.</summary>
    public static void ClearAllSingletons()
    {
        lock (_singletons)
        {
            foreach (var s in _singletons)
                s.Dispose();

            _singletons.Clear();
        }
    }

    /// <summary>Releases unmanaged resources when the instance is finalized.</summary>
    ~FExSingleton()
    {
        Dispose(false);
    }

    #region IDisposable
    /// <summary>Disposes the singleton and suppresses finalization.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the resources held by the singleton.</summary>
    /// <param name="isDisposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> when called from the finalizer.</param>
    protected abstract void Dispose(bool isDisposing);
    #endregion
}

/// <summary>Base class for a lazily created, thread-safe singleton of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The singleton type, which must have a parameterless constructor.</typeparam>
public abstract class FExSingleton<T> : FExSingleton where T : class, new()
{
    private static T? _instance;

    /// <summary>Gets the shared instance, creating it on first access.</summary>
    public static T Instance
    {
        get
        {
            if (_instance is null)
                lock (SyncRoot)
                    _instance ??= new();

            // Non-null here: either already set, or just created under the lock above.
            return _instance!;
        }
    }

    private static object SyncRoot { get; } = new();

    #region IDisposable
    /// <summary>Clears the shared instance when disposing so a new one is created on next access.</summary>
    /// <param name="isDisposing"><see langword="true"/> when called from <see cref="FExSingleton.Dispose()"/>; <see langword="false"/> when called from the finalizer.</param>
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
            _instance = null;
    }
    #endregion
}