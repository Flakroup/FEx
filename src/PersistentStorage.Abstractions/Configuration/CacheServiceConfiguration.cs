using System;

namespace FEx.PersistentStorage.Abstractions.Configuration;

public static class CacheServiceConfiguration
{
    public static TimeSpan ExpirationTimeSpan { get; set; } = TimeSpan.FromDays(5);
}