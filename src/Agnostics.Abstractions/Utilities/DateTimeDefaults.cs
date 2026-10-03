using System;
using System.Globalization;
using System.Threading;

namespace FEx.Agnostics.Abstractions.Utilities;

/// <summary>
/// Provides set of default datetime values.
/// </summary>
public static class DateTimeDefaults
{
    /// <summary>Gets the ISO date mask <c>yyyy-MM-dd</c>.</summary>
    public static string IsodateMask { get; } = "yyyy-MM-dd"; // ISO standard.
    /// <summary>Gets the ISO time mask <c>HH:mm:ss</c>.</summary>
    public static string IsotimeMask { get; } = "HH:mm:ss"; // ISO standard.
    /// <summary>Gets the short time mask <c>HH:mm</c>.</summary>
    public static string A4DtimeMask { get; } = "HH:mm";
    /// <summary>Gets the ISO date and time mask <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
    public static string IsodatetimeMask { get; } = IsodateMask + " " + IsotimeMask; // ISO standard.
    /// <summary>Gets the date mask combined with the short time mask, <c>yyyy-MM-dd HH:mm</c>.</summary>
    public static string A4DatetimeMask { get; } = IsodateMask + " " + A4DtimeMask; // ISO standard.
    /// <summary>Gets the UI culture of the thread that first accessed this class.</summary>
    public static CultureInfo DefaultCulture { get; } = Thread.CurrentThread.CurrentUICulture;
    /// <summary>Gets the Unix epoch, 1970-01-01T00:00:00 UTC.</summary>
    public static DateTime UnixEpoch { get; } = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    /// <summary>Gets the calendar of <see cref="DefaultCulture" />.</summary>
    public static Calendar CurrentCalendar => DefaultCulture.Calendar;
    /// <summary>Gets the date and time format information of <see cref="DefaultCulture" />.</summary>
    public static DateTimeFormatInfo CurrentDateTimeFormat => DefaultCulture.DateTimeFormat;
    /// <summary>Gets the calendar week rule of <see cref="DefaultCulture" />.</summary>
    public static CalendarWeekRule CurrentCalendarWeekRule => CurrentDateTimeFormat.CalendarWeekRule;
    /// <summary>Gets the first day of the week of <see cref="DefaultCulture" />.</summary>
    public static DayOfWeek CurrentFirstDayOfWeek => CurrentDateTimeFormat.FirstDayOfWeek;

    /// <summary>
    /// Gets the SQL minimum allowed date.
    /// </summary>
    public static DateTime SqlMin { get; } = new(1753, 1, 1);

    /// <summary>
    /// Gets the SQL maximum allowed date.
    /// </summary>
    public static DateTime SqlMax { get; } = new(9999, 12, 31);

    /// <summary>
    /// Gets the default date.
    /// </summary>
    public static DateTime Default { get; } = new(0001, 01, 01, 0, 0, 0, 0);

    /// <summary>
    /// Gets the default minimum date.
    /// </summary>
    public static DateTime DefaultMinDate { get; } = new(1900, 1, 1, 0, 0, 0, 0);

    /// <summary>Gets the default maximum date, 9999-12-31.</summary>
    public static DateTime DefaultMaxDate { get; } = new(9999, 12, 31, 0, 0, 0, 0);
}