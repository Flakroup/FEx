using System;

namespace FEx.LiteDbx.Enums;

[Flags]
public enum ClearCacheReason
{
    LogOut = 1,
    IsFirstLaunchForCurrentBuild = 2
}