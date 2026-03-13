using System;

namespace FEx.Common.Abstractions.Helpers;

public static class DateTimeProvider
{
    public static DateTime Today =>
        DateTimeProviderContext.Current is null
            ? DateTime.Today
            : DateTimeProviderContext.Current.ContextDateTime.Date;

    public static DateTime Now =>
        DateTimeProviderContext.Current is null
            ? DateTime.Now
            : DateTimeProviderContext.Current.ContextDateTime;

    public static DateTime UtcNow =>
        DateTimeProviderContext.Current is null
            ? new(DateTime.UtcNow.Ticks, DateTimeKind.Utc)
            : DateTimeProviderContext.Current.ContextDateTime.ToUniversalTime();
}