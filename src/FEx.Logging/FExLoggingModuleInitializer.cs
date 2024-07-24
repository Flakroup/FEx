using FEx.DependencyInjection.Abstractions;
using FEx.Fundamentals;
using FEx.Json;
using FEx.Logging.Abstractions.Interfaces;
using FEx.Logging.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FEx.Logging;

public class FExLoggingModuleInitializer : InitializeModule<IFExLoggingModule>
{
    private readonly FExFundamentalsModuleInitializer _initializer;
    private readonly FExJsonModuleInitializer _jsonModule;
    private readonly ILoggingService _loggingService;
    private readonly FExLoggingConfigurator _configurator;

    public FExLoggingModuleInitializer(FExFundamentalsModuleInitializer initializer,
                                       FExJsonModuleInitializer jsonModule,
                                       ILoggingService loggingService,
                                       FExLoggingConfigurator configurator)
    {
        _initializer = initializer;
        _jsonModule = jsonModule;
        _loggingService = loggingService;
        _configurator = configurator;
    }

    protected override void OnInitialize()
    {
        _initializer.Initialize();
        _jsonModule.Initialize();
        LoggerExtensions.SetLogger();
        FExLogging.Init(_loggingService, _configurator);
    }

    protected override void AddServices(IFExLoggingModule container, IServiceCollection services) => FExLoggingModule.AddServices(container, services);
}