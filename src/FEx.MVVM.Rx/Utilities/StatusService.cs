using FEx.Abstractions.Interfaces;
using FEx.Common.Utilities;
using FEx.Extensions.Collections.Dictionaries;
using System;
using System.Collections.Concurrent;

namespace FEx.MVVM.Rx.Utilities;

public sealed class StatusService : IStatusService
{
    public IStatusHub MainHub { get; private set; }
    public Guid? MainHubKey => MainHub?.Key;

    private ConcurrentDictionary<Guid, StatusHub> StatusHubs { get; }

    public StatusService()
    {
        StatusHubs = new();
    }

    public IStatusHub GetOrAdd(Guid? key = null,
                               Action<Guid, string> onStatusAdded = null,
                               Action<Guid, string> onStatusRemoved = null,
                               Action onStatusesReset = null,
                               bool markAsMain = false)
    {
        key ??= Guid.NewGuid();

        StatusHub hub = StatusHubs.GetOrAddValue(key.Value,
            () => new(key.Value, onStatusAdded, onStatusRemoved, onStatusesReset));

        if (markAsMain)
            MainHub = hub;

        return hub;
    }

    public Guid LogToMainHub(string status, bool unique = true) => MainHub?.AddStatus(status, unique) ?? Guid.Empty;

    public void RemoveMainLog(Guid statusKey) => MainHub?.RemoveStatus(statusKey);

    public DisposableAction Log(string status, IStatusHub hub = null, bool unique = true) =>
        (hub ?? MainHub).Log(status, unique);
}