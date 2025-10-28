using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Legacy.Mvvm.ViewModels;

public partial class ThreadingAwareViewModel
{
    protected readonly ILoggable _logger;
    protected readonly ConcurrentDictionary<string, IAsyncInitializable> _dependencies;

    protected Task _initializationTask;

    private readonly FExSemaphoreSlim _initializationSemaphore;
    private readonly FExSemaphoreSlim _taskSemaphore;

    private bool _isDisposed;

    public bool IsInitialized { get; protected set; }
    public bool HasFinishedInitialization => !IsInitializing;

    public bool IsInitializing => !IsInitialized && (_initializationTask is null || !_initializationTask.IsFinished());

    public string TypeName { get; protected set; }
    public string TypeFullName { get; protected set; }

    /// <summary>
    /// If <c>true</c> doesn't wait for dependencies initialization
    /// </summary>
    protected bool SkipDependenciesInitialization { get; set; }

    public async Task InitializeAsync()
    {
        if (HasFinishedInitialization)
            return;

        await _taskSemaphore.WaitAsync();

        try
        {
            _initializationTask ??= AsyncStatics.ExecuteTaskOnThreadPoolAsync(InitializeCoreAsync);
        }
        finally
        {
            _taskSemaphore.SafeRelease();
        }

        await _initializationTask;
    }

    public void Reset()
    {
        _initializationTask = null;
        IsInitialized = false;
    }

    public void BeginInitialization(bool waitSynchronouslyForInitialization = false)
    {
        if (waitSynchronouslyForInitialization)
        {
            JoinableAsyncHelper.AwaitWithoutDeadlock(InitFuncAsync);

            return;
        }

        _ = Task.Run(InitFuncAsync);

        return;

        Task InitFuncAsync() => AsyncStatics.ExecuteTaskOnThreadPoolAsync(InitializeAsync);
    }

    protected static async Task<Result<ExceptionError>> SafeInitializeAsync(IAsyncInitializable dependency)
    {
        try
        {
            await dependency.InitializeAsync();

            return Result<ExceptionError>.Success;
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    protected virtual async Task OnInitializeAsync()
    {
        if (!SkipDependenciesInitialization)
            await InitializeDependenciesAsync();

        _logger.LogDebug($"Initializing {TypeName}");
    }

    protected virtual async Task InitializeDependenciesAsync()
    {
        var results = await _dependencies.Values.Where(static dependency => !dependency.IsInitialized)
            .WithWhenAllTasksAsync(SafeInitializeAsync, AsyncMode.ThreadPool);

        if (!results.Any())
            return;

        var failed = results.Where(static result => result.IsFailure).ToList();

        if (!failed.Any())
            return;

        throw new AggregateException(failed.Select(static fail => fail.Error.Exception));
    }

    protected virtual async Task InitializeCoreAsync()
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
            _logger.LogError(ex);

            throw;
        }
        finally
        {
            _initializationSemaphore.SafeRelease();
        }
    }

    protected void ThrowIfNotInitialized()
    {
        if (IsInitialized)
            return;

        throw new InvalidOperationException(
            $"This instance of {TypeFullName} is still not initialized, as should be before being used");
    }

    protected void AddDependency(IAsyncInitializable dependency)
    {
        dependency.Guard(nameof(dependency));

        if (!_dependencies.TryAdd(dependency.TypeFullName, dependency))
            _logger.LogWarning($"{dependency.TypeFullName} is already referenced in {TypeFullName}");
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
        {
            _initializationSemaphore.Dispose();
            _taskSemaphore.Dispose();
        }

        _isDisposed = true;
    }
    #endregion
}