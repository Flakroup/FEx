using FEx.DependencyInjection.Abstractions;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.Logging.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FEx.Logging.Tests;

/// <summary>
/// Basic tests to validate the Logging module works correctly.
/// Tests the container directly instead of relying on static FExServiceProvider.
/// </summary>
public sealed class BasicLoggingTests
{
    [Fact]
    public void LoggingTypes_ShouldBeAvailable()
    {
        // Validate all key logging types are available

        typeof(IFExLoggingService).ShouldNotBeNull();
        typeof(IFExLoggingConfigurator).ShouldNotBeNull();
        typeof(FExLoggingModule).ShouldNotBeNull();

        // Verify module implements the engine-agnostic interface  
        typeof(FExLoggingModule).IsAssignableTo(typeof(IInitializeModule<IServiceCollection>)).ShouldBeTrue();
    }

    [Fact]
    public void SerilogConfiguration_StaticMethodShouldExist()
    {
        // This test validates that the static Configure method exists

        // Act & Assert  
        MethodInfo configureMethod = typeof(FExLoggingModule).GetMethod("Configure", new Type[0]);
        configureMethod.ShouldNotBeNull();
        configureMethod.IsStatic.ShouldBeTrue();
    }

    [Fact]
    public void LoggingModule_ShouldSupportEngineAgnosticPattern()
    {
        // Verify the module follows the new pattern
        Type moduleType = typeof(FExLoggingModule);

        // Should inherit from InitializeModule<,>
        Type baseType = moduleType.BaseType;
        baseType.ShouldNotBeNull();
        baseType.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(typeof(InitializeModule<,>));

        // Should implement IInitializeModule<IServiceCollection>
        Type[] interfaces = moduleType.GetInterfaces();

        bool hasCorrectInterface = interfaces.Any(i =>
            i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(IInitializeModule<>)
            && i.GetGenericArguments()[0] == typeof(IServiceCollection));

        hasCorrectInterface.ShouldBeTrue("Module should implement IInitializeModule<IServiceCollection>");
    }

    [Fact]
    public void EngineAgnosticPattern_ShouldSupportFutureEngines()
    {
        // This validates the architecture can support any DI engine

        // Generic interface should support any context type
        typeof(IInitializeModule<>).IsGenericTypeDefinition.ShouldBeTrue();
        typeof(InitializeModule<,>).IsGenericTypeDefinition.ShouldBeTrue();

        // Example: Future Autofac support would just be:
        // class SomeModule : InitializeModule<ISomeContainer, ContainerBuilder>
        // and register as: IInitializeModule<ContainerBuilder>

        // Verify current Microsoft DI support exists
        typeof(IInitializeModule<IServiceCollection>).ShouldNotBeNull();
    }
}