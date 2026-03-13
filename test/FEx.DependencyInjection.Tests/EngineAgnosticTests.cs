using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

/// <summary>
/// Tests to verify the engine-agnostic Multi-DI architecture works correctly.
/// </summary>
[Collection("FExServiceProvider")] // Disable parallel execution due to static state
public class EngineAgnosticTests : IDisposable
{
    [Fact]
    public void GenericModuleInterface_ShouldSupportEngineSpecificContexts()
    {
        // Arrange
        using var container = new TestContainer();

        // Act - Get Microsoft DI specific modules directly from container
        var microsoftModules = container.Resolve<IInitializeModule<IServiceCollection>[]>().Value;

        // Assert
        microsoftModules.ShouldNotBeNull();
        microsoftModules.Length.ShouldBeGreaterThan(0);

        // Verify each module has the correct generic signature
        foreach (var module in microsoftModules)
        {
            var moduleType = module.GetType();
            var interfaces = moduleType.GetInterfaces();

            var hasCorrectInterface = false;

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType
                    && iface.GetGenericTypeDefinition() == typeof(IInitializeModule<>))
                {
                    var genericArg = iface.GetGenericArguments()[0];

                    if (genericArg == typeof(IServiceCollection))
                    {
                        hasCorrectInterface = true;

                        break;
                    }
                }
            }

            hasCorrectInterface.ShouldBeTrue(
                $"Module {moduleType.Name} should implement IInitializeModule<IServiceCollection>");
        }
    }

    [Fact]
    public async Task BaseModuleClass_ShouldProvideContainerAccess()
    {
        // Arrange
        await FExServiceProvider.InitializeAsync<TestContainer>();

        // Act & Assert - Test module should be able to access its container
        Should.NotThrow(() =>
        {
            var testModule = new TestInitializeModule();
            var testContainer = testModule.TestGetModule();
            testContainer.ShouldNotBeNull();
        });
    }

    [Fact]
    public void FutureEngineSupport_ShouldBeStructurallyReady()
    {
        // This test validates that the architecture supports future DI engines
        // without requiring changes to FEx core

        // Arrange
        using var container = new TestContainer();

        // Act - Verify Microsoft DI modules exist (proving the pattern works)
        var microsoftModules = container.Resolve<IInitializeModule<IServiceCollection>[]>().Value;

        // Assert - Architecture should support any engine context
        typeof(IInitializeModule<>).IsGenericTypeDefinition.ShouldBeTrue();
        typeof(InitializeModule<,>).IsGenericTypeDefinition.ShouldBeTrue();

        microsoftModules.ShouldNotBeNull();
        microsoftModules.Length.ShouldBeGreaterThan(0);

        // Verify the pattern works - any future engine could follow this same pattern:
        // 1. Implement IInitializeModule<TheirEngineContext>
        // 2. Extend InitializeModule<TContainer, TheirEngineContext>
        // 3. Register with StrongInject as IInitializeModule<TheirEngineContext>
        // 4. Their provider discovers and runs these modules
    }

    #region IDisposable
    public void Dispose()
    {
        // Cleanup per-test containers
        FExServiceProvider.Release();
        GC.SuppressFinalize(this);
    }
    #endregion
}

/// <summary>
/// Test helper class to validate the InitializeModule base class functionality.
/// </summary>
internal class TestInitializeModule : InitializeModule<IFExDependencyInjectionContainer, IServiceCollection>
{
    public IFExDependencyInjectionContainer TestGetModule() => GetModule();

    protected override void RegisterServices(IFExDependencyInjectionContainer container, IServiceCollection services)
    {
        // Test implementation - no actual registrations needed
    }
}