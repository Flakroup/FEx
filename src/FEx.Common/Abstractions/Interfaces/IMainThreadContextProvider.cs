using System;
using System.Threading;

namespace FEx.Common.Abstractions.Interfaces;

public interface IMainThreadContextProvider
{
    event EventHandler<EventArgs> ThreadHasChanged;
    Thread Thread { get; }
    bool IsDispatcherContext { get; set; }
    SynchronizationContext Context { get; }
    void SetMainThread(bool throwOnNonMainThread = true);
}