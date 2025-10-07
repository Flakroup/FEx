using FEx.Common.Utilities;
using System.Threading;

namespace FEx.Common.Extensions;

public static class ThreadExtensions
{
    public static bool IsPlatformMainThread(this Thread currentThread, bool? isUIApp = null) =>
#pragma warning disable CS0618 // Type or member is obsolete
        !PlatformInfoProvider.IsWindows || isUIApp != true || currentThread.GetApartmentState() == ApartmentState.STA;
#pragma warning restore CS0618 // Type or member is obsolete
}