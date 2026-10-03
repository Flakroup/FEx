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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

/// <summary>
/// The acceptance cases of #197: a cached value is saved by loading its row and applying the cached state onto it, so
/// only the value itself and its owned types are written.
/// </summary>
public sealed class SynchronizedDictionaryLoadAndApplyTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private readonly ApplyDbService _dbService;
    private Action<ApplyDbContext>? _configureSavingContext;

    public SynchronizedDictionaryLoadAndApplyTests()
    {
        _connection.Open();

        using (var setup = CreateContext())
        {
            setup.Database.EnsureCreated();
            var person1 = new Person { Id = 1, Name = "person1" };
            setup.People.AddRange(person1, new Person { Id = 2, Name = "person2" });
            setup.Docs.Add(new()
            {
                Id = 1,
                Name = "original",
                Version = 1,
                Owner = person1,
                Blob = [1],
                Keywords = ["k1"],
                Lines = [new() { Id = Guid.NewGuid(), Text = "l1" }],
                Tags = [new() { Label = "t1", Detail = new() { Value = "d1" } }]
            });
            setup.NoteDocs.Add(new() { Id = 1, Name = "original", Notes = [new() { Text = "n1" }] });
            setup.Animals.Add(new Dog { Id = 1, Name = "rex", Breed = "collie" });
            setup.SaveChanges();
        }

        var services = new ServiceCollection();

        services.AddScoped(_ =>
        {
            var ctx = CreateContext();
            _configureSavingContext?.Invoke(ctx);

            return ctx;
        });

        _services = services.BuildServiceProvider();
        _dbService = new(new ScopeProvider(_services));
    }

    public void Dispose()
    {
        _dbService.Dispose();
        _services.Dispose();
        _connection.Dispose();
    }

    /// <summary>Case 1: a repointed navigation saves its foreign key, shadow or explicit.</summary>
    [Fact]
    public async Task RepointedReferenceNavigation_SavesTheForeignKey()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        var person2 = await LoadPersonAsync(2);
        sut.AddOrUpdateValue(doc);

        doc.Owner = person2;
        doc.Editor = person2;
        doc.EditorId.ShouldBeNull();
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        reader.Entry(saved).Property<int?>("OwnerId").CurrentValue.ShouldBe(2);
        saved.EditorId.ShouldBe(2);
    }

    /// <summary>Case 2: an owned collection with a shadow key keeps its items once.</summary>
    [Fact]
    public async Task OwnedCollectionWithShadowKey_IsNotDuplicated()
    {
        using var sut = new NoteDocDictionary(_dbService);
        NoteDoc doc;

        using (var ctx = CreateContext())
            doc = await ctx.NoteDocs.AsNoTracking().SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);

        sut.AddOrUpdateValue(doc);

        doc.Name = "edited";
        doc.Notes.Add(new() { Text = "n2" });
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.NoteDocs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("edited");
        saved.Notes.Select(n => n.Text).OrderBy(t => t).ShouldBe(["n1", "n2"]);
    }

    /// <summary>Case 3: a new owned item whose key the client set is inserted.</summary>
    [Fact]
    public async Task NewOwnedItemWithClientSetKey_IsInserted()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        var line = new Line { Id = Guid.NewGuid(), Text = "l2" };
        doc.Lines.Add(line);
        doc.Lines[0].Text = "l1 edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Lines.Select(l => l.Text).OrderBy(t => t).ShouldBe(["l1 edited", "l2"]);
        saved.Lines.ShouldContain(l => l.Id == line.Id);
    }

    /// <summary>Case 4: a new owned item with a nested owned entity in its own table is inserted with it.</summary>
    [Fact]
    public async Task NestedOwnedEntityUnderNewOwnedItem_IsInsertedWithTheRootEdit()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        doc.Name = "edited";
        doc.Tags.Add(new() { Label = "t2", Detail = new() { Value = "d2" } });
        doc.Tags[0].Detail!.Value = "d1 edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("edited");
        saved.Tags.OrderBy(t => t.Label)
            .Select(t => $"{t.Label}:{t.Detail?.Value}")
            .ShouldBe(["t1:d1 edited", "t2:d2"]);
    }

    /// <summary>Case 5: a principal the cached value references is not written by the cache.</summary>
    [Fact]
    public async Task PrincipalEditedByAnotherWriter_IsNotOverwritten()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        using (var writerB = CreateContext())
        {
            (await writerB.People.SingleAsync(p => p.Id == 1, TestContext.Current.CancellationToken)).Name = "from B";
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        doc.Name = "edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        (await reader.People.SingleAsync(p => p.Id == 1, TestContext.Current.CancellationToken)).Name.ShouldBe("from B");
        (await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken)).Name.ShouldBe("edited");
    }

    /// <summary>Case 6: an item removed from an owned collection is deleted, with its nested owned entity.</summary>
    [Fact]
    public async Task OwnedCollectionItemRemoved_IsDeleted()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        doc.Tags.Clear();
        doc.Lines.Clear();
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Tags.ShouldBeEmpty();
        saved.Lines.ShouldBeEmpty();
        (await reader.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM TagDetails")
            .SingleAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    /// <summary>Case 7: a concurrent edit of mutable scalars is a conflict, not silently reverted.</summary>
    [Fact]
    public async Task MutableScalarsEditedByAnotherWriter_RaiseConflict()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);
        var conflicts = new ConcurrentQueue<CacheConflict<int, ApplyDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        using (var writerB = CreateContext())
        {
            var other = await writerB.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
            other.Blob = [2];
            other.Keywords = ["from B"];
            other.Version++;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        doc.Keywords.Add("k2");
        (await sut.SaveAsync(doc)).ShouldBeFalse();

        conflicts.ShouldHaveSingleItem().CachedValue.ShouldBeSameAs(doc);
        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Blob.ShouldBe(new byte[] { 2 });
        saved.Keywords.ShouldBe(["from B"]);
        saved.Version.ShouldBe(2);
    }

    /// <summary>Case 7: a stale token is a conflict even when the cached value equals the row otherwise.</summary>
    [Fact]
    public async Task StaleTokenWithoutOtherDifferences_RaisesConflict()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);
        var conflicts = new ConcurrentQueue<CacheConflict<int, ApplyDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        using (var writerB = CreateContext())
        {
            (await writerB.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken)).Version++;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        (await sut.SaveAsync(doc)).ShouldBeFalse();
        conflicts.ShouldHaveSingleItem();
    }

    /// <summary>Case 8: members of a TPH derived type are saved.</summary>
    [Fact]
    public async Task TphDerivedMembers_AreSaved()
    {
        using var sut = new AnimalDictionary(_dbService);
        Animal dog;

        using (var ctx = CreateContext())
            dog = await ctx.Animals.AsNoTracking().SingleAsync(a => a.Id == 1, TestContext.Current.CancellationToken);

        sut.AddOrUpdateValue(dog);
        dog.Name = "max";
        ((Dog)dog).Breed = "beagle";
        (await sut.SaveAsync(dog)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = (Dog)await reader.Animals.SingleAsync(a => a.Id == 1, TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("max");
        saved.Breed.ShouldBe("beagle");
    }

    /// <summary>Case 9: the saving context's settings are left as the caller set them, and the save still works.</summary>
    [Fact]
    public async Task SavingContextSettings_AreRestored()
    {
        _configureSavingContext = ctx =>
        {
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;
            ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ctx.ChangeTracker.LazyLoadingEnabled = false;
        };

        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        doc.Name = "edited";
        doc.Tags.Add(new() { Label = "t2" });
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        sut.SavingContextSettings.ShouldBe([(false, QueryTrackingBehavior.NoTracking, false)]);
        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("edited");
        saved.Tags.Select(t => t.Label).OrderBy(t => t).ShouldBe(["t1", "t2"]);
    }

    /// <summary>A cached value with nothing to change writes nothing and leaves the row as it is.</summary>
    [Fact]
    public async Task UnchangedValue_SavesWithoutWriting()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Lines.Select(l => l.Text).ShouldBe(["l1"]);
        saved.Tags.Select(t => t.Label).ShouldBe(["t1"]);
    }

    private ApplyDbContext CreateContext() => new(_connection);

    private async Task<ApplyDoc> LoadDocAsync()
    {
        using var ctx = CreateContext();

        return await ctx.Docs.AsNoTracking()
            .Include(d => d.Owner)
            .Include(d => d.Editor)
            .SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
    }

    private async Task<Person> LoadPersonAsync(int id)
    {
        using var ctx = CreateContext();

        return await ctx.People.AsNoTracking().SingleAsync(p => p.Id == id, TestContext.Current.CancellationToken);
    }

    public sealed class Person
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    public sealed class Note
    {
        public string Text { get; set; } = "";
    }

    public sealed class Line
    {
        public Guid Id { get; set; }

        public string Text { get; set; } = "";
    }

    public sealed class Tag
    {
        public int Id { get; set; }

        public string Label { get; set; } = "";

        public TagDetail? Detail { get; set; }
    }

    public sealed class TagDetail
    {
        public string Value { get; set; } = "";
    }

    public sealed class ApplyDoc : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        [ConcurrencyCheck]
        public int Version { get; set; }

        public Person? Owner { get; set; }

        public int? EditorId { get; set; }

        public Person? Editor { get; set; }

        public byte[] Blob { get; set; } = [];

        public List<string> Keywords { get; set; } = [];

        public List<Line> Lines { get; set; } = [];

        public List<Tag> Tags { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    public sealed class NoteDoc : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public List<Note> Notes { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    public class Animal : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    public sealed class Dog : Animal
    {
        public string Breed { get; set; } = "";
    }

    public sealed class ApplyDbContext : DbContext
    {
        public DbSet<ApplyDoc> Docs => Set<ApplyDoc>();
        public DbSet<Person> People => Set<Person>();
        public DbSet<NoteDoc> NoteDocs => Set<NoteDoc>();
        public DbSet<Animal> Animals => Set<Animal>();

        public ApplyDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder<ApplyDbContext>().UseSqlite(connection).Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var doc = modelBuilder.Entity<ApplyDoc>();
            doc.HasOne(d => d.Owner).WithMany().HasForeignKey("OwnerId");
            doc.HasOne(d => d.Editor).WithMany().HasForeignKey(d => d.EditorId);

            doc.OwnsMany(d => d.Lines, l => l.HasKey(x => x.Id));

            doc.OwnsMany(d => d.Tags, t =>
            {
                t.HasKey(x => x.Id);
                t.OwnsOne(x => x.Detail, d => d.ToTable("TagDetails"));
            });

            // The EF-documented owned collection with a shadow key.
            modelBuilder.Entity<NoteDoc>()
                .OwnsMany(d => d.Notes, n =>
                {
                    n.Property<int>("Id");
                    n.HasKey("Id");
                });

            modelBuilder.Entity<Animal>();
            modelBuilder.Entity<Dog>();
        }
    }

    private sealed class ScopeProvider : IScopeProvider
    {
        private readonly IServiceProvider _services;

        public ScopeProvider(IServiceProvider services) => _services = services;

        public IServiceScope CreateScope() => _services.CreateScope();
    }

    private sealed class ApplyDbService : DbServiceBase<ApplyDbContext>, IEFCoreDatabaseBackedService<ApplyDbContext>
    {
        public string? DbKey => null;

        public ApplyDbService(IScopeProvider scopeProvider)
            : base(scopeProvider, new(Substitute.For<IFExLogger>()), Substitute.For<IFExDbConfig>(), [])
        {
        }
    }

    private sealed class DocDictionary : SynchronizedDictionary<int, ApplyDoc, ApplyDbContext>
    {
        public List<(bool AutoDetectChanges, QueryTrackingBehavior Tracking, bool LazyLoading)> SavingContextSettings
        {
            get;
        } = [];

        public DocDictionary(IEFCoreDatabaseBackedService<ApplyDbContext> dbService)
            : base(dbService, nameof(ApplyDoc.Id))
        {
        }

        public Task<bool> SaveAsync(ApplyDoc doc) =>
            SaveChangesResolvingConflictsAsync(
                [new(new Change<ApplyDoc, int>(ChangeReason.Refresh, doc.Id, doc))]);

        protected override async Task OnChangesDetectedAsync(ICollection<ChangeInfo<int, ApplyDoc>> changes) =>
            await _dbSrv.RunTaskInDbContextAsync(async ctx =>
            {
                await SaveCacheChangesAsync(ctx, changes);
                SavingContextSettings.Add((ctx.ChangeTracker.AutoDetectChangesEnabled,
                    ctx.ChangeTracker.QueryTrackingBehavior, ctx.ChangeTracker.LazyLoadingEnabled));
            });

        protected override Expression<Func<ApplyDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<ApplyDoc> DbSetAccessor(ApplyDbContext ctx) => ctx.Docs;

        protected override ApplyDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }

    private sealed class NoteDocDictionary : SynchronizedDictionary<int, NoteDoc, ApplyDbContext>
    {
        public NoteDocDictionary(IEFCoreDatabaseBackedService<ApplyDbContext> dbService)
            : base(dbService, nameof(NoteDoc.Id))
        {
        }

        public Task<bool> SaveAsync(NoteDoc doc) =>
            SaveChangesResolvingConflictsAsync(
                [new(new Change<NoteDoc, int>(ChangeReason.Refresh, doc.Id, doc))]);

        protected override Expression<Func<NoteDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<NoteDoc> DbSetAccessor(ApplyDbContext ctx) => ctx.NoteDocs;

        protected override NoteDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }

    private sealed class AnimalDictionary : SynchronizedDictionary<int, Animal, ApplyDbContext>
    {
        public AnimalDictionary(IEFCoreDatabaseBackedService<ApplyDbContext> dbService)
            : base(dbService, nameof(Animal.Id))
        {
        }

        public Task<bool> SaveAsync(Animal animal) =>
            SaveChangesResolvingConflictsAsync(
                [new(new Change<Animal, int>(ChangeReason.Refresh, animal.Id, animal))]);

        protected override Expression<Func<Animal, int>> RetriveKey() => a => a.Id;

        protected override DbSet<Animal> DbSetAccessor(ApplyDbContext ctx) => ctx.Animals;

        protected override Animal GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
