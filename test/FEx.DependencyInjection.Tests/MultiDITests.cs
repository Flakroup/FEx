using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.DependencyInjection.Tests;

[Collection("FExServiceProvider")] // Disable parallel execution due to static state
public class MultiDITests : IDisposable
{
    [Fact]
    public void StrongInjectOnly_ShouldInitializeWithoutMicrosoftDI()
    {
        // Arrange - Ensure clean state
        FExServiceProvider.Release();

        // Act
        TestContainer container = FExServiceProvider.Initialize<TestContainer>();

        // Assert
        FExServiceProvider.ServiceContainer.ShouldNotBeNull();
        container.ShouldNotBeNull();
    }

    [Fact]
    public async Task MicrosoftDI_ShouldInitializeOptionally()
    {
        // Arrange - Need fresh initialization for this test
        FExServiceProvider.Release();
        FExServiceProvider.Initialize<TestContainer>();

        // Act
        await FExServiceProvider.InitializeAsync<FExMicrosoftDIServiceProvider>();

        // Assert - Just verify the initialization completed without errors
        FExServiceProvider.ServiceContainer.ShouldNotBeNull();

        // Verify we can resolve MicrosoftDI-specific services
        FExMicrosoftDIServiceProvider microsoftProvider = FExServiceProvider.Get<FExMicrosoftDIServiceProvider>();
        microsoftProvider.ShouldNotBeNull();
    }

    [Fact]
    public void EngineModules_ShouldOnlyRunForSpecificEngine()
    {
        // Arrange
        using var container = new TestContainer();

        // Act - Get Microsoft DI specific modules directly from container
        IInitializeModule<IServiceCollection>[] microsoftModules =
            container.Resolve<IInitializeModule<IServiceCollection>[]>().Value;

        // Assert
        microsoftModules.ShouldNotBeNull();
        microsoftModules.Length.ShouldBeGreaterThan(0);

        // All modules should exist but not be completed (StrongInject path doesn't run them)
        foreach (IInitializeModule<IServiceCollection> module in microsoftModules)
            module.HasBeenCompleted.ShouldBeFalse("Engine modules should not be completed in StrongInject-only path");
    }

    [Fact]
    public async Task ProviderSwitching_ShouldMaintainServices()
    {
        // Arrange - Start with StrongInject
        FExServiceProvider.Release();
        FExServiceProvider.Initialize<TestContainer>();

        // Verify initial state
        IFExServiceContainer serviceFromStrongInject = FExServiceProvider.Get<IFExServiceContainer>();
        IFExServiceContainer serviceFromNew = FExServiceProvider.Get<IFExServiceContainer>();

        serviceFromStrongInject.ShouldNotBeNull();
        serviceFromNew.ShouldNotBeNull();
        serviceFromStrongInject.ShouldBeSameAs(serviceFromNew);

        // Act - Switch to Microsoft DI
        await FExServiceProvider.InitializeAsync<FExMicrosoftDIServiceProvider>();

        // Assert - Services should still be available
        IFExServiceContainer serviceAfterSwitch = FExServiceProvider.Get<IFExServiceContainer>();
        serviceAfterSwitch.ShouldNotBeNull();
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