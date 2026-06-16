using FEx.MVVM.Services;
using FEx.MVVM.Utilities;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace FEx.MVVM.Tests;

public sealed class ProgressServiceTests
{
    [Fact]
    public void UnsubscribeFromProgress_NonExistentContainer_ShouldReturnFalse()
    {
        var service = ProgressService.Instance;
        using var receiver = new ProgressAggregator();

        var result = service.UnsubscribeFromProgress(receiver, "non-existent-id");

        result.ShouldBeFalse();
    }

    [Fact]
    public void UnsubscribeFromProgress_NonExistentListener_ShouldReturnFalse()
    {
        var service = ProgressService.Instance;
        using var producer = new ProgressAggregator();
        using var receiver1 = new ProgressAggregator();
        using var receiver2 = new ProgressAggregator();

        service.Containers.TryAdd(producer.Id, producer);
        service.SubscribeToProgress(receiver1, producer.Id);

        var result = service.UnsubscribeFromProgress(receiver2, producer.Id);
        result.ShouldBeFalse();

        service.UnsubscribeFromProgress(receiver1, producer.Id);
        service.RemoveContainer(producer.Id);
    }

    [Fact]
    public void UnsubscribeFromProgress_ShouldNotReattachListener()
    {
        var service = ProgressService.Instance;
        using var producer = new ProgressAggregator();
        using var receiver = new ProgressAggregator();

        service.Containers.TryAdd(producer.Id, producer);
        service.SubscribeToProgress(receiver, producer.Id);

        receiver.SetMaximum(999);
        receiver.SetValue(42);

        service.UnsubscribeFromProgress(receiver, producer.Id);

        receiver.Maximum.ShouldBe(999);
        receiver.Value.ShouldBe(42);

        service.RemoveContainer(producer.Id);
    }

    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        var tasks = new Task<ProgressService>[10];

        for (var i = 0; i < tasks.Length; i++)
            tasks[i] = Task.Run(() => ProgressService.Instance);

        Task.WaitAll(tasks);

        var first = tasks[0].Result;

        for (var i = 1; i < tasks.Length; i++)
            tasks[i].Result.ShouldBeSameAs(first);
    }
}