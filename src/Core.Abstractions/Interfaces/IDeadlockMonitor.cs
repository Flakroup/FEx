using System;
using System.Diagnostics;

namespace FEx.Core.Abstractions.Interfaces;

public interface IDeadlockMonitor
{
    void Execute(Action action, StackTrace stackTrace = null, uint timeout = 3000);
}