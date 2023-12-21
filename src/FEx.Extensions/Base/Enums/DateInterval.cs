namespace FEx.Extensions.Base.Enums;

/// <summary>Indicates how to determine and format date intervals when calling date-related functions.</summary>
public enum DateInterval
{
    /// <summary>Year</summary>
    Year,

    /// <summary>Quarter of year (1 through 4)</summary>
    Quarter,

    /// <summary>Month (1 through 12)</summary>
    Month,

    /// <summary>Day of year (1 through 366)</summary>
    DayOfYear,

    /// <summary>Day of month (1 through 31)</summary>
    Day,

    /// <summary>Week of year (1 through 53)</summary>
    WeekOfYear,

    /// <summary>Day of week (1 through 7)</summary>
    Weekday,

    /// <summary>Hour (0 through 23)</summary>
    Hour,

    /// <summary>Minute (0 through 59)</summary>
    Minute,

    /// <summary>Second (0 through 59)</summary>
    Second
}