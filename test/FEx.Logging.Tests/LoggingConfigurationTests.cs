using FEx.Logging.Abstractions.Interfaces;
using Shouldly;
using Xunit;

namespace FEx.Logging.Tests;

// Configure() replaces the process-wide Log.Logger, so this must not run beside the other tests that swap it.
[Collection(FExStaticLoggerCollection.Name)]
public sealed class LoggingConfigurationTests
{
    [Fact]
    public void LoggingModule_ShouldResolveAllRequiredServices()
    {
        // Arrange & Act - Test that all logging services can be resolved
        using var container = new TestContainer();

        // Assert
        using var loggingService = container.Resolve<IFExLoggingService>();
        using var configurator = container.Resolve<IFExLoggingConfigurator>();
        using var configuration = container.Resolve<ILoggingConfiguration>();

        loggingService.Value.ShouldNotBeNull();
        configurator.Value.ShouldNotBeNull();
        configuration.Value.ShouldNotBeNull();
    }
}