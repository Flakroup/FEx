using FEx.Abstractions;
using FEx.Asyncx;
using FEx.Asyncx.Helpers;
using FEx.Basics;
using FEx.Basics.Abstractions.Interfaces;
using FEx.Extensions;
using FEx.Extensions.Base;
using Microsoft.Extensions.Logging;

namespace FEx.Fundamentals;

public class FExFundamentalsModuleInitializer : InitializeModule
{
    private readonly Foundation _foundation;
    private readonly ILogger _logger;
    private readonly AsyncHelper _asyncHelper;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IStackTraceProvider _stackTraceProvider;
    private readonly IEventDeliverer _eventDeliverer;

    public FExFundamentalsModuleInitializer(Foundation foundation,
                                            ILogger logger,
                                            AsyncHelper asyncHelper,
                                            IExceptionHandler exceptionHandler,
                                            IStackTraceProvider stackTraceProvider,
                                            IEventDeliverer eventDeliverer)
    {
        _foundation = foundation;
        _logger = logger;
        _asyncHelper = asyncHelper;
        _exceptionHandler = exceptionHandler;
        _stackTraceProvider = stackTraceProvider;
        _eventDeliverer = eventDeliverer;
    }

    protected override void OnInitialize()
    {
        _foundation.Guard();
        FExExtensionsCommon.Initialize(_exceptionHandler.Guard(nameof(_exceptionHandler)));
        FExBasics.Init(_stackTraceProvider, _eventDeliverer, _logger);
        FExAsyncx.Init(_asyncHelper, Foundation.MainThread);
    }
}