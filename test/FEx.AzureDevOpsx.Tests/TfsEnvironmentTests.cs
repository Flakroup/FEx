using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.TeamFoundation.Client;
using NSubstitute;
using Shouldly;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AzureDevOpsx.Tests;

public sealed class TfsEnvironmentTests
{
    // Only a ceiling for detecting a wedged load (a regression), never a pace the test depends on. A failed load
    // against the unreachable server takes ~2-5 s of the SDK's own retry delay plus cold JIT, which CPU load and
    // coverage instrumentation stretch, so the ceiling is far above any legitimate duration.
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    private static TfsEnvironment CreateEnvironment() =>
        new("env", "http://127.0.0.1:1/tfs", Substitute.For<IProgressAggregator>(), null);

    // Task.WaitAsync(TimeSpan, CancellationToken) does not exist on net481.
    private static async Task<T> WithTimeoutAsync<T>(Func<Task<T>> start)
    {
        var task = Task.Run(start);

        if (await Task.WhenAny(task, Task.Delay(Timeout)) != task)
            throw new TimeoutException("The operation did not complete in time.");

        return await task;
    }

    private static Task WithTimeoutAsync(Func<Task> start) =>
        WithTimeoutAsync(async () =>
        {
            await start();

            return true;
        });

    private static void SetPrivate(TfsEnvironment env, string property, object? value) =>
        typeof(TfsEnvironment).GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(env, [value]);

    [Fact]
    public async Task ProjectsCollections_Getter_ReturnsCachedListWithoutBlocking()
    {
        var env = CreateEnvironment();

        // Before the fix the getter blocked on GetProjectsCollectionsAsync (and, with no server, deadlocked on its own lock).
        (await WithTimeoutAsync(() => Task.FromResult(env.ProjectsCollections))).ShouldBeEmpty();
    }

    private static bool GetPrivateBool(TfsEnvironment env, string property) =>
        (bool)typeof(TfsEnvironment).GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetGetMethod(true)!.Invoke(env, null)!;

    private static async Task LoadIgnoringServerErrorsAsync(TfsEnvironment env)
    {
        try
        {
            await WithTimeoutAsync(env.GetProjectsCollectionsAsync);
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            // expected: the server is unreachable
        }
    }

    [Fact]
    public async Task GetProjectsCollectionsAsync_AfterFailedLoad_DoesNotWedgeTheNextLoad()
    {
        var env = CreateEnvironment();
        SetPrivate(env, "Server", new TfsConfigurationServer(new Uri("http://127.0.0.1:1/tfs")));

        for (var attempt = 0; attempt < 2; attempt++)
        {
            SetPrivate(env, "ProjectsCollectionsIsDirty", true);

            // The unreachable server makes the load throw. Neither EnvironmentLock, CollectionsLock nor the busy
            // flag may stay taken, so the second attempt must finish (throwing again) instead of hanging.
            await LoadIgnoringServerErrorsAsync(env);

            GetPrivateBool(env, "ProjectsCollectionsIsBusy").ShouldBeFalse();
        }

        SetPrivate(env, "Server", null);
        await WithTimeoutAsync(env.GetProjectsCollectionsAsync);
    }
}
