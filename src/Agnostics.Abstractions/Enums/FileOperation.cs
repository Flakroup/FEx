using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Enums;

/// <summary>File operations that can be requested.</summary>
public enum FileOperation
{
    /// <summary>Copy files.</summary>
    [Description("copy")]
    Copy = 1,

    /// <summary>Move files.</summary>
    [Description("move")]
    Move,

    /// <summary>Delete files.</summary>
    [Description("delete")]
    Delete,

    /// <summary>Synchronize one way, from the source to the destination.</summary>
    [Description("one-way synchronization from source to destination")]
    SyncSrcToDest
}