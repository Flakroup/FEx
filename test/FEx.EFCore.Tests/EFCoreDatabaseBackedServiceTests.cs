using FEx.Core.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Configuration;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Services;
using FEx.Sqlx.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

public class EFCoreDatabaseBackedServiceTests
{
    [Fact]
    public async Task EnsureIsInitializedAsync_InitializationFails_Throws()
    {
        using var sut = CreateFailingService();

        var ex = await Should.ThrowAsync<Exception>(sut.Ensure);
        ex.ToString().ShouldContain("boom");
    }

    [Fact]
    public async Task EnsureIsInitializedAsync_CalledAgainAfterFailure_ThrowsAgain()
    {
        using var sut = CreateFailingService();

        await Should.ThrowAsync<Exception>(sut.Ensure);

        var ex = await Should.ThrowAsync<Exception>(sut.Ensure);
        ex.ToString().ShouldContain("boom");
    }

    [Fact]
    public async Task EnsureIsInitializedAsync_BackgroundInitializationFaulted_Throws()
    {
        using var sut = CreateFailingService();

        sut.BeginInitialization();
        await WaitUntilFinishedAsync(sut);

        var ex = await Should.ThrowAsync<Exception>(sut.Ensure);
        ex.ToString().ShouldContain("boom");
    }

    private static async Task WaitUntilFinishedAsync(TestService sut)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (!sut.HasFinishedInitialization && DateTime.UtcNow < deadline)
            await Task.Delay(10, TestContext.Current.CancellationToken);

        sut.HasFinishedInitialization.ShouldBeTrue();
    }

    private static TestService CreateFailingService()
    {
        var dbHelper = Substitute.For<ISqlDbHelper>();
        dbHelper.InitializeAsync().Returns(Task.FromException(new InvalidOperationException("boom")));

        var config = Substitute.For<IDbServiceConfig>();
        config.DbConfig.Returns(Substitute.For<IFExDbConfig>());

        return new(Substitute.For<IScopeProvider>(),
            config,
            new(Substitute.For<FEx.Agnostics.Abstractions.Interfaces.IFExLogger>()),
            dbHelper);
    }

    private sealed class TestService : EFCoreDatabaseBackedService<TestDbContext>
    {
        public TestService(IScopeProvider scopeProvider,
                           IDbServiceConfig config,
                           ResilientTransaction transaction,
                           ISqlDbHelper dbHelper)
            : base(scopeProvider, config, transaction, dbHelper, [])
        {
        }

        public Task Ensure() => EnsureIsInitializedAsync();
    }
}
