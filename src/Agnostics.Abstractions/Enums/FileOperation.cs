using System.ComponentModel;

namespace FEx.Agnostics.Abstractions.Enums;

public enum FileOperation
{
    [Description("copy")]
    Copy = 1,

    [Description("move")]
    Move,

    [Description("delete")]
    Delete,

    [Description("one-way synchronization from source to destination")]
    SyncSrcToDest
}