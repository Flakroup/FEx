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
/// Graph shapes of a cached value the save path (load the row, apply the cached state) handles; the acceptance cases of
/// #197 are in <see cref="SynchronizedDictionaryLoadAndApplyTests" />.
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
            setup.Docs.AddRange(new GraphDoc
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Address = new() { City = "A" },
                CategoryId = 1,
                Tags = [new() { Label = "t1" }]
            }, new GraphDoc
            {
                Id = 2,
                Name = "second",
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

    /// <summary>A tracking load fixes up the principal's inverse collection, so one root reaches the other.</summary>
    [Fact]
    public async Task BatchOfTwoValuesReachingEachOtherThroughAnInverseCollection_SavesBoth()
    {
        using var sut = new GraphDictionary(_dbService);
        await sut.CacheAllAsync();
        var first = sut[1];
        var second = sut[2];
        first.Category.ShouldNotBeNull().Docs.ShouldContain(second);

        first.Name = "first edited";
        second.Name = "second edited";
        (await sut.SaveAsync(first, second)).ShouldBeTrue();

        (await NamesAsync()).ShouldBe(["first edited", "second edited"]);
    }

    [Fact]
    public async Task OwnedCollection_NewItemAndEditedItem_AreSavedWithTheValue()
    {
        using var sut = new GraphDictionary(_dbService);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);

        doc.Tags[0].Label = "t1 edited";
        doc.Tags.Add(new() { Label = "new" });
        doc.Name = "edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        (await TagsAsync()).ShouldBe(["t1 edited", "new"]);
        (await NamesAsync())[0].ShouldBe("edited");
    }

    [Fact]
    public async Task OwnedCollection_RemovedItem_IsDeleted()
    {
        using var sut = new GraphDictionary(_dbService);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);

        doc.Tags.Clear();
        doc.Name = "edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        (await TagsAsync()).ShouldBeEmpty();
    }

    private async Task<List<string>> NamesAsync()
    {
        using var reader = CreateContext();

        return await reader.Docs.OrderBy(d => d.Id).Select(d => d.Name).ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> TagsAsync()
    {
        using var reader = CreateContext();
        var doc = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);

        return [.. doc.Tags.OrderBy(t => t.Id).Select(t => t.Label)];
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

        public List<GraphDoc> Docs { get; set; } = [];
    }


    public sealed class Tag
    {
        public int Id { get; set; }

        public string Label { get; set; } = "";
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

        public List<Tag> Tags { get; set; } = [];

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var doc = modelBuilder.Entity<GraphDoc>();
            doc.OwnsOne(d => d.Address);
            doc.OwnsMany(d => d.Tags, t => t.HasKey(x => x.Id));
            doc.HasOne(d => d.Category).WithMany(c => c.Docs).HasForeignKey(d => d.CategoryId);
        }
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

        public Task<bool> SaveAsync(params GraphDoc[] docs) =>
            SaveChangesResolvingConflictsAsync(
            [
                .. docs.Select(d => new ChangeInfo<int, GraphDoc>(new Change<GraphDoc, int>(ChangeReason.Refresh, d.Id, d)))
            ]);

        protected override IQueryable<GraphDoc> IncludeInEntity(IQueryable<GraphDoc> query) =>
            query.Include(d => d.Category);

        protected override Expression<Func<GraphDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<GraphDoc> DbSetAccessor(GraphDbContext ctx) => ctx.Docs;

        protected override GraphDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
