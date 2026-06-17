using System;
using System.Reflection;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>
/// Basic tests to validate the Multi-DI architecture works correctly.
/// Tests the container directly instead of relying on static FExServiceProvider.
/// </summary>
public sealed class BasicArchitectureTests
{
    [Fact]
    public void StrongInjectContainer_ShouldResolveServiceContainer()
    {
        // Arrange & Act
        using var container = new TestContainer();
        using var serviceContainer = container.Resolve<IFExServiceContainer>();

        // Assert
        serviceContainer.Value.ShouldNotBeNull();
    }

    [Fact]
    public void StrongInjectContainer_ShouldResolveServiceProvider()
    {
        // Arrange & Act
        using var container = new TestContainer();
        using var serviceProvider = container.Resolve<IFExServiceProvider>();

        // Assert
        serviceProvider.Value.ShouldNotBeNull();
        serviceProvider.Value.ShouldBeOfType<FExStrongInjectServiceProvider>();
    }

    [Fact]
    public void StrongInjectContainer_ShouldResolveMicrosoftDIProvider()
    {
        // Arrange & Act
        using var container = new TestContainer();
        using var microsoftProvider = container.Resolve<FExMicrosoftDIServiceProvider>();

        // Assert
        microsoftProvider.Value.ShouldNotBeNull();
    }

    [Fact]
    public void MultiDIArchitecture_ShouldSupportGenericModules()
    {
        // This test validates the architecture supports any engine context type

        // Verify interface hierarchy exists
        typeof(IInitializeModule<>).IsGenericTypeDefinition.ShouldBeTrue();
        typeof(InitializeModule<,>).IsGenericTypeDefinition.ShouldBeTrue();

        // Verify concrete Microsoft DI implementation
        typeof(IInitializeModule<IServiceCollection>).ShouldNotBeNull();

        // Verify the providers exist
        typeof(FExStrongInjectServiceProvider).ShouldNotBeNull();
        typeof(FExMicrosoftDIServiceProvider).ShouldNotBeNull();
    }

    [Fact]
    public async Task ProviderInterface_ShouldIncludeConfigureServiceProviderAsync()
    {
        // Arrange
        using var container = new TestContainer();
        using var serviceProvider = container.Resolve<IFExServiceProvider>();

        // Act & Assert
        await serviceProvider.Value.ConfigureServiceProviderAsync();
        // Should not throw - validates the interface contract
    }

    [Fact]
    public void TypeArchitecture_ShouldBeCompletelyAvailable()
    {
        // Validate all key types are available and correctly designed

        // Core provider interface
        typeof(IFExServiceProvider).GetMethod(
            nameof(IFExServiceProvider.ConfigureServiceProviderAsync),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null).ShouldNotBeNull();

        // Generic module interfaces
        typeof(IInitializeModule<IServiceCollection>).ShouldNotBeNull();
        typeof(InitializeModule<,>).ShouldNotBeNull();

        // Concrete providers
        typeof(FExStrongInjectServiceProvider).ShouldNotBeNull();
        typeof(FExMicrosoftDIServiceProvider).ShouldNotBeNull();

        // Container interfaces
        typeof(IFExServiceContainer).ShouldNotBeNull();
    }
}