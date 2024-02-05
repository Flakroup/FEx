using System;

namespace FEx.LiteDBx.Enums;

[Flags]
public enum ClearCacheReason
{
    LogOut = 1,
    IsFirstLaunchForCurrentBuild = 2
}