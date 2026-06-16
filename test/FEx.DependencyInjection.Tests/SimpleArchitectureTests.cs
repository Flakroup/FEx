using System;
using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>
/// Simplified tests to validate the Multi-DI architecture fundamentals.
/// </summary>
public sealed class SimpleArchitectureTests
{
    [Fact]
    public void MultiDIArchitecture_ShouldCompileAndBuildSuccessfully()
    {
        // This test validates that the architecture compiles and basic types are available

        // Verify interface hierarchy
        typeof(IFExServiceProvider).ShouldNotBeNull();
        typeof(IInitializeModule<IServiceCollection>).ShouldNotBeNull();
        typeof(InitializeModule<,>).ShouldNotBeNull();

        // Verify concrete implementations exist
        typeof(FExStrongInjectServiceProvider).ShouldNotBeNull();
        typeof(FExMicrosoftDIServiceProvider).ShouldNotBeNull();
        typeof(FExServiceContainer).ShouldNotBeNull();
    }

    [Fact]
    public void FExServiceProviderEntryPoints_ShouldExist()
    {
        // Verify the main entry points exist with correct signatures
        var initializeAsyncMethod = typeof(FExServiceProvider).GetMethods()
            .FirstOrDefault(m => m.Name == "InitializeAsync" && m.IsGenericMethodDefinition);

        initializeAsyncMethod.ShouldNotBeNull("InitializeAsync should be available");

        // Verify static methods exist
        var getMethod = typeof(FExServiceProvider).GetMethods()
            .FirstOrDefault(m => m.Name == "Get" && m.IsGenericMethodDefinition);

        var getAsyncMethod = typeof(FExServiceProvider).GetMethods()
            .FirstOrDefault(m => m.Name == "GetAsync" && m.IsGenericMethodDefinition);

        getMethod.ShouldNotBeNull("Static service resolution should be available");
        getAsyncMethod.ShouldNotBeNull("Static async service resolution should be available");
    }

    [Fact]
    public void ProviderInterface_ShouldIncludeConfigureServiceProviderAsync()
    {
        // Verify the new ConfigureServiceProviderAsync method is available
        var configureMethod = typeof(IFExServiceProvider).GetMethod(nameof(IFExServiceProvider.ConfigureServiceProviderAsync), Type.EmptyTypes);

        configureMethod.ShouldNotBeNull("ConfigureServiceProviderAsync should be available on provider interface");
        configureMethod.ReturnType.ShouldBe(typeof(ValueTask), "Should return ValueTask for async configuration");
    }
}