using DynamicData;
using FEx.EFCore.Collections;
using FEx.EFCore.Helpers;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
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
    private readonly SqliteDbService<ApplyDbContext> _dbService;
    private readonly WriteCounter _writes = new();
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
            setup.NoteDocs.AddRange(new NoteDoc { Id = 1, Name = "original", Notes = [new() { Text = "n1" }] },
                new NoteDoc
                {
                    Id = 2,
                    Name = "duplicates",
                    Notes = [new() { Text = "a" }, new() { Text = "a" }, new() { Text = "b" }]
                });
            setup.Nodes.Add(new() { Id = 1, Name = "root" });
            setup.PNodes.Add(new() { Id = 1, Name = "root" });
            setup.Items.Add(new() { Id = 1, Name = "item", Price = 10 });
            setup.Entry(setup.Badges.Add(new() { Title = "badge" }).Entity).Property<int>("Id").CurrentValue = 1;
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
        _dbService = new(_services);
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

    /// <summary>A cached value with nothing to change issues no write command and leaves the row as it is.</summary>
    [Fact]
    public async Task UnchangedValue_SavesWithoutWriting()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);
        var writesBefore = _writes.Count;

        (await sut.SaveAsync(doc)).ShouldBeTrue();

        (_writes.Count - writesBefore).ShouldBe(0);
        doc.Name = "edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();
        (_writes.Count - writesBefore).ShouldBe(1);
    }

    /// <summary>An edited owned item is updated in place: its database-generated key survives.</summary>
    [Fact]
    public async Task EditedOwnedItem_KeepsItsRow()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);
        var tagId = doc.Tags.ShouldHaveSingleItem().Id;
        var lineId = doc.Lines.ShouldHaveSingleItem().Id;

        doc.Tags[0].Label = "t1 edited";
        doc.Lines[0].Text = "l1 edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Tags.ShouldHaveSingleItem().Id.ShouldBe(tagId);
        saved.Tags[0].Label.ShouldBe("t1 edited");
        saved.Lines.ShouldHaveSingleItem().Id.ShouldBe(lineId);
    }

    /// <summary>Unchanged shadow-keyed items keep their rows (and shadow keys) when another item is added.</summary>
    [Fact]
    public async Task UnchangedShadowKeyedItems_KeepTheirRows()
    {
        var keysBefore = await NoteKeysAsync(1);
        using var sut = new NoteDocDictionary(_dbService);
        var doc = await LoadNoteDocAsync(1);
        sut.AddOrUpdateValue(doc);

        doc.Notes.Add(new() { Text = "n2" });
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        var keysAfter = await NoteKeysAsync(1);
        keysAfter.Count.ShouldBe(2);
        keysAfter.ShouldContain(keysBefore.ShouldHaveSingleItem());
    }

    /// <summary>Equal and reordered shadow-keyed items are matched one to one: nothing is rewritten.</summary>
    [Fact]
    public async Task DuplicateAndReorderedShadowKeyedItems_KeepTheirRows()
    {
        var keysBefore = await NoteKeysAsync(2);
        using var sut = new NoteDocDictionary(_dbService);
        var doc = await LoadNoteDocAsync(2);
        sut.AddOrUpdateValue(doc);

        doc.Notes.Reverse();
        doc.Name = "edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        (await NoteKeysAsync(2)).OrderBy(k => k).ShouldBe(keysBefore.OrderBy(k => k));
        using var reader = CreateContext();
        (await reader.NoteDocs.SingleAsync(d => d.Id == 2, TestContext.Current.CancellationToken)).Notes
            .Select(n => n.Text)
            .OrderBy(t => t)
            .ShouldBe(["a", "a", "b"]);
    }

    /// <summary>A cleared owned reference is deleted and a newly set one inserted.</summary>
    [Fact]
    public async Task OwnedReference_ClearedIsDeleted_AndSetIsInserted()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        doc.Tags[0].Detail = null;
        (await sut.SaveAsync(doc)).ShouldBeTrue();
        (await TagDetailCountAsync()).ShouldBe(0);

        doc.Tags[0].Detail = new() { Value = "d new" };
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        (await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken)).Tags
            .ShouldHaveSingleItem()
            .Detail.ShouldNotBeNull()
            .Value.ShouldBe("d new");
    }

    /// <summary>An owned reference that is null in the cached value and absent from the row stays absent, untouched.</summary>
    [Fact]
    public async Task OwnedReference_NullInBothTheCachedValueAndTheRow_StaysAbsent()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        doc.Tags[0].Detail = null;
        (await sut.SaveAsync(doc)).ShouldBeTrue();
        (await TagDetailCountAsync()).ShouldBe(0);
        var writesBefore = _writes.Count;

        doc.Tags[0].Label = "t1 edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        (_writes.Count - writesBefore).ShouldBe(1);
        (await TagDetailCountAsync()).ShouldBe(0);
        using var reader = CreateContext();
        var saved = (await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken)).Tags
            .ShouldHaveSingleItem();
        saved.Label.ShouldBe("t1 edited");
        saved.Detail.ShouldBeNull();
    }

    /// <summary>
    /// A removed value whose row is loaded in the same save through another instance of the same key deletes that row,
    /// instead of attaching a second instance of it.
    /// </summary>
    [Fact]
    public async Task RemovedValue_WithTheKeyOfARowLoadedForTheSameSave_DeletesTheLoadedRow()
    {
        using var sut = NodeDictionary();
        var root = await LoadNodeAsync(1);
        sut.AddOrUpdateValue(root);

        root.Name = "root edited";
        var removed = new Node { Id = 1, Name = "another instance" };

        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, root), new(ChangeReason.Remove, 1, removed))).ShouldBeTrue();

        (await NodesAsync()).ShouldBeEmpty();
    }

    /// <summary>The key equals a key of the same root type and values, whichever way it is compared.</summary>
    [Fact]
    public void EntityKey_EqualsOnlyAKeyOfTheSameTypeAndValues()
    {
        using var ctx = CreateContext();
        var key = CachedValueApplier.EntityKey.Of(ctx.Entry(new Node { Id = 1 }))!;

        key.Equals((object)CachedValueApplier.EntityKey.Of(ctx.Entry(new Node { Id = 1, Name = "other" }))!).ShouldBeTrue();
        key.Equals((object)CachedValueApplier.EntityKey.Of(ctx.Entry(new Node { Id = 2 }))!).ShouldBeFalse();
        key.Equals((object)CachedValueApplier.EntityKey.Of(ctx.Entry(new Person { Id = 1 }))!).ShouldBeFalse();
        key.Equals((object)"1").ShouldBeFalse();
        key.Equals((object?)null).ShouldBeFalse();
    }

    /// <summary>
    /// A principal whose key is a shadow property cannot be identified from a cached instance: the foreign key is left
    /// as it is and the principal is not written.
    /// </summary>
    [Fact]
    public async Task PrincipalWithShadowKey_LeavesTheForeignKey()
    {
        using var sut = new DocDictionary(_dbService);
        var doc = await LoadDocAsync();
        sut.AddOrUpdateValue(doc);

        doc.Badge = new() { Title = "unknown" };
        doc.Name = "edited";
        (await sut.SaveAsync(doc)).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("edited");
        reader.Entry(saved).Property<int?>("BadgeId").CurrentValue.ShouldBeNull();
        (await reader.Badges.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    /// <summary>A value added in the same batch as an edit of the cached value it references: both are saved.</summary>
    [Fact]
    public async Task AddedValueReachingASavedValue_SavesBoth()
    {
        using var sut = NodeDictionary();
        var root = await LoadNodeAsync(1);
        sut.AddOrUpdateValue(root);

        root.Name = "root edited";
        var child = new Node { Id = 2, Name = "child", Parent = root };

        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, root), new(ChangeReason.Add, 2, child))).ShouldBeTrue();

        (await NodesAsync()).ShouldBe(["1:root edited:", "2:child:1"]);
    }

    /// <summary>A value removed in the same batch as an edit of the cached value it references: both are saved.</summary>
    [Fact]
    public async Task RemovedValueReachingASavedValue_SavesBoth()
    {
        using (var ctx = CreateContext())
        {
            await ctx.Nodes.AddAsync(new() { Id = 2, Name = "child", ParentId = 1 }, TestContext.Current.CancellationToken);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var sut = NodeDictionary();
        var root = await LoadNodeAsync(1);
        var child = await LoadNodeAsync(2);
        child.Parent = root;
        sut.AddOrUpdateValue(root);

        root.Name = "root edited";

        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, root), new(ChangeReason.Remove, 2, child))).ShouldBeTrue();

        (await NodesAsync()).ShouldBe(["1:root edited:"]);
    }

    /// <summary>
    /// A self-reference with a shadow foreign key: an edit of the cached root and a child added under that cached
    /// instance in one batch are both saved, instead of failing the batch on an identity conflict.
    /// </summary>
    [Fact]
    public async Task AddedChildOfAnEditedRoot_WithAShadowForeignKey_SavesBoth()
    {
        using var sut = PNodeDictionary();
        var root = await LoadPNodeAsync(1);
        sut.AddOrUpdateValue(root);

        root.Name = "root edited";
        var child = new PNode { Id = 2, Name = "child", Parent = root };

        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, root), new(ChangeReason.Add, 2, child))).ShouldBeTrue();

        (await PNodesAsync()).ShouldBe(["1:root edited:", "2:child:1"]);
    }

    /// <summary>The mirror case: a child removed in the batch that edits the cached root it points at.</summary>
    [Fact]
    public async Task RemovedChildOfAnEditedRoot_WithAShadowForeignKey_SavesBoth()
    {
        using (var ctx = CreateContext())
        {
            var trackedRoot = await ctx.PNodes.SingleAsync(n => n.Id == 1, TestContext.Current.CancellationToken);
            await ctx.PNodes.AddAsync(new() { Id = 2, Name = "child", Parent = trackedRoot },
                TestContext.Current.CancellationToken);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var sut = PNodeDictionary();
        var root = await LoadPNodeAsync(1);
        var child = await LoadPNodeAsync(2);
        child.Parent = root;
        sut.AddOrUpdateValue(root);

        root.Name = "root edited";

        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, root), new(ChangeReason.Remove, 2, child))).ShouldBeTrue();

        (await PNodesAsync()).ShouldBe(["1:root edited:"]);
    }

    /// <summary>A row hidden by a query filter still exists: it is updated, not inserted again.</summary>
    [Fact]
    public async Task RowHiddenByAQueryFilter_IsUpdated()
    {
        using var sut = ItemDictionary();
        sut.UseIndexForTest();
        sut.Index.Add(1);
        var item = await LoadItemAsync();
        sut.AddOrUpdateValue(item);

        item.Archived = true;
        (await sut.SaveAsync(new Change<Item, int>(ChangeReason.Refresh, 1, item))).ShouldBeTrue();
        item.Name = "edited after archive";
        (await sut.SaveAsync(new Change<Item, int>(ChangeReason.Refresh, 1, item))).ShouldBeTrue();

        using var reader = CreateContext();
        var saved = await reader.Items.IgnoreQueryFilters().SingleAsync(TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("edited after archive");
        saved.Archived.ShouldBeTrue();
    }

    /// <summary>
    /// A shadow concurrency token has no cached value; as with <c>DbSet.Update</c> the save expects its default, so a
    /// token another writer moved is a conflict, not a silent overwrite.
    /// </summary>
    [Fact]
    public async Task ShadowConcurrencyTokenMovedByAnotherWriter_RaisesConflict()
    {
        using var sut = ItemDictionary();
        var item = await LoadItemAsync();
        sut.AddOrUpdateValue(item);
        var conflicts = new ConcurrentQueue<CacheConflict<int, Item>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        using (var writerB = CreateContext())
        {
            var other = await writerB.Items.SingleAsync(i => i.Id == 1, TestContext.Current.CancellationToken);
            other.Name = "from B";
            writerB.Entry(other).Property<int>("Rev").CurrentValue = 1;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        item.Price = 99;
        (await sut.SaveAsync(new Change<Item, int>(ChangeReason.Refresh, 1, item))).ShouldBeFalse();

        conflicts.ShouldHaveSingleItem();
        using var reader = CreateContext();
        var saved = await reader.Items.SingleAsync(i => i.Id == 1, TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("from B");
        saved.Price.ShouldBe(10);
    }

    /// <summary>
    /// A moved shadow token is a conflict even when nothing else differs and the saving context does not detect
    /// changes on its own.
    /// </summary>
    [Fact]
    public async Task ShadowConcurrencyTokenMovedWithoutOtherDifferences_RaisesConflict()
    {
        _configureSavingContext = ctx => ctx.ChangeTracker.AutoDetectChangesEnabled = false;
        using var sut = ItemDictionary();
        var item = await LoadItemAsync();
        sut.AddOrUpdateValue(item);
        var conflicts = new ConcurrentQueue<CacheConflict<int, Item>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        using (var writerB = CreateContext())
        {
            var other = await writerB.Items.SingleAsync(i => i.Id == 1, TestContext.Current.CancellationToken);
            writerB.Entry(other).Property<int>("Rev").CurrentValue = 1;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        (await sut.SaveAsync(new Change<Item, int>(ChangeReason.Refresh, 1, item))).ShouldBeFalse();

        conflicts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ShadowConcurrencyTokenAtItsDefault_Saves()
    {
        using var sut = ItemDictionary();
        var item = await LoadItemAsync();
        sut.AddOrUpdateValue(item);

        item.Price = 99;
        (await sut.SaveAsync(new Change<Item, int>(ChangeReason.Refresh, 1, item))).ShouldBeTrue();

        using var reader = CreateContext();
        (await reader.Items.SingleAsync(i => i.Id == 1, TestContext.Current.CancellationToken)).Price.ShouldBe(99);
    }

    private ApplyDbContext CreateContext() => new(_connection, _writes);

    private IdDictionary<Node> NodeDictionary() => new(_dbService, n => n.Id, ctx => ctx.Nodes);

    private IdDictionary<Item> ItemDictionary() => new(_dbService, i => i.Id, ctx => ctx.Items);

    private IdDictionary<PNode> PNodeDictionary() => new(_dbService, n => n.Id, ctx => ctx.PNodes);

    private async Task<PNode> LoadPNodeAsync(int id)
    {
        using var ctx = CreateContext();

        return await ctx.PNodes.AsNoTracking().SingleAsync(n => n.Id == id, TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> PNodesAsync()
    {
        using var ctx = CreateContext();

        return await ctx.PNodes.OrderBy(n => n.Id)
            .Select(n => n.Id + ":" + n.Name + ":" + EF.Property<int?>(n, "ParentId"))
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Node> LoadNodeAsync(int id)
    {
        using var ctx = CreateContext();

        return await ctx.Nodes.AsNoTracking().SingleAsync(n => n.Id == id, TestContext.Current.CancellationToken);
    }

    private async Task<Item> LoadItemAsync()
    {
        using var ctx = CreateContext();

        return await ctx.Items.AsNoTracking().SingleAsync(i => i.Id == 1, TestContext.Current.CancellationToken);
    }

    private async Task<NoteDoc> LoadNoteDocAsync(int id)
    {
        using var ctx = CreateContext();

        return await ctx.NoteDocs.AsNoTracking().SingleAsync(d => d.Id == id, TestContext.Current.CancellationToken);
    }

    private async Task<List<int>> NoteKeysAsync(int docId)
    {
        using var ctx = CreateContext();
        var doc = await ctx.NoteDocs.SingleAsync(d => d.Id == docId, TestContext.Current.CancellationToken);

        return [.. doc.Notes.Select(n => ctx.Entry(n).Property<int>("Id").CurrentValue)];
    }

    private async Task<List<string>> NodesAsync()
    {
        using var ctx = CreateContext();

        return await ctx.Nodes.OrderBy(n => n.Id)
            .Select(n => n.Id + ":" + n.Name + ":" + n.ParentId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> TagDetailCountAsync()
    {
        using var ctx = CreateContext();

        return await ctx.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM TagDetails")
            .SingleAsync(TestContext.Current.CancellationToken);
    }

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

        public Badge? Badge { get; set; }

        public List<Line> Lines { get; set; } = [];

        public List<Tag> Tags { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    /// <summary>A principal whose key is a shadow property.</summary>
    public sealed class Badge
    {
        public string Title { get; set; } = "";
    }

    public sealed class Node : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int? ParentId { get; set; }

        public Node? Parent { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    /// <summary>A self-referencing node whose foreign key is a shadow property.</summary>
    public sealed class PNode : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public PNode? Parent { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
    }

    /// <summary>Soft-deleted through a query filter, with a shadow concurrency token.</summary>
    public sealed class Item : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int Price { get; set; }

        public bool Archived { get; set; }

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
        public DbSet<Node> Nodes => Set<Node>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<PNode> PNodes => Set<PNode>();

        public ApplyDbContext(SqliteConnection connection, WriteCounter writes)
            : base(new DbContextOptionsBuilder<ApplyDbContext>().UseSqlite(connection).AddInterceptors(writes).Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var doc = modelBuilder.Entity<ApplyDoc>();
            doc.HasOne(d => d.Owner).WithMany().HasForeignKey("OwnerId");
            doc.HasOne(d => d.Editor).WithMany().HasForeignKey(d => d.EditorId);
            doc.HasOne(d => d.Badge).WithMany().HasForeignKey("BadgeId");

            var badge = modelBuilder.Entity<Badge>();
            badge.Property<int>("Id").ValueGeneratedNever();
            badge.HasKey("Id");

            modelBuilder.Entity<Node>().HasOne(n => n.Parent).WithMany().HasForeignKey(n => n.ParentId);
            modelBuilder.Entity<PNode>().HasOne(n => n.Parent).WithMany().HasForeignKey("ParentId");

            var item = modelBuilder.Entity<Item>();
            item.HasQueryFilter(i => !i.Archived);
            item.Property<int>("Rev").IsConcurrencyToken();

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

    /// <summary>Counts the INSERT, UPDATE and DELETE commands sent to the database.</summary>
    public sealed class WriteCounter : DbCommandInterceptor
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
                                                                          CommandEventData eventData,
                                                                          InterceptionResult<DbDataReader> result)
        {
            CountWrite(command);

            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountWrite(command);

            return new(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
                                                                  CommandEventData eventData,
                                                                  InterceptionResult<int> result)
        {
            CountWrite(command);

            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CountWrite(command);

            return new(result);
        }

        private void CountWrite(DbCommand command)
        {
            if (Regex.IsMatch(command.CommandText, @"\b(INSERT|UPDATE|DELETE)\b"))
                Interlocked.Increment(ref _count);
        }
    }

    private sealed class IdDictionary<TValue> : SynchronizedDictionary<int, TValue, ApplyDbContext>
        where TValue : class, INotifyPropertyChanged
    {
        private readonly Expression<Func<TValue, int>> _key;
        private readonly Func<ApplyDbContext, DbSet<TValue>> _set;

        public IdDictionary(IEFCoreDatabaseBackedService<ApplyDbContext> dbService,
                            Expression<Func<TValue, int>> key,
                            Func<ApplyDbContext, DbSet<TValue>> set)
            : base(dbService, "Id")
        {
            _key = key;
            _set = set;
        }

        public void UseIndexForTest() => UseIndex = true;

        public Task<bool> SaveAsync(params Change<TValue, int>[] changes) =>
            SaveChangesResolvingConflictsAsync([.. changes.Select(c => new ChangeInfo<int, TValue>(c))]);

        protected override Expression<Func<TValue, int>> RetriveKey() => _key;

        protected override DbSet<TValue> DbSetAccessor(ApplyDbContext ctx) => _set(ctx);

        protected override TValue GetNew(int key, IDictionary<string, object>? param = null) =>
            throw new NotSupportedException();
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
