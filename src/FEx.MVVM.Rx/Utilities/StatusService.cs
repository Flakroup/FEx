using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Common.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;

namespace FEx.MVVM.Rx.Utilities;

public sealed class StatusService : IStatusService
{
    // IStatusService declares MainHub non-null; it stays null until the first GetOrAdd(markAsMain: true) call.
    public IStatusHub MainHub { get; private set; } = null!;
    public Guid? MainHubKey => MainHub?.Key;

    private ConcurrentDictionary<Guid, StatusHub> StatusHubs { get; }

    public StatusService()
    {
        StatusHubs = new();
    }

    public IStatusHub GetOrAdd(Guid? key,
                               Action<Guid, string>? onStatusAdded,
                               Action<Guid, string>? onStatusRemoved,
                               Action? onStatusesReset,
                               bool markAsMain)
    {
        key ??= Guid.NewGuid();

        var hub = StatusHubs.GetOrAddValue(key.Value,
            () => new(key.Value, onStatusAdded, onStatusRemoved, onStatusesReset));

        if (markAsMain)
            MainHub = hub;

        return hub;
    }

    public Guid LogToMainHub(string status, bool unique) => MainHub?.AddStatus(status, unique) ?? Guid.Empty;

    public void RemoveMainLog(Guid statusKey) => MainHub?.RemoveStatus(statusKey);

    // Before any hub is registered (an app that has no splash or main view yet) there is nothing to log to: a no-op scope
    // keeps startup code that wraps its steps in Log(...) working.
    public DisposableAction Log(string status, IStatusHub? hub, bool unique) =>
        (hub ?? MainHub)?.Log(status, unique) ?? new DisposableAction(static () => { });

    public IStatusHub GetOrAdd() => GetOrAdd(null, null, null, null, false);

    public Guid LogToMainHub(string status) => LogToMainHub(status, true);

    public DisposableAction Log(string status) => Log(status, null, true);

    public DisposableAction Log(string status, IStatusHub hub) => Log(status, hub, true);
}