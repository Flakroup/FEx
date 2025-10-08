using System;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>
/// Models a disposable action that is guaranteed to be invoked
/// at least on disposal (if not explicitly invoked).
/// </summary>
/// <seealso cref="IDisposable" />
public class DisposableAction : IDisposable
{
    private Action _action;

    private bool _disposedValue; // To detect redundant calls

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableAction" /> class.
    /// </summary>
    /// <param name="action">The action.</param>
    public DisposableAction(Action action)
    {
        _action = action;
    }

    #region IDisposable
    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && _action != null)
            {
                _action.Invoke();
                _action = null;
            }

            // free unmanaged resources (unmanaged objects) and override a finalizer below.
            // set large fields to null.

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // uncomment the following line if the finalizer is overridden above.
        GC.SuppressFinalize(this);
    }
    #endregion
}