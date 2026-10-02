using FEx.Json.Helpers;
using FEx.Json.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Json.Tests;

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
}
