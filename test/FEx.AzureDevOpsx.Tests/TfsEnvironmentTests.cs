using FEx.AzureDevOpsx;
using FEx.MVVM.Abstractions.Interfaces;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FEx.AzureDevOpsx.Tests;

public sealed class TfsEnvironmentTests
{
    [Fact]
    public async Task ProjectsCollections_Getter_ReturnsCachedListWithoutBlocking()
    {
        var env = new TfsEnvironment("env", "http://tfs.invalid/tfs", Substitute.For<IProgressAggregator>(), null);

        // Before the fix the getter blocked on GetProjectsCollectionsAsync (and, with no server, deadlocked on its own lock).
        var read = Task.Run(() => env.ProjectsCollections);

        (await read.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }
}
