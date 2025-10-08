using System;
using System.Collections.Concurrent;

namespace FEx.MVVM.Abstractions.Interfaces;

/// <summary>
/// Has progress status container instance
/// </summary>
/// <typeparam name="T">Type of progress status container instance</typeparam>
public interface IProgressReceiver<out T> : IAttachToContainerReceiver where T : IProgressAggregator
{
    T Progress { get; }
    ConcurrentDictionary<string, IDisposable> Subscriptions { get; }
}