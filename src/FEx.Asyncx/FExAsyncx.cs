using FEx.Agnostics.Abstractions;
using FEx.Asyncx.Helpers;
using FEx.Core.Abstractions.Interfaces;

namespace FEx.Asyncx;

public class FExAsyncx : FExInitialize
{
    private readonly IMainThreadContextProvider _mainThreadContextProvider;

    public FExAsyncx(IMainThreadContextProvider mainThreadContextProvider)
    {
        _mainThreadContextProvider = mainThreadContextProvider;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        SetMainJoinableTaskFactory();
        _mainThreadContextProvider.ThreadHasChanged += (_, _) => SetMainJoinableTaskFactory();
    }

    private void SetMainJoinableTaskFactory() =>
        JoinableAsyncHelper.SetMainJoinableTaskFactory(_mainThreadContextProvider.Thread);
}