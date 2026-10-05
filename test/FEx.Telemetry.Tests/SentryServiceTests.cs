using FEx.Telemetry.Sentry.Services;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Telemetry.Tests;

// An empty DSN keeps the Sentry SDK disabled, so nothing is ever sent over the network.
public sealed class SentryServiceTests
{
    [Fact]
    public void IsInitialized_BeforeInitialize_IsFalse()
    {
        using var sut = new SentryService();

        sut.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Initialize_SetsIsInitialized()
    {
        using var sut = new SentryService();

        sut.Initialize(string.Empty, "test", "1.0.0");

        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Initialize_CalledTwice_ReplacesPreviousHandleWithoutThrowing()
    {
        using var sut = new SentryService();

        sut.Initialize(string.Empty, "test", "1.0.0");

        Should.NotThrow(() => sut.Initialize(string.Empty, "test", "2.0.0"));
        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void CaptureException_WhenSdkDisabled_DoesNotThrow()
    {
        using var sut = new SentryService();
        sut.Initialize(string.Empty, "test", "1.0.0");

        Should.NotThrow(() => sut.CaptureException(new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task FlushAsync_WithDefaultAndExplicitTimeout_Completes()
    {
        using var sut = new SentryService();
        sut.Initialize(string.Empty, "test", "1.0.0");

        await sut.FlushAsync();
        await sut.FlushAsync(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void Dispose_WithoutInitialize_DoesNotThrow()
    {
        using IDisposable sut = new SentryService();

        Should.NotThrow(sut.Dispose);
    }

    [Fact]
    public void Dispose_CalledRepeatedlyAfterInitialize_DoesNotThrow()
    {
        var service = new SentryService();
        service.Initialize(string.Empty, "test", "1.0.0");
        IDisposable sut = service;

        Should.NotThrow(() =>
        {
            sut.Dispose();
            sut.Dispose();
        });
    }
}
