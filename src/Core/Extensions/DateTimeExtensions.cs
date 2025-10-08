using System;
using TimeZoneConverter;

namespace FEx.Core.Extensions;

public static class DateTimeExtensions
{
    public static TimeZoneInfo ConvertWindowsToIanaTimeZone(this string timeZoneToId)
    {
        string timeZoneToName = TZConvert.WindowsToIana(timeZoneToId);

        return TZConvert.GetTimeZoneInfo(timeZoneToName);
    }
}