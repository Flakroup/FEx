using System;
using System.Threading;
using System.Threading.Tasks;

namespace FEx.Asyncx.Utilities;

/// <summary>
/// An async manual reset event.
/// </summary>
/// <remarks>https://badflyer.azurewebsites.net/asyncmanualresetevent</remarks>
public sealed class ManualResetEventAsync
{
    // Inspiration from https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-1-asyncmanualresetevent/
    // and the .net implementation of SemaphoreSlim

    /// <summary>
    /// The timeout in milliseconds to wait indefinitly.
    /// </summary>
    private const int WaitIndefinitly = -1;

    /// <summary>
    /// True to run synchronous continuations on the thread which invoked Set. False to run them in the threadpool.
    /// </summary>
    private readonly bool _runSynchronousContinuationsOnSetThread;

    /// <summary>
    /// The current task completion source.
    /// </summary>
    private volatile TaskCompletionSource<bool> _completionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualResetEventAsync" /> class.
    /// </summary>
    /// <param name="isSet">True to set the task completion source on creation.</param>
    /// <param name="runSynchronousContinuationsOnSetThread">
    /// If you have synchronous continuations, they will run on the thread
    /// which invokes Set, unless you set this to false.
    /// </param>
    public ManualResetEventAsync(bool isSet = false, bool runSynchronousContinuationsOnSetThread = true)
    {
        _runSynchronousContinuationsOnSetThread = runSynchronousContinuationsOnSetThread;
        _completionSource = new();

        if (isSet)
            _completionSource.TrySetResult(true);
    }

    /// <summary>
    /// Wait for the manual reset event.
    /// </summary>
    /// <param name="timeout">A timeout.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>
    /// A task which waits for the manual reset event. Returns true if the timeout has not expired. Returns false if
    /// the timeout expired.
    /// </returns>
    public async Task<bool> WaitAsync(TimeSpan? timeout = null, CancellationToken token = default) =>
        await AwaitCompletionAsync(timeout.HasValue
                ? (int)timeout.Value.TotalMilliseconds
                : WaitIndefinitly,
            token);

    /// <summary>
    /// Set the completion source.
    /// </summary>
    public void Set()
    {
        if (_runSynchronousContinuationsOnSetThread)
            _completionSource.TrySetResult(true);
        else
            // Run synchronous completions in the thread pool.
            _ = Task.Run(() => _completionSource.TrySetResult(true));
    }

    /// <summary>
    /// Reset the manual reset event.
    /// </summary>
    public void Reset()
    {
        // Grab a reference to the current completion source.
        var currentCompletionSource = _completionSource;

        // Check if there is nothing to be done, return.
        if (!currentCompletionSource.Task.IsCompleted)
            return;

        // Otherwise, try to replace it with a new completion source (if it is the same as the reference we took before).
        Interlocked.CompareExchange(ref _completionSource, new(), currentCompletionSource);
    }

    /// <summary>
    /// Await completion based on a timeout and a cancellation token.
    /// </summary>
    /// <param name="timeoutMS">The timeout in milliseconds.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task (true if wait succeeded). (False on timeout).</returns>
    private async Task<bool> AwaitCompletionAsync(int timeoutMS, CancellationToken token)
    {
        // Validate arguments.
        if (timeoutMS < -1)
            throw new ArgumentException(
                "The timeout must be either -1ms (indefinitely) or a positive ms value <= int.MaxValue");

        CancellationTokenSource timeoutToken;

        // If the token cannot be cancelled, then we dont need to create any sort of linked token source.
        if (!token.CanBeCanceled)
        {
            // If the wait is indefinite, then we don't need to create a second task at all to wait on, just wait for set.
            if (timeoutMS == -1)
#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
                return await _completionSource.Task;
#pragma warning restore VSTHRD003

            timeoutToken = new();
        }
        else
        {
            // A token source which will get canceled either when we cancel it, or when the linked token source is canceled.
            timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(token);
        }

        using (timeoutToken)
        {
            // Create a task to account for our timeout. The continuation just eats the task cancelled exception, but makes sure to observe it.
            var delayTask = Task.Delay(timeoutMS, timeoutToken.Token)
#pragma warning disable VSTHRD105 // Avoid method overloads that assume TaskScheduler.Current
                .ContinueWith(result =>
                    {
                        var _ = result.Exception;
                    },
                    TaskContinuationOptions.ExecuteSynchronously);
#pragma warning restore VSTHRD105 // Avoid method overloads that assume TaskScheduler.Current

#pragma warning disable VSTHRD003 // TaskCompletionSource-based await is intentional
            var resultingTask = await Task.WhenAny(_completionSource.Task, delayTask).ConfigureAwait(false);
#pragma warning restore VSTHRD003

            // The actual task finished, not the timeout, so we can cancel our cancellation token and return true.
            if (resultingTask != delayTask)
            {
                // Cancel the timeout token to cancel the delay if it is still going.
#if NETSTANDARD
                timeoutToken.Cancel();
#else
                await timeoutToken.CancelAsync();
#endif

                return true;
            }

            // Otherwise, the delay task finished. So throw if it finished because it was canceled.
            token.ThrowIfCancellationRequested();

            return false;
        }
    }
}