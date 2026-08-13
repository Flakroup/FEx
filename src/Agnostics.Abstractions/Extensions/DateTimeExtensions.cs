using FEx.Agnostics.Abstractions.Utilities;
using System;
using System.Globalization;
using DateInterval = FEx.Agnostics.Abstractions.Enums.DateInterval;
#if NET9_0_OR_GREATER
using Microsoft.VisualBasic;

#else
using Conversion = FEx.Agnostics.Abstractions.Utilities.FExConversion;
#endif

namespace FEx.Agnostics.Abstractions.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Gets a value indicating if value is between or equal Minimum - Maximum values for a SqlDateTime.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <returns>True if in 1753-01-01 - 9999-12-31 range; otherwise False.</returns>
    public static bool InSqlRange(this DateTime value) =>
        value.InRange(DateTimeDefaults.SqlMin, DateTimeDefaults.SqlMax);

    /// <summary>
    /// Gets a value indicating if value is between or equal Minimum - Maximum values for a SqlDateTime or is null.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <returns>True if in 1753-01-01 - 9999-12-31 range; otherwise False.</returns>
    public static bool InSqlRangeOrNull(this DateTime? value) => value is null || value.Value.InSqlRange();

    /// <summary>
    /// Gets a value indicating if value is between or equal Minimum - Maximum values.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="min">Minimum value to test against.</param>
    /// <param name="max">Maximum value to test against.</param>
    /// <returns>True if in Minimum - Maximum range; otherwise False.</returns>
    public static bool InRange(this DateTime value, DateTime min, DateTime max) => value >= min && value <= max;

    /// <summary>
    /// Gets current week number.
    /// </summary>
    /// <param name="current">Current date.</param>
    /// <returns>A week number.</returns>
    public static int GetWeekNumber(this DateTime current) =>
        DateTimeDefaults.DefaultCulture.Calendar.GetWeekOfYear(current,
            DateTimeDefaults.CurrentCalendarWeekRule,
            DateTimeDefaults.CurrentFirstDayOfWeek);

    /// <summary>
    /// Gets a value indicating whether a day is after a specified date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <param name="target">The date to check against.</param>
    /// True if the specified date is after target; otherwise False.
    public static bool IsAfter(this DateTime? current, DateTime? target) => current > target;

    /// <summary>
    /// Gets a value indicating whether a day is after a specified date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <param name="target">The date to check against.</param>
    /// True if the specified date is after target; otherwise False.
    public static bool IsAfter(this DateTime current, DateTime target) => current > target;

    /// <summary>
    /// Gets a value indicating whether a day is after or equal a specified date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <param name="target">The date to check against.</param>
    /// True if the specified date is after target; otherwise False.
    public static bool IsAfterOrEqual(this DateTime current, DateTime target) => current >= target;

    public static bool IsBeforeOrEqual(this DateTime current, DateTime target) => current <= target;

    public static string ToDaySuffix(this int day) =>
        day switch
        {
            1 or 21 or 31 => "st",
            2 or 22 => "nd",
            3 or 23 => "rd",
            _ => "th"
        };

    public static DateTime GetStartOfWeek(this DateTime date)
    {
        var difference = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;

        return date.AddDays(-1 * difference).Date;
    }

    public static DateTime ConvertDateBetweenTimeZones(this DateTime date,
                                                       TimeZoneInfo timeZoneFrom,
                                                       TimeZoneInfo timeZoneTo) =>
        TimeZoneInfo.ConvertTime(date, timeZoneFrom, timeZoneTo);

    public static DateTime ConvertLocalToTimeZone(this DateTime dateTime, TimeZoneInfo timeZone)
    {
        var unspecifiedDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedDateTime);

        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }

    /// <summary>
    /// Gets the date of last day of month.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <returns>DateTime with last day of month.</returns>
    public static DateTime GetLastDayOfMonth(this DateTime date) =>
        new(date.Year,
            date.Month,
            DateTime.DaysInMonth(date.Year, date.Month),
            date.Hour,
            date.Minute,
            date.Second,
            date.Kind);

    /// <summary>
    /// Get a new date rounded to the specified time or a multiple of it
    /// </summary>
    /// <param name="dateToRound">Date to round</param>
    /// <param name="roundTo">Time to round</param>
    /// <returns></returns>
    public static DateTime RoundUp(this DateTime dateToRound, TimeSpan roundTo) =>
        new((dateToRound.Ticks + roundTo.Ticks - 1) / roundTo.Ticks * roundTo.Ticks, dateToRound.Kind);

    /// <summary>
    /// Gets whether the the provided date is on a Weekend.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>
    /// Returns whether the DateTime is on a Weekend.
    /// </returns>
    public static bool IsWeekend(this DateTime current) => current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// Gets whether the the provided date is on a Week Day.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>
    /// True if the DateTime is on a Week Day; otherwise False.
    /// </returns>
    public static bool IsWeekDay(this DateTime current) => !current.IsWeekend();

    /// <summary>
    /// Gets a value indicating whether the the provided date is in a leap year.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>
    /// True if the specified value is in a leap year; otherwise False.
    /// </returns>
    public static bool IsLeapYear(this DateTime current) => DateTime.IsLeapYear(current.Year);

    /// <summary>
    /// Gets the number of days in the month of the provided date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>The number of days.</returns>
    public static int GetCountDaysOfMonth(this DateTime current)
    {
        var nextMonth = current.AddMonths(1);

        return new DateTime(nextMonth.Year, nextMonth.Month, 1).AddDays(-1).Day;
    }

    /// <summary>
    /// Gets a formatted datestring from a date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>A date string with format HH:mm.</returns>
    public static string ToA4DTimeString(this DateTime current) =>
        current.ToString(DateTimeDefaults.A4DtimeMask, DateTimeDefaults.DefaultCulture);

    public static string ToA4DDateTimeString(this DateTime current) =>
        current.ToString(DateTimeDefaults.A4DatetimeMask, DateTimeDefaults.DefaultCulture);

    /// <summary>
    /// Clears the milliseconds.
    /// </summary>
    /// <param name="dateTime">The date time.</param>
    /// <returns>Truncated datetime.</returns>
    public static DateTime ClearMilliseconds(this DateTime dateTime) =>
        new(dateTime.Ticks - dateTime.Ticks % TimeSpan.TicksPerSecond, dateTime.Kind);

    public static bool IsEarlierThan(this DateTime firstDateTime, DateTime secondDateTime) =>
        DateTime.Compare(firstDateTime, secondDateTime) < 0;

    public static bool IsTheSameTimeAs(this DateTime firstDateTime, DateTime secondDateTime) =>
        DateTime.Compare(firstDateTime, secondDateTime) == 0;

    public static bool IsLaterThan(this DateTime firstDateTime, DateTime secondDateTime) =>
        DateTime.Compare(firstDateTime, secondDateTime) > 0;

    public static long GetCurrentUnixTimestampMillis() =>
        (long)(DateTime.UtcNow - DateTimeDefaults.UnixEpoch).TotalMilliseconds;

    public static DateTime DateTimeFromUnixTimestampMillis(this long millis) =>
        DateTimeDefaults.UnixEpoch.AddMilliseconds(millis);

    public static long GetCurrentUnixTimestampSeconds() =>
        (long)(DateTime.UtcNow - DateTimeDefaults.UnixEpoch).TotalSeconds;

    public static DateTime DateTimeFromUnixTimestampSeconds(this long seconds) =>
        DateTimeDefaults.UnixEpoch.AddSeconds(seconds);

    public static long GetUnixTimeFromDate(this DateTime theTime) =>
        (long)(theTime - DateTimeDefaults.UnixEpoch).TotalMilliseconds;

    public static long GetUnixTimeFromDateSeconds(this DateTime theTime) =>
        (long)Math.Floor((theTime - DateTimeDefaults.UnixEpoch).TotalSeconds);

    public static DateTime FromTicks(this long ticks) => DateTime.MinValue.Add(TimeSpan.FromTicks(ticks));

    public static string GetLocalizedShortTimeString(this DateTime date, CultureInfo culture) =>
        date.ToString(culture.DateTimeFormat.ShortTimePattern);

    public static string GetLocalizedFullDateTimeString(this DateTime date, string format = "dd.MM.yyyy HH:mm") =>
        date.ToString(format);

    public static string GetLocalizedDateString(this DateTime date, string format = "dd.MM.yyyy") =>
        date.ToString(format);

    public static string GetLocalizedDayOfWeek(this DayOfWeek dayOfWeek, CultureInfo culture) =>
        culture.DateTimeFormat.GetDayName(dayOfWeek).FirstCharToUpper();

    /// <summary>
    /// Compares the difference between <paramref name="date1" /> and <paramref name="date2" /> in the
    /// specified interval, and returns whether this difference is between the bounds given.
    /// </summary>
    /// <param name="date1">Required. <see langword="Date" />. A date/time value.</param>
    /// <param name="date2">Required. <see langword="Date" />. A date/time value.</param>
    /// <param name="interval">Required. Date interval enumeration value or string expression.</param>
    /// <param name="min">Required. <see langword="Double" />. The minimum allowed difference.</param>
    /// <param name="max">Required. <see langword="Double" />. The maximum allowed difference.</param>
    /// <returns><see langword="Boolean" />. True if the difference is between the bounds; False otherwise.</returns>
    public static bool DateDiffIsBetween(this DateTime date1,
                                         DateTime date2,
                                         DateInterval interval,
                                         double min,
                                         double max,
                                         DayOfWeek? dayOfWeek = null) =>
        date1.DateDiff(date2, interval, dayOfWeek) is var result && result >= min && result <= max;

    /// <summary>
    /// Returns a <see langword="Double" /> specifying the number of time intervals between two
    /// <see langword="Date" /> values.
    /// </summary>
    /// <param name="date1">Required. <see langword="Date" />. The first date/time value you want to use in the calculation.</param>
    /// <param name="date2">Required. <see langword="Date" />. The second date/time value you want to use in the calculation.</param>
    /// <param name="interval">
    /// Required. <see langword="DateInterval" /> enumeration value or <see langword="String" />
    /// expression representing the time interval you want to use as the unit of difference between
    /// <paramref name="date1" /> and <paramref name="date2" />.
    /// </param>
    /// <param name="dayOfWeek">
    /// Optional. A value chosen from the <see langword="FirstDayOfWeek" /> enumeration that specifies which day is
    /// considered the first day of the week. If not specified, <see langword="FirstDayOfWeek.Sunday" /> is used.
    /// </param>
    /// <returns>
    /// A <see langword="Double" /> specifying the number of time intervals between the two dates.
    /// </returns>
    public static double DateDiff(this DateTime date1,
                                  DateTime date2,
                                  DateInterval interval,
                                  DayOfWeek? dayOfWeek = null)
    {
        var timeSpan = date2.Subtract(date1);

        switch (interval)
        {
            case DateInterval.Year:
            {
                return DateTimeDefaults.CurrentCalendar.GetYear(date2)
                       - DateTimeDefaults.CurrentCalendar.GetYear(date1);
            }
            case DateInterval.Quarter:
            {
                return (DateTimeDefaults.CurrentCalendar.GetYear(date2)
                        - DateTimeDefaults.CurrentCalendar.GetYear(date1))
                       * 4
                       + (DateTimeDefaults.CurrentCalendar.GetMonth(date2) - 1) / 3
                       - (DateTimeDefaults.CurrentCalendar.GetMonth(date1) - 1) / 3;
            }
            case DateInterval.Month:
            {
                return (DateTimeDefaults.CurrentCalendar.GetYear(date2)
                        - DateTimeDefaults.CurrentCalendar.GetYear(date1))
                       * 12
                       + DateTimeDefaults.CurrentCalendar.GetMonth(date2)
                       - DateTimeDefaults.CurrentCalendar.GetMonth(date1);
            }
            case DateInterval.DayOfYear:
            case DateInterval.Day:
            {
                return Math.Round(Conversion.Fix(timeSpan.TotalDays));
            }
            case DateInterval.WeekOfYear:
            {
                date1 = date1.AddDays(0 - (int)date1.GetDayOfWeek(dayOfWeek));
                date2 = date2.AddDays(0 - (int)date2.GetDayOfWeek(dayOfWeek));
                timeSpan = date2.Subtract(date1);

                return Math.Round(Conversion.Fix(timeSpan.TotalDays)) / 7;
            }
            case DateInterval.Weekday:
            {
                return Math.Round(Conversion.Fix(timeSpan.TotalDays)) / 7;
            }
            case DateInterval.Hour:
            {
                return Math.Round(Conversion.Fix(timeSpan.TotalHours));
            }
            case DateInterval.Minute:
            {
                return Math.Round(Conversion.Fix(timeSpan.TotalMinutes));
            }
            case DateInterval.Second:
            {
                return Math.Round(Conversion.Fix(timeSpan.TotalSeconds));
            }
            default:
            {
                throw new ArgumentException("Argument_InvalidValue1", nameof(interval));
            }
        }
    }

    public static double GetDay(this DayOfWeek weekday) =>
        weekday == DayOfWeek.Sunday
            ? 6
            : (double)weekday - 1;

    public static DayOfWeek GetDayOfWeek(this DateTime dt, DayOfWeek? weekdayFirst = null)
    {
        if (weekdayFirst is < 0 or > DayOfWeek.Saturday)
            throw new ArgumentException("Invalid argument", nameof(weekdayFirst));

        weekdayFirst ??= DateTimeDefaults.CurrentDateTimeFormat.FirstDayOfWeek + 1;

        return (DayOfWeek)(((int)dt.DayOfWeek - (int)weekdayFirst + 8) % 7 + 1);
    }
}