using FEx.Extensions.Collections.Lists;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FEx.Extensions.Collections.Concurrent.Notifiers;

public class CollectionChangeNotifier : ICollectionChangeNotifier
{
    private static NotifyCollectionChangedEventArgs GetArgs(
        NotifyCollectionChangedAction changeAction,
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

        throw new InvalidOperationException($"Specified arguments set don't match ctor of {nameof(NotifyCollectionChangedEventArgs)} for {changeAction} action");
    }

    private PropertyChangedEventHandler _propertyChanged;
    private NotifyCollectionChangedEventHandler _collectionChanged;

    public CollectionChangeNotifier(bool notifyOnCreationContext)
    {
        NotifyOnCreationContext = notifyOnCreationContext;
        Initialize();
    }

    public bool NotifyOnCreationContext { get; protected set; }
    public TaskCompletionSource<bool> CollectionChangedTcs { get; protected set; }
    public TaskCompletionSource<bool> PropertyChangedTcs { get; protected set; }

    protected Thread CreationThread { get; private set; }

    protected SynchronizationContext Context { get; private set; }

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

    public void SetNotifyOnCreationContext(bool notifyOnCreationContext)
    {
        NotifyOnCreationContext = notifyOnCreationContext;
        SetContext();
    }

    public void OnCollectionChanged(object sender, string[] propertyChangedArgs, NotifyCollectionChangedAction changeAction, object changedItem, object oldItem, int? index, int? oldIndex)
    {
        if (Context != null && (_collectionChanged != null || _propertyChanged != null))
        {
            InvokePropertyChangedEvents(sender, propertyChangedArgs);
            InvokeCollectionChangedEvent(sender, changeAction, changedItem, oldItem, index, oldIndex);
        }
    }

    public async Task WaitForCollectionEventsAsync()
    {
        await Task.WhenAll(PropertyChangedTcs?.Task ?? Task.CompletedTask, CollectionChangedTcs?.Task ?? Task.CompletedTask);
    }

    private void Initialize()
    {
        if (CreationThread != Thread.CurrentThread)
        {
            CreationThread = Thread.CurrentThread;
            SetContext();
        }
    }

    private void SetContext()
    {
        if (SynchronizationContextExtensions.MainSyncCtx != null && NotifyOnCreationContext)
        {
            Context = SynchronizationContextExtensions.MainSyncCtx;
        }
        else
        {
            Context = CreationThread.GetThreadSynchronizationContext() ?? SynchronizationContextExtensions.MainSyncCtx;
        }
    }

    private void InvokeCollectionChangedEvent(
        object sender,
        NotifyCollectionChangedAction changeAction,
        object changedItem,
        object oldItem,
        int? index,
        int? oldIndex)
    {
        if (Context != null && _collectionChanged != null)
        {
            NotifyCollectionChangedEventArgs args = GetArgs(changeAction, changedItem, oldItem, index, oldIndex);
            CollectionChangedTcs = new TaskCompletionSource<bool>();
            Context.PostInContext(() => InvokeCollectionChangedEvent(sender, args, CollectionChangedTcs), sender);
        }
    }

    private void InvokeCollectionChangedEvent(object sender, NotifyCollectionChangedEventArgs args, TaskCompletionSource<bool> tcs)
    {
        try
        {
            _collectionChanged?.Invoke(sender, args);
        }
        finally
        {
            tcs.TrySetResult(true);
        }
    }

    private void InvokePropertyChangedEvents(object sender, string[] propertyChangedArgs)
    {
        if (Context != null && _propertyChanged != null && propertyChangedArgs.IsNotNullOrEmptyList())
        {
            PropertyChangedTcs = new TaskCompletionSource<bool>();
            Context.PostInContext(() => InvokePropertyChangedEvent(sender, propertyChangedArgs, PropertyChangedTcs), sender);
        }
    }

    private void InvokePropertyChangedEvent(object sender, string[] propertyChangedArgs, TaskCompletionSource<bool> tcs)
    {
        try
        {
            foreach (string arg in propertyChangedArgs)
            {
                _propertyChanged?.Invoke(sender, new PropertyChangedEventArgs(arg));
            }
        }
        finally
        {
            tcs.TrySetResult(true);
        }
    }
}