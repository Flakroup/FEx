using System;

namespace FEx.Agnostics.Abstractions.Enums;

/// <summary>Flags that tune how asynchronous work is started.</summary>
[Flags]
public enum AsyncOptions
{
    /// <summary>No special behavior; the work starts inline on the chosen context.</summary>
    None = 0,
    /// <summary>Wrap the work in <see cref="System.Threading.Tasks.Task.Run(System.Action)" /> so it is scheduled immediately instead of being deferred.</summary>
    ImmediateStart = 1 << 0
}