using System;
using System.Collections.Concurrent;

namespace FEx.MVVM.BaseObjects;

public abstract class SubscriberBase : IDisposable
{
    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    protected SubscriberBase()
    {
        Subscriptions = new();
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        foreach (var subscription in Subscriptions.Values)
            subscription?.Dispose();

        Subscriptions.Clear();
    }
    #endregion
}