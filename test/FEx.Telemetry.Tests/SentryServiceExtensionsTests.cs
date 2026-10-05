using FEx.Telemetry.Sentry;
using FEx.Telemetry.Sentry.Abstractions.Interfaces;
using FEx.Telemetry.Sentry.Extensions;
using NSubstitute;
using System.Reflection;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class SentryServiceExtensionsTests
{
    private const string Dsn = "https://key@example.invalid/1";

    private readonly ISentryService _service = Substitute.For<ISentryService>();

    [Fact]
    public void InitializeFromConfig_WhenDsnEmpty_DoesNotInitialize()
    {
        _service.InitializeFromConfig(new FExSentryConfig(string.Empty, null!), typeof(SentryServiceExtensionsTests).Assembly);

        _service.DidNotReceiveWithAnyArgs().Initialize(default!, default!, default!);
    }

    [Fact]
    public void InitializeFromConfig_WhenDsnPresent_InitializesWithEnvironmentAndAssemblyVersion()
    {
        var assembly = typeof(SentryServiceExtensionsTests).Assembly;
        var config = new FExSentryConfig(Dsn, null!);

        _service.InitializeFromConfig(config, assembly);

        _service.Received(1).Initialize(Dsn, config.SentryEnvironment, assembly.GetName().Version!.ToString());
    }

    [Fact]
    public void InitializeFromConfig_WhenAssemblyHasNoVersion_FallsBackToZeroVersion()
    {
        var config = new FExSentryConfig(Dsn, null!);

        _service.InitializeFromConfig(config, new UnversionedAssembly());

        _service.Received(1).Initialize(Dsn, config.SentryEnvironment, "0.0.0");
    }

    private sealed class UnversionedAssembly : Assembly
    {
        public override AssemblyName GetName() => new("Unversioned");
    }
}
