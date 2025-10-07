using FEx.Common.Abstractions.Interfaces;
using FEx.Common.Extensions;
using System;
using System.Threading;

namespace FEx.Common.Providers;

public class MainThreadContextProvider : IMainThreadContextProvider
{
    private readonly IAppInfoProvider _appInfoProvider;
    private Thread _mainThread;
    private SynchronizationContext _mainSynchronizationContext;
    public event EventHandler<EventArgs> ThreadHasChanged;

    public Thread Thread
    {
        get => _mainThread.Guard();
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

            SynchronizationContext context = Thread.GetThreadSynchronizationContext(IsUIApp);

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
        Thread currentThread = Thread.CurrentThread;

        bool isMainThread = currentThread.IsPlatformMainThread(IsUIApp)
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