using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Common.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace FEx.Common.Implementations;

public class FExInternetConnectionHelper : IFExInternetConnectionHelper
{
    private readonly IDeviceHelper _deviceHelper;
    private readonly IAsyncHelper _asyncHelper;

    public FExInternetConnectionHelper(IDeviceHelper deviceHelper, IAsyncHelper asyncHelper)
    {
        _deviceHelper = deviceHelper;
        _asyncHelper = asyncHelper;
    }

    /// <inheritdoc />
    public bool HasInternet(bool triggersCallbackOnLackOfInternet)
    {
        if (_deviceHelper.HasInternet)
            return true;

        if (triggersCallbackOnLackOfInternet)
            _asyncHelper.FireTaskAndForget(OnLackOfInternetAsync);

        return false;
    }

    protected virtual async Task OnLackOfInternetAsync() => await Task.CompletedTask;
}