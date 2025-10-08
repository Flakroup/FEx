using FEx.Logging.Abstractions.Interfaces;
using Shouldly;
using StrongInject;
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
        Owned<IFExLoggingService> loggingService = container.Resolve<IFExLoggingService>();
        Owned<IFExLoggingConfigurator> configurator = container.Resolve<IFExLoggingConfigurator>();
        Owned<ILoggingConfiguration> configuration = container.Resolve<ILoggingConfiguration>();

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