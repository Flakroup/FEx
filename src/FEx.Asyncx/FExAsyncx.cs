using FEx.Asyncx.Helpers;
using FEx.Extensions;
using System.Threading;

namespace FEx.Asyncx;

public class FExAsyncx
{
    public static AsyncHelper AsyncHelper { get; private set; }

    public static void Init(AsyncHelper asyncHelper, Thread mainThread)
    {
        AsyncHelper = asyncHelper.Guard(nameof(asyncHelper));
        JoinableAsyncHelper.SetMainJoinableTaskFactory(mainThread);
    }
}