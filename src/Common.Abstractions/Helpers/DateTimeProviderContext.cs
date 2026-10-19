using System;
using System.Collections.Generic;
using System.Threading;

namespace FEx.Common.Abstractions.Helpers;

public sealed class DateTimeProviderContext : IDisposable
{
    internal DateTime ContextDateTime;
    private static readonly ThreadLocal<Stack<DateTimeProviderContext>> ThreadScopeStack = new(static () => new());
    private bool _isDisposed;

    public static DateTimeProviderContext? Current =>
        ThreadScopeStack.Value?.Count == 0
            ? null
            : ThreadScopeStack.Value?.Peek();

    public DateTimeProviderContext(DateTime contextDateTime)
    {
        ContextDateTime = contextDateTime;
        ThreadScopeStack.Value?.Push(this);
    }

    #region IDisposable
    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            ThreadScopeStack.Value?.Pop();

        _isDisposed = true;
    }
    #endregion
}