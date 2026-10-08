using FEx.Telemetry.Sentry;
using Shouldly;
using System;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class FExSentryConfigTests : IDisposable
{
    private const string EnvVar = "FEX_TELEMETRY_TESTS_SENTRY_DSN";

    public FExSentryConfigTests() => Environment.SetEnvironmentVariable(EnvVar, null);

    public void Dispose() => Environment.SetEnvironmentVariable(EnvVar, null);

    [Fact]
    public void Dsn_WhenEnvVarMissing_UsesConstructorValue()
    {
        var sut = new FExSentryConfig("https://ctor", EnvVar);

        sut.SentryDsn.ShouldBe("https://ctor");
    }

    [Fact]
    public void Dsn_WhenEnvVarSet_OverridesConstructorValue()
    {
        Environment.SetEnvironmentVariable(EnvVar, "https://env");

        var sut = new FExSentryConfig("https://ctor", EnvVar);

        sut.SentryDsn.ShouldBe("https://env");
    }

    [Fact]
    public void Dsn_WhenNoOverrideNameAndNullDsn_IsEmpty()
    {
        var sut = new FExSentryConfig(null!, null!);

        sut.SentryDsn.ShouldBe(string.Empty);
    }

    [Fact]
    public void Dsn_WhenNoOverrideName_IgnoresEnvironment()
    {
        Environment.SetEnvironmentVariable(EnvVar, "https://env");

        var sut = new FExSentryConfig("https://ctor", null!);

        sut.SentryDsn.ShouldBe("https://ctor");
    }

    [Fact]
    public void DefaultEnvVarName_IsSentryDsn()
    {
        var previous = Environment.GetEnvironmentVariable("SENTRY_DSN");
        try
        {
            Environment.SetEnvironmentVariable("SENTRY_DSN", "https://default-env");

            new FExSentryConfig("https://ctor").SentryDsn.ShouldBe("https://default-env");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SENTRY_DSN", previous);
        }
    }

    [Fact]
    public void Properties_MapToTelemetryBase()
    {
        var sut = new TestSentryConfig();

        sut.AttachScreenshot.ShouldBeFalse();
        sut.EnvironmentId.ShouldBe(sut.AppEnvironment);
        sut.SentryEnvironment.ShouldBe(sut.EnvironmentParamValue);
    }

    [Fact]
    public void AccessToken_CanBeAssignedWithoutSideEffects()
    {
        var sut = new FExSentryConfig("https://ctor", EnvVar);

        sut.AccessToken = "abc";

        sut.AccessToken.ShouldBe("abc");
    }

    private sealed class TestSentryConfig : FExSentryConfig
    {
        public TestSentryConfig() : base("https://ctor", EnvVar)
        {
        }

        public string EnvironmentParamValue => EnvironmentParam;
    }
}
