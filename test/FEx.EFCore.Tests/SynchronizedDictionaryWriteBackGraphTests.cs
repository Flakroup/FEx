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
/// The write-back after a rejected change refreshes the cached graph in place or not at all: a current token over a
/// stale part of the graph would let the next edit overwrite the other writer silently. Each refusal branch of the
/// refresh has a model shape here that goes through it.
/// </summary>
public sealed class SynchronizedDictionaryWriteBackGraphTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SynchronizedDictionaryWriteBackGraphTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task OwnedValueChanged_IsRefreshed_KeepsTheCachedPrincipal_AndTheNextEditKeepsIt()
    {
        using var h = CreateGraphHarness();
        var doc = await h.LoadAsync(q => q.Include(d => d.Category));
        var category = doc.Category;
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => d.Address.City = "B");

        doc.Name = "from A";
        (await h.Sut.SaveAsync(doc)).ShouldBeFalse();

        h.Sut[1].ShouldBeSameAs(doc);
        doc.Version.ShouldBe(2);
        doc.Address.City.ShouldBe("B");
        doc.Category.ShouldBeSameAs(category);

        doc.Name = "next edit";
        (await h.Sut.SaveAsync(doc)).ShouldBeTrue();
        (await h.ReadAsync(d => $"{d.Name}|{d.Address.City}|{d.CategoryId}")).ShouldBe("next edit|B|1");
    }

    [Fact]
    public async Task ForeignKeyChanged_IsRefused_SoTheNextEditIsRejected()
    {
        using var h = CreateGraphHarness();
        var doc = await h.LoadAsync(q => q.Include(d => d.Category));
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => d.CategoryId = 2);

        await RejectAndExpectUntouchedAsync(h, doc);

        doc.Name = "next edit";
        (await h.Sut.SaveAsync(doc)).ShouldBeFalse();
        (await h.ReadAsync(d => $"{d.Name}|{d.CategoryId}")).ShouldBe("from B|2");
    }

    /// <summary>
    /// Two cached entities loaded together share one principal instance; the write-back must keep it shared, or a
    /// later batch holding both would fail to attach and lose both edits.
    /// </summary>
    [Fact]
    public async Task SharedPrincipal_StaysShared_AndABatchEditingBothIsSaved()
    {
        using var h = CreateGraphHarness(seedSecondDoc: true);
        await h.Sut.CacheAllAsync();
        var first = h.Sut[1];
        var second = h.Sut[2];
        first.Category.ShouldNotBeNull().ShouldBeSameAs(second.Category);
        await h.ChangeByOtherWriterAsync(_ => { });

        first.Name = "from A";
        (await h.Sut.SaveAsync(first)).ShouldBeFalse();
        first.Version.ShouldBe(2);
        first.Category.ShouldBeSameAs(second.Category);

        first.Name = "first next";
        second.Name = "second next";
        (await h.Sut.SaveAsync(first, second)).ShouldBeTrue();
        (await h.NamesAsync()).ShouldBe(["first next", "second next"]);
    }

    [Fact]
    public async Task OwnedValueEditedBeforeTheWriteBack_IsKept_WithTheStaleToken()
    {
        using var h = CreateGraphHarness();
        var doc = await h.LoadAsync(q => q.Include(d => d.Category));
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => d.Address.City = "B");
        h.Sut.OnConflict = () => doc.Address.City = "racing edit";

        await RejectAndExpectUntouchedAsync(h, doc);
        doc.Address.City.ShouldBe("racing edit");
    }

    [Fact]
    public async Task NavigationReplacedBeforeTheWriteBack_IsKept_WithTheStaleToken()
    {
        using var h = CreateGraphHarness();
        var doc = await h.LoadAsync(q => q.Include(d => d.Category));
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(_ => { });
        var replacement = new Category { Id = 1, Title = "replaced" };
        h.Sut.OnConflict = () => doc.Category = replacement;

        await RejectAndExpectUntouchedAsync(h, doc);
        doc.Category.ShouldBeSameAs(replacement);
    }

    /// <summary>The runtime type's members are refreshed, not only those of the dictionary's value type.</summary>
    [Fact]
    public async Task DerivedMemberChanged_IsRefreshed_AndTheNextEditKeepsIt()
    {
        using var h = new Harness<BaseDoc, TphContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new DerivedDoc { Id = 1, Name = "original", Version = 1, Extra = "A" }));
        var doc = (DerivedDoc)await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => ((DerivedDoc)d).Extra = "B");

        doc.Name = "from A";
        (await h.Sut.SaveAsync(doc)).ShouldBeFalse();
        doc.Version.ShouldBe(2);
        doc.Extra.ShouldBe("B");

        doc.Name = "next edit";
        (await h.Sut.SaveAsync(doc)).ShouldBeTrue();
        (await h.ReadAsync(d => $"{d.Name}|{((DerivedDoc)d).Extra}")).ShouldBe("next edit|B");
    }

    [Fact]
    public async Task SkipNavigation_IsRefused()
    {
        using var h = new Harness<SkipDoc, SkipContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new() { Id = 1, Name = "original", Version = 1 }));
        var doc = await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(_ => { });

        await RejectAndExpectUntouchedAsync(h, doc);
    }

    [Fact]
    public async Task ShadowForeignKey_IsRefused()
    {
        using var h = new Harness<ShadowDoc, ShadowContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new()
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Category = new() { Id = 1, Title = "one" }
            }));
        var doc = await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(_ => { });

        await RejectAndExpectUntouchedAsync(h, doc);
    }

    [Fact]
    public async Task CachedCollectionNavigation_IsRefused()
    {
        using var h = new Harness<PostDoc, PostContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new()
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Posts = [new() { Id = 1, Title = "post" }]
            }));
        var doc = await h.LoadAsync(q => q.Include(d => d.Posts));
        doc.Posts.Count.ShouldBe(1);
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(_ => { });

        await RejectAndExpectUntouchedAsync(h, doc);
    }

    [Fact]
    public async Task ComplexProperty_IsRefused()
    {
        using var h = new Harness<ComplexDoc, ComplexContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new() { Id = 1, Name = "original", Version = 1, Size = new() { Width = 1 } }));
        var doc = await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => d.Size = new() { Width = 2 });

        await RejectAndExpectUntouchedAsync(h, doc);
        doc.Size.Width.ShouldBe(1);
    }

    [Fact]
    public async Task OwnedCollectionInsideAnOwnedReference_IsRefused()
    {
        using var h = new Harness<NestedDoc, NestedContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new()
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Address = new() { City = "A", Lines = [new() { Id = 1, Text = "line" }] }
            }));
        var doc = await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => d.Address.City = "B");

        await RejectAndExpectUntouchedAsync(h, doc);
        doc.Address.City.ShouldBe("A");
    }

    /// <summary>
    /// Owned collections are refused, so an in-place edit of an item between the rejected save and the write-back is
    /// never reverted.
    /// </summary>
    [Fact]
    public async Task OwnedCollection_IsRefused_SoARacingItemEditIsKept()
    {
        using var h = new Harness<TagDoc, TagContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new() { Id = 1, Name = "original", Version = 1, Tags = [new() { Id = 1, Label = "t" }] }));
        var doc = await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(_ => { });
        h.Sut.OnConflict = () => doc.Tags[0].Label = "racing edit";

        await RejectAndExpectUntouchedAsync(h, doc);
        doc.Tags[0].Label.ShouldBe("racing edit");
    }

    /// <summary>An owned type's navigation back to its owner is not part of what is refreshed.</summary>
    [Fact]
    public async Task OwnedReferenceWithBackNavigation_IsRefreshed()
    {
        using var h = new Harness<BackDoc, BackContext>(_connection, c => new(c),
            ctx => ctx.Docs.Add(new() { Id = 1, Name = "original", Version = 1, Address = new() { City = "A" } }));
        var doc = await h.LoadAsync();
        h.Sut.AddOrUpdateValue(doc);
        await h.ChangeByOtherWriterAsync(d => d.Address.City = "B");

        doc.Name = "from A";
        (await h.Sut.SaveAsync(doc)).ShouldBeFalse();

        doc.Version.ShouldBe(2);
        doc.Address.City.ShouldBe("B");
    }

    private static async Task RejectAndExpectUntouchedAsync<TDoc, TCtx>(Harness<TDoc, TCtx> h, TDoc doc)
        where TDoc : Doc
        where TCtx : DocContext<TDoc>
    {
        doc.Name = "from A";
        (await h.Sut.SaveAsync(doc)).ShouldBeFalse();

        doc.Version.ShouldBe(1);
        doc.Name.ShouldBe("from A");
    }

    private Harness<GraphDoc, GraphContext> CreateGraphHarness(bool seedSecondDoc = false) =>
        new(_connection, c => new(c), ctx =>
        {
            ctx.Categories.AddRange(new Category { Id = 1, Title = "one" }, new Category { Id = 2, Title = "two" });
            ctx.Docs.Add(new()
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Address = new() { City = "A" },
                CategoryId = 1
            });

            if (seedSecondDoc)
                ctx.Docs.Add(new()
                {
                    Id = 2,
                    Name = "second",
                    Version = 1,
                    Address = new() { City = "A" },
                    CategoryId = 1
                });
        }, q => q.Include(d => d.Category));

    public abstract class Doc : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        [ConcurrencyCheck]
        public int Version { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
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

    public sealed class GraphDoc : Doc
    {
        public Address Address { get; set; } = new();

        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }

    public class BaseDoc : Doc;

    public sealed class DerivedDoc : BaseDoc
    {
        public string Extra { get; set; } = "";
    }

    public sealed class Label
    {
        public int Id { get; set; }

        public List<SkipDoc> Docs { get; set; } = [];
    }

    public sealed class SkipDoc : Doc
    {
        public List<Label> Labels { get; set; } = [];
    }

    public sealed class ShadowDoc : Doc
    {
        public Category? Category { get; set; }
    }

    public sealed class Post
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public int PostDocId { get; set; }

        public PostDoc? Doc { get; set; }
    }

    public sealed class PostDoc : Doc
    {
        public List<Post> Posts { get; set; } = [];
    }

    public sealed class Size
    {
        public int Width { get; set; }
    }

    public sealed class ComplexDoc : Doc
    {
        public Size Size { get; set; } = new();
    }

    public sealed class Line
    {
        public int Id { get; set; }

        public string Text { get; set; } = "";
    }

    public sealed class NestedAddress
    {
        public string City { get; set; } = "";

        public List<Line> Lines { get; set; } = [];
    }

    public sealed class NestedDoc : Doc
    {
        public NestedAddress Address { get; set; } = new();
    }

    public sealed class Tag
    {
        public int Id { get; set; }

        public string Label { get; set; } = "";
    }

    public sealed class TagDoc : Doc
    {
        public List<Tag> Tags { get; set; } = [];
    }

    public sealed class BackAddress
    {
        public string City { get; set; } = "";

        public BackDoc? Owner { get; set; }
    }

    public sealed class BackDoc : Doc
    {
        public BackAddress Address { get; set; } = new();
    }

    public abstract class DocContext<TDoc> : DbContext
        where TDoc : Doc
    {
        public DbSet<TDoc> Docs => Set<TDoc>();

        protected DocContext(DbContextOptions options)
            : base(options)
        {
        }

        protected static DbContextOptions Sqlite<TContext>(SqliteConnection connection)
            where TContext : DbContext =>
            new DbContextOptionsBuilder<TContext>().UseSqlite(connection).Options;
    }

    public sealed class GraphContext(SqliteConnection connection)
        : DocContext<GraphDoc>(Sqlite<GraphContext>(connection))
    {
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<GraphDoc>().OwnsOne(d => d.Address);
    }

    public sealed class TphContext(SqliteConnection connection) : DocContext<BaseDoc>(Sqlite<TphContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<DerivedDoc>();
    }

    public sealed class SkipContext(SqliteConnection connection) : DocContext<SkipDoc>(Sqlite<SkipContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<SkipDoc>().HasMany(d => d.Labels).WithMany(l => l.Docs);
    }

    public sealed class ShadowContext(SqliteConnection connection)
        : DocContext<ShadowDoc>(Sqlite<ShadowContext>(connection));

    public sealed class PostContext(SqliteConnection connection) : DocContext<PostDoc>(Sqlite<PostContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<PostDoc>().HasMany(d => d.Posts).WithOne(p => p.Doc).HasForeignKey(p => p.PostDocId);
    }

    public sealed class ComplexContext(SqliteConnection connection)
        : DocContext<ComplexDoc>(Sqlite<ComplexContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<ComplexDoc>().ComplexProperty(d => d.Size);
    }

    public sealed class NestedContext(SqliteConnection connection)
        : DocContext<NestedDoc>(Sqlite<NestedContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<NestedDoc>().OwnsOne(d => d.Address, a => a.OwnsMany(x => x.Lines));
    }

    public sealed class TagContext(SqliteConnection connection) : DocContext<TagDoc>(Sqlite<TagContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TagDoc>().OwnsMany(d => d.Tags);
    }

    public sealed class BackContext(SqliteConnection connection) : DocContext<BackDoc>(Sqlite<BackContext>(connection))
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<BackDoc>().OwnsOne(d => d.Address, a => a.WithOwner(x => x.Owner));
    }

    /// <summary>One model: a schema seeded with row 1, a dictionary saving through a real DbService, a second writer.</summary>
    private sealed class Harness<TDoc, TCtx> : IDisposable
        where TDoc : Doc
        where TCtx : DocContext<TDoc>
    {
        private readonly Func<SqliteConnection, TCtx> _createContext;
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _services;
        private readonly DocDbService<TCtx> _dbService;

        public DocDictionary<TDoc, TCtx> Sut { get; }

        public Harness(SqliteConnection connection,
                       Func<SqliteConnection, TCtx> createContext,
                       Action<TCtx> seed,
                       Func<IQueryable<TDoc>, IQueryable<TDoc>>? include = null)
        {
            _connection = connection;
            _createContext = createContext;

            using (var setup = createContext(connection))
            {
                setup.Database.EnsureCreated();
                seed(setup);
                setup.SaveChanges();
            }

            var services = new ServiceCollection();
            services.AddScoped(_ => createContext(connection));
            _services = services.BuildServiceProvider();
            _dbService = new(new ScopeProvider(_services));
            Sut = new(_dbService, include ?? (q => q));
        }

        public void Dispose()
        {
            Sut.Dispose();
            _dbService.Dispose();
            _services.Dispose();
        }

        public async Task<TDoc> LoadAsync(Func<IQueryable<TDoc>, IQueryable<TDoc>>? include = null)
        {
            using var ctx = _createContext(_connection);

            return await (include ?? (q => q))(ctx.Docs.AsNoTracking())
                .SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        }

        public async Task ChangeByOtherWriterAsync(Action<TDoc> change)
        {
            using var writerB = _createContext(_connection);
            var other = await writerB.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
            other.Name = "from B";
            other.Version++;
            change(other);
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public async Task<string> ReadAsync(Func<TDoc, string> describe)
        {
            using var reader = _createContext(_connection);

            return describe(await reader.Docs.AsNoTracking()
                .SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken));
        }

        public async Task<List<string>> NamesAsync()
        {
            using var reader = _createContext(_connection);

            return await reader.Docs.OrderBy(d => d.Id)
                .Select(d => d.Name)
                .ToListAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class ScopeProvider : IScopeProvider
    {
        private readonly IServiceProvider _services;

        public ScopeProvider(IServiceProvider services) => _services = services;

        public IServiceScope CreateScope() => _services.CreateScope();
    }

    private sealed class DocDbService<TCtx> : DbServiceBase<TCtx>, IEFCoreDatabaseBackedService<TCtx>
        where TCtx : DbContext
    {
        public string? DbKey => null;

        public DocDbService(IScopeProvider scopeProvider)
            : base(scopeProvider, new(Substitute.For<IFExLogger>()), Substitute.For<IFExDbConfig>(), [])
        {
        }
    }

    private sealed class DocDictionary<TDoc, TCtx> : SynchronizedDictionary<int, TDoc, TCtx>
        where TDoc : Doc
        where TCtx : DocContext<TDoc>
    {
        private readonly Func<IQueryable<TDoc>, IQueryable<TDoc>> _include;

        public Action? OnConflict { get; set; }

        public DocDictionary(IEFCoreDatabaseBackedService<TCtx> dbService, Func<IQueryable<TDoc>, IQueryable<TDoc>> include)
            : base(dbService, nameof(Doc.Id)) =>
            _include = include;

        public Task<bool> SaveAsync(params TDoc[] docs) =>
            SaveChangesResolvingConflictsAsync(
            [
                .. docs.Select(d => new ChangeInfo<int, TDoc>(new Change<TDoc, int>(ChangeReason.Refresh, d.Id, d)))
            ]);

        protected override async Task OnChangesDetectedAsync(ICollection<ChangeInfo<int, TDoc>> changes)
        {
            try
            {
                await base.OnChangesDetectedAsync(changes);
            }
            catch (DbUpdateConcurrencyException)
            {
                OnConflict?.Invoke();

                throw;
            }
        }

        protected override IQueryable<TDoc> IncludeInEntity(IQueryable<TDoc> query) => _include(query);

        protected override Expression<Func<TDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<TDoc> DbSetAccessor(TCtx ctx) => ctx.Docs;

        protected override TDoc GetNew(int key, IDictionary<string, object>? param = null) =>
            throw new NotSupportedException();
    }
}
