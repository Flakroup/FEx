using FEx.Common.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace FEx.Common.Abstractions.Extensions;

public static class DeviceHelperExtensions
{
    public static ValueTask<bool> WaitForInternetAsync(this IDeviceHelper helper) =>
        helper.WaitForInternetAsync(default);
}
