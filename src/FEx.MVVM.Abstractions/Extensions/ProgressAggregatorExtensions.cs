using FEx.MVVM.Abstractions.Enums;
using FEx.MVVM.Abstractions.Interfaces;

namespace FEx.MVVM.Abstractions.Extensions;

public static class ProgressAggregatorExtensions
{
    public static void PrgSet(this IProgressAggregator aggregator, double? value) =>
        aggregator.PrgSet(value, null, ProgressChangeMode.Set);

    public static void PrgSet(this IProgressAggregator aggregator, double? value, double? maximum) =>
        aggregator.PrgSet(value, maximum, ProgressChangeMode.Set);

    public static void PrgAdd(this IProgressAggregator aggregator) =>
        aggregator.PrgAdd(1);
}
