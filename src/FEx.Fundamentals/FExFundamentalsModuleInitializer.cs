using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
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
    private readonly ISynchronizedAccessService _synchronizedAccessService;
    private readonly IAppInfoProvider _appInfoProvider;

    public FExFundamentalsModuleInitializer(Foundation foundation,
                                            ILogger logger,
                                            AsyncHelper asyncHelper,
                                            IExceptionHandler exceptionHandler,
                                            IStackTraceProvider stackTraceProvider,
                                            IEventDeliverer eventDeliverer,
                                            ISynchronizedAccessService synchronizedAccessService,
                                            IAppInfoProvider appInfoProvider)
    {
        _foundation = foundation;
        _logger = logger;
        _asyncHelper = asyncHelper;
        _exceptionHandler = exceptionHandler;
        _stackTraceProvider = stackTraceProvider;
        _eventDeliverer = eventDeliverer;
        _synchronizedAccessService = synchronizedAccessService;
        _appInfoProvider = appInfoProvider;
    }

    protected override void OnInitialize()
    {
        FExExtensionsCommon.Initialize(_exceptionHandler.Guard(nameof(_exceptionHandler)));
        _foundation.Guard(nameof(_foundation));
        FExBasics.Init(_stackTraceProvider, _eventDeliverer, _logger, _synchronizedAccessService, _appInfoProvider);
        FExAsyncx.Init(_asyncHelper, FExBasics.MainThread);
    }
}