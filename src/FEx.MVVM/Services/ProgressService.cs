using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Helpers;
using FEx.Core.Collections.Concurrent;
using FEx.MVVM.Abstractions.Events;
using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.BaseObjects;
using FEx.MVVM.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace FEx.MVVM.Services;

public sealed class ProgressService : SubscriberBase, IProgressService
{
    private static readonly Lazy<ProgressService> _lazy = new(() => new());

    public static string MainContainerId { get; private set; }

    public static ProgressService Instance => _lazy.Value;

    public ConcurrentDictionary<string, IProgressAggregator> Containers { get; }
    public ConcurrentDictionary<string, ISet<ReceiverDefinition>> Listeners { get; }

    private ProgressService()
    {
        Containers = new();
        Listeners = new();
    }

    public bool SubscribeToProgress<T, TCon>(IProgressReceiver<T> receiver,
                                             IProgressReceiver<TCon> producer,
                                             params string[] iProgressReceiverProperties)
        where T : IProgressAggregator where TCon : IProgressAggregator =>
        SubscribeToProgress(receiver, producer.Progress, iProgressReceiverProperties);

    public bool SubscribeToProgress<TCon>(IProgressReceiver<TCon> receiver,
                                          IProgressAggregator container,
                                          params string[] iProgressReceiverProperties) where TCon : IProgressAggregator
    {
        if (!Containers.ContainsKey(container.Id))
        {
            Containers.TryAddValue(container.Id, () => container);
            AttachContainer(container);
        }

        return SubscribeToProgress(receiver.Progress, container.Id, iProgressReceiverProperties);
    }

    public bool SubscribeToProgress(IProgressAggregator receiver,
                                    string containerId,
                                    params string[] iProgressReceiverProperties)
    {
        var hasBeenAdded = false;
        var def = new ReceiverDefinition(receiver, iProgressReceiverProperties);

        Listeners.AddOrUpdate(containerId,
            _ =>
            {
                var set = new ConcurrentHashSet<ReceiverDefinition>(def.Yield());
                hasBeenAdded = true;

#pragma warning disable IDISP005 // stored in Listeners dictionary, cleanup via DetachContainer
                return set;
#pragma warning restore IDISP005
            },
            (_, v) =>
            {
                if (v.Contains(def))
                    v.Remove(def);

                hasBeenAdded = v.Add(def);

                return v;
            });

        if (hasBeenAdded)
            OnListenerAttached(def, containerId);

        return hasBeenAdded;
    }

    public bool UnsubscribeFromProgress<T, TCon>(IProgressReceiver<T> receiver, IProgressReceiver<TCon> producer)
        where T : IProgressAggregator where TCon : IProgressAggregator =>
        UnsubscribeFromProgress(receiver, producer.Progress);

    public bool UnsubscribeFromProgress<TCon>(IProgressReceiver<TCon> receiver, IProgressAggregator container)
        where TCon : IProgressAggregator =>
        UnsubscribeFromProgress(receiver.Progress, container.Id);

    public bool UnsubscribeFromProgress(IProgressAggregator receiver, string containerId)
    {
        if (!Listeners.TryGetValue(containerId, out var entry))
            return false;

        var def = entry.SingleOrDefault(x => x.Container.Id == receiver.Id);

        if (def is null)
            return false;

        entry.Remove(def);

        return true;
    }

    public TCon GetOrAddContainer<TCon>(bool isMain) where TCon : class, IProgressAggregator, new()
    {
        var container = ProgressStatusContainerFactory<TCon>();
        Containers.GetOrAdd(container.Id, container);

        if (isMain)
            MainContainerId = container.Id;

        return container;
    }

    public void RemoveContainer(string id)
    {
        if (!Containers.TryRemove(id, out var container))
            return;

        DetachContainer(container.Id);
    }

    public TCon GetOrAddContainer<TCon>() where TCon : class, IProgressAggregator, new() =>
        GetOrAddContainer<TCon>(false);

    private static void ReportToListener(ReceiverDefinition def, string propertyName, object value)
    {
        if (def.IsReceivingThisProperty(propertyName))
            def.Container.Report(propertyName, value);
    }

    private TCon ProgressStatusContainerFactory<TCon>() where TCon : class, IProgressAggregator, new()
    {
        var container = new TCon();
        AttachContainer(container);

        return container;
    }

    private void AttachContainer(IProgressAggregator container) =>
        Subscriptions.ReplaceAndDisposeOldValue(container.Id, () => GetSubscription(container));

    private IDisposable GetSubscription(IProgressAggregator container) =>
        Observable
            .FromEventPattern<ProgressPropertyChangedEventHandler, ProgressPropertyChangedEventArgs>(
                h => container.ProgressPropertyChanged += h,
                h => container.ProgressPropertyChanged -= h)
            .Subscribe(x => OnProgressChange(x.EventArgs.ContainerId, x.EventArgs.PropertyName, x.EventArgs.Value));

    private void OnProgressChange(string producerId, string propertyName, object value)
    {
        if (!Listeners.TryGetValue(producerId, out var listeners))
            return;

        listeners.ForEachInEnumerable(l => ReportToListener(l, propertyName, value));
    }

    private void OnListenerAttached(ReceiverDefinition def, string id)
    {
        var properties = Containers[id]
            .AsDictionary(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public);

        properties.ForEachInEnumerable(kv => ReportToListener(def, kv.Key, kv.Value));
    }

    private void DetachContainer(string containerId)
    {
        if (!Subscriptions.TryRemove(containerId, out var subscription))
            return;

        subscription.Dispose();
    }
}