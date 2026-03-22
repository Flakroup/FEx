using FEx.Logging.Abstractions.Interfaces;
using Shouldly;
using Xunit;

namespace FEx.Logging.Tests;

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