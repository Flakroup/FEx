using System;
using System.Globalization;
using System.Threading;

namespace FEx.Extensions.Base;

/// <summary>
///     Provides set of default datetime values.
/// </summary>
public static class DateTimeDefaults
{
    public static string IsodateMask { get; } = "yyyy-MM-dd"; // ISO standard.
    public static string IsotimeMask { get; } = "HH:mm:ss"; // ISO standard.
    public static string A4DtimeMask { get; } = "HH:mm";
    public static string IsodatetimeMask { get; } = IsodateMask + " " + IsotimeMask; // ISO standard.
    public static string A4DatetimeMask { get; } = IsodateMask + " " + A4DtimeMask; // ISO standard.
    public static CultureInfo DefaultCulture { get; } = Thread.CurrentThread.CurrentUICulture;
    public static DateTime UnixEpoch { get; } = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static Calendar CurrentCalendar => DefaultCulture.Calendar;
    public static DateTimeFormatInfo CurrentDateTimeFormat => DefaultCulture.DateTimeFormat;
    public static CalendarWeekRule CurrentCalendarWeekRule => CurrentDateTimeFormat.CalendarWeekRule;
    public static DayOfWeek CurrentFirstDayOfWeek => CurrentDateTimeFormat.FirstDayOfWeek;

    /// <summary>
    ///     Gets the SQL minimum allowed date.
    /// </summary>
    public static DateTime SqlMin { get; } = new(1753, 1, 1);

    /// <summary>
    ///     Gets the SQL maximum allowed date.
    /// </summary>
    public static DateTime SqlMax { get; } = new(9999, 12, 31);

    /// <summary>
    ///     Gets the default date.
    /// </summary>
    public static DateTime Default { get; } = new(0001, 01, 01, 0, 0, 0, 0);

    /// <summary>
    ///     Gets the default minimum date.
    /// </summary>
    public static DateTime DefaultMinDate { get; } = new(1900, 1, 1, 0, 0, 0, 0);

    public static DateTime DefaultMaxDate { get; } = new(9999, 12, 31, 0, 0, 0, 0);
}