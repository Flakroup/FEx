using Microsoft.VisualStudio.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Async;

public class JoinableAsyncHelper
{
    public static async Task DelayWithoutDeadlockAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        var jtf = new JoinableTaskFactory(context);
        await jtf.RunAsync(() => Task.Delay(millisecondsDelay, cancellationToken));
    }

    public static void DelayWithoutDeadlock(int millisecondsDelay)
    {
        var context = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        var jtf = new JoinableTaskFactory(context);
        jtf.Run(() => Task.Delay(millisecondsDelay));
    }
}