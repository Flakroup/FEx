using System;
using System.Collections.Concurrent;

namespace FEx.MVVM.Abstractions;

public abstract class SubscriberBase
{
    public ConcurrentDictionary<string, IDisposable> Subscriptions { get; }

    protected SubscriberBase()
    {
        Subscriptions = new();
    }
}