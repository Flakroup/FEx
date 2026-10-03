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

        public string Name { get; set; } = "";

        [ConcurrencyCheck]
        public int Version { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }
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

        public CacheDictionary(IEFCoreDatabaseBackedService<CacheDbContext> dbService, bool useIndex)
            : base(dbService, nameof(CachedDoc.Id)) =>
            UseIndex = useIndex;

        public Task<bool> SaveAsync(params Change<CachedDoc, int>[] changes) =>
            SaveChangesResolvingConflictsAsync([.. changes.Select(c => new ChangeInfo<int, CachedDoc>(c))]);

        public void Edit(params CachedDoc[] docs) =>
            Cache.Edit(updater =>
            {
                foreach (var doc in docs)
                    updater.AddOrUpdate(doc);
            });

        protected override async Task OnChangesDetectedAsync(ICollection<ChangeInfo<int, CachedDoc>> changes)
        {
            SaveAttempts++;
            await base.OnChangesDetectedAsync(changes);
        }

        protected override Expression<Func<CachedDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<CachedDoc> DbSetAccessor(CacheDbContext ctx) => ctx.Docs;

        protected override CachedDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
