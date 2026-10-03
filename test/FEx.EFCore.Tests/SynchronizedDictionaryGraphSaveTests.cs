using DynamicData;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Collections;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Models;
using FEx.EFCore.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

/// <summary>
/// Saving a cached entity writes the entity and its owned values, never the related entities it happens to hold:
/// a loaded principal is a snapshot from when it was cached and must not overwrite another writer's changes.
/// </summary>
public sealed class SynchronizedDictionaryGraphSaveTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private readonly GraphDbService _dbService;

    public SynchronizedDictionaryGraphSaveTests()
    {
        _connection.Open();

        using (var setup = CreateContext())
        {
            setup.Database.EnsureCreated();
            setup.Categories.Add(new() { Id = 1, Title = "one" });
            setup.Docs.Add(new()
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Address = new() { City = "A" },
                CategoryId = 1
            });
            setup.SaveChanges();
        }

        var services = new ServiceCollection();
        services.AddScoped(_ => CreateContext());
        _services = services.BuildServiceProvider();
        _dbService = new(new ScopeProvider(_services));
    }

    public void Dispose()
    {
        _dbService.Dispose();
        _services.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SavingACachedEntity_DoesNotOverwriteAnotherWritersChangeToItsLoadedPrincipal()
    {
        using var sut = new GraphDictionary(_dbService);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);

        using (var writerB = CreateContext())
        {
            var category = await writerB.Categories.SingleAsync(c => c.Id == 1, TestContext.Current.CancellationToken);
            category.Title = "renamed by B";
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        doc.Name = "from A";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        (await reader.Categories.SingleAsync(c => c.Id == 1, TestContext.Current.CancellationToken)).Title
            .ShouldBe("renamed by B");
        (await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken)).Name.ShouldBe("from A");
    }

    [Fact]
    public async Task SavingACachedEntity_StillSavesItsOwnedValues()
    {
        using var sut = new GraphDictionary(_dbService);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);

        doc.Address.City = "B";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        (await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken)).Address.City
            .ShouldBe("B");
    }

    [Fact]
    public async Task SavingACachedEntity_StillInsertsANewPrincipalItReferences()
    {
        using var sut = new GraphDictionary(_dbService);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);

        doc.Category = new() { Title = "new" };
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.Include(d => d.Category)
            .SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Category.ShouldNotBeNull().Title.ShouldBe("new");
        (await reader.Categories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
    }

    private GraphDbContext CreateContext() => new(_connection);

    private async Task<GraphDoc> LoadWithCategoryAsync()
    {
        using var ctx = CreateContext();

        return await ctx.Docs.AsNoTracking()
            .Include(d => d.Category)
            .SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
    }

    public sealed class Category
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
    }

    public sealed class Address
    {
        public string City { get; set; } = "";
    }

    public sealed class GraphDoc : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        [ConcurrencyCheck]
        public int Version { get; set; }

        public Address Address { get; set; } = new();

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    public sealed class GraphDbContext : DbContext
    {
        public DbSet<GraphDoc> Docs => Set<GraphDoc>();
        public DbSet<Category> Categories => Set<Category>();

        public GraphDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder<GraphDbContext>().UseSqlite(connection).Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<GraphDoc>().OwnsOne(d => d.Address);
    }

    private sealed class ScopeProvider : IScopeProvider
    {
        private readonly IServiceProvider _services;

        public ScopeProvider(IServiceProvider services) => _services = services;

        public IServiceScope CreateScope() => _services.CreateScope();
    }

    private sealed class GraphDbService : DbServiceBase<GraphDbContext>, IEFCoreDatabaseBackedService<GraphDbContext>
    {
        public string? DbKey => null;

        public GraphDbService(IScopeProvider scopeProvider)
            : base(scopeProvider, new(Substitute.For<IFExLogger>()), Substitute.For<IFExDbConfig>(), [])
        {
        }
    }

    private sealed class GraphDictionary : SynchronizedDictionary<int, GraphDoc, GraphDbContext>
    {
        public GraphDictionary(IEFCoreDatabaseBackedService<GraphDbContext> dbService)
            : base(dbService, nameof(GraphDoc.Id))
        {
        }

        public Task<bool> SaveAsync(GraphDoc doc) =>
            SaveChangesResolvingConflictsAsync(
                [new ChangeInfo<int, GraphDoc>(new Change<GraphDoc, int>(ChangeReason.Refresh, doc.Id, doc))]);

        protected override IQueryable<GraphDoc> IncludeInEntity(IQueryable<GraphDoc> query) =>
            query.Include(d => d.Category);

        protected override Expression<Func<GraphDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<GraphDoc> DbSetAccessor(GraphDbContext ctx) => ctx.Docs;

        protected override GraphDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
