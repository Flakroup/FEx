using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Common.Abstractions.Interfaces;
using FEx.MVVM.Rx.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace FEx.MVVM.Rx.Utilities;

public sealed class StatusHub : IDisposable, IStatusHub
{
    public EventHandler<(Guid key, string status)>? StatusAdded;
    public EventHandler<(Guid key, string status)>? StatusRemoved;
    public EventHandler<EventArgs>? Reset;

    private const int StatusAddedKind = 0;
    private const int StatusRemovedKind = 1;
    private const int ResetKind = 2;

    private readonly ConcurrentDictionary<(int kind, Delegate handler), Delegate> _subscriptions = new();

    public Guid Key { get; }
    private ConcurrentDictionary<Guid, string> Statuses { get; }

    private IDisposableProgress<(Guid key, string status, NotifyCollectionChangedAction action)> StatusChange { get; }

    public StatusHub(Guid key)
        : this(key, null, null, null)
    {
    }

    public StatusHub(Guid key,
                     Action<Guid, string>? onStatusAdded,
                     Action<Guid, string>? onStatusRemoved,
                     Action? onStatusesReset)
    {
        Key = key;
        Statuses = new();

        StatusChange =
            ObservableProgress<(Guid key, string status, NotifyCollectionChangedAction action)>.Create(StatusChanged,
                p => p);

        AttachToStatusChanges(onStatusAdded, onStatusRemoved, onStatusesReset);
    }

    public void AttachToStatusChanges(Action<Guid, string>? onStatusAdded,
                                      Action<Guid, string>? onStatusRemoved,
                                      Action? onStatusesReset)
    {
        if (onStatusAdded is not null)
        {
            EventHandler<(Guid key, string status)> handler = (_, s) => onStatusAdded(s.key, s.status);
            _subscriptions[(StatusAddedKind, onStatusAdded)] = handler;
            StatusAdded += handler;
        }

        if (onStatusRemoved is not null)
        {
            EventHandler<(Guid key, string status)> handler = (_, s) => onStatusRemoved(s.key, s.status);
            _subscriptions[(StatusRemovedKind, onStatusRemoved)] = handler;
            StatusRemoved += handler;
        }

        if (onStatusesReset is not null)
        {
            EventHandler<EventArgs> handler = (_, _) => onStatusesReset();
            _subscriptions[(ResetKind, onStatusesReset)] = handler;
            Reset += handler;
        }
    }

    /// <summary>
    /// Removes handlers previously registered with <see cref="AttachToStatusChanges"/>, so a
    /// short-lived subscriber is not kept alive by this hub. Unknown handlers are ignored.
    /// </summary>
    public void DetachFromStatusChanges(Action<Guid, string>? onStatusAdded,
                                        Action<Guid, string>? onStatusRemoved,
                                        Action? onStatusesReset)
    {
        if (onStatusAdded is not null
            && _subscriptions.TryRemove((StatusAddedKind, onStatusAdded), out var added))
            StatusAdded -= (EventHandler<(Guid key, string status)>)added;

        if (onStatusRemoved is not null
            && _subscriptions.TryRemove((StatusRemovedKind, onStatusRemoved), out var removed))
            StatusRemoved -= (EventHandler<(Guid key, string status)>)removed;

        if (onStatusesReset is not null
            && _subscriptions.TryRemove((ResetKind, onStatusesReset), out var reset))
            Reset -= (EventHandler<EventArgs>)reset;
    }

    public Guid AddStatus(string status, bool unique)
    {
        Guid? key = null;

        if (unique)
            foreach (var s in Statuses)
            {
                if (s.Value == status)
                {
                    key = s.Key;

                    break;
                }
            }

        key ??= Guid.NewGuid();

        if (Statuses.TryAddValue(key.Value, () => status))
            StatusChange?.Report((key.Value, status, NotifyCollectionChangedAction.Add));

        return key.Value;
    }

    public void RemoveStatus(Guid key)
    {
        if (Statuses.ContainsKey(key)
            && Statuses.TryRemove(key, out var v))
            StatusChange?.Report((key, v, NotifyCollectionChangedAction.Remove));
    }

    public void CleanStatuses()
    {
        if (!Statuses.IsEmpty)
        {
            Statuses.Clear();
            StatusChange?.Report((Guid.Empty, string.Empty, NotifyCollectionChangedAction.Reset));
        }
    }

    public IList<string> GetStatuses() => Statuses.Values.ToArray();

    public string GetStatusString(string? separator)
    {
        if (Statuses.IsEmpty)
            return string.Empty;

        if (Statuses.Count < 100)
            return string.Join(separator ?? string.Empty, Statuses.Values);

        var sb = new StringBuilder();

        var statuses = GetStatuses();

        for (var i = 0; i < statuses.Count; i++)
        {
            sb.Append(statuses[i]);

            if (i != statuses.Count - 1)
                sb.Append(separator);
        }

        return sb.ToString();
    }

    public DisposableAction Log(string status, bool unique)
    {
        var statusKey = AddStatus(status, unique);

        return new(() => RemoveStatus(statusKey));
    }

    public Guid AddStatus(string status) => AddStatus(status, true);

    public string GetStatusString() => GetStatusString(null);

    public DisposableAction Log(string status) => Log(status, true);

    private void StatusChanged((Guid key, string status, NotifyCollectionChangedAction action) v) =>
        StatusChanged(v.key, v.status, v.action);

    private void StatusChanged(Guid key, string status, NotifyCollectionChangedAction action)
    {
        switch (action)
        {
            case NotifyCollectionChangedAction.Add:
                StatusAdded?.Invoke(this, (key, status));

                break;
            case NotifyCollectionChangedAction.Remove:
                StatusRemoved?.Invoke(this, (key, status));

                break;
            case NotifyCollectionChangedAction.Reset:
                Reset?.Invoke(this, EventArgs.Empty);

                break;
        }
    }

    #region IDisposable
    public void Dispose() => StatusChange?.Dispose();
    #endregion
}