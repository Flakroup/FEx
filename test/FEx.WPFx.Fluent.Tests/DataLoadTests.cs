using FEx.WPFx.Fluent.Views;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FEx.WPFx.Fluent.Tests;

/// <summary>The rules behind "the tab loads itself when it is opened": plain objects, no WPF.</summary>
public sealed class DataLoadTests
{
    private static readonly DateTime Noon = new(2026, 10, 21, 12, 5, 0);

    private static DataLoad<int[]> NewLoad(bool configured = true, List<Exception>? failures = null) =>
        new(() => configured, data => data.Length == 0, ex => failures?.Add(ex), () => Noon)
        {
            Run = read => Task.FromResult(read()),
        };

    private static Func<Func<int[]>> Plan(params int[] rows) => () => () => rows;

    [Fact]
    public void ANewLoad_IsIdle_AndWantsToLoad()
    {
        var load = NewLoad();

        load.State.ShouldBe(LoadState.Idle);
        load.WantsLoad.ShouldBeTrue();
    }

    [Fact]
    public async Task Reload_WithRows_EndsLoaded_WithTheDataAndTheTimeItWasRead()
    {
        var load = NewLoad();

        await load.ReloadAsync(Plan(1, 2));

        load.State.ShouldBe(LoadState.Loaded);
        load.Data.ShouldBe([1, 2]);
        load.LoadedAt.ShouldBe(Noon);
        load.WantsLoad.ShouldBeFalse("switching back to a loaded tab must not read again");
    }

    [Fact]
    public async Task Reload_WithNoRows_EndsEmpty_NotLoaded()
    {
        var load = NewLoad();

        await load.ReloadAsync(Plan());

        load.State.ShouldBe(LoadState.Empty);
        load.LoadedAt.ShouldBe(Noon);
        load.WantsLoad.ShouldBeFalse();
    }

    [Fact]
    public async Task Reload_PassesThroughLoading_BeforeTheDataArrives()
    {
        var states = new List<LoadState>();
        var load = NewLoad();
        load.Changed += () => states.Add(load.State);

        await load.ReloadAsync(Plan(1));

        states.ShouldBe([LoadState.Loading, LoadState.Loaded]);
    }

    [Fact]
    public async Task Reload_WithoutADatabase_EndsMissing_AndNeverResolvesThePlanOrReads()
    {
        var load = NewLoad(configured: false);
        var planned = false;

        await load.ReloadAsync(() =>
        {
            planned = true;
            return () => [1];
        });

        load.State.ShouldBe(LoadState.Missing);
        planned.ShouldBeFalse("with no connection the repository must not even be resolved");
        load.WantsLoad.ShouldBeTrue("the connection may be filled in on another tab, so the next opening asks again");
    }

    [Fact]
    public async Task AFailedRead_EndsFailed_HandsTheExceptionToTheLog_AndWaitsForTheOperator()
    {
        var failures = new List<Exception>();
        var load = NewLoad(failures: failures);
        var boom = new InvalidOperationException("Dla 90010112345 nie ma połączenia");

        await load.ReloadAsync(() => () => throw boom);

        load.State.ShouldBe(LoadState.Failed);
        failures.ShouldBe([boom]);
        load.Error.ShouldBeSameAs(boom);
        load.Data.ShouldBeNull();
        load.LoadedAt.ShouldBeNull();
        load.WantsLoad.ShouldBeFalse("a dead server must not be retried on every tab switch");
    }

    [Fact]
    public async Task APlanThatThrows_IsAFailedLoad_NotAnException()
    {
        var failures = new List<Exception>();
        var load = NewLoad(failures: failures);

        await load.ReloadAsync(() => throw new InvalidOperationException("no container"));

        load.State.ShouldBe(LoadState.Failed);
        failures.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ARefreshAfterAFailure_CanRecover()
    {
        var load = NewLoad();
        await load.ReloadAsync(() => () => throw new InvalidOperationException());

        await load.ReloadAsync(Plan(7));

        load.State.ShouldBe(LoadState.Loaded);
        load.Error.ShouldBeNull();
        load.Data.ShouldBe([7]);
    }

    [Fact]
    public async Task AReload_ReadsAgain_WithTheParametersTheNewPlanCaptured()
    {
        var load = NewLoad();
        var year = 2025;
        Func<Func<int[]>> plan = () =>
        {
            var captured = year;
            return () => [captured];
        };

        await load.ReloadAsync(plan);
        year = 2024;
        await load.ReloadAsync(plan);

        load.Data.ShouldBe([2024]);
    }

    [Fact]
    public async Task ASlowAnswerForAnOldParameter_DoesNotOverwriteTheNewerOne()
    {
        var gate = new TaskCompletionSource<int[]>();
        var load = NewLoad();
        load.Run = read => read() is { Length: 0 } ? gate.Task : Task.FromResult(read());

        var slow = load.ReloadAsync(Plan());
        await load.ReloadAsync(Plan(5));
        gate.SetResult([]);
        await slow;

        load.State.ShouldBe(LoadState.Loaded);
        load.Data.ShouldBe([5]);
    }

    [Fact]
    public async Task AStaleFailure_IsDropped_TooAndNotLogged()
    {
        var failures = new List<Exception>();
        var gate = new TaskCompletionSource<int[]>();
        var load = NewLoad(failures: failures);
        load.Run = read => read() is { Length: 0 } ? gate.Task : Task.FromResult(read());

        var slow = load.ReloadAsync(Plan());
        await load.ReloadAsync(Plan(5));
        gate.SetException(new InvalidOperationException());
        await slow;

        load.State.ShouldBe(LoadState.Loaded);
        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task AChangedConnection_MakesLoadedData_StaleAndWantedAgain()
    {
        var connection = "Server=a";
        var load = new DataLoad<int[]>(() => true, data => data.Length == 0, _ => { }, () => Noon, () => connection)
        {
            Run = read => Task.FromResult(read()),
        };

        await load.ReloadAsync(Plan(1));
        load.WantsLoad.ShouldBeFalse("same database, same data");

        connection = "Server=b";
        load.WantsLoad.ShouldBeTrue("rows read from database a must not be acted on against database b");

        await load.ReloadAsync(Plan(2));
        load.WantsLoad.ShouldBeFalse();
    }

    [Fact]
    public async Task AChangedConnection_AlsoRetriesAFailedRead()
    {
        var connection = "Server=a";
        var load = new DataLoad<int[]>(() => true, data => data.Length == 0, _ => { }, () => Noon, () => connection)
        {
            Run = read => Task.FromResult(read()),
        };
        await load.ReloadAsync(() => () => throw new InvalidOperationException());

        load.WantsLoad.ShouldBeFalse("a dead server is not retried on its own");
        connection = "Server=b";
        load.WantsLoad.ShouldBeTrue("but a different server is a different question");
    }

    [Fact]
    public async Task AReload_ClearsWhatWasThereBeforeItShowsLoading()
    {
        var load = NewLoad();
        await load.ReloadAsync(Plan(1));
        LoadState? seen = null;
        int[]? dataWhileLoading = [];
        DateTime? timeWhileLoading = Noon;
        load.Changed += () =>
        {
            if (load.State is LoadState.Loading)
            {
                seen = load.State;
                dataWhileLoading = load.Data;
                timeWhileLoading = load.LoadedAt;
            }
        };

        await load.ReloadAsync(Plan(2));

        seen.ShouldBe(LoadState.Loading);
        dataWhileLoading.ShouldBeNull();
        timeWhileLoading.ShouldBeNull("the freshness text is empty while loading");
    }
}
