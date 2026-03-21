using FEx.Core.Abstractions.Interfaces;
using System;

namespace FEx.Core.Abstractions.Extensions;

public static class DeadlockMonitorExtensions
{
    public static void Execute(this IDeadlockMonitor monitor, Action action) =>
        monitor.Execute(action, null, 3000);

    public static void Execute(this IDeadlockMonitor monitor, Action action, uint timeout) =>
        monitor.Execute(action, null, timeout);
}
