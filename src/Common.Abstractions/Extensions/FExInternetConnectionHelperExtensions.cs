using FEx.Common.Abstractions.Interfaces;

namespace FEx.Common.Abstractions.Extensions;

public static class FExInternetConnectionHelperExtensions
{
    public static bool HasInternet(this IFExInternetConnectionHelper helper) =>
        helper.HasInternet(true);
}
