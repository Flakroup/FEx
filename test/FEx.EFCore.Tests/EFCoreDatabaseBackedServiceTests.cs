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

    /// <summary>
    /// A failure is kept and rethrown by InitializeAsync; EnsureIsInitializedAsync must start over, so a service whose
    /// first initialization failed transiently recovers on the next call.
    /// </summary>
    [Fact]
    public async Task EnsureIsInitializedAsync_AfterATransientFailure_RecoversOnTheNextCall()
    {
        var dbHelper = Substitute.For<ISqlDbHelper>();
        dbHelper.InitializeAsync()
            .Returns(Task.FromException(new InvalidOperationException("boom")), Task.CompletedTask);
        dbHelper.SQLInstance.Returns("instance");
        using var sut = CreateService(dbHelper);

        (await Should.ThrowAsync<Exception>(sut.Ensure)).ToString().ShouldContain("boom");

        await sut.Ensure();

        sut.IsInitialized.ShouldBeTrue();
        sut.HookRuns.ShouldBe(1);
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

        return CreateService(dbHelper);
    }

    /// <summary>The SQL instance is resolved before the dependencies initialize, so they already see it.</summary>
    [Fact]
    public async Task SqlInstance_IsSetBeforeTheDependenciesInitialize()
    {
        var dbHelper = Substitute.For<ISqlDbHelper>();
        dbHelper.InitializeAsync().Returns(Task.CompletedTask);
        dbHelper.SQLInstance.Returns("instance");
        var dbConfig = Substitute.For<IFExDbConfig>();
        string? seenByDependency = null;
        var dependency = Substitute.For<IAsyncInitializable>();
        dependency.TypeFullName.Returns("dependency");
        dependency.InitializeAsync()
            .Returns(_ =>
            {
                seenByDependency = dbConfig.SqlInstance;

                return Task.CompletedTask;
            });
        using var sut = CreateService(dbHelper, dbConfig, [dependency]);

        await sut.Ensure();

        seenByDependency.ShouldBe("instance");
        dbConfig.SqlInstance.ShouldBe("instance");
    }

    private static TestService CreateService(ISqlDbHelper dbHelper) =>
        CreateService(dbHelper, Substitute.For<IFExDbConfig>(), []);

    private static TestService CreateService(ISqlDbHelper dbHelper,
                                             IFExDbConfig dbConfig,
                                             IAsyncInitializable[] dependencies)
    {
        var config = Substitute.For<IDbServiceConfig>();
        config.DbConfig.Returns(dbConfig);

        return new(Substitute.For<IScopeProvider>(),
            config,
            new(Substitute.For<FEx.Agnostics.Abstractions.Interfaces.IFExLogger>()),
            dbHelper,
            dependencies);
    }

    private sealed class TestService : EFCoreDatabaseBackedService<TestDbContext>
    {
        public TestService(IScopeProvider scopeProvider,
                           IDbServiceConfig config,
                           ResilientTransaction transaction,
                           ISqlDbHelper dbHelper,
                           IAsyncInitializable[] dependencies)
            : base(scopeProvider, config, transaction, dbHelper, dependencies)
        {
        }

        public int HookRuns { get; private set; }

        public Task Ensure() => EnsureIsInitializedAsync();

        // Skips the SQL server probe and migrations of PooledDbService; only the pre-dependency step is under test.
        protected override Task OnInitializeAsync()
        {
            HookRuns++;

            return Task.CompletedTask;
        }
    }
}
