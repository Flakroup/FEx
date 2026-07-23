using System;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Shouldly;
using StrongInject;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>A probe whose disposal we can observe.</summary>
internal sealed class DisposableProbe : IDisposable
{
    public int DisposeCount { get; private set; }
    public bool Disposed => DisposeCount > 0;
    public void Dispose() => DisposeCount++;
}

/// <summary>Minimal StrongInject container that resolves a fresh <see cref="DisposableProbe" /> per resolution.</summary>
[Register(typeof(DisposableProbe))]
internal sealed partial class ProbeContainer : IContainer<DisposableProbe>;

/// <summary>
/// Pins the disposal contract of <see cref="FExServiceContainer" />: a disposable resolved through
/// <see cref="IFExServiceContainer.ResolveService{T}" /> (the path behind the static
/// <c>FExServiceProvider.Get&lt;T&gt;()</c>) must be disposed when the provider is released. Before the fix
/// the resolved <c>Owned&lt;T&gt;</c> was discarded, so the instance leaked - these tests would fail then.
/// </summary>
public sealed class ResolveServiceDisposalTests
{
    private static FExServiceContainer NewServiceContainer(ProbeContainer probeContainer)
    {
        var serviceContainer = new FExServiceContainer();
        serviceContainer.RegisterServices(probeContainer, services: null);
        return serviceContainer;
    }

    [Fact]
    public void ResolveService_KeepsInstanceAliveUntilRelease_ThenDisposesIt()
    {
        using var probeContainer = new ProbeContainer();

        DisposableProbe probe;
        using (var serviceContainer = NewServiceContainer(probeContainer))
        {
            probe = serviceContainer.ResolveService<DisposableProbe>();
            probe.Disposed.ShouldBeFalse("the resolved instance must stay alive while it is in use");
        }

        probe.Disposed.ShouldBeTrue("releasing the provider must dispose the resolved instance (no leak)");
    }

    [Fact]
    public void ResolveService_DisposesEveryResolvedInstance_Once()
    {
        using var probeContainer = new ProbeContainer();

        DisposableProbe first, second;
        using (var serviceContainer = NewServiceContainer(probeContainer))
        {
            first = serviceContainer.ResolveService<DisposableProbe>();
            second = serviceContainer.ResolveService<DisposableProbe>();
            first.ShouldNotBeSameAs(second); // InstancePerResolution - a distinct instance each time
        }

        first.DisposeCount.ShouldBe(1);
        second.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public void ResolveService_AfterRelease_DisposesImmediately()
    {
        using var probeContainer = new ProbeContainer();
        using var serviceContainer = NewServiceContainer(probeContainer);

        var probe = serviceContainer.ResolveService<DisposableProbe>();
        serviceContainer.Release();

        probe.Disposed.ShouldBeTrue("Release must dispose everything resolved so far");
    }
}
