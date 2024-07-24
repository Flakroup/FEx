using System;

namespace FEx.Abstractions.Enums;

[Flags]
public enum AsyncHelperOptions
{
    None = 0,
    ImmediateStart = 1 << 0
}