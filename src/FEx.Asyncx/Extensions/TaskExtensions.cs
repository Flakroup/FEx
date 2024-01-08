using FEx.Extensions;
using System.Threading.Tasks;

namespace FEx.Asyncx.Extensions;

public static class TaskExtensions
{
    /// <summary>
    ///     Waits for task to start.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>
    ///     Task
    /// </returns>
    public static async Task WaitForTaskToStartAsync(this Task task)
    {
        await FExAsyncx.AsyncHelper.DelayUntilAsync(task.IsNotStarted, milliseconds: 1);
    }

    public static async Task WaitForTaskToEndAsync(this Task task)
    {
        await FExAsyncx.AsyncHelper.DelayUntilAsync(() => !task.IsFinished(), milliseconds: 1);
    }
}