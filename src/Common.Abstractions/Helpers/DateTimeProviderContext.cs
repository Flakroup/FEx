using System;
using System.Collections.Generic;
using System.Threading;

namespace FEx.Common.Abstractions.Helpers;

public sealed class DateTimeProviderContext : IDisposable
{
    internal DateTime ContextDateTime;
    private static readonly ThreadLocal<Stack<DateTimeProviderContext>> _threadScopeStack = new(static () => new());
    private bool _isDisposed;

    public static DateTimeProviderContext? Current =>
        _threadScopeStack.Value?.Count == 0
            ? null
            : _threadScopeStack.Value?.Peek();

    public DateTimeProviderContext(DateTime contextDateTime)
    {
        ContextDateTime = contextDateTime;
        _threadScopeStack.Value?.Push(this);
    }

    #region IDisposable
    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            _threadScopeStack.Value?.Pop();

        _isDisposed = true;
    }
    #endregion
}