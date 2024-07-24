using JetBrains.Annotations;
using System.Collections.Generic;

namespace FEx.Common.Extensions;

public static class CollectionExtensions
{
    [ContractAnnotation("null => false")]
    public static bool IsNotNullOrEmptyCollection<T>(this ICollection<T> source) => source?.Count > 0;

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmptyCollection<T>(this ICollection<T> source) => source is null || source.Count == 0;
}