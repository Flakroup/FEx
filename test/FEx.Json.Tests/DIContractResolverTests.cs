using FEx.Json.Abstractions.Helpers;
using FEx.Json.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Json.Tests;

[Collection("FExServiceProvider")] // asserts on the uninitialized static provider
public sealed class DIContractResolverTests
{
    public interface IFoo
    {
        string Name { get; set; }
    }

    public sealed class Foo : IFoo
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void RegisterServices_RecordsTheRegistrationsOfTheCollection()
    {
        var services = new ServiceCollection();
        services.AddTransient<IFoo, Foo>();
        var meta = new DIMeta();

        meta.RegisterServices(services);

        meta.IsRegistered(typeof(IFoo)).ShouldBeTrue();
        meta.IsTransient(typeof(IFoo)).ShouldBeTrue();
        meta.RegisteredTypeFor(typeof(IFoo)).ShouldBe(typeof(Foo));
    }

    [Fact]
    public async Task ResolveContract_FactoryRegistration_DoesNotRecurseInfinitely()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFoo>(_ => new Foo());
        var meta = new DIMeta();
        await meta.OnCompleteInitializationAsync(services);
        var resolver = new DIContractResolver(meta);

        var contract = resolver.ResolveContract(typeof(IFoo));

        contract.UnderlyingType.ShouldBe(typeof(IFoo));
    }

    [Fact]
    public async Task ResolveContract_TypeRegistration_UsesImplementationType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFoo, Foo>();
        var meta = new DIMeta();
        await meta.OnCompleteInitializationAsync(services);
        var resolver = new DIContractResolver(meta);

        var contract = resolver.ResolveContract(typeof(IFoo));

        contract.UnderlyingType.ShouldBe(typeof(Foo));
    }

    [Fact]
    public async Task ResolveContract_SingletonRegistration_DoesNotUseDICreator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFoo, Foo>();
        var meta = new DIMeta();
        await meta.OnCompleteInitializationAsync(services);
        var resolver = new DIContractResolver(meta);

        var contract = (JsonObjectContract)resolver.ResolveContract(typeof(IFoo));

        // The DI creator would hand out the shared instance (here it would throw: no container is initialized).
        contract.DefaultCreator!().ShouldBeOfType<Foo>();
    }

    [Fact]
    public async Task ResolveContract_TransientRegistration_UsesDICreator()
    {
        var services = new ServiceCollection();
        services.AddTransient<IFoo, Foo>();
        var meta = new DIMeta();
        await meta.OnCompleteInitializationAsync(services);
        var resolver = new DIContractResolver(meta);

        var contract = (JsonObjectContract)resolver.ResolveContract(typeof(IFoo));

        // No FExServiceProvider container is initialized, so reaching the DI creator throws.
        Should.Throw<ArgumentNullException>(() => contract.DefaultCreator!());
    }
}
