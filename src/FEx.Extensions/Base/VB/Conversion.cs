using System;

namespace FEx.Extensions.Base.VB;

public static class Conversion
{
    /// <summary>Return the integer portion of a number.</summary>
    /// <param name="number">
    /// Required. A number of type <see langword="Double" /> or any valid numeric expression. If
    /// <paramref name="number" /> contains <see langword="Nothing" />, <see langword="Nothing" /> is returned.
    /// </param>
    /// <returns>Return the integer portion of a number.</returns>
    /// <exception cref="T:System.ArgumentNullException">Number is not specified.</exception>
    /// <exception cref="T:System.ArgumentException">Number is not a numeric type.</exception>
    public static double Fix(double number) =>
        number < 0.0
            ? -Math.Floor(-number)
            : Math.Floor(number);
}