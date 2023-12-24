using FEx.Asyncx.Helpers;
using FEx.Fundamentals;

namespace FEx.Asyncx;

public class FExAsyncModule
{
    public FExAsyncModule()
    {
        JoinableAsyncHelper.SetMainJoinableTaskFactory(Foundation.GetMainThread());
    }
}