using System;
using System.Collections.Generic;

namespace FEx.Basics.Utilities;

public abstract class FExSingleton : IDisposable
{
    private static readonly List<FExSingleton> Singletons = [];

    protected FExSingleton()
    {
        lock (Singletons)
            Singletons.Add(this);
    }

    public static void ClearAllSingletons()
    {
        lock (Singletons)
        {
            foreach (FExSingleton s in Singletons)
                s.Dispose();

            Singletons.Clear();
        }
    }

    ~FExSingleton()
    {
        Dispose(false);
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool isDisposing);
    #endregion
}

public abstract class FExSingleton<T> : FExSingleton where T : class, new()
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance is null)
                lock (SyncRoot)
                    _instance ??= new();

            return _instance;
        }
    }

    private static object SyncRoot { get; } = new();

    #region IDisposable
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
            _instance = null;
    }
    #endregion
}