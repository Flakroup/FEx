using FEx.LiteDbx.Enums;

namespace FEx.LiteDbx.Extensions;

public static class ClearCacheReasonExtensions
{
    public static bool HasFlagFast(this ClearCacheReason value, ClearCacheReason flag) => (value & flag) != 0;
}