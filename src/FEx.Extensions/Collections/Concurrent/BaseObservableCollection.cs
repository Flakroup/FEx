using FEx.Extensions.Collections.Concurrent.Notifiers;
using System.Collections;
using System.Collections.Specialized;

namespace FEx.Extensions.Collections.Concurrent;

public abstract class BaseObservableCollection<T>
{
    protected readonly bool PassIndexOfRemovedItem;

    [NonSerialized] private ICollectionChangeNotifier _notifier;

    protected BaseObservableCollection(bool passIndexOfRemovedItem = false)
    {
        PassIndexOfRemovedItem = passIndexOfRemovedItem;
    }

    public bool NotifyOnCreationContext { get; protected set; }

    public bool EventsSuppressed { get; protected set; }

    protected ICollectionChangeNotifier Notifier => _notifier ?? (_notifier = new CollectionChangeNotifier(NotifyOnCreationContext));

    public async Task WaitForCollectionEventsAsync()
    {
        if (_notifier != null)
        {
            await Notifier.WaitForCollectionEventsAsync();
        }
    }

    public void SetNotifyOnCreationContext(bool notifyOnCreationContext)
    {
        if (Notifier?.NotifyOnCreationContext != notifyOnCreationContext)
        {
            NotifyOnCreationContext = notifyOnCreationContext;
            Notifier?.SetNotifyOnCreationContext(NotifyOnCreationContext);
        }
    }

    public void SupressEvents()
    {
        EventsSuppressed = true;
    }

    public void ResumeEvents()
    {
        EventsSuppressed = false;
        OnCollectionReset();
    }

    protected abstract string[] GetPropertyChangedArgs();

    protected virtual void OnAddToCollection(T item, int index)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index: index);
    }

    protected virtual void OnAddToCollection(IList<T> items, int index)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Add, items, index: index);
    }

    protected virtual void OnRemoveFromCollection(T item, int index)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index: PassIndexOfRemovedItem ? index : null);
    }

    protected virtual void OnRemoveFromCollection(IList<T> items, int index)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, items, index: PassIndexOfRemovedItem ? index : null);
    }

    protected virtual void OnMoveInCollection(T item, int index, int oldIndex)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Move, item, index: index, oldIndex: oldIndex);
    }

    protected virtual void OnReplaceInCollection(T item, T oldItem, int index)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Move, item, oldItem, index);
    }

    protected virtual void OnCollectionReset()
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Reset, null);
    }

    protected virtual void OnCollectionChanged(IList newItems, IList oldItems)
    {
        OnCollectionChanged(NotifyCollectionChangedAction.Replace, newItems, oldItems);
    }

    protected void OnCollectionChanged(
        NotifyCollectionChangedAction changeAction,
        object changedItem,
        object oldItem = null,
        int? index = null,
        int? oldIndex = null)
    {
        if (EventsSuppressed)
        {
            return;
        }

        Notifier.OnCollectionChanged(this, GetPropertyChangedArgs(), changeAction, changedItem, oldItem, index, oldIndex);
    }
}