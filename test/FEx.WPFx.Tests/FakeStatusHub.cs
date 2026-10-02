using FEx.Agnostics.Abstractions.Utilities;
using FEx.Common.Abstractions.Interfaces;
using System;
using System.Collections.Generic;

namespace FEx.WPFx.Tests;

/// <summary>Status hub that keeps the registered handlers, like the real one, but can be inspected.</summary>
internal sealed class FakeStatusHub : IStatusHub
{
    private readonly List<Delegate> _handlers = [];

    public int HandlerCount => _handlers.Count;

    public Guid Key { get; } = Guid.NewGuid();

    public void AttachToStatusChanges(Action<Guid, string> onStatusAdded,
                                      Action<Guid, string> onStatusRemoved,
                                      Action onStatusesReset) =>
        _handlers.AddRange([onStatusAdded, onStatusRemoved, onStatusesReset]);

    public void DetachFromStatusChanges(Action<Guid, string> onStatusAdded,
                                        Action<Guid, string> onStatusRemoved,
                                        Action onStatusesReset)
    {
        _handlers.Remove(onStatusAdded);
        _handlers.Remove(onStatusRemoved);
        _handlers.Remove(onStatusesReset);
    }

    public Guid AddStatus(string status, bool unique = true) => Guid.NewGuid();
    public void CleanStatuses()
    {
    }

    public IList<string> GetStatuses() => [];
    public string GetStatusString(string? separator = null) => string.Empty;
    public void RemoveStatus(Guid key)
    {
    }

    public DisposableAction Log(string status, bool unique = true) => new(() => { });
}
