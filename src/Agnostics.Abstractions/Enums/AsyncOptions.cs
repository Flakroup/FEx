using System;

namespace FEx.Agnostics.Abstractions.Enums;

[Flags]
public enum AsyncOptions
{
    None = 0,
    ImmediateStart = 1 << 0
}