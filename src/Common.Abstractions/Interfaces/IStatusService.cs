using FEx.Agnostics.Abstractions.Utilities;
using System;

namespace FEx.Common.Abstractions.Interfaces;

public interface IStatusService
{
    IStatusHub MainHub { get; }
    Guid? MainHubKey { get; }

    IStatusHub GetOrAdd(Guid? key = null,
                        Action<Guid, string>? onStatusAdded = null,
                        Action<Guid, string>? onStatusRemoved = null,
                        Action? onStatusesReset = null,
                        bool markAsMain = false);

    Guid LogToMainHub(string status, bool unique = true);
    void RemoveMainLog(Guid statusKey);
    DisposableAction Log(string status, IStatusHub? hub = null, bool unique = true);
}