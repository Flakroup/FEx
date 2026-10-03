using DynamicData;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Collections;
using FEx.EFCore.Helpers;
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
    private readonly CacheDictionary _sut;

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
        _sut = new(_dbService);
        _sut.Index.Add(1);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _dbService.Dispose();
        _services.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task UpdateOfRowDeletedByOtherWriter_StillCached_IsReAdded_AndRestOfBatchSaved()
    {
        var doc = await LoadAsync(1);
        _sut.AddOrUpdateValue(doc);
        await DeleteByOtherWriterAsync(1);

        doc.Name = "from A";
        var saved = await _sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2));

        saved.ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["from A", "unrelated"]);
        _sut.Index.OrderBy(k => k).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task UpdateOfRowDeletedByOtherWriter_NoLongerCached_IsNotReAdded_AndRestOfBatchSaved()
    {
        var doc = await LoadAsync(1);
        await DeleteByOtherWriterAsync(1);

        doc.Name = "from A";
        var saved = await _sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2));

        saved.ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["unrelated"]);
        _sut.Index.ShouldBe([2]);
    }

    [Fact]
    public async Task RemovalOfRowDeletedByOtherWriter_SavesRestOfBatch()
    {
        var doc = await LoadAsync(1);
        await DeleteByOtherWriterAsync(1);

        var saved = await _sut.SaveAsync(new(ChangeReason.Remove, 1, doc), NewDoc(2));

        saved.ShouldBeTrue();
        (await NamesInDbAsync()).ShouldBe(["unrelated"]);
        _sut.Index.ShouldBe([2]);
    }

    [Fact]
    public async Task UpdateOfRowChangedByOtherWriter_GivesUpAfterBoundedRetries_WithoutOverwriting()
    {
        var doc = await LoadAsync(1);
        _sut.AddOrUpdateValue(doc);

        using (var writerB = CreateContext())
        {
            var other = await writerB.Docs.SingleAsync(d => d.Id == 1, TestContext.Current.CancellationToken);
            other.Name = "from B";
            other.Version = 2;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        doc.Name = "from A";
        var saved = await _sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2));

        saved.ShouldBeFalse();
        _sut.SaveAttempts.ShouldBe(3);
        (await NamesInDbAsync()).ShouldBe(["from B"]);
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
    }

    private sealed class CacheDictionary : SynchronizedDictionary<int, CachedDoc, CacheDbContext>
    {
        public int SaveAttempts { get; private set; }

        public CacheDictionary(IEFCoreDatabaseBackedService<CacheDbContext> dbService)
            : base(dbService, nameof(CachedDoc.Id)) =>
            UseIndex = true;

        public Task<bool> SaveAsync(params Change<CachedDoc, int>[] changes) =>
            SaveChangesResolvingConflictsAsync([.. changes.Select(c => new ChangeInfo<int, CachedDoc>(c))]);

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
