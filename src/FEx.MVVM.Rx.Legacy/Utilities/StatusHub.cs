using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Common.Abstractions.Interfaces;
using FEx.MVVM.Rx.Legacy.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace FEx.MVVM.Rx.Legacy.Utilities;

public sealed class StatusHub : IDisposable, IStatusHub
{
    public EventHandler<(Guid key, string status)>? StatusAdded;
    public EventHandler<(Guid key, string status)>? StatusRemoved;
    public EventHandler<EventArgs>? Reset;

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
            StatusAdded += (_, s) => onStatusAdded(s.key, s.status);

        if (onStatusRemoved is not null)
            StatusRemoved += (_, s) => onStatusRemoved(s.key, s.status);

        if (onStatusesReset is not null)
            Reset += (_, _) => onStatusesReset();
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

            // Reset carries no status text; StatusChanged ignores the status for the Reset action, so string.Empty is inert.
            StatusChange?.Report((Guid.Empty, string.Empty, NotifyCollectionChangedAction.Reset));
        }
    }

    public IList<string> GetStatuses() => [.. Statuses.Values];

    public string GetStatusString(string? separator)
    {
        // IStatusHub.GetStatusString is a non-nullable contract; an empty status set yields an empty string (was null before the nullable migration).
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