using FEx.AzureDevOpsx;
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
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

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

    [Fact]
    public async Task GetProjectsCollectionsAsync_AfterFailedLoad_ReleasesEnvironmentLock()
    {
        var env = CreateEnvironment();
        SetPrivate(env, "Server", new TfsConfigurationServer(new Uri("http://127.0.0.1:1/tfs")));
        SetPrivate(env, "ProjectsCollectionsIsDirty", true);

        // The unreachable server makes the load throw; the lock must not stay held afterwards.
        try
        {
            await WithTimeoutAsync(env.GetProjectsCollectionsAsync);
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            // expected: the server is unreachable
        }

        SetPrivate(env, "Server", null);
        await WithTimeoutAsync(env.GetProjectsCollectionsAsync);
    }
}
