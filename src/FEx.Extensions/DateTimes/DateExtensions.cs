using FEx.Extensions.VB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FEx.Extensions.DateTimes;

public static class DateExtensions
{
    /// <summary>
    ///     Gets a value indicating if value is between or equal Minimum - Maximum values for a SqlDateTime.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <returns>True if in 1753-01-01 - 9999-12-31 range; otherwise False.</returns>
    public static bool InSqlRange(this DateTime value)
    {
        return InRange(value, DateTimeDefaults.SqlMin, DateTimeDefaults.SqlMax);
    }

    /// <summary>
    ///     Gets a value indicating if value is between or equal Minimum - Maximum values for a SqlDateTime or is null.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <returns>True if in 1753-01-01 - 9999-12-31 range; otherwise False.</returns>
    public static bool InSqlRangeOrNull(this DateTime? value)
    {
        return value == null || InSqlRange(value.Value);
    }

    /// <summary>
    ///     Gets a value indicating if value is between or equal Minimum - Maximum values.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="min">Minimum value to test against.</param>
    /// <param name="max">Maximum value to test against.</param>
    /// <returns>True if in Minimum - Maximum range; otherwise False.</returns>
    public static bool InRange(this DateTime value, DateTime min, DateTime max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    ///     Gets current week number.
    /// </summary>
    /// <param name="current">Current date.</param>
    /// <returns>A week number.</returns>
    public static int GetWeekNumber(this DateTime current)
    {
        return DateTimeDefaults.DefaultCulture.Calendar.GetWeekOfYear(current, DateTimeDefaults.CurrentCalendarWeekRule, DateTimeDefaults.CurrentFirstDayOfWeek);
    }

    /// <summary>
    ///     Gets a value indicating whether a day is after a specified date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <param name="target">The date to check against.</param>
    /// True if the specified date is after target; otherwise False.
    public static bool IsAfter(this DateTime? current, DateTime? target)
    {
        return current > target;
    }

    /// <summary>
    ///     Gets a value indicating whether a day is after a specified date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <param name="target">The date to check against.</param>
    /// True if the specified date is after target; otherwise False.
    public static bool IsAfter(this DateTime current, DateTime target)
    {
        return current > target;
    }

    /// <summary>
    ///     Gets a value indicating whether a day is after or equal a specified date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <param name="target">The date to check against.</param>
    /// True if the specified date is after target; otherwise False.
    public static bool IsAfterOrEqual(this DateTime current, DateTime target)
    {
        return current >= target;
    }

    /// <summary>
    ///     Gets whether the the provided date is on a Weekend.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>
    ///     Returns whether the DateTime is on a Weekend.
    /// </returns>
    public static bool IsWeekend(this DateTime current)
    {
        return current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    ///     Gets whether the the provided date is on a Week Day.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>
    ///     True if the DateTime is on a Week Day; otherwise False.
    /// </returns>
    public static bool IsWeekDay(this DateTime current)
    {
        return !current.IsWeekend();
    }

    /// <summary>
    ///     Gets a value indicating whether the the provided date is in a leap year.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>
    ///     True if the specified value is in a leap year; otherwise False.
    /// </returns>
    public static bool IsLeapYear(this DateTime current)
    {
        return DateTime.IsLeapYear(current.Year);
    }

    /// <summary>
    ///     Gets the number of days in the month of the provided date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>The number of days.</returns>
    public static int GetCountDaysOfMonth(this DateTime current)
    {
        DateTime nextMonth = current.AddMonths(1);
        return new DateTime(nextMonth.Year, nextMonth.Month, 1).AddDays(-1)
            .Day;
    }

    /// <summary>
    ///     Gets a formatted datestring from a date.
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>A date string with format HH:mm.</returns>
    /// <remarks></remarks>
    public static string ToA4DTimeString(this DateTime current)
    {
        return current.ToString(DateTimeDefaults.A4DtimeMask, DateTimeDefaults.DefaultCulture);
    }

    public static string ToA4DDateTimeString(this DateTime current)
    {
        return current.ToString(DateTimeDefaults.A4DatetimeMask, DateTimeDefaults.DefaultCulture);
    }

    /// <summary>
    ///     Gets a valid A4D DateTimeString with default date and time removed
    /// </summary>
    /// <param name="current">The current date.</param>
    /// <returns>A valid A4D DateTime string</returns>
    /// <remarks></remarks>
    public static string ToA4DValidTimeString(this DateTime current)
    {
        string datePart = current.Date.Date.Equals(DateTimeDefaults.Default.Date)
            ? " "
            : current.Date.ToShortDateString();

        string timePart = current.TimeOfDay.Equals(DateTimeDefaults.Default.TimeOfDay)
            ? string.Empty
            : current.ToShortTimeString();

        return $"{datePart} {timePart}";
    }

    /// <summary>
    ///     Returns a DateTime adjusted to the beginning of the week.
    /// </summary>
    /// <param name="value">The DateTime to adjust</param>
    /// <returns>A DateTime instance adjusted to the beginning of the current week</returns>
    /// <remarks>the beginning of the week is controlled by the current Culture</remarks>
    public static DateTime StartOfWeek(this DateTime value)
    {
        DayOfWeek fdow = DateTimeDefaults.DefaultCulture.DateTimeFormat.FirstDayOfWeek;
        int offset = value.DayOfWeek - fdow < 0
            ? 7
            : 0;
        int numberOfDaysSinceBeginningOfTheWeek = value.DayOfWeek + offset - fdow;
        return value.AddDays(-numberOfDaysSinceBeginningOfTheWeek);
    }

    /// <summary>
    ///     Returns a DateTime adjusted to the beginning of the week.
    /// </summary>
    /// <param name="dateTime">The DateTime to adjust</param>
    /// <returns>A DateTime instance adjusted to the beginning of the current week</returns>
    /// <remarks>the beginning of the week is controlled by the current Culture</remarks>
    public static DateTime? StartOfWeek(this DateTime? dateTime)
    {
        return dateTime == null
            ? null
            : StartOfWeek(dateTime.Value);
    }

    /// <summary>
    ///     Returns a DateTime adjusted to the end of the week.
    /// </summary>
    /// <param name="dateTime">The DateTime to adjust</param>
    /// <returns>A DateTime instance adjusted to the end of the current week</returns>
    /// <remarks>the end of the week is controlled by the current Culture.</remarks>
    public static DateTime LastDayOfWeek(this DateTime dateTime)
    {
        return dateTime.StartOfWeek()
            .AddDays(6);
    }

    /// <summary>
    ///     Returns a DateTime adjusted to the end of the week.
    /// </summary>
    /// <param name="dateTime">The DateTime to adjust</param>
    /// <returns>A DateTime instance adjusted to the end of the current week</returns>
    /// <remarks>the end of the week is controlled by the current Culture.</remarks>
    public static DateTime? LastDayOfWeek(this DateTime? dateTime)
    {
        return dateTime != null
            ? dateTime.StartOfWeek()
                ?.AddDays(6)
            : null;
    }

    /// <summary>
    ///     Combines the date part of a DateTime with the time part from a TimeSpan
    /// </summary>
    /// <param name="date"></param>
    /// <param name="time"></param>
    /// <returns>DateTime</returns>
    public static DateTime Combine(this DateTime date, TimeSpan time)
    {
        return new(date.Year, date.Month, date.Day, int.Parse(time.Hours.ToString()), int.Parse(time.Minutes.ToString()), int.Parse(time.Seconds.ToString()), int.Parse(time.Milliseconds.ToString()));
    }

    /// <summary>
    ///     Combines the date part of a DateTime with the time part from another DateTime
    /// </summary>
    /// <param name="date"></param>
    /// <param name="time"></param>
    /// <returns>DateTime</returns>
    public static DateTime Combine(this DateTime date, DateTime time)
    {
        return new(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
    }

    /// <summary>
    ///     Gets first date of year/week using ISO8601.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="weekOfYear">The week of year.</param>
    /// <param name="weekday">The weekday of the week.</param>
    /// <returns>A DateTime</returns>
    public static DateTime GetDateFromYearWeek(this int year, int weekOfYear, DayOfWeek weekday)
    {
        return FirstDateOfWeekIso8601(year, weekOfYear)
            .AddDays(GetDay(weekday));
    }

    /// <summary>
    ///     Gets first date of year/week using ISO8601.
    /// </summary>
    /// <param name="source">The string with year/week data (format ex: 2015/W23).</param>
    /// <param name="date">The DateTime to adjust</param>
    /// <returns>A DateTime</returns>
    /// <remarks>
    ///     AS separator we use / as default.
    /// </remarks>
    public static bool TryGetDateFromYearWeek(this string source, out DateTime date)
    {
        return TryGetDateFromYearWeek(source, '/', out date);
    }

    /// <summary>
    ///     Gets first date of year/week using ISO8601.
    /// </summary>
    /// <param name="source">The string with year/week data (format ex: 2015/W23).</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="date">The DateTime to adjust</param>
    /// <returns>A DateTime</returns>
    public static bool TryGetDateFromYearWeek(this string source, char? separator, out DateTime date)
    {
        var isValid = true;

        string[] parsed = separator.HasValue
            ? source.Remove(new List<char> { 'w', 'W', 'd', 'D' })
                .Split(separator.Value)
            : source.Split(4)
                .ToArray();

        int year = parsed[0]
            .ToInt();
        int week = parsed[1]
            .ToInt();

        if (year == -1
            || week == -1)
            isValid = false;

        date = FirstDateOfWeekIso8601(year, week);
        return isValid;
    }

    /// <summary>
    ///     Gets first date of year/week using ISO8601.
    /// </summary>
    /// <param name="source">The string with year/week data (format ex: 2015/W23/D3).</param>
    /// <param name="date">The DateTime to adjust</param>
    /// <returns>A DateTime</returns>
    /// <remarks>
    ///     As separator we use / for default.
    /// </remarks>
    public static bool TryGetDateFromYearWeekDay(this string source, out DateTime date)
    {
        return source.TryGetDateFromYearWeekDay('/', out date);
    }

    /// <summary>
    ///     Gets first date of year/week/daynumber using ISO8601.
    /// </summary>
    /// <param name="source">The string with year/week data (format ex: 2015/W23/D2).</param>
    /// <param name="separator">The separator to use.</param>
    /// <param name="date">The DateTime to adjust</param>
    /// <returns>A DateTime</returns>
    public static bool TryGetDateFromYearWeekDay(this string source, char separator, out DateTime date)
    {
        var ok = true;
        string[] parsed = source.Remove(new List<char> { 'w', 'W', 'd', 'D' })
            .Split(separator);
        int year = parsed[0]
            .ToInt();
        if (year == -1)
            ok = false;

        int week = parsed[1]
            .ToInt();
        if (week == -1)
            ok = false;

        int dayresult = parsed[2]
            .ToInt();
        if (dayresult == -1)
            ok = false;

        int day = dayresult == 0
            ? dayresult
            : dayresult - 1;
        // For ex: D2 where 2 is the weekday (Tuesday) and FirstDateOfWeekISO8601 returns Monday
        // and we only want to add 1 to get date for Tuesday so reduce with 1
        date = FirstDateOfWeekIso8601(year, week)
            .AddDays(day);
        return ok;
    }

    /// <summary>
    ///     Clears the milliseconds.
    /// </summary>
    /// <param name="dateTime">The date time.</param>
    /// <returns>Truncated datetime.</returns>
    public static DateTime ClearMilliseconds(this DateTime dateTime)
    {
        return new(dateTime.Ticks - dateTime.Ticks % TimeSpan.TicksPerSecond, dateTime.Kind);
    }

    public static bool IsEarlierThan(this DateTime firstDateTime, DateTime secondDateTime)
    {
        return DateTime.Compare(firstDateTime, secondDateTime) < 0;
    }

    public static bool IsTheSameTimeAs(this DateTime firstDateTime, DateTime secondDateTime)
    {
        return DateTime.Compare(firstDateTime, secondDateTime) == 0;
    }

    public static bool IsLaterThan(this DateTime firstDateTime, DateTime secondDateTime)
    {
        return DateTime.Compare(firstDateTime, secondDateTime) > 0;
    }

    public static long GetCurrentUnixTimestampMillis()
    {
        return (long)(DateTime.UtcNow - DateTimeDefaults.UnixEpoch).TotalMilliseconds;
    }

    public static DateTime DateTimeFromUnixTimestampMillis(this long millis)
    {
        return DateTimeDefaults.UnixEpoch.AddMilliseconds(millis);
    }

    public static long GetCurrentUnixTimestampSeconds()
    {
        return (long)(DateTime.UtcNow - DateTimeDefaults.UnixEpoch).TotalSeconds;
    }

    public static DateTime DateTimeFromUnixTimestampSeconds(this long seconds)
    {
        return DateTimeDefaults.UnixEpoch.AddSeconds(seconds);
    }

    public static long GetUnixTimeFromDate(this DateTime theTime)
    {
        return (long)(theTime - DateTimeDefaults.UnixEpoch).TotalMilliseconds;
    }

    public static DateTime DateTimeFromUnixUtcTimestampSeconds(this long seconds)
    {
        return DateTime.SpecifyKind(DateTimeFromUnixTimestampSeconds(seconds), DateTimeKind.Utc);
    }

    public static DateTime DateTimeFromUnixUtcTimestampMillis(this long millis)
    {
        return DateTime.SpecifyKind(DateTimeFromUnixTimestampMillis(millis), DateTimeKind.Utc);
    }

    public static bool DateDiffIsBetween(this DateTime date1, DateTime date2, DateInterval interval, double min, double max, bool inclusive = false, DayOfWeek? dayOfWeek = null)
    {
        return date1.DateDiff(date2, interval, dayOfWeek)
            .IsBetween(min, max, inclusive);
    }

    /// <summary>
    ///     Returns a <see langword="Long" /> value specifying the number of time intervals between two
    ///     <see langword="Date" /> values.
    /// </summary>
    /// <param name="date1">Required. <see langword="Date" />. The first date/time value you want to use in the calculation. </param>
    /// <param name="date2">Required. <see langword="Date" />. The second date/time value you want to use in the calculation.</param>
    /// <param name="interval">
    ///     Required. <see langword="DateInterval" /> enumeration value or <see langword="String" />
    ///     expression representing the time interval you want to use as the unit of difference between
    ///     <paramref name="date1" /> and <paramref name="date2" />.
    /// </param>
    /// <param name="dayOfWeek">
    ///     Optional. A value chosen from the <see langword="FirstDayOfWeek" /> enumeration that specifies
    ///     the first day of the week. If not specified, <see langword="FirstDayOfWeek.Sunday" /> is used.
    /// </param>
    /// <returns>
    ///     Returns a <see langword="Long" /> value specifying the number of time intervals between two
    ///     <see langword="Date" /> values.
    /// </returns>
    /// <exception cref="T:System.ArgumentException">Invalid <paramref name="interval" />.</exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="date1" />, <paramref name="date2" />, or <paramref name="dayOfWeek" /> is out of range.
    /// </exception>
    /// <exception cref="T:System.InvalidCastException">
    ///     <paramref name="date1" /> or <paramref name="date2" /> is of an invalid type.
    /// </exception>
    public static double DateDiff(this DateTime date1, DateTime date2, DateInterval interval, DayOfWeek? dayOfWeek = null)
    {
        TimeSpan timeSpan = date2.Subtract(date1);

        switch (interval)
        {
            case DateInterval.Year:
            {
                return DateTimeDefaults.CurrentCalendar.GetYear(date2) - DateTimeDefaults.CurrentCalendar.GetYear(date1);
            }
            case DateInterval.Quarter:
            {
                return (DateTimeDefaults.CurrentCalendar.GetYear(date2) - DateTimeDefaults.CurrentCalendar.GetYear(date1)) * 4 + (DateTimeDefaults.CurrentCalendar.GetMonth(date2) - 1) / 3 - (DateTimeDefaults.CurrentCalendar.GetMonth(date1) - 1) / 3;
            }
            case DateInterval.Month:
            {
                return (DateTimeDefaults.CurrentCalendar.GetYear(date2) - DateTimeDefaults.CurrentCalendar.GetYear(date1)) * 12 + DateTimeDefaults.CurrentCalendar.GetMonth(date2) - DateTimeDefaults.CurrentCalendar.GetMonth(date1);
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

    public static DateTime FromTicks(this long ticks)
    {
        return DateTime.MinValue.Add(TimeSpan.FromTicks(ticks));
    }

    public static string GetLocalizedShortTimeString(this DateTime date, CultureInfo culture)
    {
        return date.ToString(culture.DateTimeFormat.ShortTimePattern);
    }

    public static string GetLocalizedFullDateTimeString(this DateTime date, string format = "dd.MM.yyyy HH:mm")
    {
        return date.ToString(format);
    }

    public static string GetLocalizedDateString(this DateTime date, string format = "dd.MM.yyyy")
    {
        return date.ToString(format);
    }

    public static string GetLocalizedDayOfWeek(this DayOfWeek dayOfWeek, CultureInfo culture)
    {
        return culture.DateTimeFormat.GetDayName(dayOfWeek)
            .FirstCharToUpper();
    }

    /// <summary>
    ///     Gets first date of year/week using ISO8601.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="weekOfYear">The week of year.</param>
    /// <returns>A DateTime</returns>
    private static DateTime FirstDateOfWeekIso8601(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

        DateTime firstThursday = jan1.AddDays(daysOffset);
        Calendar cal = CultureInfo.CurrentCulture.Calendar;
        int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        int weekNum = weekOfYear;
        if (firstWeek <= 1)
            weekNum--;

        DateTime result = firstThursday.AddDays(weekNum * 7);
        return result.AddDays(-3);
    }

    private static double GetDay(DayOfWeek weekday)
    {
        return weekday == DayOfWeek.Sunday
            ? 6
            : (double)weekday - 1;
    }

    private static DayOfWeek GetDayOfWeek(this DateTime dt, DayOfWeek? weekdayFirst = null)
    {
        if (weekdayFirst < 0
            || weekdayFirst > DayOfWeek.Saturday)
            throw new ArgumentException("Invalid argument", nameof(weekdayFirst));

        if (weekdayFirst == null)
            weekdayFirst = DateTimeDefaults.CurrentDateTimeFormat.FirstDayOfWeek + 1;

        return (DayOfWeek)(((int)dt.DayOfWeek - (int)weekdayFirst + 8) % 7 + 1);
    }
}