using System;
using System.Collections.Concurrent;

namespace FEx.MVVM;

public abstract class SubscriberBase
{
    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    protected SubscriberBase()
    {
        Subscriptions = new();
    }
}