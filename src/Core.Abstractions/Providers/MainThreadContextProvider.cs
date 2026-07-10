using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Core.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using System;
using System.Threading;

namespace FEx.Core.Abstractions.Providers;

public class MainThreadContextProvider : IMainThreadContextProvider
{
    private readonly IAppInfoProvider _appInfoProvider;
    private Thread? _mainThread;

    // Context is non-null-annotated (interface) but is computed lazily and may be null until a
    // synchronization context is resolved; null! preserves that behavior without a return-type change.
    private SynchronizationContext _mainSynchronizationContext = null!;
    public event EventHandler<EventArgs>? ThreadHasChanged;

    public Thread Thread
    {
        get => _mainThread.GuardProperty();
        private set
        {
            _mainThread = value.Guard(nameof(value));
            ThreadHasChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsDispatcherContext { get; set; }

    public SynchronizationContext Context
    {
        get
        {
            if (IsDispatcherContext)
                return _mainSynchronizationContext;

            var context = Thread.GetThreadSynchronizationContext(IsUIApp);

            if (context is not null)
            {
                _mainSynchronizationContext = context;

                if (_mainSynchronizationContext.GetType().Name == "DispatcherSynchronizationContext")
                    IsDispatcherContext = true;
            }

            return _mainSynchronizationContext;
        }
    }

    protected bool IsUIApp => _appInfoProvider?.AppInfo?.IsUIApp ?? false;

    public MainThreadContextProvider(IAppInfoProvider appInfoProvider)
    {
        _appInfoProvider = appInfoProvider;
    }

    public void SetMainThread(bool throwOnNonMainThread = true)
    {
        var currentThread = Thread.CurrentThread;

        var isMainThread = currentThread.IsPlatformMainThread(IsUIApp)
                           && !currentThread.IsBackground
                           && currentThread.IsAlive
                           && !currentThread.IsThreadPoolThread;

        switch (isMainThread)
        {
            case false when throwOnNonMainThread:
                throw new InvalidOperationException("This method must be called from main thread.");
            case false:
                return;
        }

        Thread = currentThread;

        if (IsUIApp)
            _ = Context;
    }
}