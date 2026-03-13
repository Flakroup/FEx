using FEx.PersistentStorage.Abstractions.Enums;

namespace FEx.PersistentStorage.Abstractions.Extensions;

public static class ClearCacheReasonExtensions
{
    public static bool HasFlagFast(this ClearCacheReason value, ClearCacheReason flag) => (value & flag) != 0;
}