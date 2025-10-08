using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>
/// Simplified tests to validate the Multi-DI architecture fundamentals.
/// </summary>
public sealed class SimpleArchitectureTests : IDisposable
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
        MethodInfo initializeMethod = typeof(FExServiceProvider).GetMethods()
            .Where(m => m.Name == "Initialize" && m.IsGenericMethodDefinition)
            .FirstOrDefault();

        MethodInfo initializeAsyncMethod = typeof(FExServiceProvider).GetMethods()
            .Where(m => m.Name == "InitializeAsync" && m.IsGenericMethodDefinition)
            .FirstOrDefault();

        initializeMethod.ShouldNotBeNull("StrongInject initialization should be available");
        initializeAsyncMethod.ShouldNotBeNull("External DI initialization should be available");

        // Verify static methods exist
        MethodInfo getMethod = typeof(FExServiceProvider).GetMethods()
            .Where(m => m.Name == "Get" && m.IsGenericMethodDefinition)
            .FirstOrDefault();

        MethodInfo getAsyncMethod = typeof(FExServiceProvider).GetMethods()
            .Where(m => m.Name == "GetAsync" && m.IsGenericMethodDefinition)
            .FirstOrDefault();

        getMethod.ShouldNotBeNull("Static service resolution should be available");
        getAsyncMethod.ShouldNotBeNull("Static async service resolution should be available");
    }

    [Fact]
    public void ProviderInterface_ShouldIncludeConfigureServiceProviderAsync()
    {
        // Verify the new ConfigureServiceProviderAsync method is available
        MethodInfo configureMethod = typeof(IFExServiceProvider).GetMethod("ConfigureServiceProviderAsync");

        configureMethod.ShouldNotBeNull("ConfigureServiceProviderAsync should be available on provider interface");
        configureMethod.ReturnType.ShouldBe(typeof(ValueTask), "Should return ValueTask for async configuration");
    }

    #region IDisposable
    public void Dispose()
    {
        // Cleanup
        GC.SuppressFinalize(this);
    }
    #endregion
}