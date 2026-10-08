using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using FEx.Telemetry.Sentry;
using NSubstitute;
using Sentry.Serilog;
using Serilog;
using Serilog.Events;
using Shouldly;
using Xunit;

namespace FEx.Telemetry.Tests;

// The configured DSN is empty, so the Sentry SDK stays disabled and nothing leaves the process.
public sealed class SentrySinkConfiguratorTests
{
    private readonly ISentryConfig _sentryConfig = Substitute.For<ISentryConfig>();
    private readonly ILoggingConfiguration _loggingConfiguration = Substitute.For<ILoggingConfiguration>();
    private readonly IAppVersionProvider _versionProvider = Substitute.For<IAppVersionProvider>();

    public SentrySinkConfiguratorTests()
    {
        _sentryConfig.SentryDsn.Returns(string.Empty);
        _sentryConfig.EnvironmentId.Returns("staging");
        _versionProvider.GetAppVersion().Returns("9.8.7");
    }

    [Fact]
    public void SinkType_IsSentry()
    {
        var sut = new SentrySinkConfigurator(_sentryConfig, _loggingConfiguration, _versionProvider);

        sut.SinkType.ShouldBe(LoggingOptions.Sentry);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_FollowsLoggingConfiguration(bool enabled)
    {
        _loggingConfiguration.HasOption(LoggingOptions.Sentry).Returns(enabled);
        var sut = new SentrySinkConfigurator(_sentryConfig, _loggingConfiguration, _versionProvider);

        sut.IsEnabled.ShouldBe(enabled);
    }

    [Fact]
    public void ConfigureSink_AppliesSentryOptionsFromConfigAndVersionProvider()
    {
        var sut = new CapturingConfigurator(_sentryConfig, _loggingConfiguration, _versionProvider);

        using var logger = sut.ConfigureSink(new LoggerConfiguration().WriteTo).CreateLogger();

        var options = sut.Captured.ShouldNotBeNull();
        options.Dsn.ShouldBeEmpty();
        options.Release.ShouldBe("9.8.7");
        options.Environment.ShouldBe("staging");
        options.AttachStacktrace.ShouldBeTrue();
        options.AutoSessionTracking.ShouldBeTrue();
        options.MinimumBreadcrumbLevel.ShouldBe(LogEventLevel.Debug);
        options.MinimumEventLevel.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void ConfigureSink_ReturnsTheLoggerConfigurationForChaining()
    {
        var sut = new SentrySinkConfigurator(_sentryConfig, _loggingConfiguration, _versionProvider);
        var configuration = new LoggerConfiguration();

        sut.ConfigureSink(configuration.WriteTo).ShouldBeSameAs(configuration);
    }

    private sealed class CapturingConfigurator(
        ISentryConfig sentryConfig,
        ILoggingConfiguration loggingConfiguration,
        IAppVersionProvider appVersionProvider)
        : SentrySinkConfigurator(sentryConfig, loggingConfiguration, appVersionProvider)
    {
        public SentrySerilogOptions? Captured { get; private set; }

        protected override void ConfigureSentrySerilogLogging(SentrySerilogOptions options)
        {
            base.ConfigureSentrySerilogLogging(options);
            Captured = options;
        }
    }
}
