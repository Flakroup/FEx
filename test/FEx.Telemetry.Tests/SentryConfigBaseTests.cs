using FEx.Telemetry.Sentry.Configuration;
using Shouldly;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class SentryConfigBaseTests
{
    [Fact]
    public void Defaults_AreProductionWithoutScreenshot()
    {
        var sut = new DefaultConfig();

        sut.SentryDsn.ShouldBe("dsn");
        sut.AttachScreenshot.ShouldBeFalse();
        sut.EnvironmentId.ShouldBe("production");
    }

    [Fact]
    public void Defaults_CanBeOverridden()
    {
        var sut = new CustomConfig();

        sut.AttachScreenshot.ShouldBeTrue();
        sut.EnvironmentId.ShouldBe("staging");
    }

    private class DefaultConfig : SentryConfigBase
    {
        public override string SentryDsn => "dsn";
    }

    private sealed class CustomConfig : DefaultConfig
    {
        public override bool AttachScreenshot => true;
        public override string EnvironmentId => "staging";
    }
}
