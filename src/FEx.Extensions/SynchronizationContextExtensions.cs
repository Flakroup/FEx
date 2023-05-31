using FEx.Extensions.Helpers;
using System;
using System.Threading;

namespace FEx.Extensions;

public static class SynchronizationContextExtensions
{
    public static SynchronizationContext GetThreadSynchronizationContext(this Thread thread, bool createNew = false)
    {
        const string propertyName = "SynchronizationContext";
        if (thread.ManagedThreadId.Equals(Environment.CurrentManagedThreadId))
            return Get(createNew);

        return thread.ExecutionContext?.GetPropertyValue(propertyName) as SynchronizationContext;
    }

    public static SynchronizationContext Get(bool createNew = false)
    {
        if (SynchronizationContext.Current is null && createNew)
            SynchronizationContext.SetSynchronizationContext(new());

        return SynchronizationContext.Current;
    }
}