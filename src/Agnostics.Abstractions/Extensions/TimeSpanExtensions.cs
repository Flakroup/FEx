using System;
using System.Diagnostics;
using System.Globalization;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>
/// Extension methods for the TimeSpan
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Gets a value indicating if the time is midnight (00:00:00).
    /// </summary>
    /// <param name="value">Current Timespan.</param>
    /// <returns>True if midnight; otherwise False.</returns>
    public static bool IsMidnight(this TimeSpan value) => value.Hours == 0 && value is { Minutes: 0, Seconds: 0 };

    /// <summary>
    /// Gets a TimeSpan for n number of Days.
    /// </summary>
    /// <param name="number">Number of days.</param>
    /// <returns>A TimeSpan.</returns>
    public static TimeSpan Days(this int number) => new(number, 0, 0, 0);

    /// <summary>
    /// Gets a TimeSpan for n number of Hours.
    /// </summary>
    /// <param name="number">Number of hours.</param>
    /// <returns>A TimeSpan.</returns>
    public static TimeSpan Hours(this int number) => new(0, number, 0, 0);

    /// <summary>
    /// Gets a TimeSpan for n number of Minutes.
    /// </summary>
    /// <param name="number">Number of minutes.</param>
    /// <returns>A TimeSpan.</returns>
    public static TimeSpan Minutes(this int number) => new(0, number, 0);

    /// <summary>
    /// Gets a TimeSpan for n number of Seconds.
    /// </summary>
    /// <param name="number">Number of seconds.</param>
    /// <returns>A TimeSpan.</returns>
    public static TimeSpan Seconds(this int number) => new(0, 0, number);

    /// <summary>
    /// Converts to the universal time.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Universal time.</returns>
    public static TimeSpan? ToUniversalTime(this TimeSpan? value)
    {
        if (value.HasValue)
        {
            var localDateTime = DateTime.Today + value.Value;

            return localDateTime.ToUniversalTime().TimeOfDay;
        }

        return null;
    }

    /// <summary>
    /// Converts to the local time.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static TimeSpan? ToLocalTime(this TimeSpan? value)
    {
        if (value.HasValue)
        {
            var localDateTime = DateTime.UtcNow.Date + value.Value;

            return localDateTime.ToLocalTime().TimeOfDay;
        }

        return null;
    }

    /// <summary>
    /// Gets the time from <see cref="Stopwatch" />.
    /// </summary>
    /// <param name="stopwatch">The stopwatch.</param>
    /// <returns><see cref="System.String" />. with time.</returns>
    public static string GetTime(this Stopwatch stopwatch) => stopwatch.Elapsed.GetTime();

    /// <summary>
    /// Gets the time.
    /// </summary>
    /// <param name="milliseconds">The milliseconds.</param>
    /// <returns>System.String.</returns>
    public static string GetTime(this long milliseconds) => TimeSpan.FromMilliseconds(milliseconds).GetTime();

    public static string GetTime(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds).GetTime();

    /// <summary>
    /// Gets the time from <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="timespan">The timespan.</param>
    /// <param name="decimals">The decimals.</param>
    /// <returns>System.String.</returns>
    public static string GetTime(this TimeSpan timespan) => timespan.GetTime(0);

    public static string GetTime(this TimeSpan timespan, int decimals)
    {
        if (timespan.TotalMilliseconds < 1000)
            return $"{FillZeros(timespan.TotalMilliseconds.RoundDown(decimals), decimals)} ms.";

        return timespan.TotalSeconds < 60
            ?
            $"{timespan.Seconds} sec. {FillZeros((timespan.TotalMilliseconds - timespan.Seconds * 1000).RoundDown(decimals), decimals)} ms."
            : timespan.TotalMinutes < 60
                ? $"{timespan.Minutes} min. {FillZeros((timespan.TotalSeconds - timespan.Minutes * 60).RoundDown(decimals), decimals)} sec."
                : $"{timespan.Hours} h. {FillZeros((timespan.TotalMinutes - timespan.Hours * 60).RoundDown(decimals), decimals)} min.";
    }

    public static double RoundDown(this double i, double decimalPlaces)
    {
        var power = Math.Pow(10, decimalPlaces);

        return Math.Floor(i * power) / power;
    }

    public static TimeSpan RoundUp(this TimeSpan timeSpan, int roundToMinutes)
    {
        var totalMinutes = (int)timeSpan.TotalMinutes;

        var remainder = totalMinutes % roundToMinutes;

        if (remainder != 0)
            totalMinutes += roundToMinutes - remainder;

        return TimeSpan.FromMinutes(totalMinutes);
    }

    public static bool IsAm(this TimeSpan timeSpan) => timeSpan.Hours < 12;

    /// <summary>
    /// Fills the zeros.
    /// </summary>
    /// <param name="toFill">To fill.</param>
    /// <param name="decimals">The decimals.</param>
    /// <returns>System.String.</returns>
    private static string FillZeros(double toFill, int decimals) =>
        decimals <= 0
            ? toFill.ToString(CultureInfo.InvariantCulture)
            : toFill.ToString(CultureInfo.InvariantCulture).PadRight(decimals, '0');
}