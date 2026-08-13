using System;

namespace FEx.PersistentStorage.Abstractions.Enums;

[Flags]
public enum ClearCacheReason
{
    LogOut = 1 << 1,
    IsFirstLaunchForCurrentBuild = 1 << 2,
    ApplicationLaunched = 1 << 3
}