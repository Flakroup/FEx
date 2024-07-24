using FEx.Basics.Abstractions.Interfaces.Collections;
using FEx.Basics.Utilities.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace FEx.Basics.Abstractions.Collections;

public abstract class BaseObservableCollection<T>
{
    protected readonly bool _sendAsyncEvents;
    protected readonly bool _passIndexOfRemovedItem;

    [NonSerialized] private ICollectionChangeNotifier _notifier;
    private bool _useDispatcherContext;

    public bool UseDispatcherContext
    {
        get => _useDispatcherContext;
        set
        {
            _useDispatcherContext = value;
            Notifier.SetUseDispatcherContext(value);
        }
    }

    public bool NotifyOnCreationContext { get; protected set; }

    public bool EventsSuppressed { get; protected set; }

    protected ICollectionChangeNotifier Notifier => _notifier ??= new CollectionChangeNotifier(NotifyOnCreationContext);

    protected BaseObservableCollection(bool passIndexOfRemovedItem = false, bool sendAsyncEvents = true)
    {
        _passIndexOfRemovedItem = passIndexOfRemovedItem;
        _sendAsyncEvents = sendAsyncEvents;
    }

    public async Task WaitForCollectionEventsAsync()
    {
        if (_notifier is not null)
            await Notifier.WaitForCollectionEventsAsync();
    }

    public void SetNotifyOnCreationContext(bool notifyOnCreationContext)
    {
        if (Notifier?.NotifyOnCreationContext != notifyOnCreationContext)
        {
            NotifyOnCreationContext = notifyOnCreationContext;
            Notifier?.SetNotifyOnCreationContext(NotifyOnCreationContext);
        }
    }

    public void SupressEvents() => EventsSuppressed = true;

    public void ResumeEvents()
    {
        EventsSuppressed = false;
        OnCollectionReset();
    }

    protected abstract string[] GetPropertyChangedArgs();

    protected virtual void OnAddToCollection(T item, int index) => OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index: index);

    protected virtual void OnAddToCollection(IList<T> items, int index) => OnCollectionChanged(NotifyCollectionChangedAction.Add, items, index: index);

    protected virtual void OnRemoveFromCollection(T item, int index) => OnCollectionChanged(NotifyCollectionChangedAction.Remove,
            item,
            index: _passIndexOfRemovedItem
                ? index
                : null);

    protected virtual void OnRemoveFromCollection(IList<T> items, int index) => OnCollectionChanged(NotifyCollectionChangedAction.Remove,
            items,
            index: _passIndexOfRemovedItem
                ? index
                : null);

    protected virtual void OnMoveInCollection(T item, int index, int oldIndex) => OnCollectionChanged(NotifyCollectionChangedAction.Move, item, index: index, oldIndex: oldIndex);

    protected virtual void OnReplaceInCollection(T item, T oldItem, int index) => OnCollectionChanged(NotifyCollectionChangedAction.Move, item, oldItem, index);

    protected virtual void OnCollectionReset() => OnCollectionChanged(NotifyCollectionChangedAction.Reset, null);

    protected virtual void OnCollectionChanged(IList newItems, IList oldItems) => OnCollectionChanged(NotifyCollectionChangedAction.Replace, newItems, oldItems);

    protected void OnCollectionChanged(NotifyCollectionChangedAction changeAction,
                                       object changedItem,
                                       object oldItem = null,
                                       int? index = null,
                                       int? oldIndex = null,
                                       string[] propertyChangedArgs = null)
    {
        if (EventsSuppressed)
            return;

        Notifier.OnCollectionChanged(this,
            propertyChangedArgs ?? GetPropertyChangedArgs(),
            changeAction,
            changedItem,
            oldItem,
            index,
            oldIndex,
            _sendAsyncEvents);
    }
}