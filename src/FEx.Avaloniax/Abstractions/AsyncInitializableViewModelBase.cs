using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Extensions;
using FEx.Asyncx.Helpers;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Basics.Utilities;
using FEx.Extensions.Helpers;
using FEx.Logging.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using StrongInject;
using System;
using System.Threading.Tasks;

namespace FEx.Avaloniax.Abstractions;

public abstract class AsyncInitializableViewModelBase : FExAvaloniaViewModelBase, IAsyncInitializable,
    IRequiresInitialization
{
    protected readonly AsyncHelper _asyncHelper;
    protected readonly ILogger _logger;

    protected Task _initializationTask;
    private readonly FExSemaphoreSlim _initializationSemaphore;
    private readonly FExSemaphoreSlim _taskSemaphore;
    private bool _isDisposed;

    public Task<bool> InitializationTask { get; }
    public bool IsInitialized { get; protected set; }
    public string TypeName { get; }
    public string TypeFullName { get; }

    protected AsyncInitializableViewModelBase(AsyncHelper asyncHelper, INavigationService navigationService)
        : base(navigationService)
    {
        _logger = this.GetLogger();
        _initializationSemaphore = new(1, 1);
        _taskSemaphore = new(1, 1);
        _asyncHelper = asyncHelper;
        Type instanceType = GetType();
        TypeName = instanceType.Name;
        TypeFullName = instanceType.FullName;
    }

    public async Task InitializeAsync()
    {
        await _taskSemaphore.WaitAsync();

        try
        {
            _initializationTask ??= StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(InternalInitializeAsync);
        }
        finally
        {
            _taskSemaphore.SafeRelease();
        }

        await _initializationTask;
    }

    public virtual void Initialize() => RunInitialize();

    protected abstract ValueTask OnInitializeAsync();

    protected virtual async Task InternalInitializeAsync()
    {
        await _initializationSemaphore.WaitAsync();

        try
        {
            if (IsInitialized)
            {
                _logger.LogWarning($"{TypeName} has been already initialized");

                return;
            }

            IsInitialized = false;

            await OnInitializeAsync();

            IsInitialized = true;

            _logger.LogDebug($"{TypeName} initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            throw;
        }
        finally
        {
            _initializationSemaphore.SafeRelease();
        }
    }

    protected void RunInitialize()
    {
        _ = Task.Run(() => StaticAsyncHelper.ExecuteTaskOnThreadPoolAsync(InitializeAsync));
    }

    #region IDisposable
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
            InitializationTask?.Dispose();

        _isDisposed = true;
        base.Dispose(disposing);
    }
    #endregion
}