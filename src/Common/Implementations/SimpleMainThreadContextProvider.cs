using FEx.Core.Abstractions.Interfaces;
using System;
using System.Threading;

namespace FEx.Common.Implementations;

public class SimpleMainThreadContextProvider : IMainThreadContextProvider
{
    public event EventHandler<EventArgs> ThreadHasChanged;
    public Thread Thread { get; private set; } = Thread.CurrentThread;
    public bool IsDispatcherContext { get; set; }
    public SynchronizationContext Context { get; }

    public SimpleMainThreadContextProvider()
    {
        Context = SynchronizationContext.Current;
    }

    public void SetMainThread(bool throwOnNonMainThread = true)
    {
        Thread = Thread.CurrentThread;
    }
}