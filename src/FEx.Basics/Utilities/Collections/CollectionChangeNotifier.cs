using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Extensions;
using FEx.Extensions.Collections.Lists;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Basics.Utilities.Collections;

public class CollectionChangeNotifier : ICollectionChangeNotifier
{
    private PropertyChangedEventHandler _propertyChanged;
    private NotifyCollectionChangedEventHandler _collectionChanged;

    /// <summary>
    ///     Event raised when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => _collectionChanged += value;
        remove => _collectionChanged -= value;
    }

    /// <summary>
    ///     Event raised when a property on the collection changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged
    {
        add => _propertyChanged += value;
        remove => _propertyChanged -= value;
    }

    public bool NotifyOnCreationContext { get; protected set; }
    public TaskCompletionSource<bool> CollectionChangedTcs { get; protected set; }
    public TaskCompletionSource<bool> PropertyChangedTcs { get; protected set; }

    public bool UseDispatcherContext { get; private set; }

    public CollectionChangeNotifier(bool notifyOnCreationContext)
    {
        NotifyOnCreationContext = notifyOnCreationContext;
    }

    public void SetNotifyOnCreationContext(bool notifyOnCreationContext)
    {
        NotifyOnCreationContext = notifyOnCreationContext;
    }

    public void OnCollectionChanged(object sender,
                                    string[] propertyChangedArgs,
                                    NotifyCollectionChangedAction changeAction,
                                    object changedItem,
                                    object oldItem,
                                    int? index,
                                    int? oldIndex,
                                    bool sendAsync)
    {
        if (_propertyChanged is not null)
            InvokePropertyChangedEvents(sender, propertyChangedArgs);

        if (_collectionChanged is not null)
            InvokeCollectionChangedEvent(sender, changeAction, changedItem, oldItem, index, oldIndex);
    }

    public async Task WaitForCollectionEventsAsync()
    {
        await Task.WhenAll(PropertyChangedTcs?.Task ?? Task.CompletedTask,
            CollectionChangedTcs?.Task ?? Task.CompletedTask);
    }

    public void SetUseDispatcherContext(bool useDispatcherContext)
    {
        UseDispatcherContext = useDispatcherContext;
    }

    private static NotifyCollectionChangedEventArgs GetArgs(NotifyCollectionChangedAction changeAction,
                                                            object changedItem,
                                                            object oldItem,
                                                            int? index,
                                                            int? oldIndex)
    {
        switch (changeAction)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
                return new NotifyCollectionChangedEventArgs(changeAction, changedItem, index ?? -1);
            case NotifyCollectionChangedAction.Move:
                return new NotifyCollectionChangedEventArgs(changeAction, changedItem, index ?? -1, oldIndex ?? -1);
            case NotifyCollectionChangedAction.Replace:
                return new NotifyCollectionChangedEventArgs(changeAction, changedItem, oldItem, index ?? -1);
            case NotifyCollectionChangedAction.Reset:
                return new NotifyCollectionChangedEventArgs(changeAction);
        }

        throw new InvalidOperationException(
            $"Specified arguments set don't match ctor of {nameof(NotifyCollectionChangedEventArgs)} for {changeAction} action");
    }

    private void InvokeCollectionChangedEvent(object sender,
                                              NotifyCollectionChangedAction changeAction,
                                              object changedItem,
                                              object oldItem,
                                              int? index,
                                              int? oldIndex)
    {
        if (_collectionChanged is null)
            return;

        NotifyCollectionChangedEventArgs args = GetArgs(changeAction, changedItem, oldItem, index, oldIndex);
        CollectionChangedTcs = new TaskCompletionSource<bool>();
        void EventDelegate() => InvokeCollectionChangedEvent(sender, args, CollectionChangedTcs);

        SynchronizationContext context = GetSynchronizationContext();

        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, sender, context);
    }

    private SynchronizationContext GetSynchronizationContext()
    {
        if (!NotifyOnCreationContext)
            return null;

        SynchronizationContext context;

        do
        {
            context = FExBasics.MainSynchronizationContext;

            if (UseDispatcherContext && !FExBasics.IsDispatcherContext)
                context = null;//todo timeout
        } while (context is null);

        return context;
    }

    private void InvokeCollectionChangedEvent(object sender,
                                              NotifyCollectionChangedEventArgs args,
                                              TaskCompletionSource<bool> tcs)
    {
        try
        {
            _collectionChanged.HandleCollectionChanged(sender, args);
        }
        finally
        {
            tcs.TrySetResult(true);
        }
    }

    private void InvokePropertyChangedEvents(object sender, IList<string> propertyChangedArgs)
    {
        if (_propertyChanged is null
            || !propertyChangedArgs.IsNotNullOrEmptyList())
            return;

        PropertyChangedTcs = new TaskCompletionSource<bool>();
        void EventDelegate() => InvokePropertyChangedEvent(sender, propertyChangedArgs, PropertyChangedTcs);

        SynchronizationContext context = GetSynchronizationContext();

        FExBasics.EventDeliverer.DeliverEvent(EventDelegate, sender, context);
    }

    private void InvokePropertyChangedEvent(object sender,
                                            IEnumerable<string> propertyChangedArgs,
                                            TaskCompletionSource<bool> tcs)
    {
        try
        {
            foreach (string propertyName in propertyChangedArgs)
                _propertyChanged.HandlePropertyChanged(sender, propertyName);
        }
        finally
        {
            tcs.TrySetResult(true);
        }
    }
}