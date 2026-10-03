#pragma warning disable IDISP001, IDISP004 // Test mocks; the container is owned by the substitute/static state
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Basics;
using FEx.DependencyInjection.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using StrongInject;
using System;
using System.Reflection;
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

        Should.Throw<InvalidOperationException>(provider.TryResolveService<IFoo>).Message.ShouldBe("boom");
    }

    [Fact]
    public void StrongInject_TryResolveService_ReturnsDefaultWhenNotRegistered()
    {
        using var provider = new FExStrongInjectServiceProvider();
        provider.SetServiceProvider(Substitute.For<IDisposable>());

        provider.TryResolveService<IFoo>().ShouldBeNull();
    }

    [Fact]
    public async Task FExServiceProvider_GetService_ResolvesRequestedTypeOrNull()
    {
        FExServiceProvider.Release();
        await FExServiceProvider.InitializeAsync<TestContainer>();
        var provider = FExServiceProvider.Instance;

        provider.GetService(typeof(IFExServiceProvider)).ShouldNotBeNull().ShouldBeAssignableTo<IFExServiceProvider>();
        provider.GetService(typeof(IFoo)).ShouldBeNull();
    }

    [Fact]
    public async Task FExServiceProvider_GetRequiredServiceByType_UnwrapsReflectionException()
    {
        FExServiceProvider.Release();
        await FExServiceProvider.InitializeAsync<TestContainer>();

        // IFoo isn't registered: the container throws inside the reflection call and the caller must
        // see the original InvalidOperationException, not a TargetInvocationException.
        Should.Throw<InvalidOperationException>(() => FExServiceProvider.Instance.GetRequiredService(typeof(IFoo)));
    }

    [Theory]
    [InlineData("Error while validating the service descriptor 'ServiceType: A Lifetime: Singleton ImplementationType: B': Unable to resolve X", "Unable to resolve X")]
    [InlineData("Error while validating the service descriptor 'ServiceType: A Lifetime: Singleton ServiceKey: k ImplementationType: B': Unable to resolve X", "Unable to resolve X")]
    [InlineData("no marker here", "no marker here")]
    [InlineData("a:b", "a:b")]
    public void MicrosoftDI_ExtractReason_HandlesKeyedAndUnexpectedShapes(string message, string expected) =>
        FExMicrosoftDIServiceProvider.ExtractReason(message).ShouldBe(expected);

    [Fact]
    public void StaticsBase_Get_UsesFallbackWhenFactoryReturnsNull()
    {
        var fallback = Substitute.For<IFoo>();

        FooStatics.GetFoo(() => null!, () => fallback).ShouldBe(fallback);
    }

    [Fact]
    public void StaticsBase_Get_UsesFallbackWhenProviderIsMissing()
    {
        var fallback = Substitute.For<IFoo>();

        using (WithStaticsProvider(null))
            FooStatics.GetFoo(null, () => fallback).ShouldBe(fallback);
    }

    [Fact]
    public void StaticsBase_Get_UsesFallbackWhenServiceIsNotRegistered()
    {
        var fallback = Substitute.For<IFoo>();
        var provider = Substitute.For<IFExServiceProvider>();
        provider.GetInstance<IFoo>().Returns(_ => throw new InvalidOperationException("not registered"));

        using (WithStaticsProvider(provider))
            FooStatics.GetFoo(null, () => fallback).ShouldBe(fallback);
    }

    [Fact]
    public void StaticsBase_Get_PropagatesUnexpectedFactoryException() =>
        Should.Throw<NullReferenceException>(() =>
            FooStatics.GetFoo(() => throw new NullReferenceException(), () => Substitute.For<IFoo>()));

    /// <summary>
    /// Temporarily replaces the process-wide provider (its setter can't be reset) and restores it on dispose.
    /// </summary>
    private static ProviderScope WithStaticsProvider(IFExServiceProvider? provider) => new(provider);

    private sealed class ProviderScope : IDisposable
    {
        private static readonly FieldInfo _field =
            typeof(StaticsBase).GetField("_serviceProvider", BindingFlags.NonPublic | BindingFlags.Static)!;

        private readonly object? _previous;

        public ProviderScope(IFExServiceProvider? provider)
        {
            _previous = _field.GetValue(null);
            _field.SetValue(null, provider);
        }

        public void Dispose() => _field.SetValue(null, _previous);
    }
}
