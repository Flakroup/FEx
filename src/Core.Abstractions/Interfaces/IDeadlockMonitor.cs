using System;
using System.Diagnostics;

namespace FEx.Core.Abstractions.Interfaces;

public interface IDeadlockMonitor
{
    void Execute(Action action, StackTrace stackTrace, uint timeout);
}