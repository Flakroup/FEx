using System.Threading.Tasks;

namespace FEx.Asyncx.Utilities;

public class AsyncTaskCompletionSource<TResult> : TaskCompletionSource<TResult>
{
    public AsyncTaskCompletionSource()
        : base(TaskCreationOptions.RunContinuationsAsynchronously)
    {
    }

    public AsyncTaskCompletionSource(object state)
        : base(state, TaskCreationOptions.RunContinuationsAsynchronously)
    {
    }
}