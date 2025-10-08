using FEx.Logging.Abstractions.Interfaces;
using Serilog.Events;
using Shouldly;
using System;
using Xunit;

namespace FEx.Logging.Tests;

/// <summary>
/// Tests to verify Serilog is correctly configured via IFExLoggingConfigurator.
/// </summary>
public class SerilogConfigurationTests : IDisposable
{
    [Fact]
    public void LoggingConfigurator_ShouldAllowPropertyConfiguration()
    {
        // Arrange
        using var container = new TestContainer();
        IFExLoggingConfigurator configurator = container.Resolve<IFExLoggingConfigurator>().Value;

        // Act
        configurator.ExternalLoggingLevel = LogEventLevel.Warning;
        configurator.ExternalDebugLoggingLevel = LogEventLevel.Error;

        // Assert
        configurator.ShouldNotBeNull();
        configurator.ExternalLoggingLevel.ShouldBe(LogEventLevel.Warning);
        configurator.ExternalDebugLoggingLevel.ShouldBe(LogEventLevel.Error);
    }

    #region IDisposable
    public void Dispose()
    {
        // No cleanup needed - each test creates its own container with using statement
        GC.SuppressFinalize(this);
    }
    #endregion
}