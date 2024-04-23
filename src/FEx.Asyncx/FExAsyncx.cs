using FEx.Asyncx.Helpers;
using FEx.Basics;
using FEx.Extensions;

namespace FEx.Asyncx;

public class FExAsyncx
{
    public static AsyncHelper AsyncHelper { get; private set; }

    public static void Init(AsyncHelper asyncHelper)
    {
        AsyncHelper = asyncHelper.Guard(nameof(asyncHelper));
        JoinableAsyncHelper.SetMainJoinableTaskFactory(FExBasics.MainThread);
    }
}