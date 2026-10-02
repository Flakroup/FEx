#pragma warning disable IDISP001, IDISP004 // Test mocks; the container is owned by the substitute/static state
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Basics;
using FEx.DependencyInjection.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using StrongInject;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

[Collection("FExServiceProvider")] // Static state
public sealed class ResolutionFixTests : IDisposable
{
    public interface IFoo;

    private sealed class FooStatics : StaticsBase
    {
        public static IFoo GetFoo(Func<IFoo>? factory, Func<IFoo>? fallback) => Get(factory, fallback);
    }

    public void Dispose() => FExServiceProvider.Release();

    [Fact]
    public async Task FExServiceProvider_GetRequiredServiceByType_ResolvesRequestedType()
    {
        FExServiceProvider.Release();
#pragma warning disable IDISP001
        await FExServiceProvider.InitializeAsync<TestContainer>();
#pragma warning restore IDISP001
        var provider = FExServiceProvider.Instance;

        provider.GetRequiredService(typeof(IFExServiceProvider)).ShouldBeAssignableTo<IFExServiceProvider>();
        provider.GetInstance(typeof(IFExServiceProvider)).ShouldBeAssignableTo<IFExServiceProvider>();
        provider.GetRequiredService<IFExServiceProvider>(typeof(IFExServiceProvider)).ShouldNotBeNull();
    }

    [Fact]
    public void StrongInject_TryResolveService_PropagatesFactoryException()
    {
        var container = Substitute.For<IDisposable, IContainer<IFoo>>();
        ((IContainer<IFoo>)container).Resolve().Returns(_ => throw new InvalidOperationException("boom"));
        using var provider = new FExStrongInjectServiceProvider();
        provider.SetServiceProvider(container);

        Should.Throw<InvalidOperationException>(() => provider.TryResolveService<IFoo>()).Message.ShouldBe("boom");
    }

    [Fact]
    public void StrongInject_TryResolveService_ReturnsDefaultWhenNotRegistered()
    {
        using var provider = new FExStrongInjectServiceProvider();
        provider.SetServiceProvider(Substitute.For<IDisposable>());

        provider.TryResolveService<IFoo>().ShouldBeNull();
    }

    [Theory]
    [InlineData("a:b:c:d: reason", " reason")]
    [InlineData("no colons here", "no colons here")]
    [InlineData("a:b", "a:b")]
    public void MicrosoftDI_ExtractReason_NeverThrowsOnUnexpectedShape(string message, string expected) =>
        FExMicrosoftDIServiceProvider.ExtractReason(message).ShouldBe(expected);

    [Fact]
    public void StaticsBase_Get_UsesFallbackWhenProviderMissing()
    {
        var fallback = Substitute.For<IFoo>();

        FooStatics.GetFoo(() => null!, () => fallback).ShouldBe(fallback);
    }

    [Fact]
    public void StaticsBase_Get_PropagatesUnexpectedFactoryException() =>
        Should.Throw<NullReferenceException>(() =>
            FooStatics.GetFoo(() => throw new NullReferenceException(), () => Substitute.For<IFoo>()));
}
