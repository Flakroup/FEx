using FEx.Common.Utilities;
using System;
using System.Collections.Generic;

namespace FEx.Abstractions.Interfaces;

public interface IStatusHub
{
    Guid Key { get; }

    Guid AddStatus(string status, bool unique = true);

    void AttachToStatusChanges(Action<Guid, string> onStatusAdded,
                               Action<Guid, string> onStatusRemoved,
                               Action onStatusesReset);

    void CleanStatuses();
    IList<string> GetStatuses();
    string GetStatusString(string separator = null);
    void RemoveStatus(Guid key);
    DisposableAction Log(string status, bool unique = true);
}