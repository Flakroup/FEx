using FEx.Asyncx.Abstractions.Interfaces;
using FEx.Avaloniax.Abstractions.Interfaces;
using FEx.Basics.Helpers;
using FEx.Extensions;
using StrongInject;
using System;
using System.Threading.Tasks;

namespace FEx.Avaloniax.Abstractions;

public abstract class AsyncInitializableViewModelBase : FExAvaloniaViewModelBase, IAsyncInitialize,
    IRequiresInitialization
{
    protected readonly AsyncHelper _asyncHelper;
    private bool _isDisposed;

    public Task<bool> InitializationTask { get; private set; }
    public bool IsInitialized { get; protected set; }

    protected AsyncInitializableViewModelBase(AsyncHelper asyncHelper, INavigationService navigationService)
        : base(navigationService)
    {
        _asyncHelper = asyncHelper;
    }

    public virtual void Initialize() => RunInitialize();

    protected abstract ValueTask OnInitializationAsync();

    protected virtual async Task<bool> InitializeAsync()
    {
        IsInitialized = false;

        try
        {
            await OnInitializationAsync();
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            IsInitialized = false;
            ex.HandleException();
        }

        return IsInitialized;
    }

    protected void RunInitialize() => InitializationTask = _asyncHelper.ExecuteTaskOnThreadPoolAsync(InitializeAsync);

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