using FEx.Logging.Abstractions.Interfaces;
using Shouldly;
using System;
using Xunit;

namespace FEx.Logging.Tests;

public class LoggingConfigurationTests : IDisposable
{
    [Fact]
    public void LoggingModule_ShouldResolveAllRequiredServices()
    {
        // Arrange & Act - Test that all logging services can be resolved
        using var container = new TestContainer();

        // Assert
        var loggingService = container.Resolve<IFExLoggingService>();
        var configurator = container.Resolve<IFExLoggingConfigurator>();
        var configuration = container.Resolve<ILoggingConfiguration>();

        loggingService.Value.ShouldNotBeNull();
        configurator.Value.ShouldNotBeNull();
        configuration.Value.ShouldNotBeNull();
    }

    #region IDisposable
    public void Dispose()
    {
        // No cleanup needed - each test creates its own container with using statement
        GC.SuppressFinalize(this);
    }
    #endregion
}