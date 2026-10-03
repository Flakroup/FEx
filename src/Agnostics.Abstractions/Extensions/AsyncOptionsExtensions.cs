using FEx.Agnostics.Abstractions.Enums;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for <see cref="AsyncOptions" />.</summary>
public static class AsyncOptionsExtensions
{
    /// <summary>Determines whether any bit of a flag is set, without boxing.</summary>
    /// <param name="value">The options to inspect.</param>
    /// <param name="flag">The flag to look for.</param>
    /// <returns><c>true</c> if <paramref name="value" /> shares a bit with <paramref name="flag" />.</returns>
    public static bool HasFlagFast(this AsyncOptions value, AsyncOptions flag) => (value & flag) != 0;
}