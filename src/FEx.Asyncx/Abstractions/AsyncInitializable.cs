using FEx.Asyncx.Abstractions.Interfaces;
using FEx.Basics;
using FEx.Fundamentals;
using FEx.Fundamentals.Extensions;
using FEx.Fundamentals.Helpers;
using Microsoft.Extensions.Logging;
using StrongInject;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Abstractions;

public abstract class AsyncInitializable : IAsyncInitialize, IDisposable, IRequiresInitialization
{
    protected readonly AsyncHelper _asyncHelper;
    private readonly SemaphoreSlim _semaphore;
    private bool _isDisposed;

    public Task<bool> InitializationTask { get; private set; }
    public bool IsInitialized { get; protected set; }

    protected AsyncInitializable()
    {
        _asyncHelper = Foundation.AsyncHelper;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public virtual void Initialize()
    {
        RunInitialize();
    }

    protected abstract Task<bool> OnInitializationAsync(bool reInitialize);

    protected virtual async Task<bool> InitializeAsync(bool reInitialize = false)
    {
        await _semaphore.WaitAsync();

        if (IsInitialized && !reInitialize)
        {
            FExBasics.Logger.LogWarning($"{GetType().FullName} has been already initialized");
            return true;
        }

        IsInitialized = false;

        try
        {
            IsInitialized = await OnInitializationAsync(reInitialize);
        }
        catch (Exception ex)
        {
            IsInitialized = false;
            ex.HandleException();
        }
        finally
        {
            _semaphore.Release();
        }

        return IsInitialized;
    }

    protected void RunInitialize(bool reInitialize = false)
    {
        InitializationTask = _asyncHelper.ExecuteTaskOnThreadPoolAsync(() => InitializeAsync(reInitialize));
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            InitializationTask?.Dispose();
            _semaphore.Dispose();
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}