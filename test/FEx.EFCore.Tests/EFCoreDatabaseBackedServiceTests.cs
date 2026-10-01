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
        var dbHelper = Substitute.For<ISqlDbHelper>();
        dbHelper.InitializeAsync().Returns(Task.FromException(new InvalidOperationException("boom")));

        var config = Substitute.For<IDbServiceConfig>();
        config.DbConfig.Returns(Substitute.For<IFExDbConfig>());

        using var sut = new TestService(Substitute.For<IScopeProvider>(),
            config,
            new(Substitute.For<FEx.Agnostics.Abstractions.Interfaces.IFExLogger>()),
            dbHelper);

        var ex = await Should.ThrowAsync<Exception>(sut.Ensure);
        ex.ToString().ShouldContain("boom");
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
