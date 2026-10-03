using FEx.Agnostics.Abstractions.Flow;
using FEx.Agnostics.Abstractions.Interfaces;
using FEx.DependencyInjection.Abstractions.Interfaces;
using FEx.EFCore.Extensions;
using FEx.EFCore.Interfaces;
using FEx.EFCore.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.EFCore.Tests;

/// <summary>
/// Real conflicting writes: writer A and writer B are two contexts on the same SQLite database.
/// </summary>
public sealed class ConcurrencyConflictTests : IDisposable
{
    public enum SavePath
    {
        ServiceSync,
        ServiceAsync,
        Extension
    }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ConcurrencyService _service = new();

    public ConcurrencyConflictTests()
    {
        _connection.Open();

        using var setup = CreateContext();
        setup.Database.EnsureCreated();
        setup.Docs.Add(new() { Id = 1, Name = "original", Version = 1 });
        setup.SaveChanges();
    }

    public void Dispose()
    {
        _service.Dispose();
        _connection.Dispose();
    }

    [Theory]
    [InlineData(SavePath.ServiceSync)]
    [InlineData(SavePath.ServiceAsync)]
    [InlineData(SavePath.Extension)]
    public async Task StaleUpdate_Throws_AndOtherWritersValueSurvives(SavePath path)
    {
        using var writerA = CreateContext();
        var staleDoc = writerA.Docs.Single(d => d.Id == 1);

        using (var writerB = CreateContext())
        {
            var doc = writerB.Docs.Single(d => d.Id == 1);
            doc.Name = "from B";
            doc.Version = 2;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        staleDoc.Name = "from A";
        staleDoc.Version = 2;

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => SaveAsync(writerA, path));

        using var reader = CreateContext();
        reader.Docs.Single(d => d.Id == 1).Name.ShouldBe("from B");
    }

    [Theory]
    [InlineData(SavePath.ServiceSync)]
    [InlineData(SavePath.ServiceAsync)]
    [InlineData(SavePath.Extension)]
    public async Task UpdateOfRowDeletedByOtherWriter_Throws_AndRowStaysDeleted(SavePath path)
    {
        using var writerA = CreateContext();
        var staleDoc = writerA.Docs.Single(d => d.Id == 1);

        using (var writerB = CreateContext())
        {
            writerB.Docs.Remove(writerB.Docs.Single(d => d.Id == 1));
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        staleDoc.Name = "from A";

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => SaveAsync(writerA, path));

        using var reader = CreateContext();
        (await reader.Docs.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Theory]
    [InlineData(SavePath.ServiceSync)]
    [InlineData(SavePath.ServiceAsync)]
    [InlineData(SavePath.Extension)]
    public async Task DeleteOfRowDeletedByOtherWriter_InMixedBatch_SavesRestOfBatch(SavePath path)
    {
        using var writerA = CreateContext();
        var staleDoc = writerA.Docs.Single(d => d.Id == 1);

        using (var writerB = CreateContext())
        {
            writerB.Docs.Remove(writerB.Docs.Single(d => d.Id == 1));
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        writerA.Docs.Remove(staleDoc);
        await writerA.Docs.AddAsync(new() { Id = 2, Name = "unrelated", Version = 1 }, TestContext.Current.CancellationToken);

        await SaveAsync(writerA, path);

        using var reader = CreateContext();
        (await reader.Docs.Select(d => d.Id).ToListAsync(TestContext.Current.CancellationToken)).ShouldBe([2]);
    }

    [Theory]
    [InlineData(SavePath.ServiceSync)]
    [InlineData(SavePath.ServiceAsync)]
    [InlineData(SavePath.Extension)]
    public async Task DeleteOfTwoRowsDeletedByOtherWriter_InOneBatch_SavesRestOfBatch(SavePath path)
    {
        using (var setup = CreateContext())
        {
            await setup.Docs.AddAsync(new() { Id = 3, Name = "third", Version = 1 }, TestContext.Current.CancellationToken);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var writerA = CreateContext();
        var staleDocs = await writerA.Docs.ToListAsync(TestContext.Current.CancellationToken);

        using (var writerB = CreateContext())
        {
            writerB.Docs.RemoveRange(await writerB.Docs.ToListAsync(TestContext.Current.CancellationToken));
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        writerA.Docs.RemoveRange(staleDocs);
        await writerA.Docs.AddAsync(new() { Id = 2, Name = "unrelated", Version = 1 }, TestContext.Current.CancellationToken);

        await SaveAsync(writerA, path);

        using var reader = CreateContext();
        (await reader.Docs.Select(d => d.Id).ToListAsync(TestContext.Current.CancellationToken)).ShouldBe([2]);
    }

    [Theory]
    [InlineData(SavePath.ServiceSync)]
    [InlineData(SavePath.ServiceAsync)]
    [InlineData(SavePath.Extension)]
    public async Task DeleteOfRowUpdatedByOtherWriter_Throws_AndOtherWritersRowSurvives(SavePath path)
    {
        using var writerA = CreateContext();
        var staleDoc = writerA.Docs.Single(d => d.Id == 1);

        using (var writerB = CreateContext())
        {
            var doc = writerB.Docs.Single(d => d.Id == 1);
            doc.Name = "from B";
            doc.Version = 2;
            await writerB.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        writerA.Docs.Remove(staleDoc);
        await writerA.Docs.AddAsync(new() { Id = 2, Name = "unrelated", Version = 1 }, TestContext.Current.CancellationToken);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => SaveAsync(writerA, path));

        using var reader = CreateContext();
        (await reader.Docs.Select(d => d.Name).ToListAsync(TestContext.Current.CancellationToken)).ShouldBe(["from B"]);
    }

    [Theory]
    [InlineData(SavePath.ServiceSync)]
    [InlineData(SavePath.ServiceAsync)]
    [InlineData(SavePath.Extension)]
    public async Task NonConflictingUpdate_IsSaved(SavePath path)
    {
        using (var writer = CreateContext())
        {
            var doc = writer.Docs.Single(d => d.Id == 1);
            doc.Name = "updated";
            doc.Version = 2;

            await SaveAsync(writer, path);
        }

        using var reader = CreateContext();
        var saved = reader.Docs.Single(d => d.Id == 1);
        saved.Name.ShouldBe("updated");
        saved.Version.ShouldBe(2);
    }

    private ConcurrencyDbContext CreateContext() => new(_connection);

    private void SaveSynchronously(ConcurrencyDbContext context) => _service.Save(context).IsSuccess.ShouldBeTrue();

    private async Task SaveAsync(ConcurrencyDbContext context, SavePath path)
    {
        switch (path)
        {
            case SavePath.ServiceSync:
                SaveSynchronously(context);

                break;
            case SavePath.ServiceAsync:
                (await _service.SaveAsync(context)).IsSuccess.ShouldBeTrue();

                break;
            case SavePath.Extension:
                await context.ValidateAndSaveChangesAsync();

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(path), path, null);
        }
    }

    public sealed class Doc
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        [ConcurrencyCheck]
        public int Version { get; set; }
    }

    public sealed class ConcurrencyDbContext : DbContext
    {
        public DbSet<Doc> Docs => Set<Doc>();

        public ConcurrencyDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder<ConcurrencyDbContext>().UseSqlite(connection).Options)
        {
        }
    }

    private sealed class ConcurrencyService : PooledDbService<ConcurrencyDbContext>
    {
        public ConcurrencyService()
            : base(Substitute.For<IScopeProvider>(),
                new(Substitute.For<IFExLogger>()),
                Substitute.For<IFExDbConfig>())
        {
        }

        public Result<Error> Save(ConcurrencyDbContext context) => ValidateAndSaveChanges(context, "test");

        public Task<Result<Error>> SaveAsync(ConcurrencyDbContext context) =>
            ValidateAndSaveChangesAsync(context, "test");
    }
}
