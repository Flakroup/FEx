using FEx.LiteDBx.Enums;

namespace FEx.LiteDBx.Extensions;

public static class ClearCacheReasonExtensions
{
    public static bool HasFlagFast(this ClearCacheReason value, ClearCacheReason flag) => (value & flag) != 0;
}