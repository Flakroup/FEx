using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace FEx.Telemetry.Tests;

public sealed class FExTelemetryConfigBaseTests
{
    private readonly TestTelemetryConfig _sut = new();

    [Fact]
    public void AccessToken_WhenSet_ReturnsValueAndInvokesHook()
    {
        _sut.AccessToken = "token-1";

        _sut.AccessToken.ShouldBe("token-1");
        _sut.LastToken.ShouldBe("token-1");
        _sut.TokenChangeCount.ShouldBe(1);
    }

    [Fact]
    public void AccessToken_WhenSetToSameValue_DoesNotInvokeHookAgain()
    {
        _sut.AccessToken = "token-1";
        _sut.AccessToken = "token-1";

        _sut.TokenChangeCount.ShouldBe(1);
    }

    [Fact]
    public void AccessToken_WhenChanged_RaisesPropertyChanged()
    {
        var raised = new List<string?>();
        _sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _sut.AccessToken = "token-1";

        raised.ShouldContain(nameof(TestTelemetryConfig.AccessToken));
    }

    [Fact]
    public void AccessToken_WhenSetToNull_ThrowsAndKeepsPreviousValue()
    {
        _sut.AccessToken = "token-1";

        Should.Throw<ArgumentNullException>(() => _sut.AccessToken = null!);

        _sut.AccessToken.ShouldBe("token-1");
        _sut.TokenChangeCount.ShouldBe(1);
    }

    [Fact]
    public void EnvironmentParam_IsDevelopmentOnlyWhenDebuggerAttached()
    {
        var expected = System.Diagnostics.Debugger.IsAttached ? "development" : "production";

        _sut.EnvironmentParamValue.ShouldBe(expected);
    }

    [Fact]
    public void AppEnvironment_ContainsEnvironmentPlatformAndCulture()
    {
        var env = _sut.AppEnvironment;

        env.ShouldStartWith(_sut.EnvironmentParamValue + " ~ ");
        env.ShouldEndWith(" ~ " + CultureInfo.InstalledUICulture.EnglishName);
    }

    [Fact]
    public void AppEnvironment_WhenPersonNotAdded_OmitsUserName()
    {
        _sut.PersonUserName = () => "alice";
        _sut.AddPersonToEnvironment = false;

        _sut.AppEnvironment.ShouldNotContain("alice");
        _sut.AppEnvironment.ShouldStartWith(_sut.EnvironmentParamValue);
    }

    [Fact]
    public void AppEnvironment_WhenPersonAdded_PrefixesUserName()
    {
        _sut.PersonUserName = () => "alice";
        _sut.AddPersonToEnvironment = true;

        _sut.AppEnvironment.ShouldStartWith("alice_" + _sut.EnvironmentParamValue);
    }

    [Fact]
    public void AppEnvironment_WhenPersonAddedButNoUserNameProvider_OmitsPrefix()
    {
        _sut.AddPersonToEnvironment = true;

        _sut.AppEnvironment.ShouldStartWith(_sut.EnvironmentParamValue);
        _sut.AppEnvironment.ShouldNotContain("_" + _sut.EnvironmentParamValue);
    }

    [Fact]
    public void AppEnvironment_EvaluatesUserNameProviderOnEachRead()
    {
        var name = "alice";
        _sut.PersonUserName = () => name;
        _sut.AddPersonToEnvironment = true;

        _sut.AppEnvironment.ShouldStartWith("alice_");
        name = "bob";
        _sut.AppEnvironment.ShouldStartWith("bob_");
    }

    [Fact]
    public void Config_ImplementsContractAndPersonEmailRoundTrips()
    {
        IFExTelemetryConfig config = _sut;
        config.PersonEmail = () => "a@b.c";

        config.PersonEmail!().ShouldBe("a@b.c");
        config.ShouldBeAssignableTo<INotifyPropertyChanged>();
    }
}
