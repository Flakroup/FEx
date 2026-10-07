using FEx.Json.Abstractions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Json.Tests;

public sealed class DIMetaTests
{
    public interface IFoo;

    public sealed class Foo : IFoo;

    private static async Task<DIMeta> CreateAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFoo, Foo>();
        var meta = new DIMeta();
        await meta.OnCompleteInitializationAsync(services);

        return meta;
    }

    [Fact]
    public async Task IsRegistered_ReportsOnlyRegisteredServiceTypes()
    {
        var meta = await CreateAsync();

        meta.IsRegistered(typeof(IFoo)).ShouldBeTrue();
        meta.IsRegistered(typeof(DIMetaTests)).ShouldBeFalse();
    }

    [Fact]
    public async Task RegisteredTypeFor_ReturnsTheImplementationOfARegisteredService()
    {
        var meta = await CreateAsync();

        meta.RegisteredTypeFor(typeof(IFoo)).ShouldBe(typeof(Foo));
    }

    [Fact]
    public async Task RegisteredTypeFor_ReturnsTheTypeItselfWhenNothingIsRegistered()
    {
        var meta = await CreateAsync();

        meta.RegisteredTypeFor(typeof(DIMetaTests)).ShouldBe(typeof(DIMetaTests));
        meta.RegisteredTypeFor(null).ShouldBeNull();
    }
}
