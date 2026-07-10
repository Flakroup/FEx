using FEx.Core.Abstractions.Interfaces;
using System;
using System.Threading;

namespace FEx.Common.Implementations;

public class SimpleMainThreadContextProvider : IMainThreadContextProvider
{
#pragma warning disable CS0067 // Required by IMainThreadContextProvider interface
    public event EventHandler<EventArgs>? ThreadHasChanged;
#pragma warning restore CS0067
    public Thread Thread { get; private set; } = Thread.CurrentThread;
    public bool IsDispatcherContext { get; set; }
    public SynchronizationContext Context { get; }

    public SimpleMainThreadContextProvider()
    {
        // IMainThreadContextProvider.Context is non-nullable, but SynchronizationContext.Current is
        // genuinely null off a UI thread. Preserve legacy behavior (may be null); every consumer
        // (e.g. MainThreadDispatcher) already accesses Context via null-conditional `?.`.
        Context = SynchronizationContext.Current!;
    }

    public void SetMainThread(bool throwOnNonMainThread = true)
    {
        Thread = Thread.CurrentThread;
    }
}