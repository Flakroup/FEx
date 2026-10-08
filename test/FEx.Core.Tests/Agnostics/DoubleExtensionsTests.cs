using FEx.Agnostics.Abstractions.Extensions.Numericals;
using Shouldly;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace FEx.Core.Tests.Agnostics;

public sealed class DoubleExtensionsTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1.2.3x")]
    public void ToDouble_InvalidInput_ThrowsFormatException(string value) =>
        Should.Throw<FormatException>(() => value.ToDouble());

    [Theory]
    [InlineData("-0")]
    [InlineData("-0.0")]
    [InlineData(" -0")]
    public void ToDouble_NegativeZero_KeepsItsSign(string value) =>
        BitConverter.DoubleToInt64Bits(value.ToDouble()).ShouldBe(BitConverter.DoubleToInt64Bits(-0.0));

    [Theory]
    [InlineData("0")]
    [InlineData("0.0")]
    public void ToDouble_PositiveZero_StaysPositive(string value) =>
        BitConverter.DoubleToInt64Bits(value.ToDouble()).ShouldBe(0L);

    [Theory]
    [InlineData("en-US")]
    [InlineData("pl-PL")]
    public void ToDouble_AcceptsEitherDecimalSeparatorUnderAnyCulture(string cultureName)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new(cultureName);

        try
        {
            "1.5".ToDouble().ShouldBe(1.5);
            "1,5".ToDouble().ShouldBe(1.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData(0.1 + 0.2)]
    [InlineData(0.1)]
    [InlineData(1.0 / 3.0)]
    [InlineData(Math.PI)]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(-0.0)]
    [InlineData(1e-300)]
    [InlineData(123456789012345678.0)]
    public void ToRoundTripString_ParsesBackToTheSameDouble(double value)
    {
        var parsed = value.ToRoundTripString().ToDouble();

        BitConverter.DoubleToInt64Bits(parsed).ShouldBe(BitConverter.DoubleToInt64Bits(value));
    }

    [Fact]
    public void ToRoundTripString_RandomDoubles_ParseBackToTheSameDouble()
    {
        var random = new Random(212);
        var buffer = new byte[8];

        for (var i = 0; i < 5000; i++)
        {
            random.NextBytes(buffer);
            var value = BitConverter.ToDouble(buffer, 0);

            if (double.IsNaN(value) || double.IsInfinity(value))
                continue;

            value.ToRoundTripString().ToDouble().ShouldBe(value);
        }
    }

    [Fact]
    public void ToRoundTripString_NeedsSeventeenDigitsForTheSumOfPointOneAndPointTwo() =>
        (0.1 + 0.2).ToRoundTripString().ShouldBe("0.30000000000000004");

    [Fact]
    public void ToRoundTripString_UsesTheShortestFormWhereTheRuntimeHasOne()
    {
        // .NET Framework only has "G15", so the helper falls back to "G17" there; every other runtime keeps the short form.
        var expected = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal)
            ? "0.10000000000000001"
            : "0.1";

        0.1.ToRoundTripString().ShouldBe(expected);
    }

    [Fact]
    public void ToRoundTripString_NegativeZero_KeepsItsSign() => (-0.0).ToRoundTripString().ShouldBe("-0");

    [Theory]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void ToRoundTripString_NonFiniteValues_UseTheInvariantSymbols(double value, string expected) =>
        value.ToRoundTripString().ShouldBe(expected);

    [Fact]
    public void ToRoundTripString_UnderCommaCulture_UsesADot()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new("pl-PL");

        try
        {
            1.5.ToRoundTripString().ShouldBe("1.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.0)]
    [InlineData(0.1 + 0.2)]
    public void ToRoundTripString_UnderCommaCulture_ToDoubleReadsItBack(double value)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new("pl-PL");

        try
        {
            BitConverter.DoubleToInt64Bits(value.ToRoundTripString().ToDouble()).ShouldBe(BitConverter.DoubleToInt64Bits(value));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData(1, 0.05, true)]
    [InlineData(1, 0.2, false)]
    [InlineData(2, 0.005, true)]
    [InlineData(2, 0.02, false)]
    [InlineData(3, 0.0005, true)]
    [InlineData(3, 0.002, false)]
    [InlineData(4, 0.00005, true)]
    [InlineData(4, 0.0002, false)]
    [InlineData(5, 0.000005, true)]
    [InlineData(5, 0.00002, false)]
    [InlineData(6, 0.0000005, true)]
    [InlineData(6, 0.000002, false)]
    [InlineData(7, 0.00000005, true)]
    [InlineData(7, 0.0000002, false)]
    public void PreciseEquals_ComparesWithinTenToTheMinusDigits(int digits, double difference, bool expected) =>
        1.0.PreciseEquals(1.0 + difference, digits).ShouldBe(expected);

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void PreciseEquals_DigitsOutOfRange_Throws(int digits) =>
        Should.Throw<ArgumentOutOfRangeException>(() => 1.0.PreciseEquals(1.0, digits));
}
