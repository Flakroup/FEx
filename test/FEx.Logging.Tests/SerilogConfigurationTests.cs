using FEx.Logging.Abstractions.Interfaces;
using Serilog.Events;
using Shouldly;
using Xunit;

namespace FEx.Logging.Tests;

/// <summary>
/// Tests to verify Serilog is correctly configured via IFExLoggingConfigurator.
/// </summary>
public sealed class SerilogConfigurationTests
{
    [Fact]
    public void LoggingConfigurator_ShouldAllowPropertyConfiguration()
    {
        // Arrange
        using var container = new TestContainer();
        using var configuratorOwned = container.Resolve<IFExLoggingConfigurator>();
        var configurator = configuratorOwned.Value;

        // Act
        configurator.ExternalLoggingLevel = LogEventLevel.Warning;
        configurator.ExternalDebugLoggingLevel = LogEventLevel.Error;

        // Assert
        configurator.ShouldNotBeNull();
        configurator.ExternalLoggingLevel.ShouldBe(LogEventLevel.Warning);
        configurator.ExternalDebugLoggingLevel.ShouldBe(LogEventLevel.Error);
    }
}