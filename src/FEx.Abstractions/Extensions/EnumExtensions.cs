using FEx.Abstractions.Enums;

namespace FEx.Abstractions.Extensions;

public static class EnumExtensions
{
    public static bool HasFlagFast(this AsyncHelperOptions value, AsyncHelperOptions flag) => (value & flag) != 0;
}