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
using System.Runtime.CompilerServices;
using System.Threading;
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

    [Fact]
    public async Task RejectedChangesOfTwoKeysInOneBatch_RaiseOneConflictEach_WithTheirOwnRows()
    {
        await InsertDocAsync(3);
        using var sut = CreateDictionary(false);
        var first = await LoadAsync(1);
        var third = await LoadAsync(3);
        sut.AddOrUpdateValue(first);
        sut.AddOrUpdateValue(third);
        await UpdateByOtherWriterAsync(1);
        await UpdateByOtherWriterAsync(3);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        first.Name = "first from A";
        third.Name = "third from A";
        (await sut.SaveAsync(new(ChangeReason.Refresh, 1, first), new(ChangeReason.Refresh, 3, third))).ShouldBeFalse();

        conflicts.Select(c => c.Key).OrderBy(k => k).ShouldBe([1, 3]);
        conflicts.ShouldAllBe(c => c.DatabaseValue != null && c.DatabaseValue.Id == c.Key);
        // SQLite reports one conflicting row per attempt, so the two rejections come from two attempts: one read.
        sut.SaveAttempts.ShouldBe(2);
        sut.IncludedQueries.ShouldBe(1);
        conflicts.Single(c => c.Key == 1).CachedValue.ShouldBeSameAs(first);
        conflicts.Single(c => c.Key == 3).CachedValue.ShouldBeSameAs(third);
    }

    [Fact]
    public async Task RejectedRemoval_RaisesAConflictMarkedAsRemoval()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        await UpdateByOtherWriterAsync(1);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);

        (await sut.SaveAsync(new Change<CachedDoc, int>(ChangeReason.Remove, 1, doc))).ShouldBeFalse();

        var raised = conflicts.ShouldHaveSingleItem();
        raised.IsRemoval.ShouldBeTrue();
        raised.CachedValue.ShouldBeSameAs(doc);
        raised.DatabaseValue.ShouldNotBeNull().Name.ShouldBe("from B");
        (await NamesInDbAsync()).ShouldBe(["from B"]);
    }

    /// <summary>
    /// The handler runs after the save left the save lock and CacheTasks, so it can wait for the cache and reload the
    /// key it was told about; the cache then matches the database.
    /// </summary>
    [Fact]
    public async Task ConflictHandler_CanWaitForTheCache_AndReloadTheKey()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        await UpdateByOtherWriterAsync(1);
        var handled = new TaskCompletionSource<(bool CacheIdle, CachedDoc? Fresh)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable VSTHRD101, VSTHRD002 // An async handler that blocks on purpose: it must not be inside the save
        sut.ConflictDetected += async (_, conflict) =>
        {
            try
            {
                var idle = sut.WaitForCacheTasksAsync().Wait(TimeSpan.FromSeconds(10));
                var reloaded = await sut.ReloadAsync(conflict.Key, TestContext.Current.CancellationToken);
                handled.TrySetResult((idle, reloaded));
            }
            catch (Exception ex)
            {
                handled.TrySetException(ex);
            }
        };
#pragma warning restore VSTHRD101, VSTHRD002

        doc.Name = "from A";
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        var (cacheIdle, fresh) = await handled.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003

        cacheIdle.ShouldBeTrue();
        sut[1].ShouldBeSameAs(fresh.ShouldNotBeNull());
        fresh.Name.ShouldBe("from B");
        (await NamesInDbAsync())[0].ShouldBe("from B");
    }

    [Fact]
    public async Task ReloadAsync_WithAnEditStillBuffered_SavesItFirst_SoTheCacheMatchesTheDatabase()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);

        doc.Name = "pending";
        var fresh = (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        fresh.Name.ShouldBe("pending");
        await FlushThroughPipelineAsync(sut, 10);
        (await NamesInDbAsync())[0].ShouldBe("pending");
        sut[1].Name.ShouldBe("pending");
    }

    [Fact]
    public async Task RejectedRemoval_OfAKeyCachedAgainSince_RaisesNoConflict()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        await UpdateByOtherWriterAsync(1);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);
        sut.OnConflict = () => sut.AddOrUpdateValue(new() { Id = 1, Name = "re-added", Version = 2 });

        (await sut.SaveAsync(new Change<CachedDoc, int>(ChangeReason.Remove, 1, doc))).ShouldBeFalse();

        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task DirectSave_ThrowingAfterARejection_StillRaisesTheEarlierConflict()
    {
        using var sut = CreateDictionary(true);
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await UpdateByOtherWriterAsync(1);
        var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
        sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);
        sut.ThrowOnAttempt = 2;

        doc.Name = "from A";
        await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.SaveAsync(new(ChangeReason.Refresh, 1, doc), NewDoc(2)));

        conflicts.ShouldHaveSingleItem().Key.ShouldBe(1);
    }

    /// <summary>
    /// The batch releases its changes before it raises the conflict, so a handler that blocks on a reload of the key
    /// it was told about does not wait for its own batch.
    /// </summary>
    [Fact]
    public async Task SynchronousConflictHandler_BlockingOnAReloadOfItsKey_Completes()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        await UpdateByOtherWriterAsync(1);
        var reloaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable VSTHRD002 // A handler that blocks on purpose
        sut.ConflictDetected += (_, conflict) =>
            reloaded.TrySetResult(sut.ReloadAsync(conflict.Key, TestContext.Current.CancellationToken)
                .Wait(TimeSpan.FromSeconds(5)));
#pragma warning restore VSTHRD002

        doc.Name = "from A";

#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        (await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken)).ShouldBeTrue();
#pragma warning restore VSTHRD003
        sut[1].Name.ShouldBe("from B");
    }

    /// <summary>
    /// The replaced subscription's batch still being saved releases only its own changes, so a reload after the
    /// reset still waits for the edit buffered in the new subscription.
    /// </summary>
    [Fact]
    public async Task ReloadAsync_AfterAResetWithABatchInFlight_StillSavesTheBufferedEditFirst()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        var gate = sut.HoldSaves();

        doc.Name = "in flight";
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.SaveHeld.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        await sut.ResetAsync();
        await sut.InitializeAsync();
        gate.SetResult(true);
        await sut.WaitForCacheTasksAsync();
        (await NamesInDbAsync())[0].ShouldBe("in flight");

        doc.Name = "pending";
        var fresh = (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        fresh.Name.ShouldBe("pending");
        await FlushThroughPipelineAsync(sut, 10);
        (await NamesInDbAsync())[0].ShouldBe("pending");
        sut[1].Name.ShouldBe("pending");
    }

    /// <summary>A later batch that saves nothing (an Add of an indexed key) is not the change the reload waits for.</summary>
    [Fact]
    public async Task ReloadAsync_WaitsForASlowEarlierBatch_EvenWhenAFilteredLaterOneFinishesFirst()
    {
        await InsertDocAsync(5);
        using var sut = CreateDictionary(true);
        sut.Index.Add(5);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        var gate = sut.HoldSaves();

        doc.Name = "pending";
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.SaveHeld.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        var reload = sut.ReloadAsync(1, TestContext.Current.CancellationToken);
        sut.AddOrUpdateValue(await LoadAsync(5));
        // Several buffer windows: the filtered batch of key 5 has been handled by now.
        await Task.Delay(500, TestContext.Current.CancellationToken);
        reload.IsCompleted.ShouldBeFalse();

        gate.SetResult(true);
        var fresh = (await reload.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken))
            .ShouldNotBeNull();

        fresh.Name.ShouldBe("pending");
        sut[1].ShouldBeSameAs(fresh);
        (await NamesInDbAsync())[0].ShouldBe("pending");
    }

    [Fact]
    public async Task Dispose_WithAReloadWaitingForBufferedChanges_FailsTheReload()
    {
        Task<CachedDoc?> reload;

        using (var sut = CreateDictionary(true))
        {
            await sut.InitializeAsync();
            var doc = await LoadAsync(1);
            sut.AddOrUpdateValue(doc);
            await FlushThroughPipelineAsync(sut, 9);

            doc.Name = "buffered";
            reload = sut.ReloadAsync(1, TestContext.Current.CancellationToken);
            reload.IsCompleted.ShouldBeFalse();
        }

        await Should.ThrowAsync<ObjectDisposedException>(() =>
            reload.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Rx drops a batch whose subscription is disposed between the buffer and the save; the reset releases its numbers
    /// with the rest of the old generation, so a later reload does not wait for them.
    /// </summary>
    [Fact]
    public async Task ResetWhileABatchIsBetweenTheBufferAndItsSave_ReloadAsyncStillCompletes()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        var selected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable VSTHRD002 // Holds the buffer's thread on purpose
        sut.BatchSelected = () =>
        {
            sut.BatchSelected = null;
            selected.TrySetResult(true);
            resume.Task.Wait(TimeSpan.FromSeconds(30));
        };
#pragma warning restore VSTHRD002

        doc.Name = "edited";
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await selected.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        var edit = sut.LastChange;
        sut.IsPending(edit).ShouldBeTrue();
        await sut.ResetAsync();
        await sut.InitializeAsync();
        resume.SetResult(true);

        sut.IsPending(edit).ShouldBeFalse();
        (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task ResetWithChangesStillBuffered_LeavesNothingOfTheOldGenerationPending()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        var generation = sut.Generation;

        doc.Name = "buffered";
        var edit = sut.LastChange;
        sut.IsPending(edit).ShouldBeTrue();
        await sut.ResetAsync();
        await sut.InitializeAsync();

        sut.Generation.ShouldNotBe(generation);
        sut.IsPending(edit).ShouldBeFalse();
    }

    [Fact]
    public async Task Publish_ForAClosedGeneration_TracksNothing()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var generation = sut.Generation;
        await sut.ResetAsync();
        await sut.InitializeAsync();

        var last = sut.LastChange;

        sut.Publish(generation).ShouldBe(0);

        sut.LastChange.ShouldBe(last);
    }

    /// <summary>The reload's own cache edit is filtered to an empty change set, which must not wait for a buffer tick.</summary>
    [Fact]
    public async Task ReloadAsync_LeavesNoEmptyChangeSetPending()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        sut.AddOrUpdateValue(await LoadAsync(1));
        await FlushThroughPipelineAsync(sut, 9);
        await WaitUntilAsync(() => sut.PendingChangeCount == 0);

        await sut.ReloadAsync(1, TestContext.Current.CancellationToken);

        sut.PendingChangeCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReloadAsync_CancelledWhileWaiting_ThrowsAndRemovesItsWaiter()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        var doc = await LoadAsync(1);
        sut.AddOrUpdateValue(doc);
        await FlushThroughPipelineAsync(sut, 9);
        var gate = sut.HoldSaves();

        doc.Name = "held";
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        await sut.SaveHeld.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        using var cancellation = new CancellationTokenSource();
        var reload = sut.ReloadAsync(1, cancellation.Token);
        sut.PendingWaiterCount.ShouldBe(1);

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            reload.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        sut.PendingWaiterCount.ShouldBe(0);
        gate.SetResult(true);
    }

    /// <summary>
    /// InitializeAsync returns once the new subscription listens, so an edit made right after it is saved, and
    /// ReloadAsync waits for it.
    /// </summary>
    [Fact]
    public async Task EditRightAfterResetAndInitialize_IsSaved_EveryTime()
    {
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        sut.AddOrUpdateValue(await LoadAsync(1));
        await FlushThroughPipelineAsync(sut, 9);

        for (var i = 0; i < 10; i++)
        {
            await sut.ResetAsync();
            await sut.InitializeAsync();
            sut[1].Name = $"v{i}";

            var fresh = (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldNotBeNull();

            fresh.Name.ShouldBe($"v{i}");
            (await NamesInDbAsync())[0].ShouldBe($"v{i}");
        }
    }

    /// <summary>
    /// Disposing the dictionary before the save pipeline listens to the cache fails InitializeAsync instead of leaving it
    /// waiting for a subscribe Rx will skip.
    /// </summary>
    [Fact]
    public async Task Dispose_BeforeTheSavePipelineListens_FailsInitializeAsync()
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task initialization;

        using (var sut = CreateDictionary(true))
        {
            await sut.InitializeAsync();
            await sut.ResetAsync();
#pragma warning disable VSTHRD002 // Holds the subscribe on purpose
            sut.BeforeSubscribe = () =>
            {
                entered.TrySetResult(true);
                resume.Task.Wait(TimeSpan.FromSeconds(30));
            };
#pragma warning restore VSTHRD002
            sut.SubscribeDecided = listens => listened.TrySetResult(listens);

            initialization = sut.InitializeAsync();
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
#pragma warning restore VSTHRD003
        }

        resume.SetResult(true);

        await Should.ThrowAsync<ObjectDisposedException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        // The held subscribe, once released, does not listen to the disposed cache.
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        (await listened.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
            .ShouldBeFalse();
#pragma warning restore VSTHRD003
    }

    /// <summary>
    /// A Dispose that lands before InitializeAsync assigns the new subscription still fails InitializeAsync, instead of
    /// letting it report success on a disposed dictionary.
    /// </summary>
    [Fact]
    public async Task Dispose_BeforeTheNewSubscriptionIsAssigned_FailsInitializeAsync() =>
        await DisposeBeforeTheNewSubscriptionIsAssignedAsync();

    /// <summary>The subscribe signal faulted by that disposal is observed, so it raises no UnobservedTaskException.</summary>
    [Fact]
    public async Task Dispose_BeforeTheNewSubscriptionIsAssigned_LeavesNoUnobservedTaskException()
    {
        var unobserved = new ConcurrentQueue<Exception>();

        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (var exception in e.Exception.InnerExceptions)
            {
                if (exception is ObjectDisposedException { ObjectName: nameof(CacheDictionary) })
                    unobserved.Enqueue(exception);
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;

        try
        {
            await DisposeBeforeTheNewSubscriptionIsAssignedAsync();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            unobserved.ShouldBeEmpty();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    public async Task InitializeAsync_AfterDispose_Throws()
    {
        using var sut = CreateDictionary(true);
        var pipelineBuilt = false;
        sut.BeforePipelineAssigned = () => pipelineBuilt = true;
#pragma warning disable IDISP016, IDISP017 // Using the disposed instance is the point
        sut.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(() =>
            sut.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
#pragma warning restore IDISP016, IDISP017
        // The base class fails it before OnInitializeAsync builds a pipeline on the disposed cache.
        pipelineBuilt.ShouldBeFalse();
    }

    /// <summary>
    /// A replaced subscription whose disposal has not run yet still sees edits made after InitializeAsync; only the new
    /// one saves them.
    /// </summary>
    [Fact]
    public async Task EditSeenByAReplacedSubscriptionStillListening_IsSavedOnce()
    {
        var replaced = new List<IDisposable?>();
        using var sut = CreateDictionary(true);

        try
        {
            await sut.InitializeAsync();
            var doc = await LoadAsync(1);
            sut.AddOrUpdateValue(doc);
            await FlushThroughPipelineAsync(sut, 9);
            var conflicts = new ConcurrentQueue<CacheConflict<int, CachedDoc>>();
            sut.ConflictDetected += (_, conflict) => conflicts.Enqueue(conflict);
            sut.ReplacedSubscription = replaced.Add;

            await sut.ResetAsync();
            await sut.InitializeAsync();
            var savesBefore = sut.SavedKeys.Count;
            doc.Name = "edited";

            (await sut.ReloadAsync(1, TestContext.Current.CancellationToken)).ShouldNotBeNull().Name.ShouldBe("edited");
            await FlushThroughPipelineAsync(sut, 10);

            sut.SavedKeys.Skip(savesBefore).SelectMany(k => k).Count(k => k == 1).ShouldBe(1);
            conflicts.ShouldBeEmpty();
        }
        finally
        {
            foreach (var subscription in replaced)
                subscription?.Dispose();
        }
    }

    // Not inlined, so nothing of the dictionary stays reachable from the caller's frame once it returns.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task DisposeBeforeTheNewSubscriptionIsAssignedAsync()
    {
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var decided = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sut = CreateDictionary(true);
        await sut.InitializeAsync();
        await sut.ResetAsync();
        // The subscribe is held until the dispose has run, so the signal is still pending when the dispose faults it.
#pragma warning disable VSTHRD002 // Holds the subscribe on purpose
        sut.BeforeSubscribe = () => resume.Task.Wait(TimeSpan.FromSeconds(30));
#pragma warning restore VSTHRD002
        sut.SubscribeDecided = listens => decided.TrySetResult(listens);
        sut.BeforePipelineAssigned = () =>
        {
            sut.BeforePipelineAssigned = null;
            sut.Dispose();
        };

        await Should.ThrowAsync<ObjectDisposedException>(() =>
            sut.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        sut.IsInitialized.ShouldBeFalse();

        resume.SetResult(true);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
        (await decided.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
            .ShouldBeFalse();
#pragma warning restore VSTHRD003
    }

    private async Task InsertDocAsync(int id)
    {
        using var ctx = CreateContext();
        await ctx.Docs.AddAsync(new() { Id = id, Name = "original", Version = 1 }, TestContext.Current.CancellationToken);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
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

        public int IncludedQueries => _includedQueries;

        public int? ThrowOnAttempt { get; set; }

        public TaskCompletionSource<bool> SaveHeld { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _includedQueries;
        private TaskCompletionSource<bool>? _saveGate;

        /// <summary>Holds the next save attempt until the returned gate is set.</summary>
        public TaskCompletionSource<bool> HoldSaves() =>
            _saveGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task OnChangesDetectedAsync(ICollection<ChangeInfo<int, CachedDoc>> changes)
        {
            SaveAttempts++;
            SavedKeys.Enqueue([.. changes.Select(c => c.Key).OrderBy(k => k)]);

            if (SaveAttempts == ThrowOnAttempt)
                throw new InvalidOperationException("transient failure");

            if (Interlocked.Exchange(ref _saveGate, null) is { } gate)
            {
                SaveHeld.TrySetResult(true);
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(30));
#pragma warning restore VSTHRD003
            }

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

        protected override IQueryable<CachedDoc> IncludeInEntity(IQueryable<CachedDoc> query)
        {
            Interlocked.Increment(ref _includedQueries);

            return FailIncludedQueries ? throw new InvalidOperationException("database unavailable") : query;
        }

        protected override Expression<Func<CachedDoc, int>> RetriveKey() => d => d.Id;

        protected override DbSet<CachedDoc> DbSetAccessor(CacheDbContext ctx) => ctx.Docs;

        protected override CachedDoc GetNew(int key, IDictionary<string, object>? param = null) => new() { Id = key };
    }
}
