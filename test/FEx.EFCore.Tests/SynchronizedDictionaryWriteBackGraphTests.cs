using DynamicData;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Collections;
using FEx.EFCore.Interfaces;
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
/// The write-back after a rejected change must refresh the whole cached graph (owned type, reference navigation) or
/// nothing: a current token over stale related values would let the next edit overwrite the other writer silently.
/// </summary>
public sealed class SynchronizedDictionaryWriteBackGraphTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private readonly GraphDbService _dbService;

    public SynchronizedDictionaryWriteBackGraphTests()
    {
        _connection.Open();

        using (var setup = CreateContext())
        {
            setup.Database.EnsureCreated();
            setup.Categories.AddRange(new Category { Id = 1, Title = "one" }, new Category { Id = 2, Title = "two" });
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
    public async Task WriteBack_ThroughIncludedNavigation_RefreshesTheGraph_AndTheNextEditKeepsOtherWritersValues()
    {
        using var sut = new GraphDictionary(_dbService, true);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);
        await ChangeByOtherWriterAsync();

        doc.Name = "from A";
        (await sut.SaveAsync(doc)).ShouldBeFalse();

        sut[1].ShouldBeSameAs(doc);
        doc.Version.ShouldBe(2);
        doc.Address.City.ShouldBe("B");
        doc.CategoryId.ShouldBe(2);
        doc.Category.ShouldNotBeNull().Id.ShouldBe(2);

        doc.Name = "next edit";
        (await sut.SaveAsync(doc)).ShouldBeTrue();
        (await RowAsync()).ShouldBe("next edit|B|2");
    }

    [Fact]
    public async Task WriteBack_WithoutTheNavigationInTheReload_LeavesInstanceAndToken_SoTheNextEditIsRejected()
    {
        using var sut = new GraphDictionary(_dbService, false);
        var doc = await LoadWithCategoryAsync();
        sut.AddOrUpdateValue(doc);
        await ChangeByOtherWriterAsync();

        doc.Name = "from A";
        (await sut.SaveAsync(doc)).ShouldBeFalse();

        doc.Version.ShouldBe(1);
        doc.Address.City.ShouldBe("A");

        doc.Name = "next edit";
        (await sut.SaveAsync(doc)).ShouldBeFalse();
        (await RowAsync()).ShouldBe("from B|B|2");
    }

    private GraphDbContext CreateContext() => new(_connection);

    private async Task<GraphDoc> LoadWithCategoryAsync()
    {
        using var ctx = CreateContext();

        return await ctx.Docs.AsNoTracking()
            .Include(d => d.Category)
            .SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
    }

    private async Task ChangeByOtherWriterAsync()
    {
        using var writerB = CreateContext();
        var other = await writerB.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        other.Name = "from B";
        other.Address.City = "B";
        other.CategoryId = 2;
        other.Version++;
        await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string> RowAsync()
    {
        using var reader = CreateContext();
        var row = await reader.Docs.AsNoTracking().SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);

        return $"{row.Name}|{row.Address.City}|{row.CategoryId}";
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
        private readonly bool _includeCategory;

        public GraphDictionary(IEFCoreDatabaseBackedService<GraphDbContext> dbService, bool includeCategory)
            : base(dbService, nameof(GraphDoc.Id)) =>
            _includeCategory = includeCategory;

        public Task<bool> SaveAsync(GraphDoc doc) =>
            SaveChangesResolvingConflictsAsync([new(new Change<GraphDoc, int>(ChangeReason.Refresh, doc.Id, doc))]);

        protected override IQueryable<GraphDoc> IncludeInEntity(IQueryable<GraphDoc> query) =>
            _includeCategory ? query.Include(d => d.Category) : query;

        protected override Expression<Func<GraphDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<GraphDoc> DbSetAccessor(GraphDbContext ctx) => ctx.Docs;

        protected override GraphDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
