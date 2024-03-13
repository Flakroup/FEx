using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Collections;

public class AsyncBuffer<T> : IDisposable
{
    private readonly SemaphoreSlim _queueLock;
    private readonly List<T> _buffer;
    private bool _isDisposed;

    public AsyncBuffer()
    {
        _buffer = [];
        _queueLock = new SemaphoreSlim(1, 1);
    }

    public async Task<IList<T>> RetrieveFromBufferAsync(CancellationToken cancellationToken)
    {
        await _queueLock.WaitAsync(cancellationToken);

        try
        {
            return RetrieveFromBuffer(_buffer);
        }
        finally
        {
            _queueLock.Release();
        }
    }

    public async Task AddToBufferAsync(IEnumerable<T> changes, CancellationToken cancellationToken)
    {
        await _queueLock.WaitAsync(cancellationToken);

        try
        {
            _buffer.AddRange(changes);
        }
        finally
        {
            _queueLock.Release();
        }
    }

    protected virtual IList<T> RetrieveFromBuffer(IList<T> buffer)
    {
        var changes = buffer.ToList();

        buffer.Clear();

        return changes;
    }

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            _queueLock?.Dispose();

        _isDisposed = true;
    }
    #endregion
}