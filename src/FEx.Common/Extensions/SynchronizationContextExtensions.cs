using FEx.Common.Helpers;
using System;
using System.Threading;

namespace FEx.Common.Extensions;

public static class SynchronizationContextExtensions
{
    public static SynchronizationContext GetThreadSynchronizationContext(this Thread thread, bool createNew = false)
    {
        if (thread.ManagedThreadId.Equals(Environment.CurrentManagedThreadId))
            return Get(createNew);
#if NETFULL
        const string propertyName = "SynchronizationContext";
        return thread.ExecutionContext?.GetPropertyValue(propertyName) as SynchronizationContext;
#else
        const string fieldName = "_synchronizationContext";

        return thread.GetFieldValue(fieldName) as SynchronizationContext;
#endif
    }

    public static SynchronizationContext Get(bool createNew = false)
    {
        if (SynchronizationContext.Current is null && createNew)
            SynchronizationContext.SetSynchronizationContext(new());

        return SynchronizationContext.Current;
    }
}