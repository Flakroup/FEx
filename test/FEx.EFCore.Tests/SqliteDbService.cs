using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace FEx.EFCore.Tests;

/// <summary>A real database service over the contexts a test registers as scoped services.</summary>
internal sealed class SqliteDbService<TDbContext> : DbServiceBase<TDbContext>, IEFCoreDatabaseBackedService<TDbContext>
    where TDbContext : DbContext
{
    public string? DbKey => null;

    public SqliteDbService(IServiceProvider services)
        : base(new ScopeProvider(services), new(Substitute.For<IFExLogger>()), Substitute.For<IFExDbConfig>(), [])
    {
    }

    // Only the mapping snapshot the dictionary needs; the base also probes the SQL server and runs migrations.
    protected override async Task OnInitializeAsync() => await EnsureMappingSnapshotAsync();

    private sealed class ScopeProvider : IScopeProvider
    {
        private readonly IServiceProvider _services;

        public ScopeProvider(IServiceProvider services) => _services = services;

        public IServiceScope CreateScope() => _services.CreateScope();
    }
}
