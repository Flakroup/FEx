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
/// The dictionary saves through a real <see cref="PooledDbService{TDbContext}" />; a second writer is a separate
/// context on the same SQLite database.
/// </summary>
public sealed class SynchronizedDictionaryConflictTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private readonly CacheDbService _dbService;

    public SynchronizedDictionaryConflictTests()
    {
        _connection.Open();

        using (var setup = CreateContext())
        {
            setup.Database.EnsureCreated();
            setup.Docs.Add(new() { Id = 1, Name = "original", Version = 1 });
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateOfRowDeletedByOtherWriter_StillCached_IsReAdded_AndRestOfBatchSaved(bool useIndex)
    {
        using var sut = CreateDictionary(useIndex);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await DeleteByOtherWriterAsync(1);

        doc.Name = "from A";
        var saved = await sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2));

        saved.ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["from A", "unrelated"]);

        if (useIndex)
            sut.Index.OrderBy(k => k).ShouldBe([1, 2]);
    }

    // Index only: without an index the save re-checks the database, so a stale Refresh of an uncached key is
    // inserted as before this change (a later Remove change for that key deletes it again).
    [Fact]
    public async Task UpdateOfRowDeletedByOtherWriter_NoLongerCached_IsNotReAdded_AndRestOfBatchSaved()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        await DeleteByOtherWriterAsync(1);

        doc.Name = "from A";
        var saved = await sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2));

        saved.ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["unrelated"]);
        sut.Index.ShouldBe([2]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemovalOfRowDeletedByOtherWriter_SavesRestOfBatch(bool useIndex)
    {
        using var sut = CreateDictionary(useIndex);
        var doc = await LoadAsync(1);
        await DeleteByOtherWriterAsync(1);

        var saved = await sut.SaveAsync(new(ChangeReason.Remove, 1, doc), NewDoc(2));

        saved.ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["unrelated"]);

        if (useIndex)
            sut.Index.ShouldBe([2]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateOfRowChangedByOtherWriter_IsRejected_OtherWriterWins_AndRestOfBatchSaved(bool useIndex)
    {
        using var sut = CreateDictionary(useIndex);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);

        doc.Name = "from A";
        var saved = await sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2), NewDoc(3));

        saved.ShouldBeFalse();
        sut.SaveAttempts.ShouldBe(2);
        (await NamesInDbAsync()).ShouldBe(["from B", "unrelated", "unrelated"]);

        if (useIndex)
            sut.Index.OrderBy(k => k).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task BufferedChanges_ThroughInitializedPipeline_RejectConflictAndSaveRestOfBatch()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        sut.IsInitialized.ShouldBeTrue();

        // Key 1 is already indexed, so caching it is not saved. The subscription starts asynchronously, so wait
        // until a probe entry has gone through it before producing the batch under test.
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        sut.AddOrUpdateValue(new() { Id = 9, Name = "probe", Version = 1 });
        await WaitUntilAsync(() => sut.Index.Contains(9));
        await sut.WaitForCacheTasksAsync();
        var attemptsBefore = sut.SaveAttempts;

        await UpdateByOtherWriterAsync(1);

        // One changeset, so one buffered batch: an Update of key 1 (stale token) and an Add of key 2.
        sut.Edit(new() { Id = 1, Name = "from A", Version = doc.Version },
            new() { Id = 2, Name = "unrelated", Version = 1 });

        await WaitUntilAsync(() => sut.Index.Contains(2));
        await sut.WaitForCacheTasksAsync();

        (await NamesInDbAsync()).ShouldBe(["from B", "unrelated", "probe"]);
        sut.Index.OrderBy(k => k).ShouldBe([1, 2, 9]);
        (sut.SaveAttempts - attemptsBefore).ShouldBe(2);
    }

    [Fact]
    public async Task RejectedChange_RaisesOneConflictPerRejectedKey_AndLeavesTheCachedValueUntouched()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        doc.Name = "from A";
        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2))).ShouldBeFalse();

        var raised = conflicts.ShouldHaveSingleItem();
        raised.Key.ShouldBe(1);
        raised.CachedValue.ShouldBeSameAs(doc);
        var database = raised.DatabaseValue.ShouldNotBeNull();
        database.Name.ShouldBe("from B");
        database.Version.ShouldBe(2);

        sut[1].ShouldBeSameAs(doc);
        doc.Name.ShouldBe("from A");
        doc.Version.ShouldBe(1);
        (await NamesInDbAsync()).ShouldBe(["from B", "unrelated"]);
    }

    [Fact]
    public async Task ConflictHandlerThrowing_DoesNotStopTheBatchOrTheOtherHandlers()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);
        var otherHandlerCalls = 0;
        sut.ConflictDetected += (_, _) => throw new InvalidOperationException("Call from invalid thread");
        sut.ConflictDetected += (_, _) => otherHandlerCalls++;

        doc.Name = "from A";
        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2))).ShouldBeFalse();

        otherHandlerCalls.ShouldBe(1);
        (await NamesInDbAsync()).ShouldBe(["from B", "unrelated"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReloadAsync_AfterARejectedChange_ReplacesTheEntry_AndTheNextEditIsSaved(bool useIndex)
    {
        using var sut = CreateDictionary(useIndex);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);
        doc.Name = "from A";
        (await sut.SaveAsync(new Change<CachedDoc, int>(ChangeReason.Refresh, 1, doc))).ShouldBeFalse();

        var fresh = (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        fresh.ShouldNotBeSameAs(doc);
        sut[1].ShouldBeSameAs(fresh);
        fresh.Name.ShouldBe("from B");
        fresh.Version.ShouldBe(2);

        fresh.Name = "edited again";
        (await sut.SaveAsync(new Change<CachedDoc, int>(ChangeReason.Refresh, 1, fresh))).ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["edited again"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReloadAsync_OfADeletedRow_RemovesTheEntry(bool useIndex)
    {
        using var sut = CreateDictionary(useIndex);
        sut.AddOrUpdateValue(await LoadAsync(1));
        await DeleteByOtherWriterAsync(1);

        (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldBeNull();

        sut.IsCached(1).ShouldBeFalse();
        sut.Index.ShouldNotContain(1);
    }

    [Fact]
    public async Task ReloadAsync_ThroughTheInitializedPipeline_IsNotSavedBack()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        sut.AddOrUpdateValue(await LoadAsync(1));
        await FlushThroughPipelineAsync(sut, 9);
        await UpdateByOtherWriterAsync(1);
        var attemptsBefore = sut.SavedKeys.Count;

        await sut.ReloadAsync(1, TestContext.Current.CancellationToken);

        // A change the reload leaked into the subscription is buffered before this probe, so it is saved by the time
        // the probe is.
        await FlushThroughPipelineAsync(sut, 10);
        sut.SavedKeys.Skip(attemptsBefore).ShouldBe([[10]]);
    }

    [Fact]
    public async Task ReloadAsync_OfADeletedRow_ThroughTheInitializedPipeline_IsNotSavedBack()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        sut.AddOrUpdateValue(await LoadAsync(1));
        await FlushThroughPipelineAsync(sut, 9);
        await DeleteByOtherWriterAsync(1);
        var attemptsBefore = sut.SavedKeys.Count;

        (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldBeNull();

        await FlushThroughPipelineAsync(sut, 10);
        sut.SavedKeys.Skip(attemptsBefore).ShouldBe([[10]]);
    }

    /// <summary>Like a value retrieved by GetOrAddValueAsync, a reloaded value is saved when OnRetrievedNew edits it.</summary>
    [Fact]
    public async Task ReloadAsync_EditsMadeByOnRetrievedNew_AreSaved()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        sut.AddOrUpdateValue(await LoadAsync(1));
        await FlushThroughPipelineAsync(sut, 9);
        var attemptsBefore = sut.SavedKeys.Count;
        sut.OnRetrieved = value => value.Name = "hydrated";

        await sut.ReloadAsync(1, TestContext.Current.CancellationToken);

        await FlushThroughPipelineAsync(sut, 10);
        sut.SavedKeys.Skip(attemptsBefore).SelectMany(k => k).ShouldContain(1);
        (await NamesInDbAsync())[0].ShouldBe("hydrated");
    }

    [Fact]
    public async Task ConflictWhoseDatabaseValuesCannotBeRead_IsRaisedWithoutThem_AndTheBatchContinues()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);
        sut.FailIncludedQueries = true;

        doc.Name = "from A";
        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2))).ShouldBeFalse();

        var raised = conflicts.ShouldHaveSingleItem();
        raised.CachedValue.ShouldBeSameAs(doc);
        raised.DatabaseValue.ShouldBeNull();
        (await NamesInDbAsync()).ShouldBe(["from B", "unrelated"]);
    }

    [Fact]
    public async Task RejectedInstanceNoLongerCached_RaisesNoConflict()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);
        sut.OnConflict = () => sut.AddOrUpdateValue(new() { Id = 1, Name = "replacement", Version = 2 });

        doc.Name = "from A";
        (await sut.SaveAsync(new Change<CachedDoc, int>(ChangeReason.Refresh, 1, doc))).ShouldBeFalse();

        conflicts.ShouldBeEmpty();
    }

    private static async Task FlushThroughPipelineAsync(CacheDictionary sut, int probeId)
    {
        sut.AddOrUpdateValue(new() { Id = probeId, Name = "probe", Version = 1 });
        await WaitUntilAsync(() => sut.Index.Contains(probeId));
        await sut.WaitForCacheTasksAsync();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20, TestContext.Current.CancellationToken);

        condition().ShouldBeTrue();
    }

    private CacheDictionary CreateDictionary(bool useIndex)
    {
        var dictionary = new CacheDictionary(_dbService, useIndex);

        if (useIndex)
            dictionary.Index.Add(1);

        return dictionary;
    }

    private async Task UpdateByOtherWriterAsync(int id)
    {
        using var writerB = CreateContext();
        var other = await writerB.Docs.SingleAsync(d => d.Id == id, TestContext.Current.CancellationToken);
        other.Name = "from B";
        other.Version++;
        await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Change<CachedDoc, int> NewDoc(int id) =>
        new(ChangeReason.Add, id, new() { Id = id, Name = "unrelated", Version = 1 });

    private CacheDbContext CreateContext() => new(_connection);

    private async Task<CachedDoc> LoadAsync(int id)
    {
        using var ctx = CreateContext();

        return await ctx.Docs.AsNoTracking().SingleAsync(d => d.Id == id, TestContext.Current.CancellationToken);
    }

    private async Task DeleteByOtherWriterAsync(int id)
    {
        using var writerB = CreateContext();
        writerB.Docs.Remove(await writerB.Docs.SingleAsync(d => d.Id == id, TestContext.Current.CancellationToken));
        await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> NamesInDbAsync()
    {
        using var reader = CreateContext();

        return await reader.Docs.OrderBy(d => d.Id)
            .Select(d => d.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    public sealed class CachedDoc : INotifyPropertyChanged
    {
        public int Id { get; set; }

        private string _name = "";

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new(nameof(Name)));
            }
        }

        [ConcurrencyCheck]
        public int Version { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class CacheDbContext : DbContext
    {
        public DbSet<CachedDoc> Docs => Set<CachedDoc>();

        public CacheDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder<CacheDbContext>().UseSqlite(connection).Options)
        {
        }
    }

    private sealed class ScopeProvider : IScopeProvider
    {
        private readonly IServiceProvider _services;

        public ScopeProvider(IServiceProvider services) => _services = services;

        public IServiceScope CreateScope() => _services.CreateScope();
    }

    private sealed class CacheDbService : DbServiceBase<CacheDbContext>, IEFCoreDatabaseBackedService<CacheDbContext>
    {
        public string? DbKey => null;

        public CacheDbService(IScopeProvider scopeProvider)
            : base(scopeProvider, new(Substitute.For<IFExLogger>()), Substitute.For<IFExDbConfig>(), [])
        {
        }

        // Only the mapping snapshot the dictionary needs; the base also probes the SQL server and runs migrations.
        protected override async Task OnInitializeAsync() => await EnsureMappingSnapshotAsync();
    }

    private sealed class CacheDictionary : SynchronizedDictionary<int, CachedDoc, CacheDbContext>
    {
        public int SaveAttempts { get; private set; }

        public ConcurrentQueue<int[]> SavedKeys { get; } = new();

        public CacheDictionary(IEFCoreDatabaseBackedService<CacheDbContext> dbService, bool useIndex)
            : base(dbService, nameof(CachedDoc.Id)) =>
            UseIndex = useIndex;

        public Task<bool> SaveAsync(params Change<CachedDoc, int>[] changes) =>
            SaveChangesResolvingConflictsAsync([.. changes.Select(c => new ChangeInfo<int, CachedDoc>(c))]);

        public bool IsCached(int key) => Cache.Lookup(key).HasValue;

        public void Edit(params CachedDoc[] docs) =>
            Cache.Edit(updater =>
            {
                foreach (var doc in docs)
                    updater.AddOrUpdate(doc);
            });

        public Action? OnConflict { get; set; }

        public Action<CachedDoc>? OnRetrieved { get; set; }

        public bool FailIncludedQueries { get; set; }

        protected override async Task OnChangesDetectedAsync(ICollection<ChangeInfo<int, CachedDoc>> changes)
        {
            SaveAttempts++;
            SavedKeys.Enqueue([.. changes.Select(c => c.Key).OrderBy(k => k)]);

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

        protected override void OnRetrievedNew(CachedDoc value) => OnRetrieved?.Invoke(value);

        protected override IQueryable<CachedDoc> IncludeInEntity(IQueryable<CachedDoc> query) =>
            FailIncludedQueries ? throw new InvalidOperationException("database unavailable") : query;

        protected override Expression<Func<CachedDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<CachedDoc> DbSetAccessor(CacheDbContext ctx) => ctx.Docs;

        protected override CachedDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
