using FEx.Agnostics.Abstractions.Enums;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class AsyncOptionsExtensions
{
    public static bool HasFlagFast(this AsyncOptions value, AsyncOptions flag) => (value & flag) != 0;
}