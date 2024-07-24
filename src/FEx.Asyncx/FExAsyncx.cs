using FEx.Abstractions;
using FEx.Abstractions.Interfaces;
using FEx.Asyncx.Helpers;

namespace FEx.Asyncx;

public class FExAsyncx : IFExInitialize
{
    protected readonly FExFoundation _foundation;

    public FExAsyncx(FExFoundation foundation)
    {
        _foundation = foundation;
    }

    public void Initialize() => JoinableAsyncHelper.SetMainJoinableTaskFactory(FExFoundation.MainThread);
}