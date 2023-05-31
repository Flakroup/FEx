using FEx.Asyncx.Helpers;
using FEx.Fundamentals;

namespace FEx.Asyncx;

public class FExAsyncModule
{
    public FExAsyncModule(Foundation foundation)
    {
        JoinableAsyncHelper.SetMainJoinableTaskFactory(foundation.GetMainThread());
    }
}