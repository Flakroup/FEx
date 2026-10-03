using FEx.Agnostics.Abstractions;
using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Agnostics.Abstractions.Utilities;
using FEx.Agnostics.BaseObjects;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FEx.Asyncx.Abstractions;

/// <summary>
/// Template for asynchronous one-time initialization. The base owns the whole sequence (lock, dependencies,
/// <see cref="OnInitializeAsync" />, <see cref="IsInitialized" />); subclasses only fill in the hooks, whose base
/// implementations do nothing, so there is no base call to forget.
/// </summary>
public abstract class AsyncInitializable : NotifyPropertyChanged, IAsyncInitializable
{
    protected readonly IFExLogger _logger;
    protected readonly ConcurrentDictionary<string, IAsyncInitializable> _dependencies;

    protected Task? _initializationTask;

    private readonly FExSemaphoreSlim _initializationSemaphore;
    private readonly FExSemaphoreSlim _taskSemaphore;

    private bool _isDisposed;

    public bool IsInitialized { get; private set; }
    public bool HasFinishedInitialization => !IsInitializing;

    public bool IsInitializing => !IsInitialized && (_initializationTask is null || !_initializationTask.IsFinished());

    public string TypeName { get; protected set; }
    public string TypeFullName { get; protected set; }

    /// <summary>
    /// If <c>true</c> doesn't wait for dependencies initialization
    /// </summary>
    protected bool SkipDependenciesInitialization { get; set; }

    /// <param name="dependencies">
    /// Initialized before <see cref="OnInitializeAsync" />. Deliberately not <c>params</c>: every subclass must state
    /// its dependencies (<c>: base([])</c> when it has none), so forgetting to forward them does not compile.
    /// </param>
    protected AsyncInitializable(IAsyncInitializable[] dependencies)
    {
        _logger = FExStaticLogger.Instance;
        _initializationSemaphore = new();
        _taskSemaphore = new();
        var instanceType = GetType();
        TypeName = instanceType.Name;
        // Type.FullName is non-null for concrete runtime types (instances); Guard proves it to the compiler.
        TypeFullName = instanceType.FullName.Guard(nameof(instanceType));
        _dependencies = new();
        AddDependencies(dependencies);
    }

    /// <summary>
    /// Completes only once this instance is initialized; a failed initialization throws. The failure is kept and
    /// rethrown by later calls until <see cref="ResetAsync" />.
    /// </summary>
    public async Task InitializeAsync()
    {
        // A reset can clear a published task before its run takes the lock; that run then exits without initializing
        // and the loop publishes a new one.
        while (!IsInitialized)
        {
            Task initializationTask;
            await _taskSemaphore.WaitAsync();

            try
            {
                initializationTask = _initializationTask ??=
                    AsyncStatics.ExecuteTaskOnThreadPoolAsync(InitializeCoreAsync);
            }
            finally
            {
                _taskSemaphore.SafeRelease();
            }

            await initializationTask;
        }
    }

    /// <summary>
    /// Waits for an in-flight initialization to finish, then clears the initialization state (including a kept
    /// failure), so a reset is never observed mid-initialization. Must not be awaited from inside a hook.
    /// </summary>
    public async Task ResetAsync()
    {
        await _initializationSemaphore.WaitAsync();

        try
        {
            await _taskSemaphore.WaitAsync();

            try
            {
                _initializationTask = null;
                IsInitialized = false;
            }
            finally
            {
                _taskSemaphore.SafeRelease();
            }

            await OnResetAsync();
        }
        finally
        {
            _initializationSemaphore.SafeRelease();
        }
    }

    public void BeginInitialization(bool waitSynchronouslyForInitialization)
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

    public void BeginInitialization() => BeginInitialization(false);

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

    /// <summary>
    /// Initialization work of the subclass. Runs once, under the initialization lock, after the dependencies are
    /// initialized (unless <see cref="SkipDependenciesInitialization" />). The base implementation does nothing.
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs under the initialization lock before the dependencies are initialized. Only for work the dependencies
    /// rely on; everything else belongs in <see cref="OnInitializeAsync" />. The base implementation does nothing.
    /// </summary>
    protected virtual Task OnBeforeDependenciesInitializationAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs at the end of <see cref="ResetAsync" />, still under the initialization lock, after the state is cleared;
    /// for dropping state the next initialization rebuilds. The base implementation does nothing.
    /// </summary>
    protected virtual Task OnResetAsync() => Task.CompletedTask;

    private async Task InitializeDependenciesAsync()
    {
        var results = await _dependencies.Values.Where(static dependency => !dependency.IsInitialized)
            .WithWhenAllTasksAsync(SafeInitializeAsync, AsyncMode.ThreadPool);

        if (!results.Any())
            return;

        var failed = results.Where(static result => result.IsFailure).ToList();

        if (!failed.Any())
            return;

        throw new AggregateException(failed.Select(static fail => fail.Error?.Exception).OfType<Exception>());
    }

    private async Task InitializeCoreAsync()
    {
        await _initializationSemaphore.WaitAsync();

        try
        {
            if (IsInitialized)
            {
                _logger.Warning($"{TypeName} has been already initialized");

                return;
            }

            // InitializeAsync publishes a new run when this one exits.
            if (await WasResetBeforeStartAsync())
            {
                _logger.Debug($"{TypeName} was reset before its initialization started");

                return;
            }

            await OnBeforeDependenciesInitializationAsync();

            if (!SkipDependenciesInitialization)
                await InitializeDependenciesAsync();

            _logger.Debug($"Initializing {TypeName}");

            await OnInitializeAsync();

            IsInitialized = true;

            _logger.Debug($"{TypeName} initialized");
        }
        catch (Exception ex)
        {
            _logger.Error(ex);

            throw;
        }
        finally
        {
            _initializationSemaphore.SafeRelease();
        }
    }

    // A reset that lands between InitializeAsync publishing the task and this run taking the lock has cleared the
    // task; initializing anyway would leave IsInitialized true with no task.
    private async Task<bool> WasResetBeforeStartAsync()
    {
        await _taskSemaphore.WaitAsync();

        try
        {
            return _initializationTask is null;
        }
        finally
        {
            _taskSemaphore.SafeRelease();
        }
    }

    protected void ThrowIfNotInitialized()
    {
        if (IsInitialized)
            return;

        throw new InvalidOperationException(
            $"This instance of {TypeFullName} is still not initialized, as should be before being used");
    }

    protected void AddDependencies(params IAsyncInitializable[] dependencies)
    {
        if (dependencies is null)
            return;

        foreach (var dependency in dependencies)
        {
            dependency.Guard(nameof(dependency));

            if (!_dependencies.TryAdd(dependency.TypeFullName, dependency))
                _logger.Warning($"{dependency.TypeFullName} is already referenced in {TypeFullName}");
        }
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